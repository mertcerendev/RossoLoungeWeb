using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // DbUpdateException için
using RossoLoungeWeb.Data;
using RossoLoungeWeb.Models;
using RossoLoungeWeb.Services;
using System.Net;
using System.Net.Mail; // Mail gönderme kütüphanesi
using BCrypt.Net; // Şifreleme kütüphanesi
using System.Security.Cryptography; // Kriptografik rastgele şifre üretimi için
using System.Text;
using System.Globalization;

namespace RossoLoungeWeb.Controllers
{
    // Yetki kontrolü ASP.NET Core Identity'de. Giriş ve şifre sıfırlama
    // sayfaları [AllowAnonymous] ile muaf; aksi halde giriş sayfası sonsuz
    // yönlendirmeye girer.
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly UserManager<IdentityUser> _kullaniciYoneticisi;
        private readonly SignInManager<IdentityUser> _girisYoneticisi;

        private readonly AyarKorumasi _ayarKorumasi;

        public AdminController(
            ApplicationDbContext context,
            ILogger<AdminController> logger,
            UserManager<IdentityUser> kullaniciYoneticisi,
            SignInManager<IdentityUser> girisYoneticisi,
            AyarKorumasi ayarKorumasi)
        {
            _context = context;
            _logger = logger;
            _kullaniciYoneticisi = kullaniciYoneticisi;
            _girisYoneticisi = girisYoneticisi;
            _ayarKorumasi = ayarKorumasi;
        }

        // Uygulama geneli invariant kültürle çalışıyor; grafik etiketlerinde
        // ay adları Türkçe görünsün diye burada açıkça belirtiyoruz.
        private static readonly CultureInfo TurkceKultur = new CultureInfo("tr-TR");

        // --- YARDIMCI: VERİTABANI KAYDI (korumalı) ---
        // SaveChanges korumasızdı; veritabanı hatası kullanıcıya ham 500 sayfası
        // olarak dönüyordu. Artık hata loglanıyor ve çağıran anlamlı mesaj gösterebiliyor.
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

        // --- YARDIMCI: GÜVENLİ ŞİFRE ÜRETİCİ ---
        // System.Random kriptografik değildir ve tahmin edilebilir; şifre sıfırlama
        // için RandomNumberGenerator kullanılıyor.
        private static string RandomSifreUret(int uzunluk = 10)
        {
            const string karakterler = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var result = new StringBuilder(uzunluk);

            for (int i = 0; i < uzunluk; i++)
            {
                result.Append(karakterler[RandomNumberGenerator.GetInt32(karakterler.Length)]);
            }
            return result.ToString();
        }

        // --- YARDIMCI: MAIL GÖNDERME ---
        // Eskiden "catch { return false; }" ile hata yutuluyordu; yönetici mailin
        // neden gitmediğini asla öğrenemiyordu. Artık sebep loglanıyor ve
        // kullanıcıya gösterilebilecek anlamlı bir mesaj dönüyor.
        private (bool Basarili, string Mesaj) MailGonder(string kime, string konu, string icerik)
        {
            var ayar = _context.Ayarlar.FirstOrDefault();
            if (ayar == null || string.IsNullOrEmpty(ayar.GonderenMail))
            {
                _logger.LogWarning("Mail gönderilemedi: sistem mail ayarları tanımlı değil.");
                return (false, "Sistem mail ayarları tanımlı değil. Panel > Mail Ayarları bölümünden gönderen adresi ve SMTP bilgilerini girin.");
            }

            try
            {
                using var client = new SmtpClient(ayar.SmtpSunucu, ayar.SmtpPort)
                {
                    EnableSsl = true,
                    // Şifre veritabanında şifreli duruyor; burada çözülüyor.
                    Credentials = new NetworkCredential(ayar.GonderenMail, _ayarKorumasi.Coz(ayar.GonderenSifre))
                };

                using var mail = new MailMessage
                {
                    From = new MailAddress(ayar.GonderenMail, "Rosso Lounge Panel"),
                    Subject = konu,
                    Body = icerik,
                    IsBodyHtml = true
                };
                mail.To.Add(kime);

                client.Send(mail);
                return (true, string.Empty);
            }
            catch (SmtpFailedRecipientException ex)
            {
                _logger.LogError(ex, "Mail alıcıya ulaşmadı. Alıcı: {Kime}", kime);
                return (false, "Mail alıcıya ulaştırılamadı. Adresin doğru yazıldığından emin olun.");
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP hatası. Sunucu: {Sunucu}:{Port}, Durum: {Durum}",
                    ayar.SmtpSunucu, ayar.SmtpPort, ex.StatusCode);
                return (false, $"Mail sunucusuna bağlanılamadı ({ex.StatusCode}). SMTP sunucusu, port ve uygulama şifresi bilgilerini kontrol edin.");
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Geçersiz mail adresi. Gönderen: {Gonderen}, Alıcı: {Kime}", ayar.GonderenMail, kime);
                return (false, "Gönderen veya alıcı e-posta adresi geçersiz.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mail gönderilirken beklenmeyen hata. Alıcı: {Kime}", kime);
                return (false, "Mail gönderilirken beklenmeyen bir hata oluştu. Sunucu kayıtlarını kontrol edin.");
            }
        }

        // --- 1. GİRİŞ İŞLEMLERİ ---
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginYap(GirisModeli model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Hata = "Kullanıcı adı ve şifre zorunludur.";
                return View("Login", model);
            }

            // Yönetici kullanıcı adıyla giriyor; e-posta yazanlar da
            // takılmasın diye ikinci bir arama yapılıyor.
            var kullanici = await _kullaniciYoneticisi.FindByNameAsync(model.KullaniciAdi)
                         ?? await _kullaniciYoneticisi.FindByEmailAsync(model.KullaniciAdi);

            if (kullanici?.UserName != null)
            {
                // lockoutOnFailure: hatalı denemeler sayılır, 5'te hesap
                // 15 dakika kilitlenir (Program.cs). Daha önce kaba kuvvete
                // karşı hiçbir koruma yoktu.
                var sonuc = await _girisYoneticisi.PasswordSignInAsync(
                    kullanici.UserName, model.Sifre, model.BeniHatirla, lockoutOnFailure: true);

                if (sonuc.Succeeded)
                {
                    _logger.LogInformation("Panel girişi başarılı. Kullanıcı: {Kullanici}, IP: {IP}",
                        kullanici.UserName, HttpContext.Connection.RemoteIpAddress);
                    return RedirectToAction("Index");
                }

                if (sonuc.IsLockedOut)
                {
                    _logger.LogWarning("Kilitli hesapta giriş denemesi. Kullanıcı: {Kullanici}, IP: {IP}",
                        kullanici.UserName, HttpContext.Connection.RemoteIpAddress);

                    ViewBag.Hata = "Çok fazla hatalı deneme yapıldı. Hesap 15 dakika kilitlendi.";
                    return View("Login", new GirisModeli { KullaniciAdi = model.KullaniciAdi });
                }
            }

            // Başarısız denemeler kaydediliyor; sunucu kayıtlarından kaba kuvvet
            // saldırısı fark edilebilsin. (Şifre asla loglanmaz.)
            _logger.LogWarning("Başarısız panel giriş denemesi. Kullanıcı adı: {Kullanici}, IP: {IP}",
                model.KullaniciAdi, HttpContext.Connection.RemoteIpAddress);

            // Mesaj bilinçli olarak tek ve genel: "kullanıcı yok" ile
            // "şifre yanlış" ayrımı geçerli kullanıcı adlarını sızdırır.
            ViewBag.Hata = "Kullanıcı adı veya şifre hatalı!";
            return View("Login", new GirisModeli { KullaniciAdi = model.KullaniciAdi });
        }

        // --- 2. DASHBOARD (ÖZET) ---
        public async Task<IActionResult> Index()
        {
            var simdi = TurkiyeSaati.Simdi;
            var bugun = simdi.Date;

            var model = new OzetModeli
            {
                Kullanici = User.Identity?.Name ?? "Yönetici",
                Simdi = simdi,
                // Çalışma saatleri 11.30 – 00.00; gece yarısına sarktığı için
                // "saat >= açılış" tek başına yetiyor.
                SuAnAcik = simdi.TimeOfDay >= new TimeSpan(11, 30, 0)
            };

            /* ---------------------------------------------------------
               BEKLEYEN İŞLER + TOPLAMLAR
               --------------------------------------------------------- */
            // Operasyonel sayılar iptalleri dışlıyor: "bugün 20 kişi geliyor"
            // dendiğinde iptal edilenler o 20'nin içinde olmamalı.
            model.ToplamRezervasyon = await _context.Rezervasyons
                .CountAsync(r => r.Durum != RezervasyonDurumu.Iptal);
            model.BekleyenRezervasyon = await _context.Rezervasyons
                .CountAsync(r => r.Durum == RezervasyonDurumu.Bekliyor);
            model.OkunmamisMesaj = await _context.IletisimMesajlari.CountAsync(m => !m.OkunduMu);
            model.ToplamMesaj = await _context.IletisimMesajlari.CountAsync();
            model.BekleyenYorum = await _context.Yorumlar.CountAsync(y => !y.OnaylandiMi);
            model.ToplamYorum = await _context.Yorumlar.CountAsync();
            model.ToplamUrun = await _context.Urunler.CountAsync();
            model.ToplamKategori = await _context.Kategoriler.CountAsync();
            model.BultenAbone = await _context.BultenAboneleri.CountAsync();

            /* ---------------------------------------------------------
               BUGÜNÜN SERVİSİ
               Sabah panele bakan kişinin asıl aradığı liste: bugün kim
               geliyor, saat kaçta, kaç kişi, onaylı mı.
               --------------------------------------------------------- */
            model.BugunListe = await _context.Rezervasyons
                .Where(r => r.Tarih.Date == bugun && r.Durum != RezervasyonDurumu.Iptal)
                .OrderBy(r => r.Tarih)
                .ToListAsync();

            model.BugunRezervasyon = model.BugunListe.Count;
            model.BugunKisi = model.BugunListe.Sum(r => r.KisiSayisi);

            /* ---------------------------------------------------------
               HAFTA KARŞILAŞTIRMASI
               Tek bir sayı bağlamsızdır: "12 rezervasyon" iyi mi kötü mü
               belli değil. Geçen haftayla kıyas anlam veriyor.
               --------------------------------------------------------- */
            var haftaBasi = bugun.AddDays(-6);
            var oncekiHaftaBasi = bugun.AddDays(-13);

            model.BuHaftaRezervasyon = await _context.Rezervasyons
                .CountAsync(r => r.Tarih.Date >= haftaBasi && r.Tarih.Date <= bugun
                                 && r.Durum != RezervasyonDurumu.Iptal);
            model.GecenHaftaRezervasyon = await _context.Rezervasyons
                .CountAsync(r => r.Tarih.Date >= oncekiHaftaBasi && r.Tarih.Date < haftaBasi
                                 && r.Durum != RezervasyonDurumu.Iptal);

            /* ---------------------------------------------------------
               GRAFİK — dört dönem, iki sorgu
               Eskiden yalnızca 7 gün vardı ve her gün için AYRI bir COUNT
               sorgusu atılıyordu (N+1). Şimdi günlük ve aylık kırılım birer
               sorguyla alınıp bellekte dönemlere dağıtılıyor.
               --------------------------------------------------------- */
            var otuzGunOnce = bugun.AddDays(-29);

            var gunlukHam = await _context.Rezervasyons
                .Where(r => r.Tarih.Date >= otuzGunOnce && r.Tarih.Date <= bugun
                            && r.Durum != RezervasyonDurumu.Iptal)
                .GroupBy(r => r.Tarih.Date)
                .Select(g => new { Tarih = g.Key, Sayi = g.Count(), Kisi = g.Sum(x => x.KisiSayisi) })
                .ToListAsync();

            var gunlukHarita = gunlukHam.ToDictionary(x => x.Tarih, x => new { x.Sayi, x.Kisi });

            List<GrafikNoktasi> GunSerisi(int gunSayisi)
            {
                var seri = new List<GrafikNoktasi>();
                for (int i = gunSayisi - 1; i >= 0; i--)
                {
                    var tarih = bugun.AddDays(-i);
                    gunlukHarita.TryGetValue(tarih, out var deger);
                    seri.Add(new GrafikNoktasi
                    {
                        // 30 günlük seride 30 etiket sığmıyor: yalnızca gün numarası.
                        Etiket = tarih.ToString(gunSayisi > 10 ? "dd" : "dd MMM", TurkceKultur),
                        TamEtiket = tarih.ToString("d MMMM yyyy, dddd", TurkceKultur),
                        Rezervasyon = deger?.Sayi ?? 0,
                        Kisi = deger?.Kisi ?? 0
                    });
                }
                return seri;
            }

            model.Grafik.Gun7 = GunSerisi(7);
            model.Grafik.Gun30 = GunSerisi(30);

            var aylikHam = await _context.Rezervasyons
                .Where(r => r.Durum != RezervasyonDurumu.Iptal)
                .GroupBy(r => new { r.Tarih.Year, r.Tarih.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Sayi = g.Count(), Kisi = g.Sum(x => x.KisiSayisi) })
                .ToListAsync();

            model.Grafik.Yillar = aylikHam.Select(a => a.Year).Distinct().OrderByDescending(y => y).ToList();
            if (model.Grafik.Yillar.Count == 0) model.Grafik.Yillar.Add(bugun.Year);
            model.Grafik.VarsayilanYil = model.Grafik.Yillar.Contains(bugun.Year)
                ? bugun.Year
                : model.Grafik.Yillar[0];

            foreach (var yil in model.Grafik.Yillar)
            {
                var aylar = new List<GrafikNoktasi>();
                for (int ay = 1; ay <= 12; ay++)
                {
                    var kayit = aylikHam.FirstOrDefault(a => a.Year == yil && a.Month == ay);
                    aylar.Add(new GrafikNoktasi
                    {
                        Etiket = new DateTime(yil, ay, 1).ToString("MMM", TurkceKultur),
                        TamEtiket = new DateTime(yil, ay, 1).ToString("MMMM yyyy", TurkceKultur),
                        Rezervasyon = kayit?.Sayi ?? 0,
                        Kisi = kayit?.Kisi ?? 0
                    });
                }
                model.Grafik.Aylik[yil] = aylar;
            }

            model.Grafik.Yillik = aylikHam
                .GroupBy(a => a.Year)
                .OrderBy(g => g.Key)
                .Select(g => new GrafikNoktasi
                {
                    Etiket = g.Key.ToString(),
                    TamEtiket = g.Key + " yılı toplamı",
                    Rezervasyon = g.Sum(x => x.Sayi),
                    Kisi = g.Sum(x => x.Kisi)
                })
                .ToList();

            /* ---------------------------------------------------------
               YAKLAŞAN 8 GÜN — ileriye dönük planlama
               --------------------------------------------------------- */
            var yediGunSonra = bugun.AddDays(7);
            var yaklasanHam = await _context.Rezervasyons
                .Where(r => r.Tarih.Date >= bugun && r.Tarih.Date <= yediGunSonra
                            && r.Durum != RezervasyonDurumu.Iptal)
                .GroupBy(r => r.Tarih.Date)
                .Select(g => new { Tarih = g.Key, Sayi = g.Count(), Kisi = g.Sum(x => x.KisiSayisi) })
                .ToListAsync();

            for (int i = 0; i <= 7; i++)
            {
                var tarih = bugun.AddDays(i);
                var kayit = yaklasanHam.FirstOrDefault(y => y.Tarih == tarih);
                model.YaklasanGunler.Add(new GunOzeti
                {
                    Tarih = tarih,
                    GunAdi = i == 0 ? "Bugün" : (i == 1 ? "Yarın" : tarih.ToString("ddd", TurkceKultur)),
                    Rezervasyon = kayit?.Sayi ?? 0,
                    Kisi = kayit?.Kisi ?? 0,
                    Bugun = i == 0
                });
            }

            /* ---------------------------------------------------------
               YOĞUN SAATLER — personel planlaması için
               --------------------------------------------------------- */
            model.YogunSaatler = (await _context.Rezervasyons
                .Where(r => r.Durum != RezervasyonDurumu.Iptal)
                .GroupBy(r => r.Tarih.Hour)
                .Select(g => new { Saat = g.Key, Sayi = g.Count(), Kisi = g.Sum(x => x.KisiSayisi) })
                .ToListAsync())
                .OrderBy(x => x.Saat)
                .Select(x => new SaatDilimi { Saat = x.Saat, Rezervasyon = x.Sayi, Kisi = x.Kisi })
                .ToList();

            /* ---------------------------------------------------------
               MEMNUNİYET
               Ortalama yalnızca YAYINDAKİ yorumlardan: sitede görünen puan
               neyse panelde de o görünmeli.
               --------------------------------------------------------- */
            var puanlar = await _context.Yorumlar
                .Where(y => y.OnaylandiMi)
                .Select(y => y.Puan)
                .ToListAsync();

            if (puanlar.Count > 0)
            {
                model.OrtalamaPuan = Math.Round(puanlar.Average(), 1);
                foreach (var p in puanlar)
                {
                    if (p >= 1 && p <= 5) model.PuanDagilimi[p - 1]++;
                }
            }

            model.SonYorumlar = await _context.Yorumlar
                .OrderByDescending(y => y.Tarih)
                .Take(3)
                .ToListAsync();

            /* ---------------------------------------------------------
               SON HAREKETLER — dört kaynak tek akışta
               --------------------------------------------------------- */
            var sonRezervasyonlar = await _context.Rezervasyons
                .OrderByDescending(r => r.OlusturulmaTarihi).Take(5)
                .Select(r => new Hareket
                {
                    Tarih = r.OlusturulmaTarihi,
                    Tur = "rezervasyon",
                    Ikon = "fa-calendar-check",
                    Baslik = r.AdSoyad + " masa ayırttı",
                    Ayrinti = r.KisiSayisi + " kişi",
                    Adres = "/Admin/Rezervasyonlar"
                }).ToListAsync();

            var sonMesajlar = await _context.IletisimMesajlari
                .OrderByDescending(m => m.Tarih).Take(5)
                .Select(m => new Hareket
                {
                    Tarih = m.Tarih,
                    Tur = "mesaj",
                    Ikon = "fa-envelope",
                    Baslik = m.AdSoyad + " mesaj gönderdi",
                    Ayrinti = null,
                    Adres = "/Admin/MesajOku/" + m.Id
                }).ToListAsync();

            var sonYorumHareket = await _context.Yorumlar
                .OrderByDescending(y => y.Tarih).Take(5)
                .Select(y => new Hareket
                {
                    Tarih = y.Tarih,
                    Tur = "yorum",
                    Ikon = "fa-star-half-stroke",
                    Baslik = y.AdSoyad + " yorum bıraktı",
                    Ayrinti = y.Puan + " puan",
                    Adres = "/Admin/Yorumlar"
                }).ToListAsync();

            var sonAboneler = await _context.BultenAboneleri
                .OrderByDescending(b => b.Tarih).Take(5)
                .Select(b => new Hareket
                {
                    Tarih = b.Tarih,
                    Tur = "bulten",
                    Ikon = "fa-paper-plane",
                    Baslik = "Yeni bülten aboneliği",
                    Ayrinti = b.Email,
                    Adres = null
                }).ToListAsync();

            model.SonHareketler = sonRezervasyonlar
                .Concat(sonMesajlar).Concat(sonYorumHareket).Concat(sonAboneler)
                .OrderByDescending(h => h.Tarih)
                .Take(7)
                .ToList();

            /* ---------------------------------------------------------
               VERİ KALİTESİ UYARILARI
               Menüde sessizce bozulan şeyleri yöneticiye söyler; her biri
               tıklanabilir ve doğrudan düzeltileceği ekrana götürür.
               --------------------------------------------------------- */
            var gorselsiz = (await _context.Urunler.Select(u => u.ResimUrl).ToListAsync())
                .Count(r => !UrunGorseli.Gecerli(r));

            if (gorselsiz > 0)
            {
                model.Uyarilar.Add(new Uyari
                {
                    Metin = gorselsiz + " ürünün görseli yok ya da geçersiz.",
                    Adres = "/Urun/Index",
                    Eylem = "Ürünlere git"
                });
            }

            var bosKategori = await _context.Kategoriler.CountAsync(k => !k.Urunler.Any());
            if (bosKategori > 0)
            {
                model.Uyarilar.Add(new Uyari
                {
                    Metin = bosKategori + " kategori boş — menüde görünmüyor.",
                    Adres = "/Kategori/Index",
                    Eylem = "Kategorilere git"
                });
            }

            var aciklamasiz = await _context.Urunler
                .CountAsync(u => u.Aciklama == null || u.Aciklama == "");
            if (aciklamasiz > 0)
            {
                model.Uyarilar.Add(new Uyari
                {
                    Metin = aciklamasiz + " ürünün açıklaması boş.",
                    Adres = "/Urun/Index",
                    Eylem = "Ürünlere git"
                });
            }

            return View(model);
        }

        // --- 3. REZERVASYON LİSTESİ ---
        /// <summary>
        /// Süzülebilir, sıralanabilir, sayfalanabilir rezervasyon listesi.
        ///
        /// Eskiden tüm kayıtlar tek sayfada basılıyordu; birkaç yüz
        /// rezervasyondan sonra sayfa hem yavaşlar hem taranamaz hâle gelir.
        /// Süzme ve sayfalama SUNUCUDA yapılıyor — tarayıcıda gizlemek
        /// kayıtların yine de indirilmesi demekti.
        /// </summary>
        public async Task<IActionResult> Rezervasyonlar(
            string durum = "yaklasan",
            string? ara = null,
            DateTime? bas = null,
            DateTime? bit = null,
            string sirala = "",
            int sayfa = 1,
            int boyut = 25)
        {
            var bugun = TurkiyeSaati.Bugun;

            // Dışarıdan gelen değerler beyaz listeye çekiliyor: adres
            // çubuğuna yazılan rastgele bir değer sorguyu bozmasın.
            var gecerliDurumlar = new[] { "yaklasan", "bugun", "bekleyen", "gecmis", "iptal", "tumu" };
            if (!gecerliDurumlar.Contains(durum)) durum = "yaklasan";

            var gecerliSiralar = new[] { "tarih_artan", "tarih_azalan", "kisi_azalan", "talep_azalan" };
            // Varsayılan sıra sekmeye göre: geçmişte en yeni üstte, ileriye
            // dönük listelerde en yakın tarih üstte olmalı.
            if (!gecerliSiralar.Contains(sirala))
                sirala = durum == "gecmis" ? "tarih_azalan" : "tarih_artan";

            if (boyut != 25 && boyut != 50 && boyut != 100) boyut = 25;
            if (sayfa < 1) sayfa = 1;

            var suzgec = new RezervasyonSuzgeci
            {
                Durum = durum,
                Ara = string.IsNullOrWhiteSpace(ara) ? null : ara.Trim(),
                Bas = bas,
                Bit = bit,
                Sirala = sirala,
                Sayfa = sayfa,
                Boyut = boyut
            };

            var model = new RezervasyonListeModeli { Suzgec = suzgec };

            // Sekme rozetleri — kullanıcı sekmeye geçmeden kaç kayıt
            // olduğunu görsün.
            // İptal edilen rezervasyon gerçekleşmeyecek: yaklaşan/bugün/geçmiş
            // sekmelerinde masa işgal ediyormuş gibi görünmemeli. Kendi
            // sekmesinde duruyor ki kayıt kaybolmasın.
            model.SayiTumu = await _context.Rezervasyons.CountAsync();
            model.SayiYaklasan = await _context.Rezervasyons
                .CountAsync(r => r.Tarih.Date >= bugun && r.Durum != RezervasyonDurumu.Iptal);
            model.SayiBugun = await _context.Rezervasyons
                .CountAsync(r => r.Tarih.Date == bugun && r.Durum != RezervasyonDurumu.Iptal);
            model.SayiBekleyen = await _context.Rezervasyons
                .CountAsync(r => r.Durum == RezervasyonDurumu.Bekliyor);
            model.SayiGecmis = await _context.Rezervasyons
                .CountAsync(r => r.Tarih.Date < bugun && r.Durum != RezervasyonDurumu.Iptal);
            model.SayiIptal = await _context.Rezervasyons
                .CountAsync(r => r.Durum == RezervasyonDurumu.Iptal);

            IQueryable<Rezervasyon> sorgu = _context.Rezervasyons;

            sorgu = durum switch
            {
                "bugun" => sorgu.Where(r => r.Tarih.Date == bugun && r.Durum != RezervasyonDurumu.Iptal),
                // Onay bekleyenler tarihten bağımsız: geçmişte kalmış ama
                // hiç yanıtlanmamış bir talep de yöneticinin işidir.
                "bekleyen" => sorgu.Where(r => r.Durum == RezervasyonDurumu.Bekliyor),
                "gecmis" => sorgu.Where(r => r.Tarih.Date < bugun && r.Durum != RezervasyonDurumu.Iptal),
                "iptal" => sorgu.Where(r => r.Durum == RezervasyonDurumu.Iptal),
                "tumu" => sorgu,
                _ => sorgu.Where(r => r.Tarih.Date >= bugun && r.Durum != RezervasyonDurumu.Iptal)
            };

            if (suzgec.Ara != null)
            {
                var kalip = suzgec.Ara;
                sorgu = sorgu.Where(r =>
                    EF.Functions.Like(r.AdSoyad, "%" + kalip + "%") ||
                    EF.Functions.Like(r.Telefon, "%" + kalip + "%") ||
                    (r.Not != null && EF.Functions.Like(r.Not, "%" + kalip + "%")));
            }

            if (bas.HasValue) sorgu = sorgu.Where(r => r.Tarih.Date >= bas.Value.Date);
            if (bit.HasValue) sorgu = sorgu.Where(r => r.Tarih.Date <= bit.Value.Date);

            // Toplamlar süzgece göre, SAYFAYA göre değil: "kaç kişi
            // bekleniyor" sorusunun cevabı sayfa 2'de değişmemeli.
            model.ToplamKayit = await sorgu.CountAsync();
            model.ToplamKisi = model.ToplamKayit == 0 ? 0 : await sorgu.SumAsync(r => r.KisiSayisi);

            model.ToplamSayfa = Math.Max(1, (int)Math.Ceiling(model.ToplamKayit / (double)boyut));
            if (suzgec.Sayfa > model.ToplamSayfa) suzgec.Sayfa = model.ToplamSayfa;

            sorgu = sirala switch
            {
                "tarih_azalan" => sorgu.OrderByDescending(r => r.Tarih),
                "kisi_azalan" => sorgu.OrderByDescending(r => r.KisiSayisi).ThenBy(r => r.Tarih),
                "talep_azalan" => sorgu.OrderByDescending(r => r.OlusturulmaTarihi),
                _ => sorgu.OrderBy(r => r.Tarih)
            };

            model.Kayitlar = await sorgu
                .Skip((suzgec.Sayfa - 1) * boyut)
                .Take(boyut)
                .ToListAsync();

            return View(model);
        }

        // --- 4. ONAYLAMA & SİLME ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Onayla(int id, string? donus = null)
        {
            var rez = _context.Rezervasyons.Find(id);
            if (rez == null)
            {
                TempData["Hata"] = "Onaylanacak rezervasyon bulunamadı.";
                return ListeyeDon(donus);
            }

            rez.Durum = RezervasyonDurumu.Onaylandi;
            if (GuvenliKaydet("Rezervasyon onaylama"))
                TempData["Mesaj"] = $"{rez.AdSoyad} adına rezervasyon onaylandı.";
            else
                TempData["Hata"] = "Rezervasyon onaylanamadı. Lütfen tekrar deneyin.";

            return ListeyeDon(donus);
        }

        /// <summary>
        /// Talebi geri çevirir ya da onaylı bir rezervasyonu iptal eder.
        ///
        /// Eskiden bunun tek yolu kaydı SİLMEKTİ; silinen kayıttan geriye iz
        /// kalmadığı için "bu isim daha önce gelmiş miydi, iptal mi etmişti"
        /// sorusu cevapsız kalıyordu. Kayıt duruyor, sayımların dışında.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Iptal(int id, string? donus = null)
        {
            var rez = _context.Rezervasyons.Find(id);
            if (rez == null)
            {
                TempData["Hata"] = "İptal edilecek rezervasyon bulunamadı.";
                return ListeyeDon(donus);
            }

            rez.Durum = RezervasyonDurumu.Iptal;
            if (GuvenliKaydet("Rezervasyon iptali"))
                TempData["Mesaj"] = $"{rez.AdSoyad} adına rezervasyon iptal edildi.";
            else
                TempData["Hata"] = "Rezervasyon iptal edilemedi. Lütfen tekrar deneyin.";

            return ListeyeDon(donus);
        }

        /// <summary>
        /// İşlem sonrası kullanıcıyı GELDİĞİ süzgeç/sayfaya döndürür.
        /// Aksi hâlde 3. sayfadaki bir kaydı onaylayan kişi listenin
        /// başına düşüyor ve kaldığı yeri kaybediyordu.
        ///
        /// <c>Url.IsLocalUrl</c> şart: adres dışarıdan (formdan) geliyor,
        /// denetlenmezse açık yönlendirme (open redirect) açığı olur.
        /// </summary>
        private IActionResult ListeyeDon(string? donus, string yedekEylem = "Rezervasyonlar")
        {
            if (!string.IsNullOrWhiteSpace(donus) && Url.IsLocalUrl(donus))
                return Redirect(donus);

            return RedirectToAction(yedekEylem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int id, string? donus = null)
        {
            var rez = _context.Rezervasyons.Find(id);
            if (rez == null)
            {
                TempData["Hata"] = "Silinecek rezervasyon bulunamadı.";
                return ListeyeDon(donus);
            }

            _context.Rezervasyons.Remove(rez);
            if (GuvenliKaydet("Rezervasyon silme"))
                TempData["Mesaj"] = $"{rez.AdSoyad} adına rezervasyon silindi.";
            else
                TempData["Hata"] = "Rezervasyon silinemedi. Lütfen tekrar deneyin.";

            return ListeyeDon(donus);
        }

        // --- 5. HESAP AYARLARI ---
        public async Task<IActionResult> Profil()
        {
            var kullanici = await _kullaniciYoneticisi.GetUserAsync(User);
            if (kullanici == null) return RedirectToAction("Login");

            return View(new ProfilModeli
            {
                Id = kullanici.Id,
                KullaniciAdi = kullanici.UserName ?? string.Empty,
                Eposta = kullanici.Email ?? string.Empty
                // Sifre bilerek boş: özet asla görünüme gitmez.
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfilGuncelle(ProfilModeli gelenVeri)
        {
            // Kimlik oturumdan alınıyor; formdan gelen Id'ye GÜVENİLMEZ,
            // yoksa başka bir hesabın bilgileri değiştirilebilirdi.
            var kullanici = await _kullaniciYoneticisi.GetUserAsync(User);
            if (kullanici == null) return RedirectToAction("Login");

            // Şifre alanı boş bırakılabilir (değiştirilmek istenmiyor demektir).
            ModelState.Remove(nameof(ProfilModeli.Sifre));

            if (!ModelState.IsValid)
            {
                ViewBag.Hata = "Bilgiler kaydedilmedi. Lütfen işaretli alanları düzeltin.";
                gelenVeri.Sifre = null;
                gelenVeri.Id = kullanici.Id;
                return View("Profil", gelenVeri);
            }

            kullanici.UserName = gelenVeri.KullaniciAdi;
            kullanici.Email = gelenVeri.Eposta;

            var sonuc = await _kullaniciYoneticisi.UpdateAsync(kullanici);

            if (sonuc.Succeeded && !string.IsNullOrEmpty(gelenVeri.Sifre))
            {
                // Mevcut şifreyi sormadan değiştirmek, oturumu ele geçiren
                // birinin hesabı kalıcı olarak devralmasını kolaylaştırırdı;
                // ama bu akış eskiden de böyleydi ve tek yönetici var.
                // Jetonla sıfırlama, eski şifreyi bilmeden değiştirmenin
                // Identity'deki doğru yolu.
                var jeton = await _kullaniciYoneticisi.GeneratePasswordResetTokenAsync(kullanici);
                sonuc = await _kullaniciYoneticisi.ResetPasswordAsync(kullanici, jeton, gelenVeri.Sifre);
            }

            if (sonuc.Succeeded)
            {
                // Kullanıcı adı/şifre değişmiş olabilir: çerezdeki kimliği
                // tazelemezsek kullanıcı bir sonraki istekte dışarı atılır.
                await _girisYoneticisi.RefreshSignInAsync(kullanici);
                ViewBag.Mesaj = "Bilgileriniz başarıyla güncellendi!";
            }
            else
            {
                ViewBag.Hata = "Bilgileriniz kaydedilemedi: " +
                    string.Join(" ", sonuc.Errors.Select(h => h.Description));
            }

            return View("Profil", new ProfilModeli
            {
                Id = kullanici.Id,
                KullaniciAdi = kullanici.UserName ?? string.Empty,
                Eposta = kullanici.Email ?? string.Empty
            });
        }

        // --- 6. MAIL AYARLARI ---
        public IActionResult MailAyarlari()
        {
            var ayar = _context.Ayarlar.FirstOrDefault() ?? new SistemAyarlari();

            // Saklanan şifre tarayıcıya gönderilmiyor; görünüm yalnızca
            // "kayıtlı bir şifre var mı" bilgisini kullanıyor.
            ViewBag.SifreKayitli = !string.IsNullOrEmpty(ayar.GonderenSifre);
            ayar.GonderenSifre = "";
            return View(ayar);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MailAyarlariGuncelle(SistemAyarlari gelenAyar)
        {
            /* ŞİFRE ALANI BOŞ GELEBİLİR — ve bu geçerli bir durum: "değiştirme"
               demek. Ama GonderenSifre nullable OLMAYAN bir string, bu yüzden
               MVC ona örtük bir [Required] kuralı ekliyor; boş girdi de
               ConvertEmptyStringToNull ile null'a çevrildiği için doğrulama
               patlıyordu. Sonuç: şifreye dokunmadan e-posta/SMTP güncellemek
               imkânsızdı ("Ayarlar kaydedilmedi" dönüyordu).

               Kuralı burada düşürüyoruz; boş/dolu ayrımını aşağıda kendimiz
               ele alıyoruz. Modelin nullable yapılması şema değişikliği
               gerektirirdi. UrunController da Kategori için aynı kalıbı
               kullanıyor. */
            ModelState.Remove(nameof(SistemAyarlari.GonderenSifre));

            // Modeldeki [EmailAddress] / [Range] doğrulamaları burada da devreye girsin.
            if (!ModelState.IsValid)
            {
                ViewBag.Hata = "Ayarlar kaydedilmedi. Gönderen e-posta adresi, SMTP sunucusu ve port alanlarını kontrol edin.";
                return View("MailAyarlari", gelenAyar);
            }

            var mevcut = _context.Ayarlar.FirstOrDefault();

            /* ŞİFRE ALANI TARAYICIYA HİÇ GÖNDERİLMİYOR (bkz. MailAyarlari.cshtml).
               Bu yüzden alan boş geldiyse "değiştirilmedi" demektir; kayıtlı
               şifre korunur. Doluysa şifrelenip yazılır.

               Önceden form, saklanan şifreyi value= ile geri basıyordu: şifre
               her sayfa açılışında HTML kaynağında görünüyordu. */
            if (mevcut == null)
            {
                if (string.IsNullOrEmpty(gelenAyar.GonderenSifre))
                {
                    ViewBag.Hata = "Ayarlar kaydedilmedi. İlk kayıtta uygulama şifresi zorunludur.";
                    gelenAyar.GonderenSifre = "";
                    return View("MailAyarlari", gelenAyar);
                }

                gelenAyar.GonderenSifre = _ayarKorumasi.Koru(gelenAyar.GonderenSifre);
                _context.Ayarlar.Add(gelenAyar);
            }
            else
            {
                mevcut.GonderenMail = gelenAyar.GonderenMail;
                mevcut.SmtpSunucu = gelenAyar.SmtpSunucu;
                mevcut.SmtpPort = gelenAyar.SmtpPort;

                if (!string.IsNullOrEmpty(gelenAyar.GonderenSifre))
                    mevcut.GonderenSifre = _ayarKorumasi.Koru(gelenAyar.GonderenSifre);
            }

            if (GuvenliKaydet("Mail ayarları güncelleme"))
                ViewBag.Mesaj = "Mail ayarları başarıyla kaydedildi!";
            else
                ViewBag.Hata = "Mail ayarları kaydedilemedi. Lütfen tekrar deneyin.";

            // Kullanıcının yazdığı şifre görünüme geri gitmesin.
            gelenAyar.GonderenSifre = "";
            ViewBag.SifreKayitli = _context.Ayarlar.Any(a => a.GonderenSifre != "");
            return View("MailAyarlari", gelenAyar);
        }

        // --- 7. ŞİFRE SIFIRLAMA ---
        [AllowAnonymous]
        public IActionResult SifremiUnuttum() { return View(); }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SifreSifirla(string Eposta)
        {
            var kullanici = await _kullaniciYoneticisi.FindByEmailAsync(Eposta);

            if (kullanici == null)
            {
                _logger.LogWarning("Kayıtlı olmayan adres için şifre sıfırlama denemesi. IP: {IP}",
                    HttpContext.Connection.RemoteIpAddress);
                ViewBag.Hata = "Bu e-posta adresi sistemde kayıtlı değil.";
                return View("SifremiUnuttum");
            }

            string yeniSifre = RandomSifreUret(10);

            // Identity'nin kendi jetonu: eski şifreyi bilmeden değiştirmenin
            // doğru yolu. Jeton üretimi ve tüketimi aynı istekte olduğu için
            // e-postayla bağlantı göndermeye gerek yok — akış eskisiyle aynı,
            // yeni şifre doğrudan mailleniyor.
            var jeton = await _kullaniciYoneticisi.GeneratePasswordResetTokenAsync(kullanici);
            var sonucSifre = await _kullaniciYoneticisi.ResetPasswordAsync(kullanici, jeton, yeniSifre);

            if (!sonucSifre.Succeeded)
            {
                _logger.LogError("Şifre sıfırlanamadı. Kullanıcı: {Kullanici} — {Hatalar}",
                    kullanici.UserName, string.Join("; ", sonucSifre.Errors.Select(h => h.Description)));
                ViewBag.Hata = "Şifre sıfırlanamadı. Lütfen tekrar deneyin.";
                return View("SifremiUnuttum");
            }

            // Sıfırlama sonrası varsa kilit kalksın; kullanıcı yeni şifresiyle
            // hemen girebilmeli.
            await _kullaniciYoneticisi.SetLockoutEndDateAsync(kullanici, null);
            await _kullaniciYoneticisi.ResetAccessFailedCountAsync(kullanici);

            string konu = "Rosso Lounge - Şifre Sıfırlama";
            string icerik = $"Merhaba {kullanici.UserName},<br><br>" +
                            $"Şifre sıfırlama talebiniz üzerine yeni şifreniz oluşturuldu.<br>" +
                            $"Yeni Şifreniz: <b>{yeniSifre}</b><br><br>" +
                            $"Lütfen giriş yaptıktan sonra güvenliğiniz için şifrenizi değiştirin.";

            var sonuc = MailGonder(Eposta, konu, icerik);

            if (sonuc.Basarili)
            {
                _logger.LogInformation("Şifre sıfırlandı ve mail gönderildi. Kullanıcı: {Kullanici}", kullanici.UserName);
                ViewBag.Basari = "Yeni şifreniz e-posta adresinize gönderildi.";
            }
            else
            {
                // Şifre değişti ama mail gitmedi — kullanıcının bunu bilmesi şart,
                // aksi halde eski şifresiyle giriş yapmayı dener ve neden olmadığını anlamaz.
                ViewBag.Hata = "Şifreniz sıfırlandı fakat mail gönderilemedi: " + sonuc.Mesaj +
                               " Yeni şifreyi öğrenmek için site yöneticinizle iletişime geçin.";
            }

            return View("SifremiUnuttum");
        }

        // --- 8. ÇIKIŞ YAP ---
        // GET ile tetiklenebilen çıkış, başka bir sitedeki <img src="/Admin/CikisYap">
        // ya da bağlantı ön-yüklemesiyle istem dışı kapanabiliyordu. POST + token.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CikisYap()
        {
            await _girisYoneticisi.SignOutAsync();
            return RedirectToAction("Login");
        }

        // --- MESAJLAR (GELEN KUTUSU) ---
        public IActionResult Mesajlar()
        {
            // Okunmayanlar en üstte olsun
            var mesajlar = _context.IletisimMesajlari
                                   .OrderBy(m => m.OkunduMu)
                                   .ThenByDescending(m => m.Tarih)
                                   .ToList();
            return View(mesajlar);
        }

        // --- MESAJ OKU ---
        public IActionResult MesajOku(int id)
        {
            var mesaj = _context.IletisimMesajlari.Find(id);
            if (mesaj == null)
            {
                TempData["Hata"] = "Mesaj bulunamadı.";
                return RedirectToAction("Mesajlar");
            }

            if (!mesaj.OkunduMu)
            {
                mesaj.OkunduMu = true;
                // Okundu işaretlemesi başarısız olsa da mesaj gösterilmeli;
                // sadece loglayıp devam ediyoruz.
                GuvenliKaydet("Mesajı okundu işaretleme");
            }

            return View(mesaj);
        }

        // --- MESAJ SİL ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MesajSil(int id)
        {
            var mesaj = _context.IletisimMesajlari.Find(id);
            if (mesaj == null)
            {
                TempData["Hata"] = "Silinecek mesaj bulunamadı.";
                return RedirectToAction("Mesajlar");
            }

            _context.IletisimMesajlari.Remove(mesaj);
            if (GuvenliKaydet("Mesaj silme"))
                TempData["Mesaj"] = "Mesaj silindi.";
            else
                TempData["Hata"] = "Mesaj silinemedi. Lütfen tekrar deneyin.";

            return RedirectToAction("Mesajlar");
        }

        // --- YORUMLAR SAYFASI ---
        /// <summary>
        /// Süzülebilir, sıralanabilir, sayfalanabilir yorum listesi.
        ///
        /// Eskiden bütün yorumlar tek sayfada basılıyordu ve arama
        /// tarayıcıda satır gizliyordu: kayıtlar yine indiriliyor,
        /// "arama" yalnızca ekrandakini süzüyordu.
        ///
        /// Varsayılan sekme "tumu" ve varsayılan sıra "bekleyen_once" —
        /// yani sayfa açılışta eski davranışı koruyor (onay bekleyenler
        /// üstte). "Bekleyen" sekmesini varsayılan yapmadım: bekleyen
        /// yokken yönetici boş bir ekrana düşerdi.
        /// </summary>
        public async Task<IActionResult> Yorumlar(
            string durum = "tumu",
            string? ara = null,
            int? puan = null,
            string sirala = "bekleyen_once",
            int sayfa = 1,
            int boyut = 25)
        {
            // Dışarıdan gelen değerler beyaz listeye çekiliyor: adres
            // çubuğuna yazılan rastgele bir değer sorguyu bozmasın.
            var gecerliDurumlar = new[] { "tumu", "bekleyen", "yayinda" };
            if (!gecerliDurumlar.Contains(durum)) durum = "tumu";

            var gecerliSiralar = new[] { "bekleyen_once", "yeni", "eski", "puan_azalan", "puan_artan" };
            if (!gecerliSiralar.Contains(sirala)) sirala = "bekleyen_once";

            // Tek durumlu sekmede "bekleyen önce" sıralaması anlamsız:
            // hepsi aynı durumda. Sessizce tarihe düşüyor.
            if (sirala == "bekleyen_once" && durum != "tumu") sirala = "yeni";

            if (puan is < 1 or > 5) puan = null;
            if (boyut != 25 && boyut != 50 && boyut != 100) boyut = 25;
            if (sayfa < 1) sayfa = 1;

            var suzgec = new YorumSuzgeci
            {
                Durum = durum,
                Ara = string.IsNullOrWhiteSpace(ara) ? null : ara.Trim(),
                Puan = puan,
                Sirala = sirala,
                Sayfa = sayfa,
                Boyut = boyut
            };

            var model = new YorumListeModeli { Suzgec = suzgec };

            model.SayiTumu = await _context.Yorumlar.CountAsync();
            model.SayiBekleyen = await _context.Yorumlar.CountAsync(y => !y.OnaylandiMi);
            model.SayiYayinda = model.SayiTumu - model.SayiBekleyen;

            // Sitedeki puan ortalaması yalnızca ONAYLI yorumlardan
            // hesaplanıyor (HomeController); panelde başka bir sayı
            // göstermek kafa karıştırırdı.
            if (model.SayiYayinda > 0)
            {
                model.YayindakiOrtalama = await _context.Yorumlar
                    .Where(y => y.OnaylandiMi)
                    .AverageAsync(y => (double)y.Puan);
            }

            // Kenar menüsündeki rozet
            ViewData["BekleyenYorum"] = (int?)model.SayiBekleyen;

            IQueryable<Yorum> sorgu = _context.Yorumlar;

            sorgu = durum switch
            {
                "bekleyen" => sorgu.Where(y => !y.OnaylandiMi),
                "yayinda" => sorgu.Where(y => y.OnaylandiMi),
                _ => sorgu
            };

            if (suzgec.Ara != null)
            {
                var kalip = suzgec.Ara;
                sorgu = sorgu.Where(y =>
                    EF.Functions.Like(y.AdSoyad, "%" + kalip + "%") ||
                    EF.Functions.Like(y.Mesaj, "%" + kalip + "%"));
            }

            if (puan.HasValue) sorgu = sorgu.Where(y => y.Puan == puan.Value);

            model.ToplamKayit = await sorgu.CountAsync();
            model.ToplamSayfa = Math.Max(1, (int)Math.Ceiling(model.ToplamKayit / (double)boyut));
            if (suzgec.Sayfa > model.ToplamSayfa) suzgec.Sayfa = model.ToplamSayfa;

            sorgu = sirala switch
            {
                "yeni" => sorgu.OrderByDescending(y => y.Tarih),
                "eski" => sorgu.OrderBy(y => y.Tarih),
                "puan_azalan" => sorgu.OrderByDescending(y => y.Puan).ThenByDescending(y => y.Tarih),
                "puan_artan" => sorgu.OrderBy(y => y.Puan).ThenByDescending(y => y.Tarih),
                // Onay bekleyenler üstte — sayfanın asıl işi bu.
                _ => sorgu.OrderBy(y => y.OnaylandiMi).ThenByDescending(y => y.Tarih)
            };

            model.Kayitlar = await sorgu
                .Skip((suzgec.Sayfa - 1) * boyut)
                .Take(boyut)
                .ToListAsync();

            return View(model);
        }

        // --- YORUM YAYINA AL ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YorumOnayla(int id, string? donus = null)
        {
            var yorum = _context.Yorumlar.Find(id);
            if (yorum == null)
            {
                TempData["Hata"] = "Onaylanacak yorum bulunamadı.";
                return ListeyeDon(donus, "Yorumlar");
            }

            yorum.OnaylandiMi = true; // Yayına al
            if (GuvenliKaydet("Yorum onaylama"))
                TempData["Mesaj"] = $"{yorum.AdSoyad} adlı ziyaretçinin yorumu yayına alındı.";
            else
                TempData["Hata"] = "Yorum onaylanamadı. Lütfen tekrar deneyin.";

            return ListeyeDon(donus, "Yorumlar");
        }

        // --- YORUM YAYINDAN KALDIR ---
        /// <summary>
        /// Yayındaki bir yorumu tekrar bekleyene çeker.
        ///
        /// NEDEN VAR: yayına alınmış bir yorumu siteden çıkarmanın tek yolu
        /// SİLMEKTİ; silinen yorumdan geriye iz kalmıyordu. Rezervasyonlarda
        /// iptal/silme için kurulan ayrımın aynısı: kaldırmak geri
        /// alınabilir, silmek değil. Şema değişmiyor — OnaylandiMi zaten
        /// iki yönlü bir alan, yalnızca tek yönde kullanılıyordu.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YorumGizle(int id, string? donus = null)
        {
            var yorum = _context.Yorumlar.Find(id);
            if (yorum == null)
            {
                TempData["Hata"] = "Yorum bulunamadı.";
                return ListeyeDon(donus, "Yorumlar");
            }

            yorum.OnaylandiMi = false;
            if (GuvenliKaydet("Yorum yayından kaldırma"))
                TempData["Mesaj"] = $"{yorum.AdSoyad} adlı ziyaretçinin yorumu siteden kaldırıldı. Kayıt duruyor, tekrar yayınlayabilirsiniz.";
            else
                TempData["Hata"] = "Yorum kaldırılamadı. Lütfen tekrar deneyin.";

            return ListeyeDon(donus, "Yorumlar");
        }

        // --- YORUM SİL ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YorumSil(int id, string? donus = null)
        {
            var yorum = _context.Yorumlar.Find(id);
            if (yorum == null)
            {
                TempData["Hata"] = "Silinecek yorum bulunamadı.";
                return ListeyeDon(donus, "Yorumlar");
            }

            _context.Yorumlar.Remove(yorum);
            if (GuvenliKaydet("Yorum silme"))
                TempData["Mesaj"] = "Yorum silindi.";
            else
                TempData["Hata"] = "Yorum silinemedi. Lütfen tekrar deneyin.";

            return ListeyeDon(donus, "Yorumlar");
        }
    }
}
