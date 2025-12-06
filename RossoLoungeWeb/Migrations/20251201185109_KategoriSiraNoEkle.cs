using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RossoLoungeWeb.Migrations
{
    /// <inheritdoc />
    public partial class KategoriSiraNoEkle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SiraNo",
                table: "Kategoriler",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SiraNo",
                table: "Kategoriler");
        }
    }
}
