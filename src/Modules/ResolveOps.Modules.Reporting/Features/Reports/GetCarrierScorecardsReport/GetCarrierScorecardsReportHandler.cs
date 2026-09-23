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

namespace ResolveOps.Modules.Reporting.Features.Reports.GetCarrierScorecardsReport;

public sealed class GetCarrierScorecardsReportHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public GetCarrierScorecardsReportHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<GetCarrierScorecardsReportResponse>> HandleAsync(
        GetCarrierScorecardsReportQuery query,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var nowUtc = _timeProvider.GetUtcNow();

        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        // 1. Query snapshots first
        const string snapshotSql = """
            SELECT
                carrier_id AS CarrierId,
                carrier_name AS CarrierName,
                SUM(total_shipments) AS ShipmentCount,
                SUM(on_time_shipments) AS OnTimeShipments,
                SUM(delayed_shipments) AS DelayedShipments,
                SUM(exception_count) AS ExceptionCount,
                SUM(critical_severity_count) AS CriticalSeverityCount,
                SUM(high_severity_count) AS HighSeverityCount,
                SUM(medium_severity_count) AS MediumSeverityCount,
                SUM(low_severity_count) AS LowSeverityCount,
                SUM(total_claims) AS TotalClaims,
                SUM(approved_claims) AS ApprovedClaims,
                SUM(rejected_claims) AS RejectedClaims,
                SUM(total_claimed_amount) AS TotalClaimedAmount,
                SUM(total_approved_amount) AS TotalApprovedAmount,
                SUM(total_recovered_amount) AS TotalRecoveredAmount,
                AVG(avg_response_time_hours) AS AvgResponseTimeHours
            FROM carrier_performance_snapshots
            WHERE tenant_id = @TenantId
              AND (@CarrierId IS NULL OR carrier_id = @CarrierId)
              AND (@FromDate IS NULL OR period_date >= @FromDate)
              AND (@ToDate IS NULL OR period_date <= @ToDate)
            GROUP BY carrier_id, carrier_name;
            """;

        var command = new CommandDefinition(
            snapshotSql,
            new
            {
                query.TenantId,
                query.CarrierId,
                query.FromDate,
                query.ToDate
            },
            cancellationToken: cancellationToken);

        var snapshotRows = (await connection.QueryAsync<CarrierRawAggregateRow>(command)).ToList();

        List<CarrierRawAggregateRow> rowsToProcess;

        if (snapshotRows.Count > 0)
        {
            rowsToProcess = snapshotRows;
        }
        else
        {
            // 2. Fallback to live calculation if snapshots are empty
            const string liveSql = """
                SELECT
                    cr.id AS CarrierId,
                    cr.name AS CarrierName,
                    COUNT(DISTINCT s.id) AS ShipmentCount,
                    COUNT(DISTINCT CASE WHEN s.is_delayed = 0 AND s.status = 'Delivered' THEN s.id END) AS OnTimeShipments,
                    COUNT(DISTINCT CASE WHEN s.is_delayed = 1 THEN s.id END) AS DelayedShipments,
                    COUNT(DISTINCT ec.id) AS ExceptionCount,
                    COUNT(DISTINCT CASE WHEN ec.severity = 'Critical' THEN ec.id END) AS CriticalSeverityCount,
                    COUNT(DISTINCT CASE WHEN ec.severity = 'High' THEN ec.id END) AS HighSeverityCount,
                    COUNT(DISTINCT CASE WHEN ec.severity = 'Medium' THEN ec.id END) AS MediumSeverityCount,
                    COUNT(DISTINCT CASE WHEN ec.severity = 'Low' THEN ec.id END) AS LowSeverityCount,
                    COUNT(DISTINCT cl.id) AS TotalClaims,
                    COUNT(DISTINCT CASE WHEN cl.status IN ('Approved', 'PartiallyApproved', 'Paid', 'Closed') THEN cl.id END) AS ApprovedClaims,
                    COUNT(DISTINCT CASE WHEN cl.status = 'Denied' THEN cl.id END) AS RejectedClaims,
                    COALESCE(SUM(cl.claimed_amount), 0) AS TotalClaimedAmount,
                    COALESCE(SUM(cl.approved_amount), 0) AS TotalApprovedAmount,
                    COALESCE(SUM(cl.recovered_amount), 0) AS TotalRecoveredAmount,
                    0.0 AS AvgResponseTimeHours
                FROM carriers cr
                LEFT JOIN shipment_legs leg ON leg.carrier_id = cr.id AND leg.tenant_id = cr.tenant_id
                LEFT JOIN shipments s ON s.id = leg.shipment_id AND s.tenant_id = cr.tenant_id
                LEFT JOIN exception_cases ec ON ec.shipment_id = s.id AND ec.tenant_id = cr.tenant_id
                LEFT JOIN claims cl ON cl.carrier_id = cr.id AND cl.tenant_id = cr.tenant_id
                WHERE cr.tenant_id = @TenantId
                  AND (@CarrierId IS NULL OR cr.id = @CarrierId)
                GROUP BY cr.id, cr.name;
                """;

            var liveCommand = new CommandDefinition(
                liveSql,
                new
                {
                    query.TenantId,
                    query.CarrierId
                },
                cancellationToken: cancellationToken);

            rowsToProcess = (await connection.QueryAsync<CarrierRawAggregateRow>(liveCommand)).ToList();
        }

        sw.Stop();
        ReportingMetrics.QueriesDurationSeconds.Record(
            sw.Elapsed.TotalSeconds,
            new KeyValuePair<string, object?>("report_name", "CarrierScorecardsReport"));

        var scorecards = rowsToProcess.Select(r =>
        {
            var exceptionRate = r.ShipmentCount > 0
                ? Math.Round((double)r.ExceptionCount * 100.0 / r.ShipmentCount, 2)
                : 0.0;

            var onTimeRate = r.ShipmentCount > 0
                ? Math.Round((double)r.OnTimeShipments * 100.0 / r.ShipmentCount, 2)
                : 100.0;

            var claimApprovalRate = r.TotalClaims > 0
                ? Math.Round((double)r.ApprovedClaims * 100.0 / r.TotalClaims, 2)
                : 0.0;

            var recoveryRate = r.TotalApprovedAmount > 0
                ? Math.Round((double)(r.TotalRecoveredAmount / r.TotalApprovedAmount) * 100.0, 2)
                : 0.0;

            var severityDistribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Critical"] = r.CriticalSeverityCount,
                ["High"] = r.HighSeverityCount,
                ["Medium"] = r.MediumSeverityCount,
                ["Low"] = r.LowSeverityCount
            };

            return new CarrierScorecardDto(
                CarrierId: r.CarrierId,
                CarrierName: r.CarrierName,
                ShipmentCount: r.ShipmentCount,
                ExceptionRate: exceptionRate,
                OnTimeRate: onTimeRate,
                SeverityDistribution: severityDistribution,
                AvgResponseTimeHours: Math.Round(r.AvgResponseTimeHours, 2),
                ClaimApprovalRate: claimApprovalRate,
                RecoveryRate: recoveryRate,
                TotalClaimedAmount: r.TotalClaimedAmount,
                TotalApprovedAmount: r.TotalApprovedAmount,
                TotalRecoveredAmount: r.TotalRecoveredAmount);
        }).ToList();

        var response = new GetCarrierScorecardsReportResponse(
            Scorecards: scorecards,
            GeneratedAtUtc: nowUtc);

        return Result<GetCarrierScorecardsReportResponse>.Success(response);
    }

    private sealed class CarrierRawAggregateRow
    {
        public Guid CarrierId { get; set; }
        public string CarrierName { get; set; } = string.Empty;
        public int ShipmentCount { get; set; }
        public int OnTimeShipments { get; set; }
        public int DelayedShipments { get; set; }
        public int ExceptionCount { get; set; }
        public int CriticalSeverityCount { get; set; }
        public int HighSeverityCount { get; set; }
        public int MediumSeverityCount { get; set; }
        public int LowSeverityCount { get; set; }
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalClaimedAmount { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public decimal TotalRecoveredAmount { get; set; }
        public double AvgResponseTimeHours { get; set; }
    }
}
