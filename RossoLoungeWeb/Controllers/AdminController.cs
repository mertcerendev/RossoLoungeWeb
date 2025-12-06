using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Session için
using RossoLoungeWeb.Data;
using RossoLoungeWeb.Models;
using System.Net;
using System.Net.Mail; // Mail gönderme kütüphanesi
using BCrypt.Net; // Şifreleme kütüphanesi
using System.Text; // Random şifre üretimi için

namespace RossoLoungeWeb.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- YARDIMCI: GÜVENLİ ŞİFRE ÜRETİCİ ---
        private static string RandomSifreUret(int uzunluk = 10)
        {
            const string karakterler = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var result = new StringBuilder(uzunluk);
            var random = new Random();

            for (int i = 0; i < uzunluk; i++)
            {
                result.Append(karakterler[random.Next(karakterler.Length)]);
            }
            return result.ToString();
        }

        // --- YARDIMCI: MAIL GÖNDERME ---
        private bool MailGonder(string kime, string konu, string icerik)
        {
            try
            {
                var ayar = _context.Ayarlar.FirstOrDefault();
                if (ayar == null || string.IsNullOrEmpty(ayar.GonderenMail)) return false;

                SmtpClient client = new SmtpClient(ayar.SmtpSunucu, ayar.SmtpPort);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(ayar.GonderenMail, ayar.GonderenSifre);

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(ayar.GonderenMail, "Rosso Lounge Panel");
                mail.To.Add(kime);
                mail.Subject = konu;
                mail.Body = icerik;
                mail.IsBodyHtml = true;

                client.Send(mail);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // --- 1. GİRİŞ İŞLEMLERİ ---
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult LoginYap(string KullaniciAdi, string Sifre)
        {
            var yonetici = _context.Yoneticiler
                .FirstOrDefault(x => x.KullaniciAdi == KullaniciAdi);

            if (yonetici != null && BCrypt.Net.BCrypt.Verify(Sifre, yonetici.Sifre))
            {
                HttpContext.Session.SetString("User", yonetici.KullaniciAdi);
                HttpContext.Session.SetInt32("UserId", yonetici.Id);
                return RedirectToAction("Index");
            }

            ViewBag.Hata = "Kullanıcı adı veya şifre hatalı!";
            return View("Login");
        }

        // --- 2. DASHBOARD (ÖZET) ---
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login");

            // İstatistikler
            ViewBag.ToplamRezervasyon = _context.Rezervasyons.Count();
            ViewBag.BekleyenRezervasyon = _context.Rezervasyons.Count(x => !x.OnaylandiMi);
            ViewBag.OnayliRezervasyon = _context.Rezervasyons.Count(x => x.OnaylandiMi);
            ViewBag.ToplamUrun = _context.Urunler.Count();

            // Grafik Verisi (Son 7 Gün)
            var son7Gun = new List<string>();
            var rezervasyonSayilari = new List<int>();

            for (int i = 6; i >= 0; i--)
            {
                var tarih = DateTime.Today.AddDays(-i);
                son7Gun.Add(tarih.ToString("dd MMM"));
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
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login");
            var liste = _context.Rezervasyons.OrderBy(r => r.Tarih).ToList();
            return View(liste);
        }

        // --- 4. ONAYLAMA & SİLME ---
        [HttpPost]
        public IActionResult Onayla(int id)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login");
            var rez = _context.Rezervasyons.Find(id);
            if (rez != null) { rez.OnaylandiMi = true; _context.SaveChanges(); }
            return RedirectToAction("Rezervasyonlar");
        }

        [HttpPost]
        public IActionResult Sil(int id)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login");
            var rez = _context.Rezervasyons.Find(id);
            if (rez != null) { _context.Rezervasyons.Remove(rez); _context.SaveChanges(); }
            return RedirectToAction("Rezervasyonlar");
        }

        // --- 5. PROFİL AYARLARI ---
        public IActionResult Profil()
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login");
            int? adminId = HttpContext.Session.GetInt32("UserId");
            var yonetici = _context.Yoneticiler.Find(adminId);

            if (yonetici != null) yonetici.Sifre = string.Empty; // Güvenlik: Hash'i gösterme

            return View(yonetici);
        }

        [HttpPost]
        public IActionResult ProfilGuncelle(Yonetici gelenVeri)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login");
            var yonetici = _context.Yoneticiler.Find(gelenVeri.Id);
            if (yonetici != null)
            {
                yonetici.KullaniciAdi = gelenVeri.KullaniciAdi;
                yonetici.Eposta = gelenVeri.Eposta;

                if (!string.IsNullOrEmpty(gelenVeri.Sifre))
                {
                    yonetici.Sifre = BCrypt.Net.BCrypt.HashPassword(gelenVeri.Sifre);
                }

                _context.SaveChanges();
                ViewBag.Mesaj = "Bilgileriniz başarıyla güncellendi!";
                return View("Profil", yonetici);
            }
            return RedirectToAction("Index");
        }

        // --- 6. MAIL AYARLARI ---
        public IActionResult MailAyarlari()
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login");
            var ayar = _context.Ayarlar.FirstOrDefault() ?? new SistemAyarlari();
            return View(ayar);
        }

        [HttpPost]
        public IActionResult MailAyarlariGuncelle(SistemAyarlari gelenAyar)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login");
            var mevcut = _context.Ayarlar.FirstOrDefault();

            if (mevcut == null) _context.Ayarlar.Add(gelenAyar);
            else
            {
                mevcut.GonderenMail = gelenAyar.GonderenMail;
                mevcut.GonderenSifre = gelenAyar.GonderenSifre;
                mevcut.SmtpSunucu = gelenAyar.SmtpSunucu;
                mevcut.SmtpPort = gelenAyar.SmtpPort;
            }
            _context.SaveChanges();
            ViewBag.Mesaj = "Mail ayarları başarıyla kaydedildi!";
            return View("MailAyarlari", gelenAyar);
        }

        // --- 7. ŞİFRE SIFIRLAMA ---
        public IActionResult SifremiUnuttum() { return View(); }

        [HttpPost]
        public IActionResult SifreSifirla(string Eposta)
        {
            var yonetici = _context.Yoneticiler.FirstOrDefault(x => x.Eposta == Eposta);

            if (yonetici != null)
            {
                string yeniSifre = RandomSifreUret(10);

                yonetici.Sifre = BCrypt.Net.BCrypt.HashPassword(yeniSifre);
                _context.SaveChanges();

                string konu = "Rosso Lounge - Şifre Sıfırlama";
                string icerik = $"Merhaba {yonetici.KullaniciAdi},<br><br>" +
                                $"Şifre sıfırlama talebiniz üzerine yeni şifreniz oluşturuldu.<br>" +
                                $"Yeni Şifreniz: <b>{yeniSifre}</b><br><br>" +
                                $"Lütfen giriş yaptıktan sonra güvenliğiniz için şifrenizi değiştirin.";

                bool sonuc = MailGonder(Eposta, konu, icerik);

                if (sonuc) ViewBag.Basari = "Yeni şifreniz e-posta adresinize gönderildi.";
                else ViewBag.Hata = "Mail gönderilemedi. Lütfen sistem mail ayarlarını kontrol edin.";

                return View("SifremiUnuttum");
            }
            else
            {
                ViewBag.Hata = "Bu e-posta adresi sistemde kayıtlı değil.";
                return View("SifremiUnuttum");
            }
        }

        // --- 8. ÇIKIŞ YAP ---
        public IActionResult CikisYap()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
        // --- YORUMLAR SAYFASI ---
        public IActionResult Yorumlar()
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login");

            // Onay bekleyenler en üstte olsun
            var yorumlar = _context.Yorumlar
                                   .OrderBy(y => y.OnaylandiMi)
                                   .ThenByDescending(y => y.Tarih)
                                   .ToList();
            return View(yorumlar);
        }

        // --- YORUM ONAYLA ---
        [HttpPost]
        public IActionResult YorumOnayla(int id)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login");
            var yorum = _context.Yorumlar.Find(id);
            if (yorum != null)
            {
                yorum.OnaylandiMi = true; // Yayına al
                _context.SaveChanges();
            }
            return RedirectToAction("Yorumlar");
        }

        // --- YORUM SİL ---
        [HttpPost]
        public IActionResult YorumSil(int id)
        {
            if (HttpContext.Session.GetString("User") == null) return RedirectToAction("Login");
            var yorum = _context.Yorumlar.Find(id);
            if (yorum != null)
            {
                _context.Yorumlar.Remove(yorum);
                _context.SaveChanges();
            }
            return RedirectToAction("Yorumlar");
        }
    }
}