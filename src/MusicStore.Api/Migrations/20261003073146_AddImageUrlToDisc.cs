using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicStore.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToDisc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Stock",
                table: "Discs",
                newName: "MusicianId");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Discs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Discs_MusicianId",
                table: "Discs",
                column: "MusicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_Discs_Musicians_MusicianId",
                table: "Discs",
                column: "MusicianId",
                principalTable: "Musicians",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Discs_Musicians_MusicianId",
                table: "Discs");

            migrationBuilder.DropIndex(
                name: "IX_Discs_MusicianId",
                table: "Discs");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Discs");

            migrationBuilder.RenameColumn(
                name: "MusicianId",
                table: "Discs",
                newName: "Stock");
        }
    }
}
