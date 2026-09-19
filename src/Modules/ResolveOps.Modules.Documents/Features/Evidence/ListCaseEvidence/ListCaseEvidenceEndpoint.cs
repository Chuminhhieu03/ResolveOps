using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;

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
