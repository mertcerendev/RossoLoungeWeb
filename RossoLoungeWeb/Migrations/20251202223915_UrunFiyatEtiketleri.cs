using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RossoLoungeWeb.Migrations
{
    /// <inheritdoc />
    public partial class UrunFiyatEtiketleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiyatBuyukTur",
                table: "Urunler",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiyatTur",
                table: "Urunler",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiyatBuyukTur",
                table: "Urunler");

            migrationBuilder.DropColumn(
                name: "FiyatTur",
                table: "Urunler");
        }
    }
}
