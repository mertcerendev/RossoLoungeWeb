using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RossoLoungeWeb.Migrations
{
    /// <inheritdoc />
    public partial class UrunOneCikan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OneCikan",
                table: "Urunler",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OneCikan",
                table: "Urunler");
        }
    }
}
