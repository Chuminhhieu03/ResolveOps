using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace ResolveOps.PerformanceTests.DataGeneration;

/// <summary>
/// High-throughput database seeder using SqlBulkCopy (Master Spec §20.7, §24 Phase 16)
/// to stream synthetic entities directly into SQL Server without EF Core change tracking overhead
/// or transaction timeouts.
/// </summary>
public static class SqlBulkDataWriter
{
    public static async Task<long> BulkInsertAsync(
        SqlConnection connection,
        string tableName,
        DataTable dataTable,
        int batchSize = 10_000,
        int timeoutSeconds = 300,
        SqlTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(dataTable);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        using var bulkCopy = new SqlBulkCopy(
            connection,
            SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.CheckConstraints,
            transaction)
        {
            DestinationTableName = tableName,
            BatchSize = batchSize,
            BulkCopyTimeout = timeoutSeconds,
            EnableStreaming = true
        };

        foreach (DataColumn column in dataTable.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
        return dataTable.Rows.Count;
    }
}
