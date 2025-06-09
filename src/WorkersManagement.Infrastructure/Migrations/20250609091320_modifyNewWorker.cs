using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkersManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class modifyNewWorker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsecutiveSundayCount",
                table: "Workers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveSundayCount",
                table: "Workers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
