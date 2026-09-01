using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RossoLoungeWeb.Data;   // Veritabanı bağlantısı için
using RossoLoungeWeb.Models; // Rezervasyon sınıfı için
using RossoLoungeWeb.Services; // TurkiyeSaati için
using System.Diagnostics;
using System.Globalization;

namespace RossoLoungeWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        // 1. Veritabanı Bağlantısını Tanımlıyoruz
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context; // Veritabanını hafızaya aldık
        }

        // Veritabanı hatası ziyaretçiye ham 500 sayfası olarak dönmesin diye
        // SaveChanges tek bir korumalı yardımcıdan geçiyor.
        private bool GuvenliKaydet(string islemAdi)
        {
            try
            {
                _context.SaveChanges();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Veritabanı kaydı başarısız: {Islem}", islemAdi);
                return false;
            }
        }

        public IActionResult Index()
        {
            // Sadece ONAYLANMIŞ yorumları tarihe göre (yeniden eskiye) getir
            var onayliYorumlar = _context.Yorumlar
                                         .Where(y => y.OnaylandiMi == true)
                                         .OrderByDescending(y => y.Tarih)
                                         .ToList();

            // Öne çıkan ürünler sabit kodluydu; artık menünün ilk üç ürünü
            // gösteriliyor, sıralamayı panelden SiraNo ile yönetebiliyoruz.
            ViewBag.OneCikanlar = _context.Urunler
                                          .OrderBy(u => u.SiraNo)
                                          .Take(3)
                                          .ToList();

            // Ana sayfadaki menü sergisi: kategoriler + ürünleri, panelden
            // yönetilen SiraNo düzeninde. Boş kategori sergide görünmesin.
            ViewBag.MenuKategorileri = _context.Kategoriler
                                               .Include(k => k.Urunler)
                                               .Where(k => k.Urunler.Any())
                                               .OrderBy(k => k.SiraNo)
                                               .ToList()
                                               .Select(k => { k.Urunler = k.Urunler.OrderBy(u => u.SiraNo).ToList(); return k; })
                                               .ToList();

            ViewBag.KategoriSayisi = ((List<RossoLoungeWeb.Models.Kategori>)ViewBag.MenuKategorileri).Count;

            // Çalışma saatleri 11.30 – 00.00. Gece yarısına sarktığı için
            // "saat >= açılış" tek başına yetmiyor; 00.00 kapanışı ertesi güne denk geliyor.
            // Hakkımızda bölümündeki rakam şeridi — uydurma değil, gerçek veriden
            ViewBag.ToplamUrun = _context.Urunler.Count();
            ViewBag.YorumSayisi = onayliYorumlar.Count;
            ViewBag.OrtalamaPuan = onayliYorumlar.Count > 0
                ? onayliYorumlar.Average(y => y.Puan).ToString("0.0", new CultureInfo("tr-TR"))
                : "—";

            var suan = TurkiyeSaati.Simdi.TimeOfDay;
            ViewBag.SuAnAcik = suan >= new TimeSpan(11, 30, 0);

            return View(onayliYorumlar); // Listeyi View'a gönder
        }

        public IActionResult Menu()
        {
            // Kategorileri SiraNo'ya göre (OrderBy) çekiyoruz.
            // İçinde ürün olmayan kategoriler menüde boş başlık olarak
            // görünüyordu; artık eleniyorlar (panelde görünmeye devam ederler).
            var menu = _context.Kategoriler
                               .Include(k => k.Urunler)
                               .Where(k => k.Urunler.Any()) // Boş kategori menüde çıkmasın
                               .OrderBy(k => k.SiraNo) // BURASI ÖNEMLİ: Kategori Sıralaması
                               .ToList();

            // Her kategorinin içindeki ürünleri de kendi SiraNo'suna göre diziyoruz
            foreach (var kategori in menu)
            {
                kategori.Urunler = kategori.Urunler.OrderBy(u => u.SiraNo).ToList();
            }

            return View(menu);
        }

        // 2. REZERVASYON KAYDETME İŞLEMİ (Form buraya veri gönderecek)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RezervasyonYap(Rezervasyon yeniRezervasyon)
        {
            // Gelen veriler kurallara uygun mu? (Boş değilse, tarih düzgünse vb.)
            if (ModelState.IsValid)
            {
                // A. Veritabanına ekle
                _context.Rezervasyons.Add(yeniRezervasyon);

                // B. Değişiklikleri Kaydet (SQL'e INSERT komutu gider)
                if (GuvenliKaydet("Rezervasyon oluşturma"))
                {
                    // C. Başarılı mesajı oluştur (Bir sonraki sayfada göstermek için)
                    TempData["Mesaj"] = "Tebrikler! Rezervasyonunuz başarıyla alındı.";
                }
                else
                {
                    TempData["Hata"] = "Rezervasyonunuz şu anda kaydedilemedi. Lütfen biraz sonra tekrar deneyin veya bizi telefonla arayın.";
                }

                // D. Ana sayfaya geri dön
                return RedirectToAction("Index");
            }

            // Hata varsa kullanıcı sessizce yönlendirilmesin, sebebi görsün
            TempData["Hata"] = "Rezervasyon alınamadı. Lütfen ad, telefon, tarih ve kişi sayısı alanlarını kontrol edin.";
            return RedirectToAction("Index");
        }

        // 3. İLETİŞİM MESAJI KAYDETME
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IletisimGonder(Iletisim yeniMesaj)
        {
            if (ModelState.IsValid)
            {
                yeniMesaj.Tarih = TurkiyeSaati.Simdi;
                yeniMesaj.OkunduMu = false;

                _context.IletisimMesajlari.Add(yeniMesaj);

                if (GuvenliKaydet("İletişim mesajı kaydetme"))
                    TempData["Mesaj"] = "Mesajınız bize ulaştı. En kısa sürede dönüş yapacağız.";
                else
                    TempData["Hata"] = "Mesajınız şu anda gönderilemedi. Lütfen biraz sonra tekrar deneyin.";
            }
            else
            {
                TempData["Hata"] = "Lütfen ad soyad, e-posta ve mesaj alanlarını doldurun.";
            }

            return RedirectToAction("Index", "Home", null, "contact");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? kod)
        {
            ViewBag.DurumKodu = kod;
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YorumYap(string AdSoyad, string Mesaj, int Puan)
        {
            // Basit validasyon
            if (!string.IsNullOrEmpty(AdSoyad) && !string.IsNullOrEmpty(Mesaj) && Puan > 0)
            {
                Yorum yeniYorum = new Yorum();
                yeniYorum.AdSoyad = AdSoyad;
                yeniYorum.Mesaj = Mesaj;
                yeniYorum.Puan = Puan;
                yeniYorum.OnaylandiMi = false; // Yönetici onayı bekleyecek
                yeniYorum.Tarih = TurkiyeSaati.Simdi;

                _context.Yorumlar.Add(yeniYorum);

                if (GuvenliKaydet("Yorum kaydetme"))
                    TempData["Mesaj"] = "Yorumunuz alındı! Onaylandıktan sonra yayınlanacaktır.";
                else
                    TempData["Hata"] = "Yorumunuz şu anda kaydedilemedi. Lütfen biraz sonra tekrar deneyin.";
            }
            else
            {
                TempData["Hata"] = "Lütfen tüm alanları doldurun ve puan verin.";
            }

            return RedirectToAction("Index", "Home", new { fragment = "reviews" }); // Yorumlar kısmına dön
        }

    }
}