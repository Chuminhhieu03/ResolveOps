namespace ResolveOps.Modules.Documents.Features.Evidence.GetEvidenceChecklist;

public sealed record ChecklistItem(
    string EvidenceType,
    bool IsMandatory,
    bool IsSatisfied,
    Guid? SatisfiedByDocumentId,
    string? UnsatisfiedReason);
