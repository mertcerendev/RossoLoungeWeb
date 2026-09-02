using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RossoLoungeWeb.Data;
using Microsoft.AspNetCore.Authorization;
using RossoLoungeWeb.Models;
using System.Globalization;

namespace RossoLoungeWeb.Controllers
{
    // Oturum kontrolü artık tek yerden (filtre ile) yapılıyor.
    [Authorize]
    public class UrunController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<UrunController> _logger;

        public UrunController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment, ILogger<UrunController> logger)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        private static readonly string[] IzinliUzantilar = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaksimumBoyut = 5 * 1024 * 1024; // 5 MB
        private const string ResimKlasoruYolu = "/img/urunler/";

        // Kategori açılır listesi her action'da tek tek kuruluyordu; tek yere alındı.
        private void KategorileriYukle(int? seciliId = null)
        {
            ViewBag.Kategoriler = new SelectList(
                _context.Kategoriler.OrderBy(k => k.SiraNo).ToList(), "Id", "Ad", seciliId);
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

        // Görseli kaydeder ve web yolunu döner. Geçersizse null döner ve
        // sebebini ModelState'e yazar. Doğrulama olmadığı için daha önce
        // resim yerine .url kısayol dosyaları yüklenebiliyordu.
        /// <summary>
        /// Dosyanın ilk baytlarına bakarak gerçekten görsel olup olmadığını doğrular.
        /// Uzantı kontrolü tek başına yeterli değil: metin dosyasının adı .jpg
        /// yapılınca kabul ediliyordu ve sitede bozuk görsel olarak çıkıyordu.
        /// </summary>
        private static bool GercektenGorselMi(IFormFile dosya)
        {
            Span<byte> bas = stackalloc byte[12];

            using var akis = dosya.OpenReadStream();
            if (akis.ReadAtLeast(bas, bas.Length, throwOnEndOfStream: false) < bas.Length)
                return false;

            // JPEG: FF D8 FF
            if (bas[0] == 0xFF && bas[1] == 0xD8 && bas[2] == 0xFF) return true;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (bas[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return true;

            // GIF: "GIF87a" veya "GIF89a"
            if (bas[0] == 'G' && bas[1] == 'I' && bas[2] == 'F' && bas[3] == '8') return true;

            // WEBP: "RIFF" .... "WEBP"
            if (bas[0] == 'R' && bas[1] == 'I' && bas[2] == 'F' && bas[3] == 'F' &&
                bas[8] == 'W' && bas[9] == 'E' && bas[10] == 'B' && bas[11] == 'P') return true;

            return false;
        }

        private string? ResimKaydet(IFormFile dosya)
        {
            string uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();

            if (!IzinliUzantilar.Contains(uzanti))
            {
                ModelState.AddModelError("ResimDosyasi", "Sadece JPG, PNG, WEBP veya GIF yükleyebilirsiniz.");
                return null;
            }

            if (dosya.Length > MaksimumBoyut)
            {
                ModelState.AddModelError("ResimDosyasi", "Görsel boyutu en fazla 5 MB olabilir.");
                return null;
            }

            if (!GercektenGorselMi(dosya))
            {
                ModelState.AddModelError("ResimDosyasi", "Dosya geçerli bir görsel değil. Uzantısı değiştirilmiş olabilir.");
                return null;
            }

            string klasor = Path.Combine(_hostEnvironment.WebRootPath, "img", "urunler");
            Directory.CreateDirectory(klasor);

            string dosyaAdi = Guid.NewGuid().ToString() + uzanti;
            using (var stream = new FileStream(Path.Combine(klasor, dosyaAdi), FileMode.Create))
            {
                dosya.CopyTo(stream);
            }

            return ResimKlasoruYolu + dosyaAdi;
        }

        /// <summary>
        /// Ürünün görseli değiştirildiğinde diskte kalan eski dosyayı siler.
        /// Yeni görsel kaydedildikten SONRA çağrılmalıdır.
        ///
        /// Bilerek korunanlar (silinmez):
        ///  - Dış URL'ler (placehold.co gibi) ve boş değerler,
        ///  - <c>/img/urunler/</c> dışındaki yollar,
        ///  - Başka bir ürünün de kullandığı görseller (paylaşılan fotoğraflar).
        /// </summary>
        private void EskiResmiSil(string? eskiResimUrl)
        {
            if (string.IsNullOrWhiteSpace(eskiResimUrl)) return;
            if (!eskiResimUrl.StartsWith(ResimKlasoruYolu, StringComparison.OrdinalIgnoreCase)) return;

            // Aynı görseli kullanan başka bir ürün kaldıysa dosyaya dokunma.
            if (_context.Urunler.Any(u => u.ResimUrl == eskiResimUrl)) return;

            // Yol gezinmesine (../) karşı sadece dosya adını kullanıyoruz.
            string dosyaAdi = Path.GetFileName(eskiResimUrl);
            if (string.IsNullOrEmpty(dosyaAdi)) return;

            string tamYol = Path.Combine(_hostEnvironment.WebRootPath, "img", "urunler", dosyaAdi);

            try
            {
                if (System.IO.File.Exists(tamYol)) System.IO.File.Delete(tamYol);
            }
            catch (IOException ex)
            {
                // Dosya kilitliyse ürün güncellemesi yine de başarılı sayılmalı.
                _logger.LogWarning(ex, "Eski ürün görseli silinemedi: {Dosya}", tamYol);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Eski ürün görseli için silme yetkisi yok: {Dosya}", tamYol);
            }
        }

        // 1. ÜRÜN LİSTESİ
        public IActionResult Index(int? kategoriId)
        {
            KategorileriYukle(kategoriId);
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
            KategorileriYukle();

            // Veritabanındaki en büyük sıra numarasını bul (Kategori fark etmeksizin)
            int sonSira = _context.Urunler.Any() ? _context.Urunler.Max(x => x.SiraNo) : 0;

            // Varsayılan olarak sonuncunun bir fazlasını öneriyoruz
            return View(new Urun { SiraNo = sonSira + 1 });
        }

        // 3. EKLEME İŞLEMİ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ekle(Urun urun, IFormFile? ResimDosyasi)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            ModelState.Remove("Id");
            ModelState.Remove("Kategori");

            if (ModelState.IsValid)
            {
                if (ResimDosyasi != null)
                {
                    string? yol = ResimKaydet(ResimDosyasi);
                    if (yol == null)
                    {
                        KategorileriYukle(urun.KategoriId);
                        return View(urun);
                    }
                    urun.ResimUrl = yol;
                }
                else
                {
                    urun.ResimUrl = "https://placehold.co/600x400?text=Resim+Yok";
                }

                _context.Urunler.Add(urun);
                if (GuvenliKaydet("Ürün ekleme"))
                {
                    TempData["Mesaj"] = $"'{urun.Ad}' ürünü eklendi.";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError(string.Empty, "Ürün kaydedilemedi. Seçtiğiniz kategori silinmiş olabilir; lütfen kontrol edip tekrar deneyin.");
            }

            KategorileriYukle(urun.KategoriId);
            return View(urun);
        }

        // 4. DÜZENLEME SAYFASI
        public IActionResult Duzenle(int id)
        {
            var urun = _context.Urunler.Find(id);
            if (urun == null) return NotFound();
            KategorileriYukle(urun.KategoriId);
            return View(urun);
        }

        // 5. DÜZENLEME İŞLEMİ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Duzenle(Urun gelenUrun, IFormFile? ResimDosyasi)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            ModelState.Remove("Kategori");

            if (ModelState.IsValid)
            {
                var mevcutUrun = _context.Urunler.Find(gelenUrun.Id);
                if (mevcutUrun == null)
                {
                    TempData["Hata"] = "Düzenlenecek ürün bulunamadı.";
                    return RedirectToAction("Index");
                }

                mevcutUrun.Ad = gelenUrun.Ad;
                mevcutUrun.Aciklama = gelenUrun.Aciklama;
                mevcutUrun.Fiyat = gelenUrun.Fiyat;
                mevcutUrun.FiyatBuyuk = gelenUrun.FiyatBuyuk;
                mevcutUrun.FiyatTur = gelenUrun.FiyatTur;
                mevcutUrun.FiyatBuyukTur = gelenUrun.FiyatBuyukTur;
                mevcutUrun.KategoriId = gelenUrun.KategoriId;
                mevcutUrun.SiraNo = gelenUrun.SiraNo;

                // Görsel değiştiyse eski dosyanın yolunu kaydediyoruz; kayıt
                // başarılı olduktan sonra diskten temizlenecek. Daha önce eski
                // dosyalar sunucuda birikip yer kaplıyordu.
                string? eskiResim = null;
                if (ResimDosyasi != null)
                {
                    string? yol = ResimKaydet(ResimDosyasi);
                    if (yol == null)
                    {
                        KategorileriYukle(gelenUrun.KategoriId);
                        return View(gelenUrun);
                    }
                    eskiResim = mevcutUrun.ResimUrl;
                    mevcutUrun.ResimUrl = yol;
                }

                if (GuvenliKaydet("Ürün güncelleme"))
                {
                    if (eskiResim != null) EskiResmiSil(eskiResim);
                    TempData["Mesaj"] = $"'{mevcutUrun.Ad}' ürünü güncellendi.";
                    return RedirectToAction("Index");
                }

                ModelState.AddModelError(string.Empty, "Ürün kaydedilemedi. Seçtiğiniz kategori silinmiş olabilir; lütfen kontrol edip tekrar deneyin.");
            }

            KategorileriYukle(gelenUrun.KategoriId);
            return View(gelenUrun);
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

            var kayitlar = _context.Urunler.Where(x => sira.Contains(x.Id)).ToList();
            if (kayitlar.Count != sira.Length)
                return BadRequest(new { mesaj = "Sıralanacak kayıtlar bulunamadı." });

            // Bu kayıtların hâlihazırda sahip olduğu sıra numaraları havuzu
            var numaralar = kayitlar.Select(x => x.SiraNo).OrderBy(n => n).ToList();

            for (int i = 0; i < sira.Length; i++)
            {
                var kayit = kayitlar.First(x => x.Id == sira[i]);
                kayit.SiraNo = numaralar[i];
            }

            if (!GuvenliKaydet("Ürün sıralama"))
                return StatusCode(500, new { mesaj = "Sıralama kaydedilemedi." });

            return Ok(new { mesaj = "Sıralama kaydedildi." });
        }

        // 6. SİLME İŞLEMİ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int id)
        {
            var urun = _context.Urunler.Find(id);
            if (urun == null)
            {
                TempData["Hata"] = "Silinecek ürün bulunamadı.";
                return RedirectToAction("Index");
            }

            // Yolu kayıttan ÖNCE alıyoruz; Remove sonrası nesne izlenmiyor olabilir.
            string? silinecekResim = urun.ResimUrl;

            _context.Urunler.Remove(urun);
            if (GuvenliKaydet("Ürün silme"))
            {
                /* Ürün gidince görseli de gitmeli — aksi hâlde dosyalar
                   sunucuda birikiyordu. Kayıt BAŞARILI olduktan sonra
                   çağrılıyor: EskiResmiSil "bu görseli başka ürün de
                   kullanıyor mu" diye veritabanına bakıyor ve silinen satır
                   o sorguya artık dahil olmamalı. Paylaşılan görseller,
                   dış adresler ve klasör dışı yollar zaten korunuyor. */
                EskiResmiSil(silinecekResim);
                TempData["Mesaj"] = $"'{urun.Ad}' ürünü silindi.";
            }
            else
                TempData["Hata"] = $"'{urun.Ad}' ürünü silinemedi. Lütfen tekrar deneyin.";

            return RedirectToAction("Index");
        }
    }
}
