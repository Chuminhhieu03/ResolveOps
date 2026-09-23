using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Features.Dashboards.GetClaimsDashboard;

public sealed class GetClaimsDashboardHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public GetClaimsDashboardHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<GetClaimsDashboardResponse>> HandleAsync(
        GetClaimsDashboardQuery query,
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
            -- 1. Claims counts and monetary aggregates
            SELECT
                COUNT(*) AS TotalClaims,
                COUNT(CASE WHEN status = 'Draft' THEN 1 END) AS DraftCount,
                COUNT(CASE WHEN status IN ('UnderReview', 'ReadyForReview', 'ApprovedForSubmission') THEN 1 END) AS UnderReviewCount,
                COUNT(CASE WHEN status IN ('Submitted', 'Acknowledged', 'MoreInformationRequested') THEN 1 END) AS SubmittedCount,
                COUNT(CASE WHEN status IN ('Appealed', 'Denied') THEN 1 END) AS DisputedCount,
                COALESCE(SUM(claimed_amount), 0) AS TotalClaimed,
                COALESCE(SUM(approved_amount), 0) AS TotalApproved,
                COALESCE(SUM(recovered_amount), 0) AS TotalRecovered
            FROM claims
            WHERE tenant_id = @TenantId;

            -- 2. Average time to carrier decision in hours
            SELECT AVG(CAST(DATEDIFF(MINUTE, c.submitted_at_utc, r.recorded_at_utc) AS float) / 60.0)
            FROM claims c
            INNER JOIN carrier_claim_responses r ON r.claim_id = c.id AND r.tenant_id = c.tenant_id
            WHERE c.tenant_id = @TenantId
              AND c.submitted_at_utc IS NOT NULL
              AND r.response_type IN ('Approved', 'PartiallyApproved', 'Denied', 'SettlementOffered');
            """;

        var command = new CommandDefinition(
            sql,
            new { query.TenantId },
            cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);

        var metrics = await multi.ReadSingleOrDefaultAsync<ClaimsMetricsRow>() ?? new ClaimsMetricsRow();
        var avgDecisionHours = await multi.ReadSingleOrDefaultAsync<double?>() ?? 0.0;

        sw.Stop();
        ReportingMetrics.QueriesDurationSeconds.Record(
            sw.Elapsed.TotalSeconds,
            new KeyValuePair<string, object?>("report_name", "ClaimsDashboard"));

        var recoveryRate = metrics.TotalApproved > 0
            ? Math.Round((double)(metrics.TotalRecovered / metrics.TotalApproved) * 100.0, 2)
            : 0.0;

        var response = new GetClaimsDashboardResponse(
            TotalClaims: metrics.TotalClaims,
            DraftClaimsCount: metrics.DraftCount,
            UnderReviewClaimsCount: metrics.UnderReviewCount,
            SubmittedClaimsCount: metrics.SubmittedCount,
            DisputedClaimsCount: metrics.DisputedCount,
            TotalClaimedAmount: metrics.TotalClaimed,
            TotalApprovedAmount: metrics.TotalApproved,
            TotalRecoveredAmount: metrics.TotalRecovered,
            RecoveryRatePercentage: recoveryRate,
            AvgTimeToCarrierDecisionHours: Math.Round(avgDecisionHours, 2),
            GeneratedAtUtc: nowUtc);

        return Result<GetClaimsDashboardResponse>.Success(response);
    }

    private sealed class ClaimsMetricsRow
    {
        public int TotalClaims { get; set; }
        public int DraftCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int SubmittedCount { get; set; }
        public int DisputedCount { get; set; }
        public decimal TotalClaimed { get; set; }
        public decimal TotalApproved { get; set; }
        public decimal TotalRecovered { get; set; }
    }
}
