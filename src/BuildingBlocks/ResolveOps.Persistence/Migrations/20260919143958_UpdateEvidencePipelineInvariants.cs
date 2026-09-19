using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEvidencePipelineInvariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_evidence_documents_tenant_sha256",
                table: "evidence_documents");

            migrationBuilder.CreateIndex(
                name: "uix_evidence_documents_tenant_sha256_available",
                table: "evidence_documents",
                columns: new[] { "tenant_id", "sha256" },
                unique: true,
                filter: "[status] = 'Available'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uix_evidence_documents_tenant_sha256_available",
                table: "evidence_documents");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_documents_tenant_sha256",
                table: "evidence_documents",
                columns: new[] { "tenant_id", "sha256" });
        }
    }
}
