using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RossoLoungeWeb.Migrations
{
    /// <summary>
    /// bool OnaylandiMi -> int Durum (0 Bekliyor, 1 Onaylandı, 2 İptal).
    ///
    /// DİKKAT: EF'in ürettiği hâli veri kaybediyordu — önce kolonu düşürüp
    /// Durum'u varsayılan 0 ile ekliyordu, yani ONAYLI bütün rezervasyonlar
    /// "Bekliyor"a düşerdi. Sıra elle düzeltildi: önce yeni kolon, sonra
    /// veriyi taşı, en son eski kolonu düşür. Down da aynı özeni gösteriyor,
    /// böylece geri alınabilir.
    /// </summary>
    public partial class RezervasyonDurumu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Durum",
                table: "Rezervasyons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Onaylı kayıtlar Durum = 1; kalanlar zaten varsayılan 0 (Bekliyor).
            migrationBuilder.Sql(
                "UPDATE Rezervasyons SET Durum = 1 WHERE OnaylandiMi = 1;");

            migrationBuilder.DropColumn(
                name: "OnaylandiMi",
                table: "Rezervasyons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OnaylandiMi",
                table: "Rezervasyons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Geri dönüşte iptaller onaysız sayılıyor: eski şemada iptal
            // diye bir kavram yok, en yakın karşılığı "onaylanmamış".
            migrationBuilder.Sql(
                "UPDATE Rezervasyons SET OnaylandiMi = 1 WHERE Durum = 1;");

            migrationBuilder.DropColumn(
                name: "Durum",
                table: "Rezervasyons");
        }
    }
}
