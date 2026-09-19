using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.GetTimeline;

public sealed record CaseTimelineEntryResponse(
    Guid Id,
    Guid CaseId,
    string EntryType,
    string ActorType,
    Guid? ActorId,
    string Summary,
    string? DetailsJson,
    DateTimeOffset CreatedAtUtc,
    string? CorrelationId);
