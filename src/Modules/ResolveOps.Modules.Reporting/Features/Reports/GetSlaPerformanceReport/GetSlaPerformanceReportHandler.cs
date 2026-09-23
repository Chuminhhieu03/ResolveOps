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

namespace ResolveOps.Modules.Reporting.Features.Reports.GetSlaPerformanceReport;

public sealed class GetSlaPerformanceReportHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public GetSlaPerformanceReportHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<GetSlaPerformanceReportResponse>> HandleAsync(
        GetSlaPerformanceReportQuery query,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var nowUtc = _timeProvider.GetUtcNow();

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        const string sql = """
            -- 1. Overall SLA totals
            SELECT
                COUNT(*) AS TotalClocks,
                COUNT(CASE WHEN sc.status = 'Completed' THEN 1 END) AS MetCount,
                COUNT(CASE WHEN sc.status = 'Breached' THEN 1 END) AS BreachedCount,
                AVG(CASE WHEN sc.clock_type = 'Triage' AND sc.status = 'Completed' AND sc.completed_at_utc IS NOT NULL
                    THEN CAST(DATEDIFF(MINUTE, sc.started_at_utc, sc.completed_at_utc) AS float) / 60.0 END) AS AvgTriageHours,
                AVG(CASE WHEN sc.clock_type = 'Resolution' AND sc.status = 'Completed' AND sc.completed_at_utc IS NOT NULL
                    THEN CAST(DATEDIFF(MINUTE, sc.started_at_utc, sc.completed_at_utc) AS float) / 60.0 END) AS AvgResolutionHours
            FROM sla_clocks sc
            INNER JOIN exception_cases c ON c.id = sc.case_id AND c.tenant_id = sc.tenant_id
            LEFT JOIN shipments s ON s.id = c.shipment_id AND s.tenant_id = c.tenant_id
            LEFT JOIN shipment_legs leg ON leg.shipment_id = s.id AND leg.tenant_id = s.tenant_id AND leg.sequence_number = 1
            WHERE sc.tenant_id = @TenantId
              AND (@CarrierId IS NULL OR leg.carrier_id = @CarrierId)
              AND (@FromDate IS NULL OR sc.created_at_utc >= @FromDate)
              AND (@ToDate IS NULL OR sc.created_at_utc <= @ToDate);

            -- 2. Per-carrier breakdown
            SELECT
                leg.carrier_id AS CarrierId,
                COALESCE(cr.name, 'Unassigned') AS CarrierName,
                COUNT(*) AS TotalClocks,
                COUNT(CASE WHEN sc.status = 'Completed' THEN 1 END) AS MetCount,
                COUNT(CASE WHEN sc.status = 'Breached' THEN 1 END) AS BreachedCount
            FROM sla_clocks sc
            INNER JOIN exception_cases c ON c.id = sc.case_id AND c.tenant_id = sc.tenant_id
            LEFT JOIN shipments s ON s.id = c.shipment_id AND s.tenant_id = c.tenant_id
            LEFT JOIN shipment_legs leg ON leg.shipment_id = s.id AND leg.tenant_id = s.tenant_id AND leg.sequence_number = 1
            LEFT JOIN carriers cr ON cr.id = leg.carrier_id AND cr.tenant_id = c.tenant_id
            WHERE sc.tenant_id = @TenantId
              AND (@CarrierId IS NULL OR leg.carrier_id = @CarrierId)
              AND (@FromDate IS NULL OR sc.created_at_utc >= @FromDate)
              AND (@ToDate IS NULL OR sc.created_at_utc <= @ToDate)
            GROUP BY leg.carrier_id, cr.name
            ORDER BY TotalClocks DESC;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                query.TenantId,
                query.CarrierId,
                query.FromDate,
                query.ToDate
            },
            cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);

        var overall = await multi.ReadSingleOrDefaultAsync<OverallSlaRow>() ?? new OverallSlaRow();
        var carrierRows = (await multi.ReadAsync<CarrierSlaRawRow>()).ToList();

        sw.Stop();
        ReportingMetrics.QueriesDurationSeconds.Record(
            sw.Elapsed.TotalSeconds,
            new KeyValuePair<string, object?>("report_name", "SlaPerformanceReport"));

        var decidedClocks = overall.MetCount + overall.BreachedCount;
        var overallCompliance = decidedClocks > 0
            ? Math.Round((double)overall.MetCount * 100.0 / decidedClocks, 2)
            : 100.0;

        var carrierBreakdown = carrierRows.Select(r =>
        {
            var carrierDecided = r.MetCount + r.BreachedCount;
            var compliance = carrierDecided > 0
                ? Math.Round((double)r.MetCount * 100.0 / carrierDecided, 2)
                : 100.0;

            return new CarrierSlaPerformanceDto(
                CarrierId: r.CarrierId,
                CarrierName: r.CarrierName,
                TotalClocks: r.TotalClocks,
                MetCount: r.MetCount,
                BreachedCount: r.BreachedCount,
                CompliancePercentage: compliance);
        }).ToList();

        var response = new GetSlaPerformanceReportResponse(
            TotalClocksTracked: overall.TotalClocks,
            MetCount: overall.MetCount,
            BreachedCount: overall.BreachedCount,
            OverallCompliancePercentage: overallCompliance,
            AvgTimeToTriageHours: Math.Round(overall.AvgTriageHours ?? 0.0, 2),
            AvgTimeToResolutionHours: Math.Round(overall.AvgResolutionHours ?? 0.0, 2),
            CarrierBreakdown: carrierBreakdown,
            GeneratedAtUtc: nowUtc);

        return Result<GetSlaPerformanceReportResponse>.Success(response);
    }

    private sealed class OverallSlaRow
    {
        public int TotalClocks { get; set; }
        public int MetCount { get; set; }
        public int BreachedCount { get; set; }
        public double? AvgTriageHours { get; set; }
        public double? AvgResolutionHours { get; set; }
    }

    private sealed class CarrierSlaRawRow
    {
        public Guid? CarrierId { get; set; }
        public string CarrierName { get; set; } = string.Empty;
        public int TotalClocks { get; set; }
        public int MetCount { get; set; }
        public int BreachedCount { get; set; }
    }
}
