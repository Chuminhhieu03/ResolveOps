using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingAndQuarantine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inbound_event_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    carrier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    source_system = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    external_event_id = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    idempotency_key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    payload_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    raw_payload_uri = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    raw_payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    signature_valid = table.Column<bool>(type: "bit", nullable: true),
                    processing_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    failure_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    failure_detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbound_event_receipts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quarantined_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    inbound_receipt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reason_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    detail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    assigned_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    resolved_shipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quarantined_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tracking_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    shipment_leg_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    carrier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    inbound_receipt_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    external_event_id = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    event_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    event_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    location_text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    package_count = table.Column<int>(type: "int", nullable: true),
                    correction_of_event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    causation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracking_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "UIX_InboundEventReceipts_TenantSourceExternalEvent",
                table: "inbound_event_receipts",
                columns: new[] { "tenant_id", "source_system", "external_event_id" },
                unique: true,
                filter: "[external_event_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantinedEvents_TenantStatusCreated",
                table: "quarantined_events",
                columns: new[] { "tenant_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_CarrierExternalEvent",
                table: "tracking_events",
                columns: new[] { "tenant_id", "carrier_id", "external_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_ShipmentTimeline",
                table: "tracking_events",
                columns: new[] { "tenant_id", "shipment_id", "occurred_at_utc", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbound_event_receipts");

            migrationBuilder.DropTable(
                name: "quarantined_events");

            migrationBuilder.DropTable(
                name: "tracking_events");
        }
    }
}
