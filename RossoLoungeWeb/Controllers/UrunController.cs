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

        /// <summary>
        /// Ana sayfa vitrininde kaç tabak gösterilebilir.
        /// Sayı keyfî değil: ızgara CSS'te repeat(3, 1fr) ve ortadaki kart
        /// bilerek aşağı kaydırılıyor. Dördüncü tabak ikinci satırda tek
        /// başına kalır, ritim bozulur.
        /// </summary>
        private const int VitrinSiniri = 3;

        /// <summary>Vitrinde işaretli tabak sayısı; <paramref name="haricId"/> sayılmaz.</summary>
        private int VitrinDolulugu(int haricId = 0) =>
            _context.Urunler.Count(u => u.OneCikan && u.Id != haricId);

        // Kategori açılır listesi her action'da tek tek kuruluyordu; tek yere alındı.
        private void KategorileriYukle(int? seciliId = null)
        {
            var kategoriler = _context.Kategoriler.OrderBy(k => k.SiraNo).ToList();

            ViewBag.Kategoriler = new SelectList(kategoriler, "Id", "Ad", seciliId);

            // Düzenleme sayfasının başlığındaki bağlam rozeti. Listeden ayrıca
            // sorgulamıyoruz; kategoriler zaten elde.
            ViewBag.KategoriAdi = seciliId.HasValue
                ? kategoriler.FirstOrDefault(k => k.Id == seciliId.Value)?.Ad
                : null;
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
        /// <summary>
        /// Süzülebilir, sıralanabilir, sayfalanabilir ürün listesi.
        ///
        /// Eskiden bütün kalemler tek sayfada basılıyordu (canlıda 100+)
        /// ve arama tarayıcıda satır gizleyerek yapılıyordu: kayıtlar yine
        /// indiriliyordu ve "arama" yalnızca ekrandakini süzüyordu.
        /// </summary>
        public async Task<IActionResult> Index(
            int? kategoriId = null,
            string? ara = null,
            bool vitrin = false,
            string sirala = "menu",
            int sayfa = 1,
            int boyut = 25)
        {
            // Dışarıdan gelen değerler beyaz listeye çekiliyor: adres
            // çubuğuna yazılan rastgele bir değer sorguyu bozmasın.
            var gecerliSiralar = new[] { "menu", "ad", "fiyat_artan", "fiyat_azalan", "yeni" };
            if (!gecerliSiralar.Contains(sirala)) sirala = "menu";

            if (boyut != 25 && boyut != 50 && boyut != 100) boyut = 25;
            if (sayfa < 1) sayfa = 1;
            if (kategoriId is <= 0) kategoriId = null;

            var suzgec = new UrunSuzgeci
            {
                KategoriId = kategoriId,
                Ara = string.IsNullOrWhiteSpace(ara) ? null : ara.Trim(),
                Vitrin = vitrin,
                Sirala = sirala,
                Sayfa = sayfa,
                Boyut = boyut
            };

            var model = new UrunListeModeli
            {
                Suzgec = suzgec,
                VitrinSiniri = VitrinSiniri
            };

            // Süzgeç açılır listesi menü sırasında; ürün sayıları rozetlerde.
            model.Kategoriler = await _context.Kategoriler.OrderBy(k => k.SiraNo).ToListAsync();
            model.SeciliKategoriAdi = kategoriId.HasValue
                ? model.Kategoriler.FirstOrDefault(k => k.Id == kategoriId.Value)?.Ad
                : null;

            // Kategori silinmiş ya da uydurma bir id gelmişse süzgeci düşür:
            // aksi hâlde "0 ürün" gösterip sebebini söylemeyen bir ekran çıkıyor.
            if (kategoriId.HasValue && model.SeciliKategoriAdi == null)
            {
                suzgec.KategoriId = null;
                kategoriId = null;
            }

            model.TumKayit = await _context.Urunler.CountAsync();
            model.VitrinSayisi = await _context.Urunler.CountAsync(u => u.OneCikan);

            IQueryable<Urun> sorgu = _context.Urunler.Include(u => u.Kategori);

            if (kategoriId.HasValue)
                sorgu = sorgu.Where(u => u.KategoriId == kategoriId.Value);

            if (vitrin)
                sorgu = sorgu.Where(u => u.OneCikan);

            if (suzgec.Ara != null)
            {
                var kalip = suzgec.Ara;
                sorgu = sorgu.Where(u =>
                    EF.Functions.Like(u.Ad, "%" + kalip + "%") ||
                    (u.Aciklama != null && EF.Functions.Like(u.Aciklama, "%" + kalip + "%")));
            }

            model.ToplamKayit = await sorgu.CountAsync();
            model.ToplamSayfa = Math.Max(1, (int)Math.Ceiling(model.ToplamKayit / (double)boyut));
            if (suzgec.Sayfa > model.ToplamSayfa) suzgec.Sayfa = model.ToplamSayfa;

            // VARSAYILAN SIRA MENÜNÜN SIRASI. Eskiden kategoriler ADA göre
            // diziliyordu (OrderBy(Kategori.Ad)); site ise kategorileri
            // SiraNo'ya göre basıyor. Panelde gördüğün sıra ile misafirin
            // gördüğü sıra birbirini tutmuyordu.
            sorgu = sirala switch
            {
                "ad" => sorgu.OrderBy(u => u.Ad),
                "fiyat_artan" => sorgu.OrderBy(u => u.Fiyat),
                "fiyat_azalan" => sorgu.OrderByDescending(u => u.Fiyat),
                "yeni" => sorgu.OrderByDescending(u => u.Id),
                _ => sorgu.OrderBy(u => u.Kategori.SiraNo).ThenBy(u => u.SiraNo)
            };

            model.Kayitlar = await sorgu
                .Skip((suzgec.Sayfa - 1) * boyut)
                .Take(boyut)
                .ToListAsync();

            return View(model);
        }

        /// <summary>
        /// İşlem sonrası kullanıcıyı GELDİĞİ süzgeç/sayfaya döndürür; aksi
        /// hâlde 3. sayfadaki bir ürünü silen kişi listenin başına düşüyor.
        /// <c>Url.IsLocalUrl</c> şart: adres formdan geliyor, denetlenmezse
        /// açık yönlendirme (open redirect) açığı olur.
        /// </summary>
        private IActionResult ListeyeDon(string? donus)
        {
            if (!string.IsNullOrWhiteSpace(donus) && Url.IsLocalUrl(donus))
                return Redirect(donus);

            return RedirectToAction("Index");
        }

        // 2. EKLEME SAYFASI
        // Sıra numarası artık formda sorulmuyor (bkz. Ekle POST).
        public IActionResult Ekle()
        {
            KategorileriYukle();
            return View(new Urun());
        }

        // 3. EKLEME İŞLEMİ (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Ekle(Urun urun, IFormFile? ResimDosyasi)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            ModelState.Remove("Id");
            ModelState.Remove("Kategori");

            // Vitrin üç tabak alıyor; dördüncüsü sessizce kaybolmasın.
            if (urun.OneCikan && VitrinDolulugu() >= VitrinSiniri)
            {
                ModelState.AddModelError(nameof(Urun.OneCikan),
                    $"Vitrinde zaten {VitrinSiniri} tabak var. Önce Ürünler listesinden birinin yıldızını kapatın.");
            }

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
                    // Eskiden buraya "https://placehold.co/..." yazılıyordu.
                    // O adres UrunGorseli.Gecerli tarafından zaten reddediliyor
                    // (dış adres) — yani veritabanına hiç kullanılmayan bir
                    // değer kaydediliyordu. Görsel yoksa alan boş kalsın.
                    urun.ResimUrl = null;
                }

                // SIRA NO'YU SUNUCU VERİYOR. Formdaki sayı kutusu kaldırıldı:
                // sıralamanın iki sahibi (elle numara + listedeki sürükle-bırak)
                // birbirinden habersizdi. Yeni ürün sona ekleniyor; sırası
                // listede, kendi kategorisi seçiliyken sürüklenerek değişiyor.
                urun.SiraNo = _context.Urunler.Any() ? _context.Urunler.Max(x => x.SiraNo) + 1 : 1;

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

            // Yalnızca AÇILIRKEN bakılıyor: zaten işaretli bir ürünün
            // adını değiştirmek sınıra takılmamalı (kendisi hariç sayılıyor).
            if (gelenUrun.OneCikan && VitrinDolulugu(gelenUrun.Id) >= VitrinSiniri)
            {
                ModelState.AddModelError(nameof(Urun.OneCikan),
                    $"Vitrinde zaten {VitrinSiniri} tabak var. Önce Ürünler listesinden birinin yıldızını kapatın.");
            }

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
                mevcutUrun.OneCikan = gelenUrun.OneCikan;

                // SIRAYA DOKUNULMUYOR. Form artık SiraNo göndermiyor; gövdeden
                // gelmeyen alan modelde 0 olur ve buraya yazılsaydı ürün kendi
                // kategorisinin en başına fırlardı. Sıra SiraGuncelle'nin işi.

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
                        gelenUrun.ResimUrl = mevcutUrun.ResimUrl; // önizleme kaybolmasın
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

            // Form ResimUrl göndermiyor; hata sayfasında "mevcut görsel"
            // önizlemesi kaybolup görsel silinmiş gibi görünüyordu.
            // Alt başlık da Model.Ad'den geliyor: ad boş bırakıldığında
            // hangi ürünün düzenlendiği kayboluyordu. İkisi de yalnızca
            // gösterim için kayıttan tazeleniyor.
            var kayit = _context.Urunler
                .Where(u => u.Id == gelenUrun.Id)
                .Select(u => new { u.Ad, u.ResimUrl })
                .FirstOrDefault();

            if (kayit != null)
            {
                if (string.IsNullOrEmpty(gelenUrun.ResimUrl)) gelenUrun.ResimUrl = kayit.ResimUrl;
                ViewBag.MevcutAd = kayit.Ad;
            }

            return View(gelenUrun);
        }


        // --- VİTRİN İŞARETİ ---
        /// <summary>
        /// Ana sayfadaki vitrine ekler/çıkarır. Listeden tek tıkla
        /// yapılıyor: üç tabağı seçmek için üç düzenleme formu açmak
        /// gereksiz. Düzenleme formunda da aynı işaret var.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Vitrin(int id, string? donus = null)
        {
            var urun = _context.Urunler.Find(id);
            if (urun == null)
            {
                TempData["Hata"] = "Ürün bulunamadı.";
                return ListeyeDon(donus);
            }

            // Çıkarmak her zaman serbest; sınır yalnızca eklemede.
            if (!urun.OneCikan && VitrinDolulugu(id) >= VitrinSiniri)
            {
                TempData["Hata"] = $"Vitrinde en fazla {VitrinSiniri} tabak olabilir. " +
                                   "Yeni bir tabak eklemek için önce birinin yıldızını kapatın.";
                return ListeyeDon(donus);
            }

            urun.OneCikan = !urun.OneCikan;

            if (GuvenliKaydet("Vitrin işareti"))
            {
                TempData["Mesaj"] = urun.OneCikan
                    ? $"'{urun.Ad}' ana sayfadaki vitrine eklendi."
                    : $"'{urun.Ad}' vitrinden çıkarıldı.";
            }
            else
            {
                TempData["Hata"] = "İşaret kaydedilemedi. Lütfen tekrar deneyin.";
            }

            return ListeyeDon(donus);
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
        public IActionResult Sil(int id, string? donus = null)
        {
            var urun = _context.Urunler.Find(id);
            if (urun == null)
            {
                TempData["Hata"] = "Silinecek ürün bulunamadı.";
                return ListeyeDon(donus);
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

            return ListeyeDon(donus);
        }
    }
}
