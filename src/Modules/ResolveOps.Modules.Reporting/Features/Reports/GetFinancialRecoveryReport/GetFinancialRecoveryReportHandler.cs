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

namespace ResolveOps.Modules.Reporting.Features.Reports.GetFinancialRecoveryReport;

public sealed class GetFinancialRecoveryReportHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public GetFinancialRecoveryReportHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<GetFinancialRecoveryReportResponse>> HandleAsync(
        GetFinancialRecoveryReportQuery query,
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
            -- 1. Monetary Totals
            SELECT
                COALESCE(SUM(approved_amount), 0) AS TotalApproved,
                COALESCE(SUM(recovered_amount), 0) AS TotalRecovered,
                COALESCE(SUM(written_off_amount), 0) AS TotalWrittenOff
            FROM claims
            WHERE tenant_id = @TenantId
              AND (@CarrierId IS NULL OR carrier_id = @CarrierId)
              AND (@FromDate IS NULL OR created_at_utc >= @FromDate)
              AND (@ToDate IS NULL OR created_at_utc <= @ToDate);

            -- 2. Recovery breakdown by transaction type
            SELECT
                rt.transaction_type AS TransactionType,
                COALESCE(SUM(rt.amount), 0) AS Amount,
                COUNT(*) AS Count
            FROM recovery_transactions rt
            INNER JOIN claims c ON c.id = rt.claim_id AND c.tenant_id = rt.tenant_id
            WHERE rt.tenant_id = @TenantId
              AND (@CarrierId IS NULL OR c.carrier_id = @CarrierId)
              AND (@FromDate IS NULL OR rt.received_at_utc >= @FromDate)
              AND (@ToDate IS NULL OR rt.received_at_utc <= @ToDate)
            GROUP BY rt.transaction_type;

            -- 3. Carrier breakdown
            SELECT
                cr.id AS CarrierId,
                cr.name AS CarrierName,
                COALESCE(SUM(c.approved_amount), 0) AS TotalApproved,
                COALESCE(SUM(c.recovered_amount), 0) AS TotalRecovered,
                COALESCE(SUM(c.written_off_amount), 0) AS TotalWrittenOff
            FROM carriers cr
            LEFT JOIN claims c ON c.carrier_id = cr.id AND c.tenant_id = cr.tenant_id
            WHERE cr.tenant_id = @TenantId
              AND (@CarrierId IS NULL OR cr.id = @CarrierId)
            GROUP BY cr.id, cr.name;

            -- 4. Write-off reason breakdown
            SELECT
                c.write_off_reason AS ReasonCode,
                COALESCE(SUM(c.written_off_amount), 0) AS Amount,
                COUNT(*) AS Count
            FROM claims c
            WHERE c.tenant_id = @TenantId
              AND c.written_off_amount > 0
              AND c.write_off_reason IS NOT NULL
              AND (@CarrierId IS NULL OR c.carrier_id = @CarrierId)
              AND (@FromDate IS NULL OR c.created_at_utc >= @FromDate)
              AND (@ToDate IS NULL OR c.created_at_utc <= @ToDate)
            GROUP BY c.write_off_reason;
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

        var totals = await multi.ReadSingleOrDefaultAsync<TotalsRow>() ?? new TotalsRow();
        var typeRows = (await multi.ReadAsync<RecoveryTypeBreakdownDto>()).ToList();
        var carrierRawRows = (await multi.ReadAsync<CarrierBreakdownRawRow>()).ToList();
        var writeOffRows = (await multi.ReadAsync<WriteOffBreakdownDto>()).ToList();

        sw.Stop();
        ReportingMetrics.QueriesDurationSeconds.Record(
            sw.Elapsed.TotalSeconds,
            new KeyValuePair<string, object?>("report_name", "FinancialRecoveryReport"));

        var pendingRecoveryBalance = Math.Max(0m, totals.TotalApproved - totals.TotalRecovered - totals.TotalWrittenOff);

        var carrierBreakdown = carrierRawRows.Select(r =>
        {
            var pending = Math.Max(0m, r.TotalApproved - r.TotalRecovered - r.TotalWrittenOff);
            return new CarrierRecoveryBreakdownDto(
                CarrierId: r.CarrierId,
                CarrierName: r.CarrierName,
                TotalApproved: r.TotalApproved,
                TotalRecovered: r.TotalRecovered,
                TotalWrittenOff: r.TotalWrittenOff,
                PendingBalance: pending);
        }).ToList();

        var response = new GetFinancialRecoveryReportResponse(
            TotalApproved: totals.TotalApproved,
            TotalRecovered: totals.TotalRecovered,
            TotalWrittenOff: totals.TotalWrittenOff,
            PendingRecoveryBalance: pendingRecoveryBalance,
            RecoveryTypeBreakdown: typeRows,
            CarrierBreakdown: carrierBreakdown,
            WriteOffBreakdown: writeOffRows,
            GeneratedAtUtc: nowUtc);

        return Result<GetFinancialRecoveryReportResponse>.Success(response);
    }

    private sealed class TotalsRow
    {
        public decimal TotalApproved { get; set; }
        public decimal TotalRecovered { get; set; }
        public decimal TotalWrittenOff { get; set; }
    }

    private sealed class CarrierBreakdownRawRow
    {
        public Guid CarrierId { get; set; }
        public string CarrierName { get; set; } = string.Empty;
        public decimal TotalApproved { get; set; }
        public decimal TotalRecovered { get; set; }
        public decimal TotalWrittenOff { get; set; }
    }
}
