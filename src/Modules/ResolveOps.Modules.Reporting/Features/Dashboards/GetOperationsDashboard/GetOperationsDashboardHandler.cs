using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Features.Dashboards.GetOperationsDashboard;

public sealed class GetOperationsDashboardHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public GetOperationsDashboardHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<GetOperationsDashboardResponse>> HandleAsync(
        GetOperationsDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var nowUtc = _timeProvider.GetUtcNow();
        var riskCutoffUtc = nowUtc.AddHours(2);
        var todayStartUtc = new DateTimeOffset(nowUtc.Date, TimeSpan.Zero);

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        const string sql = """
            -- 1. Severity counts for open cases
            SELECT severity, COUNT(*) AS count
            FROM exception_cases
            WHERE tenant_id = @TenantId
              AND status NOT IN ('Closed', 'Cancelled')
            GROUP BY severity;

            -- 2. SLA Clocks
            SELECT
                COUNT(CASE WHEN status = 'Breached' THEN 1 END) AS breached_count,
                COUNT(CASE WHEN status = 'Running' AND target_deadline_utc <= @RiskCutoffUtc AND target_deadline_utc > @NowUtc THEN 1 END) AS at_risk_count
            FROM sla_clocks
            WHERE tenant_id = @TenantId;

            -- 3. Unassigned open tasks
            SELECT COUNT(*)
            FROM workflow_tasks
            WHERE tenant_id = @TenantId
              AND assigned_to_user_id IS NULL
              AND status NOT IN ('Completed', 'Cancelled');

            -- 4. Delay rate on active shipments
            SELECT
                COUNT(CASE WHEN is_delayed = 1 THEN 1 END) AS delayed_count,
                COUNT(*) AS total_count
            FROM shipments
            WHERE tenant_id = @TenantId
              AND status NOT IN ('Delivered', 'Cancelled', 'Lost');

            -- 5. Today's tracking events and exceptions
            SELECT
                (SELECT COUNT(*) FROM tracking_events WHERE tenant_id = @TenantId AND created_at_utc >= @TodayStartUtc) AS today_events,
                (SELECT COUNT(*) FROM exception_cases WHERE tenant_id = @TenantId AND created_at_utc >= @TodayStartUtc) AS today_exceptions;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                query.TenantId,
                NowUtc = nowUtc,
                RiskCutoffUtc = riskCutoffUtc,
                TodayStartUtc = todayStartUtc
            },
            cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);

        // 1. Severity counts
        var severityRows = await multi.ReadAsync<SeverityCountRow>();
        var severityDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Critical"] = 0,
            ["High"] = 0,
            ["Medium"] = 0,
            ["Low"] = 0
        };

        var totalOpenCases = 0;
        foreach (var row in severityRows)
        {
            severityDict[row.Severity] = row.Count;
            totalOpenCases += row.Count;
        }

        // 2. SLA Clocks
        var slaRow = await multi.ReadSingleOrDefaultAsync<SlaMetricsRow>() ?? new SlaMetricsRow();

        // 3. Unassigned Tasks
        var unassignedTasks = await multi.ReadSingleOrDefaultAsync<int>();

        // 4. Delay rate
        var delayRow = await multi.ReadSingleOrDefaultAsync<DelayMetricsRow>() ?? new DelayMetricsRow();
        var delayRate = delayRow.TotalCount > 0
            ? Math.Round((double)delayRow.DelayedCount * 100.0 / delayRow.TotalCount, 2)
            : 0.0;

        // 5. Today's metrics
        var todayMetrics = await multi.ReadSingleOrDefaultAsync<TodayMetricsRow>() ?? new TodayMetricsRow();

        sw.Stop();
        ReportingMetrics.QueriesDurationSeconds.Record(
            sw.Elapsed.TotalSeconds,
            new KeyValuePair<string, object?>("report_name", "OperationsDashboard"));

        var response = new GetOperationsDashboardResponse(
            TotalOpenCases: totalOpenCases,
            OpenCasesBySeverity: severityDict,
            SlaBreachedCount: slaRow.BreachedCount,
            SlaAtRiskCount: slaRow.AtRiskCount,
            UnassignedTasksCount: unassignedTasks,
            ActiveCarrierDelayRate: delayRate,
            TodayTrackingEventsCount: todayMetrics.TodayEvents,
            TodayExceptionsDetectedCount: todayMetrics.TodayExceptions,
            GeneratedAtUtc: nowUtc);

        return Result<GetOperationsDashboardResponse>.Success(response);
    }

    private sealed class SeverityCountRow
    {
        public string Severity { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    private sealed class SlaMetricsRow
    {
        public int BreachedCount { get; set; }
        public int AtRiskCount { get; set; }
    }

    private sealed class DelayMetricsRow
    {
        public int DelayedCount { get; set; }
        public int TotalCount { get; set; }
    }

    private sealed class TodayMetricsRow
    {
        public int TodayEvents { get; set; }
        public int TodayExceptions { get; set; }
    }
}
