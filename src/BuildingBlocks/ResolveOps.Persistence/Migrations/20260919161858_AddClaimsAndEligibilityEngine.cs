using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimsAndEligibilityEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    case_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    carrier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    eligibility_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    eligibility_reason_codes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    claim_deadline_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    claimed_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    approved_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    recovered_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    external_submission_reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    approved_for_submission_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    approved_for_submission_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claims", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "claim_loss_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    component_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    unit_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    source_document_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_loss_components", x => x.id);
                    table.ForeignKey(
                        name: "FK_claim_loss_components_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "money",
                columns: table => new
                {
                    claim_loss_component_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_money", x => x.claim_loss_component_id);
                    table.ForeignKey(
                        name: "FK_money_claim_loss_components_claim_loss_component_id",
                        column: x => x.claim_loss_component_id,
                        principalTable: "claim_loss_components",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_claim_loss_components_claim_id",
                table: "claim_loss_components",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "uix_claims_active_case_carrier",
                table: "claims",
                columns: new[] { "tenant_id", "case_id", "carrier_id" },
                unique: true,
                filter: "status NOT IN ('Cancelled', 'Closed')");

            migrationBuilder.CreateIndex(
                name: "uix_claims_claim_number",
                table: "claims",
                columns: new[] { "tenant_id", "claim_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "money");

            migrationBuilder.DropTable(
                name: "claim_loss_components");

            migrationBuilder.DropTable(
                name: "claims");
        }
    }
}
