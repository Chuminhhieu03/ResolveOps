using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.TenantSettings;

internal sealed class TenantSettingsHandlers
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public TenantSettingsHandlers(AppDbContext dbContext, ITenantContext tenantContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<GetTenantSettingsResponse>> HandleGetAsync(GetTenantSettingsQuery query, CancellationToken cancellationToken)
    {
        var settings = await _dbContext.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == _tenantContext.TenantId.Value, cancellationToken);

        if (settings == null)
        {
            // Settings might not exist yet if this is a newly created tenant
            return new GetTenantSettingsResponse("{}", 0);
        }

        return new GetTenantSettingsResponse(settings.SettingsJson, settings.Version);
    }

    public async Task<Result> HandleUpdateAsync(UpdateTenantSettingsCommand command, CancellationToken cancellationToken)
    {
        var settings = await _dbContext.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == _tenantContext.TenantId.Value, cancellationToken);

        if (settings == null)
        {
            // Create new settings record
            settings = ResolveOps.Domain.Tenancy.TenantSettings.CreateDefault(_tenantContext.TenantId.Value, _timeProvider);
            settings.Update(command.SettingsJson, _timeProvider);
            _dbContext.TenantSettings.Add(settings);
        }
        else
        {
            // Update existing with optimistic concurrency check
            _dbContext.Entry(settings).Property(s => s.Version).OriginalValue = command.ExpectedVersion;
            settings.Update(command.SettingsJson, _timeProvider);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return DomainError.ConcurrencyConflict with { Message = "The tenant settings were updated by another user. Please refresh and try again." };
        }
    }
}
