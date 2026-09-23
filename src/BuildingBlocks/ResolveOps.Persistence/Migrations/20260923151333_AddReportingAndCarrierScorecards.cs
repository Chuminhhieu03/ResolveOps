using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingAndCarrierScorecards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carrier_performance_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    carrier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    carrier_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    period_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_shipments = table.Column<int>(type: "int", nullable: false),
                    on_time_shipments = table.Column<int>(type: "int", nullable: false),
                    delayed_shipments = table.Column<int>(type: "int", nullable: false),
                    exception_count = table.Column<int>(type: "int", nullable: false),
                    critical_severity_count = table.Column<int>(type: "int", nullable: false),
                    high_severity_count = table.Column<int>(type: "int", nullable: false),
                    medium_severity_count = table.Column<int>(type: "int", nullable: false),
                    low_severity_count = table.Column<int>(type: "int", nullable: false),
                    total_claims = table.Column<int>(type: "int", nullable: false),
                    approved_claims = table.Column<int>(type: "int", nullable: false),
                    rejected_claims = table.Column<int>(type: "int", nullable: false),
                    total_claimed_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    total_approved_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    total_recovered_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    avg_response_time_hours = table.Column<double>(type: "float", nullable: false),
                    last_calculated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carrier_performance_snapshots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "export_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    export_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    filter_criteria_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    container = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    blob_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    row_count = table.Column<int>(type: "int", nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_carrier_performance_snapshots_tenant_carrier_period",
                table: "carrier_performance_snapshots",
                columns: new[] { "tenant_id", "carrier_id", "period_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_carrier_performance_snapshots_tenant_period",
                table: "carrier_performance_snapshots",
                columns: new[] { "tenant_id", "period_date" });

            migrationBuilder.CreateIndex(
                name: "ix_export_requests_status_created",
                table: "export_requests",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_export_requests_tenant_user_status_created",
                table: "export_requests",
                columns: new[] { "tenant_id", "user_id", "status", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "carrier_performance_snapshots");

            migrationBuilder.DropTable(
                name: "export_requests");
        }
    }
}
