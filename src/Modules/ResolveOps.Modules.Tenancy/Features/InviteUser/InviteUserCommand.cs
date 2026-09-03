using System.Text.Json.Serialization;

namespace ResolveOps.Modules.Tenancy.Features.InviteUser;

public sealed record InviteUserCommand
{
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("roles")]
    public string[] Roles { get; init; } = [];
}
