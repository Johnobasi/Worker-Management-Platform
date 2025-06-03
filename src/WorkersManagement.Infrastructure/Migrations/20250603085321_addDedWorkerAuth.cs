using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkersManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addDedWorkerAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLogin",
                table: "Workers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Workers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Workers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiration",
                table: "Workers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLogin",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Workers");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiration",
                table: "Workers");
        }
    }
}
