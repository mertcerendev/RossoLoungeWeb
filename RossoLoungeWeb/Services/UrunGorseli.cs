namespace RossoLoungeWeb.Services
{
    /// <summary>
    /// Ürün görseli yolunu güvenle çözer.
    ///
    /// NEDEN VAR: <c>wwwroot/img/urunler/</c> klasöründe görsel sanılan
    /// <c>.url</c> kısayol dosyaları duruyor ve bunlara işaret eden eski ürün
    /// kayıtları sitede bozuk resim ikonu gösteriyordu. Yükleme tarafı artık
    /// hem uzantı hem sihirli bayt kontrolü yapıyor (UrunController), ama
    /// ESKİDEN kaydedilmiş satırlar veritabanında duruyor — canlıda da.
    ///
    /// Aynı üçlü koşul (boş / dış adres / geçersiz uzantı) dört ayrı görünümde
    /// elle tekrarlanıyordu; biri güncellenip diğeri unutulmasın diye tek yere
    /// alındı.
    /// </summary>
    public static class UrunGorseli
    {
        private static readonly string[] Uzantilar = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        /// <summary>Bu yol ekranda gösterilebilir bir görsele işaret ediyor mu?</summary>
        public static bool Gecerli(string? resimUrl)
        {
            if (string.IsNullOrWhiteSpace(resimUrl)) return false;

            // Dış adresler (placehold.co gibi) tasarım diline uymuyor; yer tutucu yeğleniyor.
            if (resimUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return false;

            var uzanti = Path.GetExtension(resimUrl);
            if (string.IsNullOrEmpty(uzanti)) return false;

            return Uzantilar.Contains(uzanti, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Gösterilecek yolu döner; geçersizse <paramref name="yedek"/>.</summary>
        public static string Yolu(string? resimUrl, string yedek)
        {
            return Gecerli(resimUrl) ? resimUrl! : yedek;
        }
    }
}
