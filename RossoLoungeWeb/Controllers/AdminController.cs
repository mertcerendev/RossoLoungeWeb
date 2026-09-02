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
        public IActionResult Index()
        {
            // Rezervasyon istatistikleri
            ViewBag.ToplamRezervasyon = _context.Rezervasyons.Count();
            ViewBag.BekleyenRezervasyon = _context.Rezervasyons.Count(x => !x.OnaylandiMi);
            ViewBag.OnayliRezervasyon = _context.Rezervasyons.Count(x => x.OnaylandiMi);
            ViewBag.ToplamUrun = _context.Urunler.Count();

            // Panelde gerçekten işe yarayan sayaçlar: yöneticinin bekleyen işleri.
            var bugun = TurkiyeSaati.Bugun;
            ViewBag.BugunkuRezervasyon = _context.Rezervasyons.Count(x => x.Tarih.Date == bugun);
            ViewBag.OkunmamisMesaj = _context.IletisimMesajlari.Count(m => !m.OkunduMu);
            ViewBag.ToplamMesaj = _context.IletisimMesajlari.Count();
            ViewBag.OnayBekleyenYorum = _context.Yorumlar.Count(y => !y.OnaylandiMi);
            ViewBag.ToplamYorum = _context.Yorumlar.Count();
            ViewBag.ToplamKategori = _context.Kategoriler.Count();

            // Grafik Verisi (Son 7 Gün) — Türkiye saatiyle.
            // DateTime.Today sunucu saat dilimini kullanıyordu; sunucu UTC ise
            // pencere kayıyor ve bugünün rezervasyonları yanlış güne düşüyordu.
            var son7Gun = new List<string>();
            var rezervasyonSayilari = new List<int>();

            for (int i = 6; i >= 0; i--)
            {
                var tarih = bugun.AddDays(-i);
                son7Gun.Add(tarih.ToString("dd MMM", TurkceKultur));
                int sayi = _context.Rezervasyons.Count(x => x.Tarih.Date == tarih);
                rezervasyonSayilari.Add(sayi);
            }

            ViewBag.GrafikGunler = son7Gun;
            ViewBag.GrafikSayilar = rezervasyonSayilari;

            return View();
        }

        // --- 3. REZERVASYON LİSTESİ ---
        public IActionResult Rezervasyonlar()
        {
            var liste = _context.Rezervasyons.OrderBy(r => r.Tarih).ToList();
            return View(liste);
        }

        // --- 4. ONAYLAMA & SİLME ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Onayla(int id)
        {
            var rez = _context.Rezervasyons.Find(id);
            if (rez == null)
            {
                TempData["Hata"] = "Onaylanacak rezervasyon bulunamadı.";
                return RedirectToAction("Rezervasyonlar");
            }

            rez.OnaylandiMi = true;
            if (GuvenliKaydet("Rezervasyon onaylama"))
                TempData["Mesaj"] = $"{rez.AdSoyad} adına rezervasyon onaylandı.";
            else
                TempData["Hata"] = "Rezervasyon onaylanamadı. Lütfen tekrar deneyin.";

            return RedirectToAction("Rezervasyonlar");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(int id)
        {
            var rez = _context.Rezervasyons.Find(id);
            if (rez == null)
            {
                TempData["Hata"] = "Silinecek rezervasyon bulunamadı.";
                return RedirectToAction("Rezervasyonlar");
            }

            _context.Rezervasyons.Remove(rez);
            if (GuvenliKaydet("Rezervasyon silme"))
                TempData["Mesaj"] = $"{rez.AdSoyad} adına rezervasyon silindi.";
            else
                TempData["Hata"] = "Rezervasyon silinemedi. Lütfen tekrar deneyin.";

            return RedirectToAction("Rezervasyonlar");
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
        public IActionResult Yorumlar()
        {
            // Onay bekleyenler en üstte olsun
            var yorumlar = _context.Yorumlar
                                   .OrderBy(y => y.OnaylandiMi)
                                   .ThenByDescending(y => y.Tarih)
                                   .ToList();
            return View(yorumlar);
        }

        // --- YORUM ONAYLA ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YorumOnayla(int id)
        {
            var yorum = _context.Yorumlar.Find(id);
            if (yorum == null)
            {
                TempData["Hata"] = "Onaylanacak yorum bulunamadı.";
                return RedirectToAction("Yorumlar");
            }

            yorum.OnaylandiMi = true; // Yayına al
            if (GuvenliKaydet("Yorum onaylama"))
                TempData["Mesaj"] = $"{yorum.AdSoyad} adlı ziyaretçinin yorumu yayına alındı.";
            else
                TempData["Hata"] = "Yorum onaylanamadı. Lütfen tekrar deneyin.";

            return RedirectToAction("Yorumlar");
        }

        // --- YORUM SİL ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult YorumSil(int id)
        {
            var yorum = _context.Yorumlar.Find(id);
            if (yorum == null)
            {
                TempData["Hata"] = "Silinecek yorum bulunamadı.";
                return RedirectToAction("Yorumlar");
            }

            _context.Yorumlar.Remove(yorum);
            if (GuvenliKaydet("Yorum silme"))
                TempData["Mesaj"] = "Yorum silindi.";
            else
                TempData["Hata"] = "Yorum silinemedi. Lütfen tekrar deneyin.";

            return RedirectToAction("Yorumlar");
        }
    }
}
