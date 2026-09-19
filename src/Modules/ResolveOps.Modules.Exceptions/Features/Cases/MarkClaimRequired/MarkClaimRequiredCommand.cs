using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Observability;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.MarkClaimRequired;

public sealed record MarkClaimRequiredCommand(
    Guid CaseId,
    string ClaimType,
    decimal? EstimatedLoss,
    string Reason,
    string ConcurrencyStamp,
    string? DetailsJson = null);
