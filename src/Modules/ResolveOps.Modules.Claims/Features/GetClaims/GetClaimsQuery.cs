using System;
using ResolveOps.Domain.Claims;

namespace ResolveOps.Modules.Claims.Features.GetClaims;

public record GetClaimsQuery(
    Guid? CaseId,
    string? Status,
    int Page = 1,
    int PageSize = 20);
