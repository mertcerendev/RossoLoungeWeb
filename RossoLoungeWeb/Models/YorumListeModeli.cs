namespace RossoLoungeWeb.Models
{
    /// <summary>
    /// Yorum listesi ekranının verisi.
    ///
    /// NEDEN SAYFALAMA VAR: liste bütün yorumları tek sayfada basıyordu ve
    /// arama tarayıcıda satır gizleyerek yapılıyordu — kayıtlar yine
    /// indiriliyor, "arama" yalnızca ekrandakini süzüyordu. Ürünler ve
    /// Rezervasyonlar'da çözülen sorunun aynısı.
    /// </summary>
    public class YorumListeModeli
    {
        public List<Yorum> Kayitlar { get; set; } = new();
        public YorumSuzgeci Suzgec { get; set; } = new();

        /// <summary>Süzgece uyan toplam kayıt (sayfadaki değil).</summary>
        public int ToplamKayit { get; set; }

        /// <summary>
        /// Yayındaki yorumların puan ortalaması — ziyaretçinin sitede
        /// gördüğü rakamla aynı hesap (HomeController onaylı yorumları
        /// ortalıyor). Bekleyenler dahil edilseydi panelde başka, sitede
        /// başka bir sayı görünürdü.
        /// </summary>
        public double? YayindakiOrtalama { get; set; }

        public int ToplamSayfa { get; set; }
        public bool OncekiVar => Suzgec.Sayfa > 1;
        public bool SonrakiVar => Suzgec.Sayfa < ToplamSayfa;

        // Sekme rozetleri: sekmeye geçmeden kaç kayıt olduğu görünsün.
        public int SayiBekleyen { get; set; }
        public int SayiYayinda { get; set; }
        public int SayiTumu { get; set; }

        public int SayiGetir(string durum) => durum switch
        {
            "bekleyen" => SayiBekleyen,
            "yayinda" => SayiYayinda,
            _ => SayiTumu
        };
    }

    /// <summary>
    /// Listedeki süzgeç durumu. Adres çubuğunda taşınıyor: yönetici
    /// süzülmüş bir listeyi yer imine ekleyebilsin ve onay/silme
    /// sonrasında aynı görünüme dönebilsin.
    /// </summary>
    public class YorumSuzgeci
    {
        /// <summary>tumu | bekleyen | yayinda</summary>
        public string Durum { get; set; } = "tumu";

        public string? Ara { get; set; }

        /// <summary>1–5; null ise puan süzgeci kapalı.</summary>
        public int? Puan { get; set; }

        /// <summary>bekleyen_once | yeni | eski | puan_azalan | puan_artan</summary>
        public string Sirala { get; set; } = "bekleyen_once";

        public int Sayfa { get; set; } = 1;
        public int Boyut { get; set; } = 25;

        /// <summary>Süzgeci adres sorgusuna çevirir (sayfa dışarıdan verilebilir).</summary>
        public string SorguKur(int? sayfa = null, string? sirala = null, string? durum = null)
        {
            var parcalar = new List<string>
            {
                "durum=" + Uri.EscapeDataString(durum ?? Durum)
            };

            if (!string.IsNullOrWhiteSpace(Ara)) parcalar.Add("ara=" + Uri.EscapeDataString(Ara));
            if (Puan.HasValue) parcalar.Add("puan=" + Puan.Value);

            parcalar.Add("sirala=" + Uri.EscapeDataString(sirala ?? Sirala));
            parcalar.Add("boyut=" + Boyut);

            var s = sayfa ?? Sayfa;
            if (s > 1) parcalar.Add("sayfa=" + s);

            return "?" + string.Join("&", parcalar);
        }

        public bool SuzgecAktif => !string.IsNullOrWhiteSpace(Ara) || Puan.HasValue;
    }
}
