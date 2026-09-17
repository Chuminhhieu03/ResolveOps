using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResolveOps.Application;
using ResolveOps.Domain.Partners;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Workflow.Services;

/// <summary>
/// Service calculating working-hours-aware deadlines and durations based on tenant business calendars (spec §15.2, §18.2).
/// </summary>
public sealed class BusinessCalendarService : IBusinessCalendarService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<BusinessCalendarService> _logger;

    private static readonly Action<ILogger, string, Guid, Exception?> _logTimezoneFallback =
        LoggerMessage.Define<string, Guid>(
            LogLevel.Warning,
            new EventId(1, "TimezoneFallback"),
            "Timezone '{Timezone}' on calendar '{CalendarId}' could not be resolved. Falling back to UTC.");

    public BusinessCalendarService(AppDbContext dbContext, ILogger<BusinessCalendarService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<DateTimeOffset> CalculateDeadlineAsync(
        Guid tenantId,
        DateTimeOffset startAtUtc,
        int durationMinutes,
        Guid? calendarId = null,
        CancellationToken cancellationToken = default)
    {
        if (durationMinutes <= 0)
        {
            return startAtUtc;
        }

        var calendarQuery = _dbContext.BusinessCalendars
            .Include(c => c.Holidays)
            .Where(c => c.TenantId == tenantId && c.Status == BusinessCalendarStatus.Active);

        BusinessCalendar? calendar;
        if (calendarId.HasValue && calendarId.Value != Guid.Empty)
        {
            calendar = await calendarQuery.FirstOrDefaultAsync(c => c.Id == calendarId.Value, cancellationToken);
        }
        else
        {
            calendar = await calendarQuery.FirstOrDefaultAsync(cancellationToken);
        }

        if (calendar is null)
        {
            return startAtUtc.AddMinutes(durationMinutes);
        }

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(calendar.Timezone);
        }
        catch (Exception ex)
        {
            _logTimezoneFallback(_logger, calendar.Timezone, calendar.Id, ex);
            timeZone = TimeZoneInfo.Utc;
        }

        var currentLocal = TimeZoneInfo.ConvertTime(startAtUtc, timeZone);
        var remainingMinutes = durationMinutes;
        var safetyCounter = 0;
        const int maxDays = 365; // Prevent runaway loops

        while (remainingMinutes > 0 && safetyCounter++ < maxDays)
        {
            var dateOnly = DateOnly.FromDateTime(currentLocal.DateTime);
            var isWorkingDay = IsDateWorkingDay(calendar, dateOnly, currentLocal.DayOfWeek);

            if (!isWorkingDay)
            {
                currentLocal = AdvanceToNextWorkingStart(currentLocal, calendar.WorkingStart);
                continue;
            }

            var localTime = TimeOnly.FromDateTime(currentLocal.DateTime);

            if (localTime < calendar.WorkingStart)
            {
                currentLocal = new DateTimeOffset(
                    currentLocal.Year, currentLocal.Month, currentLocal.Day,
                    calendar.WorkingStart.Hour, calendar.WorkingStart.Minute, calendar.WorkingStart.Second,
                    currentLocal.Offset);
                localTime = calendar.WorkingStart;
            }

            if (localTime >= calendar.WorkingEnd)
            {
                currentLocal = AdvanceToNextWorkingStart(currentLocal, calendar.WorkingStart);
                continue;
            }

            var availableMinutes = (int)(calendar.WorkingEnd - localTime).TotalMinutes;
            if (availableMinutes <= 0)
            {
                currentLocal = AdvanceToNextWorkingStart(currentLocal, calendar.WorkingStart);
                continue;
            }

            if (remainingMinutes <= availableMinutes)
            {
                currentLocal = currentLocal.AddMinutes(remainingMinutes);
                remainingMinutes = 0;
                break;
            }

            remainingMinutes -= availableMinutes;
            currentLocal = AdvanceToNextWorkingStart(currentLocal, calendar.WorkingStart);
        }

        return currentLocal.ToUniversalTime();
    }

    public Task<DateTimeOffset> AddDurationAsync(
        Guid tenantId,
        DateTimeOffset currentDueAtUtc,
        TimeSpan durationToAdd,
        Guid? calendarId = null,
        CancellationToken cancellationToken = default)
    {
        var minutes = (int)Math.Ceiling(durationToAdd.TotalMinutes);
        return CalculateDeadlineAsync(tenantId, currentDueAtUtc, minutes, calendarId, cancellationToken);
    }

    private static bool IsDateWorkingDay(BusinessCalendar calendar, DateOnly date, DayOfWeek dayOfWeek)
    {
        var holiday = calendar.Holidays.FirstOrDefault(h => h.HolidayDate == date);
        if (holiday is not null)
        {
            return holiday.IsWorkingOverride;
        }

        return calendar.IsWorkingDay(dayOfWeek);
    }

    private static DateTimeOffset AdvanceToNextWorkingStart(DateTimeOffset current, TimeOnly workingStart)
    {
        var nextDay = current.Date.AddDays(1);
        return new DateTimeOffset(
            nextDay.Year, nextDay.Month, nextDay.Day,
            workingStart.Hour, workingStart.Minute, workingStart.Second,
            current.Offset);
    }
}
