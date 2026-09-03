using System.Text.Json.Serialization;

namespace ResolveOps.Modules.Tenancy.Features.UpdateUserRoles;

public sealed record UpdateUserRolesCommand
{
    [JsonIgnore]
    public Guid UserId { get; init; }

    [JsonPropertyName("roles")]
    public string[] Roles { get; init; } = [];
}
