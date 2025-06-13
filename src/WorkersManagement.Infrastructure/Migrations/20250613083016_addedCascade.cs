using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkersManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HabitCompletions_Workers_WorkerId",
                table: "HabitCompletions");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkerId1",
                table: "HabitCompletions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HabitCompletions_WorkerId1",
                table: "HabitCompletions",
                column: "WorkerId1");

            migrationBuilder.AddForeignKey(
                name: "FK_HabitCompletions_Workers_WorkerId",
                table: "HabitCompletions",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HabitCompletions_Workers_WorkerId1",
                table: "HabitCompletions",
                column: "WorkerId1",
                principalTable: "Workers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HabitCompletions_Workers_WorkerId",
                table: "HabitCompletions");

            migrationBuilder.DropForeignKey(
                name: "FK_HabitCompletions_Workers_WorkerId1",
                table: "HabitCompletions");

            migrationBuilder.DropIndex(
                name: "IX_HabitCompletions_WorkerId1",
                table: "HabitCompletions");

            migrationBuilder.DropColumn(
                name: "WorkerId1",
                table: "HabitCompletions");

            migrationBuilder.AddForeignKey(
                name: "FK_HabitCompletions_Workers_WorkerId",
                table: "HabitCompletions",
                column: "WorkerId",
                principalTable: "Workers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
