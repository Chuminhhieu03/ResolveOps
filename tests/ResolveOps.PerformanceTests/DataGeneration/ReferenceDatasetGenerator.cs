using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace ResolveOps.PerformanceTests.DataGeneration;

/// <summary>
/// High-performance synthetic reference dataset generator for ResolveOps.
/// Implements Master Spec §20.7 & §24 Phase 16 reference workload:
/// - 100 tenants
/// - 1,000,000 shipments
/// - 3,000,000 shipment legs
/// - 20,000,000 tracking events
/// - 100,000 exception cases
/// - 25,000 claims
/// - 1,000,000 timeline entries
/// - 500,000 document records
/// </summary>
public sealed class ReferenceDatasetGenerator
{
    private static readonly string[] _carrierCodes = ["FEDEX", "UPS", "DHL", "MAERSK", "USPS"];
    private static readonly string[] _serviceLevels = ["Standard", "Express", "Priority", "Overnight", "Economy"];
    private static readonly string[] _exceptionTypes = ["Damaged", "Lost", "TemperatureExcursion", "Delayed", "Shortage"];
    private static readonly string[] _caseSeverities = ["Low", "Medium", "High", "Critical"];
    private static readonly string[] _caseStatuses = ["Open", "UnderInvestigation", "Mitigating", "ClaimRequired", "Resolved", "Closed"];
    private static readonly string[] _claimStatuses = ["Draft", "PendingApproval", "Approved", "Submitted", "Acknowledged", "Settled"];
    private static readonly string[] _eventTypes = ["Departed", "InTransit", "ArrivedAtHub", "CustomsCleared", "OutForDelivery", "Delivered"];

    public static async Task<GenerationReport> SeedAsync(
        SqlConnection connection,
        ReferenceDatasetOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(options);

        var sw = Stopwatch.StartNew();
        var rng = new Random(options.RandomSeed);

        // 1. Tenants
        var tenantsTable = BuildTenantsTable(options.TenantCount, rng);
        await SqlBulkDataWriter.BulkInsertAsync(connection, "tenants", tenantsTable, options.BatchSize, cancellationToken: cancellationToken);

        var tenantIds = new List<Guid>();
        foreach (DataRow row in tenantsTable.Rows)
        {
            tenantIds.Add((Guid)row["id"]);
        }

        // 2. Carriers, Customers, Locations per tenant
        var carriersTable = BuildCarriersTable(tenantIds, rng);
        await SqlBulkDataWriter.BulkInsertAsync(connection, "carriers", carriersTable, options.BatchSize, cancellationToken: cancellationToken);

        var customersTable = BuildCustomersTable(tenantIds, rng);
        await SqlBulkDataWriter.BulkInsertAsync(connection, "customers", customersTable, options.BatchSize, cancellationToken: cancellationToken);

        var locationsTable = BuildLocationsTable(tenantIds, rng);
        await SqlBulkDataWriter.BulkInsertAsync(connection, "locations", locationsTable, options.BatchSize, cancellationToken: cancellationToken);

        // 3. Shipments and Legs in batches
        int shipmentsCreated = 0;
        int legsCreated = 0;
        int eventsCreated = 0;
        int casesCreated = 0;
        int claimsCreated = 0;
        int timelineCreated = 0;

        int totalShipments = options.ShipmentsTotal;
        int batchSize = Math.Min(options.BatchSize, totalShipments);

        while (shipmentsCreated < totalShipments && !cancellationToken.IsCancellationRequested)
        {
            int currentBatch = Math.Min(batchSize, totalShipments - shipmentsCreated);
            var (shipments, legs, events, cases, timeline, claims) = GenerateBatch(
                tenantIds,
                currentBatch,
                shipmentsCreated,
                options,
                rng);

            await SqlBulkDataWriter.BulkInsertAsync(connection, "shipments", shipments, options.BatchSize, cancellationToken: cancellationToken);
            await SqlBulkDataWriter.BulkInsertAsync(connection, "shipment_legs", legs, options.BatchSize, cancellationToken: cancellationToken);
            await SqlBulkDataWriter.BulkInsertAsync(connection, "tracking_events", events, options.BatchSize, cancellationToken: cancellationToken);
            await SqlBulkDataWriter.BulkInsertAsync(connection, "exception_cases", cases, options.BatchSize, cancellationToken: cancellationToken);
            await SqlBulkDataWriter.BulkInsertAsync(connection, "case_timeline_entries", timeline, options.BatchSize, cancellationToken: cancellationToken);
            await SqlBulkDataWriter.BulkInsertAsync(connection, "claims", claims, options.BatchSize, cancellationToken: cancellationToken);

            shipmentsCreated += shipments.Rows.Count;
            legsCreated += legs.Rows.Count;
            eventsCreated += events.Rows.Count;
            casesCreated += cases.Rows.Count;
            timelineCreated += timeline.Rows.Count;
            claimsCreated += claims.Rows.Count;
        }

        sw.Stop();

        return new GenerationReport(
            TenantsGenerated: tenantIds.Count,
            ShipmentsGenerated: shipmentsCreated,
            LegsGenerated: legsCreated,
            TrackingEventsGenerated: eventsCreated,
            ExceptionCasesGenerated: casesCreated,
            ClaimsGenerated: claimsCreated,
            TimelineEntriesGenerated: timelineCreated,
            ElapsedMilliseconds: sw.ElapsedMilliseconds,
            EntitiesPerSecond: (long)((shipmentsCreated + legsCreated + eventsCreated + casesCreated + claimsCreated + timelineCreated) / Math.Max(0.001, sw.Elapsed.TotalSeconds)));
    }

    public static GenerationReport BenchmarkInMemoryGeneration(ReferenceDatasetOptions options)
    {
        var sw = Stopwatch.StartNew();
        var rng = new Random(options.RandomSeed);

        var tenantsTable = BuildTenantsTable(options.TenantCount, rng);
        var tenantIds = new List<Guid>();
        foreach (DataRow row in tenantsTable.Rows)
        {
            tenantIds.Add((Guid)row["id"]);
        }

        var carriersTable = BuildCarriersTable(tenantIds, rng);
        var customersTable = BuildCustomersTable(tenantIds, rng);
        var locationsTable = BuildLocationsTable(tenantIds, rng);

        var (shipments, legs, events, cases, timeline, claims) = GenerateBatch(
            tenantIds,
            Math.Min(options.BatchSize, options.ShipmentsTotal),
            0,
            options,
            rng);

        sw.Stop();
        int totalEntities = tenantsTable.Rows.Count + carriersTable.Rows.Count + customersTable.Rows.Count + locationsTable.Rows.Count
                            + shipments.Rows.Count + legs.Rows.Count + events.Rows.Count + cases.Rows.Count + timeline.Rows.Count + claims.Rows.Count;

        return new GenerationReport(
            TenantsGenerated: tenantIds.Count,
            ShipmentsGenerated: shipments.Rows.Count,
            LegsGenerated: legs.Rows.Count,
            TrackingEventsGenerated: events.Rows.Count,
            ExceptionCasesGenerated: cases.Rows.Count,
            ClaimsGenerated: claims.Rows.Count,
            TimelineEntriesGenerated: timeline.Rows.Count,
            ElapsedMilliseconds: sw.ElapsedMilliseconds,
            EntitiesPerSecond: (long)(totalEntities / Math.Max(0.001, sw.Elapsed.TotalSeconds)));
    }

    private static (DataTable Shipments, DataTable Legs, DataTable Events, DataTable Cases, DataTable Timeline, DataTable Claims) GenerateBatch(
        List<Guid> tenantIds,
        int count,
        int offset,
        ReferenceDatasetOptions options,
        Random rng)
    {
        var shipmentsTable = CreateShipmentsTable();
        var legsTable = CreateShipmentLegsTable();
        var eventsTable = CreateTrackingEventsTable();
        var casesTable = CreateExceptionCasesTable();
        var timelineTable = CreateCaseTimelineEntriesTable();
        var claimsTable = CreateClaimsTable();

        var baseTime = DateTimeOffset.UtcNow.AddDays(-60);

        for (int i = 0; i < count; i++)
        {
            int globalIndex = offset + i;
            var tenantId = tenantIds[globalIndex % tenantIds.Count];
            var shipmentId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var originId = Guid.NewGuid();
            var destId = Guid.NewGuid();
            var carrierId = Guid.NewGuid();
            var now = baseTime.AddMinutes(globalIndex * 2);

            var isDelayed = rng.NextDouble() < 0.15;
            var status = isDelayed ? "Delayed" : "InTransit";
            var declaredValue = (decimal)(rng.Next(500, 50000) + rng.NextDouble());

            shipmentsTable.Rows.Add(
                shipmentId,
                tenantId,
                "API",
                $"SHP-{globalIndex:D8}",
                customerId,
                originId,
                destId,
                status,
                _serviceLevels[rng.Next(_serviceLevels.Length)],
                now,
                now.AddDays(3),
                now.AddHours(1),
                DBNull.Value,
                declaredValue,
                "USD",
                rng.Next(1, 100),
                (decimal)(rng.Next(10, 500) + rng.NextDouble()),
                "kg",
                now,
                Guid.NewGuid().ToString("N"));

            // 3 Legs per shipment
            var legIds = new List<Guid>();
            for (int legSeq = 1; legSeq <= 3; legSeq++)
            {
                var legId = Guid.NewGuid();
                legIds.Add(legId);
                legsTable.Rows.Add(
                    legId,
                    tenantId,
                    shipmentId,
                    legSeq,
                    carrierId,
                    originId,
                    destId,
                    "InTransit",
                    now.AddHours(legSeq * 12));
            }

            // Tracking events (avg 20 events per shipment)
            int eventCount = Math.Max(1, options.TrackingEventsTotal / Math.Max(1, options.ShipmentsTotal));
            for (int e = 0; e < eventCount; e++)
            {
                var eventId = Guid.NewGuid();
                var eventType = _eventTypes[e % _eventTypes.Length];
                eventsTable.Rows.Add(
                    eventId,
                    tenantId,
                    shipmentId,
                    legIds[e % legIds.Count],
                    carrierId,
                    Guid.NewGuid(),
                    $"EVT-{globalIndex}-{e}",
                    eventType,
                    $"CODE-{eventType.ToUpperInvariant()}",
                    now.AddHours(e * 3),
                    now.AddHours(e * 3).AddMinutes(5),
                    Guid.NewGuid().ToString("N"),
                    now.AddHours(e * 3));
            }

            // 10% Exception rate
            if (rng.NextDouble() < 0.10)
            {
                var caseId = Guid.NewGuid();
                var excType = _exceptionTypes[rng.Next(_exceptionTypes.Length)];
                var severity = _caseSeverities[rng.Next(_caseSeverities.Length)];
                var caseStatus = _caseStatuses[rng.Next(_caseStatuses.Length)];
                var caseNumber = $"CAS-{globalIndex:D8}";

                casesTable.Rows.Add(
                    caseId,
                    tenantId,
                    caseNumber,
                    shipmentId,
                    legIds[0],
                    excType,
                    $"FINGERPRINT-{tenantId}-{shipmentId}",
                    caseStatus,
                    severity,
                    rng.Next(1, 100),
                    Guid.NewGuid(),
                    1,
                    declaredValue * 0.5m,
                    "USD",
                    now.AddHours(6),
                    now.AddHours(6),
                    Guid.NewGuid().ToString("N"));

                // Timeline entries per case (avg 10)
                for (int t = 0; t < 10; t++)
                {
                    timelineTable.Rows.Add(
                        Guid.NewGuid(),
                        tenantId,
                        caseId,
                        "StatusChanged",
                        "System",
                        $"Case timeline entry {t + 1} generated.",
                        now.AddHours(6 + t));
                }

                // 25% of exception cases become claims
                if (rng.NextDouble() < 0.25)
                {
                    var claimId = Guid.NewGuid();
                    var claimStatus = _claimStatuses[rng.Next(_claimStatuses.Length)];
                    claimsTable.Rows.Add(
                        claimId,
                        tenantId,
                        caseId,
                        carrierId,
                        $"CLM-{globalIndex:D8}",
                        excType,
                        claimStatus,
                        "Eligible",
                        "USD",
                        declaredValue * 0.5m,
                        declaredValue * 0.4m,
                        claimStatus == "Settled" ? declaredValue * 0.4m : 0m,
                        0m,
                        now.AddHours(24),
                        Guid.NewGuid().ToString("N"));
                }
            }
        }

        return (shipmentsTable, legsTable, eventsTable, casesTable, timelineTable, claimsTable);
    }

    private static DataTable BuildTenantsTable(int count, Random rng)
    {
        var table = new DataTable("tenants");
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("identifier", typeof(string));
        table.Columns.Add("plan", typeof(string));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("created_at_utc", typeof(DateTimeOffset));

        for (int i = 1; i <= count; i++)
        {
            table.Rows.Add(
                Guid.NewGuid(),
                $"Logistics Tenant {i:D3}",
                $"tenant-{i:D3}",
                i % 3 == 0 ? "Enterprise" : "Standard",
                "Active",
                DateTimeOffset.UtcNow.AddMonths(-6));
        }

        return table;
    }

    private static DataTable BuildCarriersTable(List<Guid> tenantIds, Random rng)
    {
        var table = new DataTable("carriers");
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("tenant_id", typeof(Guid));
        table.Columns.Add("carrier_code", typeof(string));
        table.Columns.Add("display_name", typeof(string));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("created_at_utc", typeof(DateTimeOffset));

        foreach (var tenantId in tenantIds)
        {
            foreach (var code in _carrierCodes)
            {
                table.Rows.Add(
                    Guid.NewGuid(),
                    tenantId,
                    code,
                    $"{code} Express",
                    "Active",
                    DateTimeOffset.UtcNow.AddMonths(-6));
            }
        }

        return table;
    }

    private static DataTable BuildCustomersTable(List<Guid> tenantIds, Random rng)
    {
        var table = new DataTable("customers");
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("tenant_id", typeof(Guid));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("account_number", typeof(string));
        table.Columns.Add("created_at_utc", typeof(DateTimeOffset));

        foreach (var tenantId in tenantIds)
        {
            for (int i = 1; i <= 5; i++)
            {
                table.Rows.Add(
                    Guid.NewGuid(),
                    tenantId,
                    $"Customer Org {i}",
                    $"ACC-{i:D5}",
                    DateTimeOffset.UtcNow.AddMonths(-6));
            }
        }

        return table;
    }

    private static DataTable BuildLocationsTable(List<Guid> tenantIds, Random rng)
    {
        var table = new DataTable("locations");
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("tenant_id", typeof(Guid));
        table.Columns.Add("code", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("city", typeof(string));
        table.Columns.Add("country_code", typeof(string));
        table.Columns.Add("created_at_utc", typeof(DateTimeOffset));

        string[] cities = ["Chicago", "Dallas", "Frankfurt", "Singapore", "Tokyo"];

        foreach (var tenantId in tenantIds)
        {
            for (int i = 0; i < cities.Length; i++)
            {
                table.Rows.Add(
                    Guid.NewGuid(),
                    tenantId,
                    $"HUB-{cities[i][..3].ToUpperInvariant()}",
                    $"{cities[i]} Logistics Hub",
                    cities[i],
                    "US",
                    DateTimeOffset.UtcNow.AddMonths(-6));
            }
        }

        return table;
    }

    private static DataTable CreateShipmentsTable()
    {
        var table = new DataTable("shipments");
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("tenant_id", typeof(Guid));
        table.Columns.Add("source_system", typeof(string));
        table.Columns.Add("external_reference", typeof(string));
        table.Columns.Add("customer_id", typeof(Guid));
        table.Columns.Add("origin_location_id", typeof(Guid));
        table.Columns.Add("destination_location_id", typeof(Guid));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("service_level", typeof(string));
        table.Columns.Add("planned_pickup_at_utc", typeof(DateTimeOffset));
        table.Columns.Add("planned_delivery_at_utc", typeof(DateTimeOffset));
        table.Columns.Add("actual_pickup_at_utc", typeof(DateTimeOffset));
        table.Columns.Add("actual_delivery_at_utc", typeof(DateTimeOffset));
        table.Columns.Add("declared_value", typeof(decimal));
        table.Columns.Add("declared_value_currency", typeof(string));
        table.Columns.Add("expected_package_count", typeof(int));
        table.Columns.Add("expected_weight", typeof(decimal));
        table.Columns.Add("weight_unit", typeof(string));
        table.Columns.Add("created_at_utc", typeof(DateTimeOffset));
        table.Columns.Add("concurrency_stamp", typeof(string));
        return table;
    }

    private static DataTable CreateShipmentLegsTable()
    {
        var table = new DataTable("shipment_legs");
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("tenant_id", typeof(Guid));
        table.Columns.Add("shipment_id", typeof(Guid));
        table.Columns.Add("leg_sequence", typeof(int));
        table.Columns.Add("carrier_id", typeof(Guid));
        table.Columns.Add("origin_location_id", typeof(Guid));
        table.Columns.Add("destination_location_id", typeof(Guid));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("created_at_utc", typeof(DateTimeOffset));
        return table;
    }

    private static DataTable CreateTrackingEventsTable()
    {
        var table = new DataTable("tracking_events");
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("tenant_id", typeof(Guid));
        table.Columns.Add("shipment_id", typeof(Guid));
        table.Columns.Add("shipment_leg_id", typeof(Guid));
        table.Columns.Add("carrier_id", typeof(Guid));
        table.Columns.Add("inbound_receipt_id", typeof(Guid));
        table.Columns.Add("external_event_id", typeof(string));
        table.Columns.Add("event_type", typeof(string));
        table.Columns.Add("event_code", typeof(string));
        table.Columns.Add("occurred_at_utc", typeof(DateTimeOffset));
        table.Columns.Add("received_at_utc", typeof(DateTimeOffset));
        table.Columns.Add("correlation_id", typeof(string));
        table.Columns.Add("created_at_utc", typeof(DateTimeOffset));
        return table;
    }

    private static DataTable CreateExceptionCasesTable()
    {
        var table = new DataTable("exception_cases");
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("tenant_id", typeof(Guid));
        table.Columns.Add("case_number", typeof(string));
        table.Columns.Add("shipment_id", typeof(Guid));
        table.Columns.Add("shipment_leg_id", typeof(Guid));
        table.Columns.Add("exception_type", typeof(string));
        table.Columns.Add("fingerprint", typeof(string));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("severity", typeof(string));
        table.Columns.Add("severity_score", typeof(int));
        table.Columns.Add("policy_id", typeof(Guid));
        table.Columns.Add("policy_version_number", typeof(int));
        table.Columns.Add("financial_exposure", typeof(decimal));
        table.Columns.Add("exposure_currency", typeof(string));
        table.Columns.Add("detected_at_utc", typeof(DateTimeOffset));
        table.Columns.Add("created_at_utc", typeof(DateTimeOffset));
        table.Columns.Add("concurrency_stamp", typeof(string));
        return table;
    }

    private static DataTable CreateCaseTimelineEntriesTable()
    {
        var table = new DataTable("case_timeline_entries");
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("tenant_id", typeof(Guid));
        table.Columns.Add("case_id", typeof(Guid));
        table.Columns.Add("entry_type", typeof(string));
        table.Columns.Add("actor_type", typeof(string));
        table.Columns.Add("summary", typeof(string));
        table.Columns.Add("created_at_utc", typeof(DateTimeOffset));
        return table;
    }

    private static DataTable CreateClaimsTable()
    {
        var table = new DataTable("claims");
        table.Columns.Add("id", typeof(Guid));
        table.Columns.Add("tenant_id", typeof(Guid));
        table.Columns.Add("case_id", typeof(Guid));
        table.Columns.Add("carrier_id", typeof(Guid));
        table.Columns.Add("claim_number", typeof(string));
        table.Columns.Add("claim_type", typeof(string));
        table.Columns.Add("status", typeof(string));
        table.Columns.Add("eligibility_status", typeof(string));
        table.Columns.Add("currency", typeof(string));
        table.Columns.Add("claimed_amount", typeof(decimal));
        table.Columns.Add("approved_amount", typeof(decimal));
        table.Columns.Add("recovered_amount", typeof(decimal));
        table.Columns.Add("written_off_amount", typeof(decimal));
        table.Columns.Add("created_at_utc", typeof(DateTimeOffset));
        table.Columns.Add("concurrency_stamp", typeof(string));
        return table;
    }
}

public sealed record GenerationReport(
    int TenantsGenerated,
    int ShipmentsGenerated,
    int LegsGenerated,
    int TrackingEventsGenerated,
    int ExceptionCasesGenerated,
    int ClaimsGenerated,
    int TimelineEntriesGenerated,
    long ElapsedMilliseconds,
    long EntitiesPerSecond);
