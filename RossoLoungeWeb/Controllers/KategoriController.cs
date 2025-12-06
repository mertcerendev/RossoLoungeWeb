using Microsoft.AspNetCore.Mvc;
using RossoLoungeWeb.Data;
using RossoLoungeWeb.Models;

namespace RossoLoungeWeb.Controllers
{
    public class KategoriController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KategoriController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. KATEGORİ LİSTESİ (Sıralı)
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");

            // Kategorileri Sıra Numarasına Göre Getir
            var kategoriler = _context.Kategoriler.OrderBy(k => k.SiraNo).ToList();
            return View(kategoriler);
        }

        // 2. EKLEME SAYFASI (OTOMATİK SIRA NO EKLENDİ)
        public IActionResult Ekle()
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");

            // Veritabanında hiç kategori var mı?
            // Varsa en büyük SiraNo'yu al, yoksa 0 kabul et.
            int sonSira = _context.Kategoriler.Any() ? _context.Kategoriler.Max(x => x.SiraNo) : 0;

            // Yeni bir boş model oluştur ve sırayı ata (En sonuncunun 1 fazlası)
            var yeniKategori = new Kategori
            {
                SiraNo = sonSira + 1
            };

            return View(yeniKategori); // Modeli sayfaya gönder
        }

        // 3. EKLEME İŞLEMİ
        [HttpPost]
        public IActionResult Ekle(Kategori yeniKategori)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");

            ModelState.Remove("Id");
            ModelState.Remove("Urunler");

            if (ModelState.IsValid)
            {
                _context.Kategoriler.Add(yeniKategori);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(yeniKategori);
        }

        // 4. DÜZENLEME SAYFASI
        public IActionResult Duzenle(int id)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");
            var kategori = _context.Kategoriler.Find(id);
            if (kategori == null) return NotFound();
            return View(kategori);
        }

        // 5. DÜZENLEME İŞLEMİ
        [HttpPost]
        public IActionResult Duzenle(Kategori gelenKategori)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");

            var mevcut = _context.Kategoriler.Find(gelenKategori.Id);
            if (mevcut != null)
            {
                mevcut.Ad = gelenKategori.Ad;
                mevcut.SiraNo = gelenKategori.SiraNo; // Sırayı güncelle
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(gelenKategori);
        }

        // 6. SİLME İŞLEMİ
        [HttpPost]
        public IActionResult Sil(int id)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");

            var kategori = _context.Kategoriler.Find(id);
            if (kategori != null)
            {
                _context.Kategoriler.Remove(kategori);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}