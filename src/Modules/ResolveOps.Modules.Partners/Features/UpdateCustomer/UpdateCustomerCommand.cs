namespace ResolveOps.Modules.Partners.Features.UpdateCustomer;

public sealed record UpdateCustomerCommand(
    Guid CustomerId,
    string Name,
    string Priority,
    string? DefaultTimezone,
    long ExpectedVersion);
