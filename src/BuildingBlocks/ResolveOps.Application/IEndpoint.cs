using Microsoft.AspNetCore.Routing;

namespace ResolveOps.Application;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
