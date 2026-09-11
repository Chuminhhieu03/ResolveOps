using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentsAndMessaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    scope = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    request_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    response_status = table.Column<int>(type: "int", nullable: false),
                    response_body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => new { x.tenant_id, x.scope, x.idempotency_key });
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                columns: table => new
                {
                    consumer_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    message_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    received_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    result_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.consumer_name, x.message_id });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_type = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    event_version = table.Column<int>(type: "int", nullable: false),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    causation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    partition_key = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    processing_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    processing_attempts = table.Column<int>(type: "int", nullable: false),
                    next_attempt_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    last_error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    external_reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    source_system = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    origin_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    destination_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    service_level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    planned_pickup_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    planned_delivery_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    actual_pickup_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    actual_delivery_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    declared_value = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    declared_value_currency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    expected_package_count = table.Column<int>(type: "int", nullable: true),
                    expected_weight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    weight_unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipment_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    line_reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    sku = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    expected_quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    quantity_unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    unit_value = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    currency = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_shipment_items_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shipment_legs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sequence_number = table.Column<int>(type: "int", nullable: false),
                    carrier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tracking_number = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    origin_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    destination_location_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    planned_departure_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    planned_arrival_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    actual_departure_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    actual_arrival_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_legs", x => x.id);
                    table.ForeignKey(
                        name: "FK_shipment_legs_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shipment_tracking_aliases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    shipment_leg_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    carrier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    alias_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    alias_value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_tracking_aliases", x => x.id);
                    table.ForeignKey(
                        name: "FK_shipment_tracking_aliases_shipments_shipment_id",
                        column: x => x.shipment_id,
                        principalTable: "shipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_StatusNextAttempt",
                table: "outbox_messages",
                columns: new[] { "processing_status", "next_attempt_at_utc", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_shipment_items_shipment_id",
                table: "shipment_items",
                column: "shipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_legs_shipment_id",
                table: "shipment_legs",
                column: "shipment_id");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_tracking_aliases_shipment_id",
                table: "shipment_tracking_aliases",
                column: "shipment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records");

            migrationBuilder.DropTable(
                name: "inbox_messages");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "shipment_items");

            migrationBuilder.DropTable(
                name: "shipment_legs");

            migrationBuilder.DropTable(
                name: "shipment_tracking_aliases");

            migrationBuilder.DropTable(
                name: "shipments");
        }
    }
}
