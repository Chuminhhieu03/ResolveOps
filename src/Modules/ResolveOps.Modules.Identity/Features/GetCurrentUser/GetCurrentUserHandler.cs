using Microsoft.AspNetCore.Identity;
using ResolveOps.Domain;
using ResolveOps.Domain.Identity;
using ResolveOps.Security;

namespace ResolveOps.Modules.Identity.Features.GetCurrentUser;

internal sealed class GetCurrentUserHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantContext _tenantContext;

    public GetCurrentUserHandler(
        UserManager<ApplicationUser> userManager,
        ITenantContext tenantContext)
    {
        _userManager = userManager;
        _tenantContext = tenantContext;
    }

    public async Task<Result<GetCurrentUserResponse>> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        // Identity user
        var user = await _userManager.FindByIdAsync(_tenantContext.UserId.Value.ToString());
        if (user == null)
        {
            return DomainError.ResourceNotFound with { Message = "User not found." };
        }

        var response = new GetCurrentUserResponse(
            user.Id,
            user.Email!,
            _tenantContext.TenantId.Value,
            _tenantContext.Roles);

        return response;
    }
}
