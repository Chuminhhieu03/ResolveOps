using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;

namespace ResolveOps.Modules.Documents.Features.Evidence.GetEvidenceChecklist;

/// <summary>
/// GET /api/claims/{claimId}/evidence-checklist (spec §8.6, §10.4)
///
/// Evaluates EvidenceRequirement records against Available documents for the case.
/// Returns each required evidence type with its satisfaction status.
/// Only deterministic rule evaluation — no AI (spec §10.4, Phase 9).
/// </summary>
public sealed class GetEvidenceChecklistEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/claims/{claimId}/evidence-checklist", async (
            Guid claimId,
            GetEvidenceChecklistHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(claimId, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("GetEvidenceChecklist")
        .WithTags("Evidence")
        .Produces<EvidenceChecklistResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
