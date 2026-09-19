using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Documents;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Documents.Features.Evidence.ListCaseEvidence;

/// <summary>GET /api/exceptions/{caseId}/evidence — lists all evidence documents for a case.</summary>
public sealed class ListCaseEvidenceEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/exceptions/{caseId}/evidence", async (
            Guid caseId,
            ListCaseEvidenceHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(caseId, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ListCaseEvidence")
        .WithTags("Evidence")
        .Produces<ListCaseEvidenceResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}

public sealed record ListCaseEvidenceResponse(IReadOnlyList<CaseEvidenceSummary> Items);

public sealed record CaseEvidenceSummary(
    Guid Id,
    string EvidenceType,
    string Status,
    string ScanStatus,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    int VersionNumber,
    Guid? SupersedesDocumentId,
    Guid UploadedBy,
    DateTimeOffset UploadedAtUtc,
    DateTimeOffset? ScanCompletedAtUtc,
    bool LegalHold);

internal sealed class ListCaseEvidenceHandler
{
    private readonly AppDbContext _dbContext;

    public ListCaseEvidenceHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ListCaseEvidenceResponse>> HandleAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        // Tenant query filter enforced by EF Core.
        var caseExists = await _dbContext.ExceptionCases
            .AnyAsync(c => c.Id == caseId, cancellationToken);

        if (!caseExists)
        {
            return DomainError.Failure("ERR_CASE_NOT_FOUND", $"Exception case '{caseId}' was not found.");
        }

        var docs = await _dbContext.EvidenceDocuments
            .AsNoTracking()
            .Where(d => d.CaseId == caseId && d.Status != DocumentStatus.Removed)
            .OrderByDescending(d => d.UploadedAtUtc)
            .Select(d => new CaseEvidenceSummary(
                d.Id,
                d.EvidenceType,
                d.Status,
                d.ScanStatus,
                d.OriginalFileName,
                d.ContentType,
                d.SizeBytes,
                d.VersionNumber,
                d.SupersedesDocumentId,
                d.UploadedBy,
                d.UploadedAtUtc,
                d.ScanCompletedAtUtc,
                d.LegalHold))
            .ToListAsync(cancellationToken);

        return new ListCaseEvidenceResponse(docs);
    }
}
