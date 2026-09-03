using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // DbUpdateException için
using RossoLoungeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using RossoLoungeWeb.Models;

namespace RossoLoungeWeb.Controllers
{
    // Oturum kontrolü artık tek yerden (filtre ile) yapılıyor.
    [Authorize]
    public class KategoriController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<KategoriController> _logger;

        public KategoriController(ApplicationDbContext context, ILogger<KategoriController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Veritabanı hatası kullanıcıya ham 500 sayfası olarak dönmesin diye
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

        // 1. KATEGORİ LİSTESİ (Sıralı)
        public IActionResult Index()
        {
            // Kategorileri Sıra Numarasına Göre Getir
            var kategoriler = _context.Kategoriler.OrderBy(k => k.SiraNo).ToList();

            // Listede "içinde kaç ürün var" bilgisini gösterebilmek için
            // (silme uyarısı bu sayıya dayanıyor).
            ViewBag.UrunSayilari = _context.Urunler
                .GroupBy(u => u.KategoriId)
                .ToDictionary(g => g.Key, g => g.Count());

            return View(kategoriler);
        }

        // 2. EKLEME SAYFASI
        // Sıra numarası artık formda sorulmuyor (bkz. Ekle POST).
        public IActionResult Ekle()
        {
            return View(new Kategori());
        }

        // 3. EKLEME İŞLEMİ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ekle(Kategori yeniKategori)
        {
            ModelState.Remove("Id");
            ModelState.Remove("Urunler");

            if (ModelState.IsValid)
            {
                // SIRA NO'YU SUNUCU VERİYOR. Formdaki sayı kutusu kaldırıldı:
                // sıralamanın iki sahibi (elle numara + listedeki sürükle-bırak)
                // birbirinden habersizdi, elle girilen numara çakışabiliyordu.
                // Yeni kategori sona ekleniyor, sırası listeden sürüklenerek
                // değiştiriliyor. Gövdeden gelen SiraNo bilerek yok sayılıyor.
                yeniKategori.SiraNo = _context.Kategoriler.Any()
                    ? _context.Kategoriler.Max(x => x.SiraNo) + 1
                    : 1;

                _context.Kategoriler.Add(yeniKategori);
                if (GuvenliKaydet("Kategori ekleme"))
                {
                    TempData["Mesaj"] = $"'{yeniKategori.Ad}' kategorisi eklendi.";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError(string.Empty, "Kategori kaydedilemedi. Lütfen tekrar deneyin.");
            }
            return View(yeniKategori);
        }

        // 4. DÜZENLEME SAYFASI
        public IActionResult Duzenle(int id)
        {
            var kategori = _context.Kategoriler.Find(id);
            if (kategori == null) return NotFound();

            // Başlık yanındaki bağlam rozeti için: kaç ürün var, boş mu.
            ViewBag.UrunSayisi = _context.Urunler.Count(u => u.KategoriId == id);

            return View(kategori);
        }

        // 5. DÜZENLEME İŞLEMİ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Duzenle(Kategori gelenKategori)
        {
            var mevcut = _context.Kategoriler.Find(gelenKategori.Id);
            if (mevcut == null)
            {
                TempData["Hata"] = "Düzenlenecek kategori bulunamadı.";
                return RedirectToAction("Index");
            }

            // Doğrulama olmadan kaydedilirse [Range]/[Required] kuralları
            // atlanıyor, boş ad ise veritabanı istisnasıyla engelleniyordu.
            ModelState.Remove(nameof(Kategori.Urunler));
            if (!ModelState.IsValid)
            {
                // Form geri geliyorsa başlıktaki bağlam rozeti de dolmalı.
                ViewBag.UrunSayisi = _context.Urunler.Count(u => u.KategoriId == gelenKategori.Id);

                // Alt başlık normalde Model.Ad; ad boş bırakıldığı için hata
                // sayfasında hangi kategorinin düzenlendiği kaybolmasın.
                ViewBag.MevcutAd = mevcut.Ad;

                return View(gelenKategori);
            }

            mevcut.Ad = gelenKategori.Ad;

            // SIRAYA DOKUNULMUYOR. Form artık SiraNo göndermiyor; gövdeden
            // gelmeyen alan modelde 0 olur ve buraya yazılsaydı kategori
            // menünün en başına fırlardı. Sıra yalnızca SiraGuncelle'nin işi.

            if (GuvenliKaydet("Kategori güncelleme"))
            {
                TempData["Mesaj"] = $"'{mevcut.Ad}' kategorisi güncellendi.";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, "Kategori kaydedilemedi. Lütfen tekrar deneyin.");
            return View(gelenKategori);
        }


        // --- SÜRÜKLE-BIRAK SIRALAMA ---
        // Gelen id dizisi, listedeki YENİ görsel sırayı temsil eder.
        // Yalnızca gönderilen kayıtların mevcut sıra numaraları kendi aralarında
        // yeniden dağıtılır; böylece listede görünmeyen kayıtların (ör. kategori
        // filtresi açıkken diğer ürünler) yeri bozulmaz.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SiraGuncelle(int[] sira)
        {
            if (sira == null || sira.Length == 0)
                return BadRequest(new { mesaj = "Sıralama bilgisi boş." });

            var kayitlar = _context.Kategoriler.Where(x => sira.Contains(x.Id)).ToList();
            if (kayitlar.Count != sira.Length)
                return BadRequest(new { mesaj = "Sıralanacak kayıtlar bulunamadı." });

            // Bu kayıtların hâlihazırda sahip olduğu sıra numaraları havuzu
            var numaralar = kayitlar.Select(x => x.SiraNo).OrderBy(n => n).ToList();

            for (int i = 0; i < sira.Length; i++)
            {
                var kayit = kayitlar.First(x => x.Id == sira[i]);
                kayit.SiraNo = numaralar[i];
            }

            if (!GuvenliKaydet("Kategori sıralama"))
                return StatusCode(500, new { mesaj = "Sıralama kaydedilemedi." });

            return Ok(new { mesaj = "Sıralama kaydedildi." });
        }

        // 6. SİLME İŞLEMİ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int id)
        {
            var kategori = _context.Kategoriler.Find(id);
            if (kategori == null)
            {
                TempData["Hata"] = "Silinecek kategori bulunamadı.";
                return RedirectToAction("Index");
            }

            // Kategori-ürün ilişkisi artık Cascade değil Restrict; dolu bir kategori
            // silinmeye çalışılırsa veritabanı hata 547 fırlatır ve kullanıcı ham
            // 500 sayfası görür. Önden kontrol edip anlamlı mesaj veriyoruz.
            // (Eski Cascade davranışında ise ürünler sessizce siliniyordu.)
            int urunSayisi = _context.Urunler.Count(u => u.KategoriId == id);
            if (urunSayisi > 0)
            {
                TempData["Hata"] = $"'{kategori.Ad}' kategorisi silinemedi: içinde {urunSayisi} ürün var. " +
                                   "Önce bu ürünleri başka bir kategoriye taşıyın veya silin.";
                return RedirectToAction("Index");
            }

            _context.Kategoriler.Remove(kategori);
            if (GuvenliKaydet("Kategori silme"))
                TempData["Mesaj"] = $"'{kategori.Ad}' kategorisi silindi.";
            else
                TempData["Hata"] = $"'{kategori.Ad}' kategorisi silinemedi. Lütfen tekrar deneyin.";

            return RedirectToAction("Index");
        }
    }
}
