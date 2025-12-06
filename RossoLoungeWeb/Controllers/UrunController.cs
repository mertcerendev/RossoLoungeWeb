using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RossoLoungeWeb.Data;
using RossoLoungeWeb.Models;
using System.Globalization;

namespace RossoLoungeWeb.Controllers
{
    public class UrunController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public UrunController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // 1. ÜRÜN LİSTESİ
        public IActionResult Index(int? kategoriId)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");

            ViewBag.Kategoriler = new SelectList(_context.Kategoriler, "Id", "Ad");
            ViewBag.SeciliKategoriId = kategoriId;

            var urunlerSorgu = _context.Urunler.Include(u => u.Kategori).AsQueryable();

            if (kategoriId.HasValue && kategoriId.Value > 0)
            {
                urunlerSorgu = urunlerSorgu.Where(u => u.KategoriId == kategoriId.Value);
            }

            return View(urunlerSorgu.OrderBy(u => u.Kategori.Ad).ThenBy(u => u.SiraNo).ToList());
        }

        // 2. EKLEME SAYFASI (Basit ve Manuel)
        public IActionResult Ekle()
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");

            ViewBag.Kategoriler = new SelectList(_context.Kategoriler, "Id", "Ad");

            // Veritabanındaki en büyük sıra numarasını bul (Kategori fark etmeksizin)
            int sonSira = _context.Urunler.Any() ? _context.Urunler.Max(x => x.SiraNo) : 0;

            // Varsayılan olarak sonuncunun bir fazlasını öneriyoruz
            return View(new Urun { SiraNo = sonSira + 1 });
        }

        // 3. EKLEME İŞLEMİ (POST)
        [HttpPost]
        public IActionResult Ekle(Urun urun, IFormFile? ResimDosyasi)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");

            ModelState.Remove("Id");
            ModelState.Remove("Kategori");

            if (ModelState.IsValid)
            {
                if (ResimDosyasi != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ResimDosyasi.FileName);
                    string path = Path.Combine(_hostEnvironment.WebRootPath, "img/urunler", fileName);

                    if (!Directory.Exists(Path.Combine(_hostEnvironment.WebRootPath, "img/urunler")))
                        Directory.CreateDirectory(Path.Combine(_hostEnvironment.WebRootPath, "img/urunler"));

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        ResimDosyasi.CopyTo(stream);
                    }
                    urun.ResimUrl = "/img/urunler/" + fileName;
                }
                else
                {
                    urun.ResimUrl = "https://placehold.co/600x400?text=Resim+Yok";
                }

                _context.Urunler.Add(urun);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Kategoriler = new SelectList(_context.Kategoriler, "Id", "Ad");
            return View(urun);
        }

        // 4. DÜZENLEME SAYFASI
        public IActionResult Duzenle(int id)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");
            var urun = _context.Urunler.Find(id);
            if (urun == null) return NotFound();
            ViewBag.Kategoriler = new SelectList(_context.Kategoriler, "Id", "Ad", urun.KategoriId);
            return View(urun);
        }

        // 5. DÜZENLEME İŞLEMİ
        [HttpPost]
        public IActionResult Duzenle(Urun gelenUrun, IFormFile? ResimDosyasi)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");
            ModelState.Remove("Kategori");

            if (ModelState.IsValid)
            {
                var mevcutUrun = _context.Urunler.Find(gelenUrun.Id);
                if (mevcutUrun != null)
                {
                    mevcutUrun.Ad = gelenUrun.Ad;
                    mevcutUrun.Aciklama = gelenUrun.Aciklama;
                    mevcutUrun.Fiyat = gelenUrun.Fiyat;
                    mevcutUrun.FiyatBuyuk = gelenUrun.FiyatBuyuk;
                    mevcutUrun.FiyatTur = gelenUrun.FiyatTur;
                    mevcutUrun.FiyatBuyukTur = gelenUrun.FiyatBuyukTur;
                    mevcutUrun.KategoriId = gelenUrun.KategoriId;
                    mevcutUrun.SiraNo = gelenUrun.SiraNo;

                    if (ResimDosyasi != null)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ResimDosyasi.FileName);
                        string path = Path.Combine(_hostEnvironment.WebRootPath, "img/urunler", fileName);
                        using (var stream = new FileStream(path, FileMode.Create)) { ResimDosyasi.CopyTo(stream); }
                        mevcutUrun.ResimUrl = "/img/urunler/" + fileName;
                    }
                    _context.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            ViewBag.Kategoriler = new SelectList(_context.Kategoriler, "Id", "Ad", gelenUrun.KategoriId);
            return View(gelenUrun);
        }

        // 6. SİLME İŞLEMİ
        [HttpPost]
        public IActionResult Sil(int id)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login", "Admin");
            var urun = _context.Urunler.Find(id);
            if (urun != null) { _context.Urunler.Remove(urun); _context.SaveChanges(); }
            return RedirectToAction("Index");
        }
    }
}