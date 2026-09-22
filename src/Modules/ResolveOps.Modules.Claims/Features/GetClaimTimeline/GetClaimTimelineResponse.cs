using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Claims.Features.GetClaimTimeline;

public record ClaimTimelineItem(
    string EventType,
    DateTimeOffset TimestampUtc,
    string Summary,
    string? Actor,
    string? DetailsJson);

public record GetClaimTimelineResponse(
    Guid ClaimId,
    string ClaimNumber,
    string CurrentStatus,
    IReadOnlyList<ClaimTimelineItem> Timeline);
