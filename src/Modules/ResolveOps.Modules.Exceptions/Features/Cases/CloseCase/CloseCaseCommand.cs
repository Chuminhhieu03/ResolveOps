namespace ResolveOps.Modules.Exceptions.Features.Cases.CloseCase;

public sealed record CloseCaseCommand(Guid CaseId, string? Notes, string ConcurrencyStamp, string? DetailsJson = null);
