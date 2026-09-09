namespace ResolveOps.Modules.Partners.Features.ListBusinessCalendars;

public sealed record ListBusinessCalendarsResponse(
    IReadOnlyList<BusinessCalendarSummary> Items,
    int TotalCount);

public sealed record BusinessCalendarSummary(
    Guid Id,
    string Name,
    string Timezone,
    string Status);
