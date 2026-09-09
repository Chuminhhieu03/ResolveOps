using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.GetCustomer;

internal sealed class GetCustomerHandler
{
    private readonly AppDbContext _dbContext;

    public GetCustomerHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CustomerResponse>> HandleAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer is null)
        {
            return DomainError.ResourceNotFound with { Message = "Customer not found." };
        }

        return new CustomerResponse(
            customer.Id,
            customer.Code,
            customer.Name,
            customer.Priority,
            customer.DefaultTimezone,
            customer.Status,
            customer.CreatedAtUtc,
            customer.UpdatedAtUtc,
            customer.Version);
    }
}
