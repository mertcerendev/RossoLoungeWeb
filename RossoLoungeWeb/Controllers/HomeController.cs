using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RossoLoungeWeb.Data;   // Veritabaný baðlantýsý için
using RossoLoungeWeb.Models; // Rezervasyon sýnýfý için
using System.Diagnostics;

namespace RossoLoungeWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        // 1. Veritabaný Baðlantýsýný Tanýmlýyoruz
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context; // Veritabanýný hafýzaya aldýk
        }

        public IActionResult Index()
        {
            // Sadece ONAYLANMIÞ yorumlarý tarihe göre (yeniden eskiye) getir
            var onayliYorumlar = _context.Yorumlar
                                         .Where(y => y.OnaylandiMi == true)
                                         .OrderByDescending(y => y.Tarih)
                                         .ToList();

            return View(onayliYorumlar); // Listeyi View'a gönder
        }

        public IActionResult Menu()
        {
            // Kategorileri SiraNo'ya göre (OrderBy) çekiyoruz
            var menu = _context.Kategoriler
                               .Include(k => k.Urunler)
                               .OrderBy(k => k.SiraNo) // BURASI ÖNEMLÝ: Kategori Sýralamasý
                               .ToList();

            // Her kategorinin içindeki ürünleri de kendi SiraNo'suna göre diziyoruz
            foreach (var kategori in menu)
            {
                kategori.Urunler = kategori.Urunler.OrderBy(u => u.SiraNo).ToList();
            }

            return View(menu);
        }

        // 2. REZERVASYON KAYDETME ÝÞLEMÝ (Form buraya veri gönderecek)
        [HttpPost]
        public IActionResult RezervasyonYap(Rezervasyon yeniRezervasyon)
        {
            // Gelen veriler kurallara uygun mu? (Boþ deðilse, tarih düzgünse vb.)
            if (ModelState.IsValid)
            {
                // A. Veritabanýna ekle
                _context.Rezervasyons.Add(yeniRezervasyon);

                // B. Deðiþiklikleri Kaydet (SQL'e INSERT komutu gider)
                _context.SaveChanges();

                // C. Baþarýlý mesajý oluþtur (Bir sonraki sayfada göstermek için)
                TempData["Mesaj"] = "Tebrikler! Rezervasyonunuz baþarýyla alýndý.";

                // D. Ana sayfaya geri dön
                return RedirectToAction("Index");
            }

            // Hata varsa (örneðin eksik bilgi), formu tekrar göster (veya ana sayfaya dön)
            return RedirectToAction("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult YorumYap(string AdSoyad, string Mesaj, int Puan)
        {
            // Basit validasyon
            if (!string.IsNullOrEmpty(AdSoyad) && !string.IsNullOrEmpty(Mesaj) && Puan > 0)
            {
                Yorum yeniYorum = new Yorum();
                yeniYorum.AdSoyad = AdSoyad;
                yeniYorum.Mesaj = Mesaj;
                yeniYorum.Puan = Puan;
                yeniYorum.OnaylandiMi = false; // Yönetici onayý bekleyecek
                yeniYorum.Tarih = DateTime.Now;

                _context.Yorumlar.Add(yeniYorum);
                _context.SaveChanges();

                TempData["Mesaj"] = "Yorumunuz alýndý! Onaylandýktan sonra yayýnlanacaktýr.";
            }
            else
            {
                TempData["Hata"] = "Lütfen tüm alanlarý doldurun ve puan verin.";
            }

            return RedirectToAction("Index", "Home", new { fragment = "reviews" }); // Yorumlar kýsmýna dön
        }

    }
}