using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResolveOps.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "version",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "version",
                table: "refresh_token_sessions");

            migrationBuilder.DropColumn(
                name: "version",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "version",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "version",
                table: "carriers");

            migrationBuilder.DropColumn(
                name: "version",
                table: "business_calendars");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "tenants",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "tenants",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "tenants",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "tenants",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "tenant_settings",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "tenant_settings",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at_utc",
                table: "tenant_settings",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "tenant_settings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "tenant_settings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "refresh_token_sessions",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "locations",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "locations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "locations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "locations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "customers",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "customers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "carriers",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "carriers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "carriers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "carriers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "business_calendars",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AddColumn<string>(
                name: "concurrency_stamp",
                table: "business_calendars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "business_calendars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "updated_by",
                table: "business_calendars",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "error_templates",
                columns: table => new
                {
                    code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    message_template = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    http_status_code = table.Column<int>(type: "int", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_error_templates", x => x.code);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "error_templates");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "refresh_token_sessions");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "carriers");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "carriers");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "carriers");

            migrationBuilder.DropColumn(
                name: "concurrency_stamp",
                table: "business_calendars");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "business_calendars");

            migrationBuilder.DropColumn(
                name: "updated_by",
                table: "business_calendars");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "tenants",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "tenants",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "tenant_settings",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "tenant_settings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "refresh_token_sessions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "locations",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "locations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "customers",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "customers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "carriers",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "carriers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "updated_at_utc",
                table: "business_calendars",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "business_calendars",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
