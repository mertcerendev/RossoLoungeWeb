namespace RossoLoungeWeb.Models
{
    /// <summary>
    /// Rezervasyon listesi ekranının verisi.
    ///
    /// NEDEN SAYFALAMA VAR: liste tek sayfada bütün kayıtları basıyordu.
    /// Birkaç yüz rezervasyondan sonra sayfa hem yavaşlıyor hem de
    /// taranamaz hâle geliyor. Süzme ve sayfalama SUNUCU tarafında —
    /// tarayıcıda gizlemek sorunu çözmez, kayıtlar yine indirilirdi.
    /// </summary>
    public class RezervasyonListeModeli
    {
        public List<Rezervasyon> Kayitlar { get; set; } = new();
        public RezervasyonSuzgeci Suzgec { get; set; } = new();

        /// <summary>Süzgece uyan toplam kayıt (sayfadaki değil).</summary>
        public int ToplamKayit { get; set; }
        public int ToplamKisi { get; set; }

        public int ToplamSayfa { get; set; }
        public bool OncekiVar => Suzgec.Sayfa > 1;
        public bool SonrakiVar => Suzgec.Sayfa < ToplamSayfa;

        /// <summary>Sekme rozetleri: kullanıcı sekmeye geçmeden kaç kayıt olduğunu görsün.</summary>
        public int SayiYaklasan { get; set; }
        public int SayiBugun { get; set; }
        public int SayiBekleyen { get; set; }
        public int SayiGecmis { get; set; }
        public int SayiTumu { get; set; }

        public int SayiGetir(string durum) => durum switch
        {
            "bugun" => SayiBugun,
            "bekleyen" => SayiBekleyen,
            "gecmis" => SayiGecmis,
            "tumu" => SayiTumu,
            _ => SayiYaklasan
        };
    }

    /// <summary>
    /// Listedeki süzgeç durumu. Adres çubuğunda taşınıyor: yönetici
    /// süzülmüş bir listeyi yer imine ekleyebilsin ve onay/silme
    /// sonrasında aynı görünüme dönebilsin.
    /// </summary>
    public class RezervasyonSuzgeci
    {
        /// <summary>yaklasan | bugun | bekleyen | gecmis | tumu</summary>
        public string Durum { get; set; } = "yaklasan";

        public string? Ara { get; set; }
        public DateTime? Bas { get; set; }
        public DateTime? Bit { get; set; }

        /// <summary>tarih_artan | tarih_azalan | kisi_azalan | talep_azalan</summary>
        public string Sirala { get; set; } = "tarih_artan";

        public int Sayfa { get; set; } = 1;
        public int Boyut { get; set; } = 25;

        /// <summary>Süzgeci adres sorgusuna çevirir (sayfa değeri dışarıdan verilebilir).</summary>
        public string SorguKur(int? sayfa = null, string? sirala = null, string? durum = null)
        {
            var parcalar = new List<string>
            {
                "durum=" + Uri.EscapeDataString(durum ?? Durum)
            };

            if (!string.IsNullOrWhiteSpace(Ara)) parcalar.Add("ara=" + Uri.EscapeDataString(Ara));
            if (Bas.HasValue) parcalar.Add("bas=" + Bas.Value.ToString("yyyy-MM-dd"));
            if (Bit.HasValue) parcalar.Add("bit=" + Bit.Value.ToString("yyyy-MM-dd"));

            parcalar.Add("sirala=" + Uri.EscapeDataString(sirala ?? Sirala));
            parcalar.Add("boyut=" + Boyut);

            var s = sayfa ?? Sayfa;
            if (s > 1) parcalar.Add("sayfa=" + s);

            return "?" + string.Join("&", parcalar);
        }

        public bool SuzgecAktif =>
            !string.IsNullOrWhiteSpace(Ara) || Bas.HasValue || Bit.HasValue;
    }
}
