using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowTasksAndSlaClocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sla_clocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    case_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    clock_type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    due_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    paused_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    total_paused_seconds = table.Column<long>(type: "bigint", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    breached_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_clocks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sla_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    policy_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sla_policy_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sla_policy_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    calendar_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    acknowledgement_minutes = table.Column<int>(type: "int", nullable: true),
                    first_action_minutes = table.Column<int>(type: "int", nullable: true),
                    resolution_minutes = table.Column<int>(type: "int", nullable: true),
                    claim_submission_minutes = table.Column<int>(type: "int", nullable: true),
                    pause_reason_codes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    effective_from_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    effective_to_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_policy_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    case_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    task_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    owner_team_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    due_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    blocked_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    completion_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    is_mandatory = table.Column<bool>(type: "bit", nullable: false),
                    waived_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    waived_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    waived_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_by_policy_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_tasks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sla_clock_pauses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sla_clock_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reason_code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ended_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    started_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ended_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_clock_pauses", x => x.id);
                    table.ForeignKey(
                        name: "FK_sla_clock_pauses_sla_clocks_sla_clock_id",
                        column: x => x.sla_clock_id,
                        principalTable: "sla_clocks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sla_clock_pauses_sla_clock_id",
                table: "sla_clock_pauses",
                column: "sla_clock_id");

            migrationBuilder.CreateIndex(
                name: "ix_sla_clock_pauses_tenant_clock",
                table: "sla_clock_pauses",
                columns: new[] { "tenant_id", "sla_clock_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sla_clocks_tenant_case_clock",
                table: "sla_clocks",
                columns: new[] { "tenant_id", "case_id", "clock_type" });

            migrationBuilder.CreateIndex(
                name: "ix_sla_clocks_tenant_status_due",
                table: "sla_clocks",
                columns: new[] { "tenant_id", "status", "due_at_utc" });

            migrationBuilder.CreateIndex(
                name: "uix_sla_policies_tenant_key",
                table: "sla_policies",
                columns: new[] { "tenant_id", "policy_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uix_sla_policy_versions_tenant_policy_ver",
                table: "sla_policy_versions",
                columns: new[] { "tenant_id", "sla_policy_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_tasks_tenant_case_status",
                table: "workflow_tasks",
                columns: new[] { "tenant_id", "case_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_tasks_tenant_owner_status",
                table: "workflow_tasks",
                columns: new[] { "tenant_id", "owner_user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_tasks_tenant_status_due",
                table: "workflow_tasks",
                columns: new[] { "tenant_id", "status", "due_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sla_clock_pauses");

            migrationBuilder.DropTable(
                name: "sla_policies");

            migrationBuilder.DropTable(
                name: "sla_policy_versions");

            migrationBuilder.DropTable(
                name: "workflow_tasks");

            migrationBuilder.DropTable(
                name: "sla_clocks");
        }
    }
}
