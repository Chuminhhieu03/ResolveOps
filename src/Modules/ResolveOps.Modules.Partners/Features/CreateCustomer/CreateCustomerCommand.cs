namespace ResolveOps.Modules.Partners.Features.CreateCustomer;

public sealed record CreateCustomerCommand(
    string Code,
    string Name,
    string Priority,
    string? DefaultTimezone);
