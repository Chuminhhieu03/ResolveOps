namespace ResolveOps.Modules.Exceptions.Features.Cases.CancelExceptionCase;

public sealed record CancelExceptionCaseCommand(
    Guid CaseId,
    string Reason,
    string ConcurrencyStamp,
    string? DetailsJson = null);
