using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.UpdateCustomer;

internal sealed class UpdateCustomerHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public UpdateCustomerHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(UpdateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Id == command.CustomerId, cancellationToken);

        if (customer is null)
        {
            return DomainError.ResourceNotFound with { Message = "Customer not found." };
        }

        // Apply EF's original value for optimistic concurrency
        _dbContext.Entry(customer).Property(c => c.ConcurrencyStamp).OriginalValue = command.ConcurrencyStamp;

        customer.Update(
            command.Name,
            command.Priority,
            command.DefaultTimezone,
            _timeProvider);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return DomainError.ConcurrencyConflict with
            {
                Message = "The customer was updated by another user. Please refresh and try again."
            };
        }
    }
}
