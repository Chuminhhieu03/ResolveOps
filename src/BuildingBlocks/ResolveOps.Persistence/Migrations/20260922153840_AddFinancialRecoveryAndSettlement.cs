using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialRecoveryAndSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "closed_by",
                table: "claims",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "closing_notes",
                table: "claims",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "write_off_reason",
                table: "claims",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "written_off_amount",
                table: "claims",
                type: "decimal(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "written_off_at_utc",
                table: "claims",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "written_off_by",
                table: "claims",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recovery_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    external_reference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    received_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recovery_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_recovery_transactions_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recovery_transactions_claim_id",
                table: "recovery_transactions",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "ix_recovery_transactions_tenant_claim",
                table: "recovery_transactions",
                columns: new[] { "tenant_id", "claim_id" });

            migrationBuilder.CreateIndex(
                name: "uix_recovery_transactions_tenant_ref",
                table: "recovery_transactions",
                columns: new[] { "tenant_id", "external_reference" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recovery_transactions");

            migrationBuilder.DropColumn(
                name: "closed_by",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "closing_notes",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "write_off_reason",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "written_off_amount",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "written_off_at_utc",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "written_off_by",
                table: "claims");
        }
    }
}
