namespace RossoLoungeWeb.Models
{
    /// <summary>
    /// Panel özet ekranının tüm verisi.
    ///
    /// Neden ViewBag değil: ekran on beşten fazla değer taşıyor ve bunların
    /// bir kısmı liste. ViewBag'de yazım hatası derleme zamanında değil,
    /// ekranda boş kutu olarak ortaya çıkıyordu.
    ///
    /// NOT — CİRO YOK: bu sitede sipariş/ödeme kaydı tutulmuyor (rezervasyon
    /// + menü sitesi). Bir yönetici panelinde ilk beklenen "günlük ciro"
    /// olurdu ama veri yok; uydurma sayı göstermektense o kutuyu hiç
    /// koymuyoruz. POS entegrasyonu gelirse buraya eklenir.
    /// </summary>
    public class OzetModeli
    {
        // --- Karşılama ---
        public string Kullanici { get; set; } = "Yönetici";
        public DateTime Simdi { get; set; }
        public bool SuAnAcik { get; set; }

        // --- Bekleyen işler ---
        public int BugunRezervasyon { get; set; }
        public int BugunKisi { get; set; }
        public int BekleyenRezervasyon { get; set; }
        public int OkunmamisMesaj { get; set; }
        public int BekleyenYorum { get; set; }

        // --- Toplamlar ---
        public int ToplamRezervasyon { get; set; }
        public int ToplamMesaj { get; set; }
        public int ToplamYorum { get; set; }
        public int ToplamUrun { get; set; }
        public int ToplamKategori { get; set; }
        public int BultenAbone { get; set; }

        // --- Karşılaştırma: bu hafta / geçen hafta ---
        public int BuHaftaRezervasyon { get; set; }
        public int GecenHaftaRezervasyon { get; set; }
        public int? HaftaDegisimYuzde =>
            GecenHaftaRezervasyon == 0
                ? (BuHaftaRezervasyon > 0 ? (int?)100 : null)
                : (int)Math.Round((BuHaftaRezervasyon - GecenHaftaRezervasyon) * 100.0 / GecenHaftaRezervasyon);

        // --- Memnuniyet ---
        public double? OrtalamaPuan { get; set; }
        public int[] PuanDagilimi { get; set; } = new int[5]; // [0]=1 yıldız … [4]=5 yıldız
        public List<Yorum> SonYorumlar { get; set; } = new();

        // --- Listeler ---
        public List<Rezervasyon> BugunListe { get; set; } = new();
        public List<GunOzeti> YaklasanGunler { get; set; } = new();
        public List<SaatDilimi> YogunSaatler { get; set; } = new();
        public List<Hareket> SonHareketler { get; set; } = new();
        public List<Uyari> Uyarilar { get; set; } = new();

        // --- Grafik ---
        public GrafikVerisi Grafik { get; set; } = new();
    }

    /// <summary>Grafikte tek bir sütun.</summary>
    public class GrafikNoktasi
    {
        public string Etiket { get; set; } = string.Empty;      // eksende görünen kısa ad
        public string TamEtiket { get; set; } = string.Empty;   // ipucunda görünen uzun ad
        public int Rezervasyon { get; set; }
        public int Kisi { get; set; }
    }

    /// <summary>
    /// Dört dönemin verisi tek seferde hesaplanıp gönderiliyor: dönem
    /// değiştirmek sunucuya gitmesin, tıklama anında değişsin. Nokta sayısı
    /// küçük (30 + 12 + yıl sayısı), maliyeti yok.
    /// </summary>
    public class GrafikVerisi
    {
        public List<GrafikNoktasi> Gun7 { get; set; } = new();
        public List<GrafikNoktasi> Gun30 { get; set; } = new();
        public Dictionary<int, List<GrafikNoktasi>> Aylik { get; set; } = new();
        public List<GrafikNoktasi> Yillik { get; set; } = new();
        public List<int> Yillar { get; set; } = new();
        public int VarsayilanYil { get; set; }
    }

    public class GunOzeti
    {
        public DateTime Tarih { get; set; }
        public string GunAdi { get; set; } = string.Empty;
        public int Rezervasyon { get; set; }
        public int Kisi { get; set; }
        public bool Bugun { get; set; }
    }

    public class SaatDilimi
    {
        public int Saat { get; set; }
        public int Rezervasyon { get; set; }
        public int Kisi { get; set; }
    }

    /// <summary>Son hareketler akışındaki tek satır.</summary>
    public class Hareket
    {
        public DateTime Tarih { get; set; }
        public string Tur { get; set; } = string.Empty;      // rezervasyon | mesaj | yorum | bulten
        public string Ikon { get; set; } = string.Empty;
        public string Baslik { get; set; } = string.Empty;
        public string? Ayrinti { get; set; }
        public string? Adres { get; set; }
    }

    /// <summary>Yöneticinin düzeltmesi gereken veri sorunu.</summary>
    public class Uyari
    {
        public string Metin { get; set; } = string.Empty;
        public string Adres { get; set; } = string.Empty;
        public string Eylem { get; set; } = "Düzelt";
    }
}
