using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Policies.CreatePolicyVersion;

public sealed class CreatePolicyVersionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exception-policies", async (
            CreatePolicyVersionRequest request,
            IValidator<CreatePolicyVersionCommand> validator,
            CreatePolicyVersionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreatePolicyVersionCommand(
                request.PolicyKey,
                request.VersionNumber,
                request.ExceptionType,
                request.RuleDefinitionJson,
                request.SeverityDefinitionJson,
                request.AssignmentDefinitionJson,
                request.EffectiveFromUtc,
                request.EffectiveToUtc,
                request.EvidencePolicyVersionId,
                request.SlaPolicyVersionId,
                request.ActivateNow);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Created($"/api/exception-policies/{response.Id}", response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("CreateExceptionPolicyVersion")
        .WithTags("ExceptionPolicies")
        .Produces<CreatePolicyVersionResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireManagePolicies);
    }
}

public sealed record CreatePolicyVersionRequest(
    string PolicyKey,
    int VersionNumber,
    string ExceptionType,
    string RuleDefinitionJson,
    string SeverityDefinitionJson,
    string AssignmentDefinitionJson,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc = null,
    Guid? EvidencePolicyVersionId = null,
    Guid? SlaPolicyVersionId = null,
    bool ActivateNow = false);
