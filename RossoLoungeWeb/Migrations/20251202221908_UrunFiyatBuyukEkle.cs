using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RossoLoungeWeb.Migrations
{
    /// <inheritdoc />
    public partial class UrunFiyatBuyukEkle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FiyatBuyuk",
                table: "Urunler",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiyatBuyuk",
                table: "Urunler");
        }
    }
}
