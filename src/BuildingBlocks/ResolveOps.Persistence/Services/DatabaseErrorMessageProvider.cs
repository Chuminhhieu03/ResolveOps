using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using ResolveOps.Application;
using ResolveOps.Domain;

namespace ResolveOps.Persistence.Services;

public sealed record CachedErrorTemplate(string MessageTemplate, int HttpStatusCode);

public sealed class DatabaseErrorMessageProvider : IErrorMessageProvider
{
    private readonly AppDbContext _context;
    private readonly HybridCache _hybridCache;
    private readonly ILogger<DatabaseErrorMessageProvider> _logger;

    private static readonly Action<ILogger, string, Exception?> _logCacheError =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, "ErrorTemplateCacheError"),
            "Failed to retrieve error template for code '{Code}' from HybridCache. Falling back to database.");

    private static readonly Action<ILogger, string, Exception?> _logDbError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, "ErrorTemplateDbError"),
            "Failed to retrieve error template for code '{Code}' from database.");

    public DatabaseErrorMessageProvider(
        AppDbContext context,
        HybridCache hybridCache,
        ILogger<DatabaseErrorMessageProvider> logger)
    {
        _context = context;
        _hybridCache = hybridCache;
        _logger = logger;
    }

    public async Task<ErrorDetails> GetErrorDetailsAsync(
        DomainError domainError,
        int defaultStatusCode = 400,
        CancellationToken cancellationToken = default)
    {
        CachedErrorTemplate? template = null;

        try
        {
            template = await _hybridCache.GetOrCreateAsync(
                $"errortemplate:{domainError.Code}",
                async ct =>
                {
                    var dbTemplate = await _context.ErrorTemplates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Code == domainError.Code, ct);

                    return dbTemplate is not null
                        ? new CachedErrorTemplate(dbTemplate.MessageTemplate, dbTemplate.HttpStatusCode)
                        : null;
                },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logCacheError(_logger, domainError.Code, ex);

            // Resilient fallback: direct DB query when cache fails/times out
            try
            {
                var dbTemplate = await _context.ErrorTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Code == domainError.Code, cancellationToken);

                if (dbTemplate is not null)
                {
                    template = new CachedErrorTemplate(dbTemplate.MessageTemplate, dbTemplate.HttpStatusCode);
                }
            }
            catch (Exception dbEx)
            {
                _logDbError(_logger, domainError.Code, dbEx);
            }
        }

        if (template is not null)
        {
            var message = template.MessageTemplate;
            if (domainError.Arguments is { Length: > 0 })
            {
                try
                {
                    message = string.Format(CultureInfo.InvariantCulture, template.MessageTemplate, domainError.Arguments);
                }
                catch (FormatException)
                {
                    // Fallback to raw template if string.Format fails due to argument count/format mismatch
                }
            }

            return new ErrorDetails(message, template.HttpStatusCode);
        }

        // Final fallback to the domain error code if no template exists in cache or DB
        return new ErrorDetails(domainError.Code, defaultStatusCode);
    }
}
