using System;
using System.Collections.Generic;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Modules.Claims.Features.GetClaims;

public record GetClaimsResponse(
    IReadOnlyList<ClaimSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record ClaimSummaryResponse(
    Guid Id,
    string ClaimNumber,
    Guid CaseId,
    Guid CarrierId,
    string ClaimType,
    string Status,
    decimal ClaimedAmount,
    string Currency,
    DateTimeOffset CreatedAtUtc);
