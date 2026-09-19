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

namespace ResolveOps.Modules.Exceptions.Features.Cases.AssignCase;

public sealed record AssignCaseRequest(
    Guid? OwnerUserId,
    string? OwnerTeamCode,
    string Reason,
    string ConcurrencyStamp,
    string? DetailsJson = null);
