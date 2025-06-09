using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkersManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedNewColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_SubTeams_SubteamId",
                table: "Departments");

            migrationBuilder.RenameColumn(
                name: "SubteamId",
                table: "Departments",
                newName: "SubTeamId");

            migrationBuilder.RenameIndex(
                name: "IX_Departments_SubteamId",
                table: "Departments",
                newName: "IX_Departments_SubTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_SubTeams_SubTeamId",
                table: "Departments",
                column: "SubTeamId",
                principalTable: "SubTeams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_SubTeams_SubTeamId",
                table: "Departments");

            migrationBuilder.RenameColumn(
                name: "SubTeamId",
                table: "Departments",
                newName: "SubteamId");

            migrationBuilder.RenameIndex(
                name: "IX_Departments_SubTeamId",
                table: "Departments",
                newName: "IX_Departments_SubteamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_SubTeams_SubteamId",
                table: "Departments",
                column: "SubteamId",
                principalTable: "SubTeams",
                principalColumn: "Id");
        }
    }
}
