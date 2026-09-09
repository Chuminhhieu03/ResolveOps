using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Partners;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.CreateBusinessCalendar;

internal sealed class CreateBusinessCalendarHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public CreateBusinessCalendarHandler(AppDbContext dbContext, ITenantContext tenantContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreateBusinessCalendarCommand command, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var nameExists = await _dbContext.BusinessCalendars
            .AsNoTracking()
            .AnyAsync(bc => bc.TenantId == tenantId && bc.Name == command.Name.Trim(),
                cancellationToken);

        if (nameExists)
        {
            return DomainError.ValidationFailed with
            {
                Message = $"A business calendar with name '{command.Name}' already exists in this tenant."
            };
        }

        var calendar = BusinessCalendar.Create(
            tenantId,
            command.Name,
            command.Timezone,
            command.WorkingDaysMask,
            command.WorkingStart,
            command.WorkingEnd,
            _timeProvider);

        _dbContext.BusinessCalendars.Add(calendar);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return calendar.Id;
    }
}
