using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimApprovalSubmissionAndResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "claim_loss_components");

            migrationBuilder.CreateTable(
                name: "carrier_claim_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    response_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    carrier_reference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    response_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    approved_amount = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: true),
                    reason_codes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    recorded_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    source_channel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carrier_claim_responses", x => x.id);
                    table.ForeignKey(
                        name: "FK_carrier_claim_responses_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "claim_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approval_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    requested_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requested_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    decided_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    decided_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    decision_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    claim_version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_approvals", x => x.id);
                    table.ForeignKey(
                        name: "FK_claim_approvals_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uix_claims_carrier_submission_ref",
                table: "claims",
                columns: new[] { "tenant_id", "carrier_id", "external_submission_reference" },
                unique: true,
                filter: "external_submission_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_carrier_claim_responses_claim_id",
                table: "carrier_claim_responses",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "ix_carrier_claim_responses_tenant_claim",
                table: "carrier_claim_responses",
                columns: new[] { "tenant_id", "claim_id" });

            migrationBuilder.CreateIndex(
                name: "IX_claim_approvals_claim_id",
                table: "claim_approvals",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "ix_claim_approvals_tenant_claim",
                table: "claim_approvals",
                columns: new[] { "tenant_id", "claim_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "carrier_claim_responses");

            migrationBuilder.DropTable(
                name: "claim_approvals");

            migrationBuilder.DropIndex(
                name: "uix_claims_carrier_submission_ref",
                table: "claims");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "claim_loss_components",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
