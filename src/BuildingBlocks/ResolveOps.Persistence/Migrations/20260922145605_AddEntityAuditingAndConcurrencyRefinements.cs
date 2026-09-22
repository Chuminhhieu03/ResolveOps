using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityAuditingAndConcurrencyRefinements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "claim_approvals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "claim_approvals",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "claim_approvals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "claim_approvals",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "claim_approvals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "carrier_claim_responses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "carrier_claim_responses",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "carrier_claim_responses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "claim_approvals");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "claim_approvals");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "claim_approvals");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "claim_approvals");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "claim_approvals");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "carrier_claim_responses");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                table: "carrier_claim_responses");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "carrier_claim_responses");
        }
    }
}
