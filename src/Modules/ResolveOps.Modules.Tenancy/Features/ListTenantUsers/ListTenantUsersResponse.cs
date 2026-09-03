using System.Text.Json.Serialization;

namespace ResolveOps.Modules.Tenancy.Features.ListTenantUsers;

public sealed record ListTenantUsersResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("roles")]
    public string[] Roles { get; init; } = [];

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; init; }
}
