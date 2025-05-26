using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkersManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addBarcodeEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QRCodes_Users_UserId",
                table: "QRCodes");

            migrationBuilder.DropColumn(
                name: "QRCodeData",
                table: "QRCodes");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "QRCodes",
                newName: "WorkerId");

            migrationBuilder.RenameColumn(
                name: "IsDisabled",
                table: "QRCodes",
                newName: "IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_QRCodes_UserId",
                table: "QRCodes",
                newName: "IX_QRCodes_WorkerId");

            migrationBuilder.AddColumn<byte[]>(
                name: "QRCodeImage",
                table: "QRCodes",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_QRCodes_Users_WorkerId",
                table: "QRCodes",
                column: "WorkerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QRCodes_Users_WorkerId",
                table: "QRCodes");

            migrationBuilder.DropColumn(
                name: "QRCodeImage",
                table: "QRCodes");

            migrationBuilder.RenameColumn(
                name: "WorkerId",
                table: "QRCodes",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "QRCodes",
                newName: "IsDisabled");

            migrationBuilder.RenameIndex(
                name: "IX_QRCodes_WorkerId",
                table: "QRCodes",
                newName: "IX_QRCodes_UserId");

            migrationBuilder.AddColumn<string>(
                name: "QRCodeData",
                table: "QRCodes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_QRCodes_Users_UserId",
                table: "QRCodes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
