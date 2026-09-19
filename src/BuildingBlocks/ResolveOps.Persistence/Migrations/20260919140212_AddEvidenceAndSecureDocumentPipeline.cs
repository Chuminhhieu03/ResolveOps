using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceAndSecureDocumentPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evidence_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    case_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    claim_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    evidence_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    original_file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    storage_object_name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    storage_container = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    document_date = table.Column<DateOnly>(type: "date", nullable: true),
                    issuer = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    supersedes_document_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    uploaded_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    scan_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    scan_completed_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    retention_until = table.Column<DateOnly>(type: "date", nullable: true),
                    legal_hold = table.Column<bool>(type: "bit", nullable: false),
                    concurrency_stamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_documents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "evidence_requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    policy_version_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    exception_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    claim_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    evidence_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_mandatory = table.Column<bool>(type: "bit", nullable: false),
                    condition_json = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_requirements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_documents_tenant_case_type_status",
                table: "evidence_documents",
                columns: new[] { "tenant_id", "case_id", "evidence_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_documents_tenant_claim_status",
                table: "evidence_documents",
                columns: new[] { "tenant_id", "claim_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_documents_tenant_sha256",
                table: "evidence_documents",
                columns: new[] { "tenant_id", "sha256" });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_documents_tenant_status_uploaded",
                table: "evidence_documents",
                columns: new[] { "tenant_id", "status", "uploaded_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_requirements_tenant_exception_evidence",
                table: "evidence_requirements",
                columns: new[] { "tenant_id", "exception_type", "evidence_type" });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_requirements_tenant_policy_version",
                table: "evidence_requirements",
                columns: new[] { "tenant_id", "policy_version_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidence_documents");

            migrationBuilder.DropTable(
                name: "evidence_requirements");
        }
    }
}
