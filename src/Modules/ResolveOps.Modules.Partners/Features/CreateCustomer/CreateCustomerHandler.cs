using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Partners;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.CreateCustomer;

internal sealed class CreateCustomerHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public CreateCustomerHandler(AppDbContext dbContext, ITenantContext tenantContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var normalizedCode = command.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.Customers
            .AsNoTracking()
            .AnyAsync(c => c.TenantId == tenantId && c.Code == normalizedCode,
                cancellationToken);

        if (codeExists)
        {
            return DomainError.ValidationFailed with
            {
                Message = $"A customer with code '{command.Code}' already exists in this tenant."
            };
        }

        var customer = Customer.Create(tenantId, command.Code, command.Name, command.Priority,
            command.DefaultTimezone, _timeProvider);

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }
}
