using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExceptionPoliciesAndCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exception_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    case_number = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    shipment_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    shipment_leg_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    exception_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    fingerprint = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    severity_score = table.Column<int>(type: "int", nullable: true),
                    policy_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    policy_version_number = table.Column<int>(type: "int", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    owner_team_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    financial_exposure = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    exposure_currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    root_cause_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    disposition_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    detected_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exception_cases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "exception_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    policy_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    exception_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    effective_from_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    effective_to_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    rule_definition_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    severity_definition_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    assignment_definition_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    evidence_policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    sla_policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exception_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "case_timeline_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    case_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    entry_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    actor_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    actor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    details_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    correlation_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_case_timeline_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_case_timeline_entries_exception_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "exception_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exception_occurrences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    case_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tracking_event_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    occurrence_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    observed_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exception_occurrences", x => x.id);
                    table.ForeignKey(
                        name: "FK_exception_occurrences_exception_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "exception_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_case_timeline_entries_case_id",
                table: "case_timeline_entries",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTimelineEntries_TenantCaseCreatedAt",
                table: "case_timeline_entries",
                columns: new[] { "tenant_id", "case_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCases_TenantShipment",
                table: "exception_cases",
                columns: new[] { "tenant_id", "shipment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionCases_TenantStatusSeverity",
                table: "exception_cases",
                columns: new[] { "tenant_id", "status", "severity" });

            migrationBuilder.CreateIndex(
                name: "UIX_ExceptionCases_ActiveFingerprint",
                table: "exception_cases",
                columns: new[] { "tenant_id", "fingerprint" },
                unique: true,
                filter: "[status] NOT IN ('Closed', 'Cancelled')");

            migrationBuilder.CreateIndex(
                name: "UIX_ExceptionCases_TenantCaseNumber",
                table: "exception_cases",
                columns: new[] { "tenant_id", "case_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_exception_occurrences_case_id",
                table: "exception_occurrences",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionOccurrences_TenantCase",
                table: "exception_occurrences",
                columns: new[] { "tenant_id", "case_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionPolicies_TenantTypeStatus",
                table: "exception_policies",
                columns: new[] { "tenant_id", "exception_type", "status" });

            migrationBuilder.CreateIndex(
                name: "UIX_ExceptionPolicies_TenantKeyVersion",
                table: "exception_policies",
                columns: new[] { "tenant_id", "policy_key", "version_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "case_timeline_entries");

            migrationBuilder.DropTable(
                name: "exception_occurrences");

            migrationBuilder.DropTable(
                name: "exception_policies");

            migrationBuilder.DropTable(
                name: "exception_cases");
        }
    }
}
