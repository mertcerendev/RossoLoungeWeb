using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RossoLoungeWeb.Migrations
{
    /// <inheritdoc />
    public partial class IndeksVeKategoriSilmeKisitlamasi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Urunler_Kategoriler_KategoriId",
                table: "Urunler");

            migrationBuilder.CreateIndex(
                name: "IX_Yorumlar_OnaylandiMi",
                table: "Yorumlar",
                column: "OnaylandiMi");

            migrationBuilder.CreateIndex(
                name: "IX_Urunler_SiraNo",
                table: "Urunler",
                column: "SiraNo");

            migrationBuilder.CreateIndex(
                name: "IX_Rezervasyons_Tarih",
                table: "Rezervasyons",
                column: "Tarih");

            migrationBuilder.CreateIndex(
                name: "IX_IletisimMesajlari_OkunduMu",
                table: "IletisimMesajlari",
                column: "OkunduMu");

            migrationBuilder.AddForeignKey(
                name: "FK_Urunler_Kategoriler_KategoriId",
                table: "Urunler",
                column: "KategoriId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Urunler_Kategoriler_KategoriId",
                table: "Urunler");

            migrationBuilder.DropIndex(
                name: "IX_Yorumlar_OnaylandiMi",
                table: "Yorumlar");

            migrationBuilder.DropIndex(
                name: "IX_Urunler_SiraNo",
                table: "Urunler");

            migrationBuilder.DropIndex(
                name: "IX_Rezervasyons_Tarih",
                table: "Rezervasyons");

            migrationBuilder.DropIndex(
                name: "IX_IletisimMesajlari_OkunduMu",
                table: "IletisimMesajlari");

            migrationBuilder.AddForeignKey(
                name: "FK_Urunler_Kategoriler_KategoriId",
                table: "Urunler",
                column: "KategoriId",
                principalTable: "Kategoriler",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
