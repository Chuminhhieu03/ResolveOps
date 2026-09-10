using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ResolveOps.Domain;

namespace ResolveOps.Application;

public sealed class DomainErrorResult : IResult
{
    private readonly DomainError _error;

    public DomainErrorResult(DomainError error)
    {
        _error = error;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var provider = httpContext.RequestServices.GetRequiredService<IErrorMessageProvider>();
        var details = await provider.GetErrorDetailsAsync(_error, defaultStatusCode: StatusCodes.Status400BadRequest);

        await Results.Problem(
            statusCode: details.HttpStatusCode,
            title: _error.Code,
            detail: details.Message
        ).ExecuteAsync(httpContext);
    }
}

public static class DomainErrorExtensions
{
    public static IResult ToProblemDetails(this DomainError error)
    {
        return new DomainErrorResult(error);
    }
}
