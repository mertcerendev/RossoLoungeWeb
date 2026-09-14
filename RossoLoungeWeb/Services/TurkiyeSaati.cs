namespace RossoLoungeWeb.Services
{
    /// <summary>
    /// Türkiye yerel saatini (UTC+3) veren yardımcı.
    ///
    /// Neden gerekli: <c>DateTime.Now</c> sunucunun saat dilimini kullanır.
    /// Paylaşımlı hosting sunucuları çoğunlukla UTC çalışır; bu durumda
    /// rezervasyon saatleri, yorum tarihleri ve panel grafiği 3 saat kayar.
    /// Bu sınıf saati her ortamda açıkça Türkiye saatine çevirir.
    /// </summary>
    public static class TurkiyeSaati
    {
        private static readonly TimeZoneInfo SaatDilimi = SaatDiliminiBul();

        /// <summary>Türkiye saatiyle şu an.</summary>
        public static DateTime Simdi => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SaatDilimi);

        /// <summary>Türkiye saatiyle bugünün tarihi (saat 00:00).</summary>
        public static DateTime Bugun => Simdi.Date;

        /// <summary>Verilen UTC tarihini Türkiye saatine çevirir.</summary>
        public static DateTime UtcdenCevir(DateTime utcTarih) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTarih, DateTimeKind.Utc), SaatDilimi);

        // Windows ve Linux aynı saat dilimi için farklı isim kullanır:
        // Windows -> "Turkey Standard Time", Linux/macOS (IANA) -> "Europe/Istanbul".
        // .NET 8 çoğu platformda ikisini de çözebilir ama garanti değil, bu yüzden
        // sırayla denenip son çare olarak sabit UTC+3 kullanılıyor.
        // (Türkiye 2016'dan beri yaz saati uygulamıyor, kalıcı UTC+3.)
        private static TimeZoneInfo SaatDiliminiBul()
        {
            foreach (var id in new[] { "Turkey Standard Time", "Europe/Istanbul" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            return TimeZoneInfo.CreateCustomTimeZone(
                "Turkiye-UTC+3",
                TimeSpan.FromHours(3),
                "Türkiye Saati",
                "Türkiye Saati");
        }
    }
}
