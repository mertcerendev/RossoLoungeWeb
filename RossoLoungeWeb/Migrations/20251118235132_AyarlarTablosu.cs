using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RossoLoungeWeb.Migrations
{
    /// <inheritdoc />
    public partial class AyarlarTablosu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ayarlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GonderenMail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GonderenSifre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SmtpSunucu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ayarlar", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ayarlar");
        }
    }
}
