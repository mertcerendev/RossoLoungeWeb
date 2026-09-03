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

            /* VİTRİN SEÇİMİ — artık panelden işaretleniyor (Urun.OneCikan).
               Önce sabit kodluydu, sonra "SiraNo'su en küçük 3 ürün" oldu:
               ikisinde de vitrini değiştirmenin yolu tabağı bütün menünün
               en tepesine sürüklemekti, yani vitrin ile menü sırası aynı
               düğmeye bağlıydı.

               Take(3): vitrin ızgarası CSS'te repeat(3, 1fr). Panel zaten
               üçten fazlasını işaretletmiyor; bu, doğrudan veritabanına
               dokunulursa düzenin bozulmamasını sağlayan ikinci kilit.

               EKSİK KALAN YER MENÜ SIRASINDAN TAMAMLANIYOR. Izgara üç
               sütun: tek tabak işaretlenirse yanında iki boş sütun kalır
               ve bölüm bozuk görünür. Ayrıca sütun canlıya yeni
               eklendiğinde hiçbir ürün işaretli olmaz — tamamlama
               olmasaydı vitrin o an tamamen boşalırdı.
               İşaretliler her zaman ÖNCE geliyor. */
            const int vitrinYeri = 3;

            var vitrin = _context.Urunler
                                 .Where(u => u.OneCikan)
                                 .OrderBy(u => u.SiraNo)
                                 .Take(vitrinYeri)
                                 .ToList();

            if (vitrin.Count < vitrinYeri)
            {
                var secilenler = vitrin.Select(u => u.Id).ToList();

                vitrin.AddRange(_context.Urunler
                                        .Where(u => !secilenler.Contains(u.Id))
                                        .OrderBy(u => u.SiraNo)
                                        .Take(vitrinYeri - vitrin.Count)
                                        .ToList());
            }

            ViewBag.OneCikanlar = vitrin;

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

            /* İletişim bölümündeki canlı saat + durum ışığı.
               ÇALIŞMA SAATLERİ TEK KAYNAK BURASI. Mola yoksa MolaBas/MolaBit
               boş bırakılır; o zaman sarı "molada" durumu hiç oluşmaz.
               Aynı değerler data-* nitelikleriyle görünüme geçiyor, istemci
               tarafı Europe/Istanbul saatiyle her dakika tazeliyor. */
            const string acilis = "11:30";
            const string kapanis = "00:00";
            const string molaBas = "";
            const string molaBit = "";

            var simdi = TurkiyeSaati.Simdi;
            ViewBag.Acilis = acilis;
            ViewBag.Kapanis = kapanis;
            ViewBag.MolaBas = molaBas;
            ViewBag.MolaBit = molaBit;
            ViewBag.SaatSimdi = simdi.ToString("HH:mm");
            ViewBag.SaatDurum = CalismaDurumu(simdi.TimeOfDay, acilis, kapanis, molaBas, molaBit);

            return View(onayliYorumlar); // Listeyi View'a gönder
        }

        /* Açık / molada / kapalı.
           Kapanış açılıştan küçükse (ör. 11:30 - 00:00) aralık gece
           yarısını aşıyor demektir; karşılaştırma ona göre çevriliyor. */
        private static string CalismaDurumu(TimeSpan simdi, string acilis, string kapanis,
                                            string molaBas, string molaBit)
        {
            if (!TimeSpan.TryParse(acilis, out var acilisSaat) ||
                !TimeSpan.TryParse(kapanis, out var kapanisSaat))
                return "kapali";

            bool AralikIcinde(TimeSpan bas, TimeSpan bit)
                => bas <= bit ? (simdi >= bas && simdi < bit)
                              : (simdi >= bas || simdi < bit);

            if (!AralikIcinde(acilisSaat, kapanisSaat)) return "kapali";

            if (TimeSpan.TryParse(molaBas, out var molaBasSaat) &&
                TimeSpan.TryParse(molaBit, out var molaBitSaat) &&
                molaBasSaat != molaBitSaat &&
                AralikIcinde(molaBasSaat, molaBitSaat))
                return "mola";

            return "acik";
        }

        /* Adres /Home/Menu yerine /menu. Nitelikli rota koyulunca bu eylem
           varsayılan {controller}/{action} kalıbından ÇIKIYOR, eski adres
           artık eşleşmiyor — Program.cs'te kalıcı yönlendirmesi var. */
        [Route("menu")]
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

                // D. Formun bulunduğu yere geri dön
                return RedirectToAction("Index", "Home", null, "rezervasyon");
            }

            // Hata varsa kullanıcı sessizce yönlendirilmesin, sebebi görsün
            TempData["Hata"] = "Rezervasyon alınamadı. Lütfen ad, telefon, tarih ve kişi sayısı alanlarını kontrol edin.";
            return RedirectToAction("Index", "Home", null, "rezervasyon");
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

            return RedirectToAction("Index", "Home", null, "iletisim");
        }

        // 4. E-BÜLTEN KAYDI (alt bilgideki tek alanlı form)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BultenKayit(BultenAbone yeniAbone)
        {
            if (ModelState.IsValid)
            {
                string eposta = yeniAbone.Email.Trim();

                // Koleksiyon CI olduğu için karşılaştırma zaten büyük/küçük harf duyarsız
                bool zatenKayitli = _context.BultenAboneleri.Any(a => a.Email == eposta);

                if (zatenKayitli)
                {
                    TempData["Mesaj"] = "Bu e-posta zaten kayıtlı, tekrar eklemedik.";
                }
                else
                {
                    yeniAbone.Email = eposta;
                    yeniAbone.Tarih = TurkiyeSaati.Simdi;
                    yeniAbone.AktifMi = true;

                    _context.BultenAboneleri.Add(yeniAbone);

                    if (GuvenliKaydet("Bülten kaydı"))
                        TempData["Mesaj"] = "Bültene kaydolundunuz. Yeni menü ve etkinliklerden haberdar olacaksınız.";
                    else
                        TempData["Hata"] = "Kaydınız şu anda alınamadı. Lütfen biraz sonra tekrar deneyin.";
                }
            }
            else
            {
                TempData["Hata"] = "Geçerli bir e-posta adresi girin.";
            }

            return RedirectToAction("Index", "Home", null, "finale");
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

            /* DİKKAT: burada 3 argümanlı aşırı yükleme kullanılıyordu
               (new { fragment = "reviews" }); o imza fragment'i ROTA DEĞERİ
               sayıyor ve adres /?fragment=reviews çıkıyordu. Fragment'in
               kendi parametresi 4. sırada. */
            return RedirectToAction("Index", "Home", null, "yorumlar");
        }

    }
}