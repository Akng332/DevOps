using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicStore.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantityAndSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SalePrice",
                table: "Sales",
                newName: "TotalAmount");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Discs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Discs");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "Sales",
                newName: "SalePrice");
        }
    }
}
