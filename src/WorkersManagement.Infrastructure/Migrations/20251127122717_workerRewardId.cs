using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkersManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class workerRewardId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Rewards",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Rewards");
        }
    }
}
