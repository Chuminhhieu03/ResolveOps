using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Documents.Features.Evidence.CreateDownloadIntent;

/// <summary>
/// POST /api/evidence/{documentId}/download-intent (spec §16.9)
///
/// Returns a short-lived presigned GET URL. Permanent public URLs are NEVER returned.
/// Only Available documents can be downloaded.
/// </summary>
public sealed class CreateDownloadIntentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/evidence/{documentId}/download-intent", async (
            Guid documentId,
            CreateDownloadIntentHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(documentId, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("CreateEvidenceDownloadIntent")
        .WithTags("Evidence")
        .Produces<CreateDownloadIntentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireDownloadEvidence);
    }
}
