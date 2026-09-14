namespace RossoLoungeWeb.Models
{
    /// <summary>
    /// Gelen kutusu ekranının verisi.
    ///
    /// NEDEN SAYFALAMA VAR: liste bütün mesajları tek sayfada basıyordu ve
    /// arama tarayıcıda satır gizleyerek yapılıyordu — kayıtlar yine
    /// indiriliyor, "arama" yalnızca ekrandakini süzüyordu. Ürünler,
    /// Rezervasyonlar ve Yorumlar'da çözülen sorunun aynısı.
    /// </summary>
    public class MesajListeModeli
    {
        public List<Iletisim> Kayitlar { get; set; } = new();
        public MesajSuzgeci Suzgec { get; set; } = new();

        /// <summary>Süzgece uyan toplam kayıt (sayfadaki değil).</summary>
        public int ToplamKayit { get; set; }

        public int ToplamSayfa { get; set; }
        public bool OncekiVar => Suzgec.Sayfa > 1;
        public bool SonrakiVar => Suzgec.Sayfa < ToplamSayfa;

        // Sekme rozetleri: sekmeye geçmeden kaç kayıt olduğu görünsün.
        public int SayiOkunmamis { get; set; }
        public int SayiOkundu { get; set; }
        public int SayiTumu { get; set; }

        public int SayiGetir(string durum) => durum switch
        {
            "okunmamis" => SayiOkunmamis,
            "okundu" => SayiOkundu,
            _ => SayiTumu
        };
    }

    /// <summary>
    /// Listedeki süzgeç durumu. Adres çubuğunda taşınıyor: yönetici
    /// süzülmüş bir gelen kutusunu yer imine ekleyebilsin ve bir mesajı
    /// okuyup döndüğünde aynı görünümde kalsın.
    /// </summary>
    public class MesajSuzgeci
    {
        /// <summary>tumu | okunmamis | okundu</summary>
        public string Durum { get; set; } = "tumu";

        public string? Ara { get; set; }

        /// <summary>okunmamis_once | yeni | eski | ad</summary>
        public string Sirala { get; set; } = "okunmamis_once";

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

            parcalar.Add("sirala=" + Uri.EscapeDataString(sirala ?? Sirala));
            parcalar.Add("boyut=" + Boyut);

            var s = sayfa ?? Sayfa;
            if (s > 1) parcalar.Add("sayfa=" + s);

            return "?" + string.Join("&", parcalar);
        }

        public bool SuzgecAktif => !string.IsNullOrWhiteSpace(Ara);
    }
}
