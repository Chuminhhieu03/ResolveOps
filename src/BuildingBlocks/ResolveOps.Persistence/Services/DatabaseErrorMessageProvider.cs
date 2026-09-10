using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;

namespace ResolveOps.Persistence.Services;

public sealed class DatabaseErrorMessageProvider : IErrorMessageProvider
{
    private readonly AppDbContext _context;

    public DatabaseErrorMessageProvider(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ErrorDetails> GetErrorDetailsAsync(DomainError domainError, int defaultStatusCode = 400, CancellationToken cancellationToken = default)
    {
        var template = await _context.ErrorTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == domainError.Code, cancellationToken);

        if (template != null)
        {
            var message = template.MessageTemplate;
            if (domainError.Arguments != null && domainError.Arguments.Length > 0)
            {
                try
                {
                    message = string.Format(System.Globalization.CultureInfo.InvariantCulture, template.MessageTemplate, domainError.Arguments);
                }
                catch (FormatException)
                {
                    // Fallback to raw template if string.Format fails due to argument mismatch
                }
            }

            return new ErrorDetails(message, template.HttpStatusCode);
        }

        // Fallback to the domain error code if no template is found
        return new ErrorDetails(domainError.Code, defaultStatusCode);
    }
}
