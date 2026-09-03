namespace RossoLoungeWeb.Models
{
    /// <summary>
    /// Ürün listesi ekranının verisi.
    ///
    /// NEDEN SAYFALAMA VAR: liste bütün ürünleri tek sayfada basıyordu
    /// (canlıda 100'ün üzerinde kalem) ve arama tarayıcıda satır
    /// gizleyerek yapılıyordu. Süzme, sıralama ve sayfalama artık
    /// SUNUCUDA — tarayıcıda gizlemek kayıtların yine indirilmesi
    /// demekti. Aynı gerekçe Rezervasyonlar'da da geçerliydi.
    /// </summary>
    public class UrunListeModeli
    {
        public List<Urun> Kayitlar { get; set; } = new();
        public UrunSuzgeci Suzgec { get; set; } = new();

        /// <summary>Süzgece uyan toplam kayıt (sayfadaki değil).</summary>
        public int ToplamKayit { get; set; }

        /// <summary>Menüdeki tüm kalem sayısı — süzgeçten bağımsız.</summary>
        public int TumKayit { get; set; }

        public int ToplamSayfa { get; set; }
        public bool OncekiVar => Suzgec.Sayfa > 1;
        public bool SonrakiVar => Suzgec.Sayfa < ToplamSayfa;

        /// <summary>Süzgeç açılır listesi için kategoriler (menü sırasında).</summary>
        public List<Kategori> Kategoriler { get; set; } = new();

        /// <summary>Seçili kategorinin adı — başlıkta ve boş durumda kullanılıyor.</summary>
        public string? SeciliKategoriAdi { get; set; }

        /// <summary>
        /// Sürükle-bırak sıralama YALNIZCA burada anlamlı: tek bir kategori
        /// seçiliyken ve liste menü sırasındayken. Gerekçe:
        ///
        ///  - Menüde ürünler kendi kategorilerinin İÇİNDE sıralanıyor
        ///    (kategori.Urunler.OrderBy(SiraNo)). Karışık listede bir
        ///    pizzayı bir başlangıcın üstüne sürüklemek ekranda bir şey
        ///    ifade ediyor ama sitede etmiyor.
        ///  - Fiyata veya ada göre sıralanmış bir listede sürükleme zaten
        ///    saçma: bıraktığın yer bir sonraki yüklemede kaybolur.
        ///
        /// Arama açıkken de kapalı: eldeki satırlar kategorinin tamamı
        /// değil, numara havuzu eksik dağıtılır.
        /// </summary>
        public bool SiralanabilirMi =>
            Suzgec.KategoriId.HasValue
            && Suzgec.Sirala == "menu"
            && string.IsNullOrWhiteSpace(Suzgec.Ara);
    }

    /// <summary>
    /// Listedeki süzgeç durumu. Adres çubuğunda taşınıyor: yönetici
    /// süzülmüş bir listeyi yer imine ekleyebilsin ve silme sonrasında
    /// aynı görünüme dönebilsin.
    /// </summary>
    public class UrunSuzgeci
    {
        public int? KategoriId { get; set; }
        public string? Ara { get; set; }

        /// <summary>menu | ad | fiyat_artan | fiyat_azalan | yeni</summary>
        public string Sirala { get; set; } = "menu";

        public int Sayfa { get; set; } = 1;
        public int Boyut { get; set; } = 25;

        /// <summary>Süzgeci adres sorgusuna çevirir (sayfa dışarıdan verilebilir).</summary>
        public string SorguKur(int? sayfa = null, string? sirala = null, int? kategoriId = null)
        {
            var parcalar = new List<string>();

            var kat = kategoriId ?? KategoriId;
            if (kat.HasValue) parcalar.Add("kategoriId=" + kat.Value);

            if (!string.IsNullOrWhiteSpace(Ara)) parcalar.Add("ara=" + Uri.EscapeDataString(Ara));

            parcalar.Add("sirala=" + Uri.EscapeDataString(sirala ?? Sirala));
            parcalar.Add("boyut=" + Boyut);

            var s = sayfa ?? Sayfa;
            if (s > 1) parcalar.Add("sayfa=" + s);

            return parcalar.Count == 0 ? "" : "?" + string.Join("&", parcalar);
        }

        public bool SuzgecAktif => KategoriId.HasValue || !string.IsNullOrWhiteSpace(Ara);
    }
}
