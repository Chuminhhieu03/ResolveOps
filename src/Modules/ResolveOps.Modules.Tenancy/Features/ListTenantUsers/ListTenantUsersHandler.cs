using Microsoft.EntityFrameworkCore;
using ResolveOps.Persistence;
using ResolveOps.Security;
using System.Text.Json;

namespace ResolveOps.Modules.Tenancy.Features.ListTenantUsers;

public sealed class ListTenantUsersHandler
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ListTenantUsersHandler(AppDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<List<ListTenantUsersResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var memberships = await _context.UserTenantMemberships
            .Where(m => m.TenantId == _tenantContext.TenantId.Value)
            .ToListAsync(cancellationToken);

        var userIds = memberships.Select(m => m.UserId).ToList();

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        var result = new List<ListTenantUsersResponse>();
        foreach (var membership in memberships)
        {
            var roles = string.IsNullOrEmpty(membership.RolesJson) 
                ? [] 
                : JsonSerializer.Deserialize<string[]>(membership.RolesJson) ?? [];

            result.Add(new ListTenantUsersResponse
            {
                Id = membership.UserId,
                Email = users.TryGetValue(membership.UserId, out var email) ? email! : string.Empty,
                Roles = roles,
                Status = membership.Status,
                CreatedAtUtc = membership.CreatedAtUtc
            });
        }

        return result;
    }
}
