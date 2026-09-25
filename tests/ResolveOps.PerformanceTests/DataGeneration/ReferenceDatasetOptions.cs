namespace ResolveOps.PerformanceTests.DataGeneration;

/// <summary>
/// Configuration options for the synthetic reference dataset generator (Master Spec §20.7, §24 Phase 16).
/// Defines full reference scale (100 tenants, 1M shipments, 3M legs, 20M tracking events, 100K cases, 25K claims)
/// as well as fast benchmark profiles.
/// </summary>
public sealed class ReferenceDatasetOptions
{
    public int TenantCount { get; init; } = 100;
    public int ShipmentsTotal { get; init; } = 1_000_000;
    public int LegsTotal { get; init; } = 3_000_000;
    public int TrackingEventsTotal { get; init; } = 20_000_000;
    public int ExceptionCasesTotal { get; init; } = 100_000;
    public int ClaimsTotal { get; init; } = 25_000;
    public int TimelineEntriesTotal { get; init; } = 1_000_000;
    public int DocumentRecordsTotal { get; init; } = 500_000;
    public int RandomSeed { get; init; } = 42;
    public int BatchSize { get; init; } = 10_000;

    /// <summary>
    /// Creates the standard Reference Workload specified in Master Spec §20.7.
    /// </summary>
    public static ReferenceDatasetOptions FullReferenceWorkload(int seed = 42) => new()
    {
        TenantCount = 100,
        ShipmentsTotal = 1_000_000,
        LegsTotal = 3_000_000,
        TrackingEventsTotal = 20_000_000,
        ExceptionCasesTotal = 100_000,
        ClaimsTotal = 25_000,
        TimelineEntriesTotal = 1_000_000,
        DocumentRecordsTotal = 500_000,
        RandomSeed = seed,
        BatchSize = 10_000
    };

    /// <summary>
    /// Creates a calibrated benchmark dataset optimized for continuous integration and local performance validation.
    /// Preserves exact statistical distribution (100 tenants, 3 legs/shipment, 20 events/shipment, 10% exceptions, 25% claims).
    /// </summary>
    public static ReferenceDatasetOptions BenchmarkWorkload(int scaleDivisor = 100, int seed = 42) => new()
    {
        TenantCount = 100,
        ShipmentsTotal = Math.Max(100, 1_000_000 / scaleDivisor),
        LegsTotal = Math.Max(300, 3_000_000 / scaleDivisor),
        TrackingEventsTotal = Math.Max(2_000, 20_000_000 / scaleDivisor),
        ExceptionCasesTotal = Math.Max(100, 100_000 / scaleDivisor),
        ClaimsTotal = Math.Max(25, 25_000 / scaleDivisor),
        TimelineEntriesTotal = Math.Max(1_000, 1_000_000 / scaleDivisor),
        DocumentRecordsTotal = Math.Max(500, 500_000 / scaleDivisor),
        RandomSeed = seed,
        BatchSize = 5_000
    };
}
