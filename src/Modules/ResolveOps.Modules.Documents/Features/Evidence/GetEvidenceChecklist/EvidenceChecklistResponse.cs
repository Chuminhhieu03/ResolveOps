namespace ResolveOps.Modules.Documents.Features.Evidence.GetEvidenceChecklist;

public sealed record EvidenceChecklistResponse(
    Guid CaseId,
    Guid? ClaimId,
    bool IsReady,
    IReadOnlyList<ChecklistItem> Items);
