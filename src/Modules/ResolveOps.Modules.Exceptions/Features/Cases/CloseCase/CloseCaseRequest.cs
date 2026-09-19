namespace ResolveOps.Modules.Exceptions.Features.Cases.CloseCase;

public sealed record CloseCaseRequest(string? Notes, string ConcurrencyStamp, string? DetailsJson = null);
