using System;
using ResolveOps.Domain.Claims.Enums;

namespace ResolveOps.Modules.Claims.Features.GetClaims;

public record GetClaimsQuery(
    Guid? CaseId,
    ClaimStatus? Status,
    int Page = 1,
    int PageSize = 20);
