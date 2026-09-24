using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ResolveOps.Domain.Reporting;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Reporting.Services.Exports;

/// <summary>
/// Strategy interface for generating export file streams by export type (Open/Closed Principle).
/// </summary>
public interface IExportDataGenerator
{
    /// <summary>
    /// The export type this generator handles (matching ExportType constants, e.g. "ExceptionCases", "Claims", "CarrierScorecards").
    /// </summary>
    string ExportType { get; }

    /// <summary>
    /// Generates the formula-safe CSV content stream and returns the row count and stream.
    /// </summary>
    Task<(int RowCount, Stream Stream)> GenerateAsync(AppDbContext dbContext, ExportRequest request, CancellationToken ct);
}
