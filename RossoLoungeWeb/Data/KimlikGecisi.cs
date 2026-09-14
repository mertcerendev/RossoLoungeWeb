using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace RossoLoungeWeb.Data
{
    /// <summary>
    /// Eski <c>Yoneticiler</c> tablosundaki hesapları Identity'ye taşır.
    ///
    /// Açılışta bir kez çalışır ve FİKİRSİZDİR (idempotent): aynı kullanıcı
    /// adı Identity'de zaten varsa hiçbir şey yapmaz. Böylece her uygulama
    /// başlangıcında güvenle çağrılabilir ve canlıya çıkarken elle bir adım
    /// atlanmış olmaz.
    ///
    /// Şifre özeti OLDUĞU GİBİ taşınır (BCrypt). Doğrulamayı
    /// <see cref="Services.BcryptGecisliSifreleyici"/> üstleniyor ve ilk
    /// başarılı girişte özeti Identity biçimine yükseltiyor — yani mevcut
    /// yönetici eski şifresiyle girmeye devam eder.
    ///
    /// Eski tablo SİLİNMEZ: proje kuralı gereği şema değişiklikleri eklemeli.
    /// Geçiş canlıda doğrulandıktan sonra ayrı bir migration ile kaldırılabilir.
    /// </summary>
    public static class KimlikGecisi
    {
        public static async Task TasiAsync(IServiceProvider saglayici, ILogger kayit)
        {
            var baglam = saglayici.GetRequiredService<ApplicationDbContext>();
            var kullaniciYoneticisi = saglayici.GetRequiredService<UserManager<IdentityUser>>();

            // Identity tabloları henüz yoksa (migration uygulanmamış) sessizce çık;
            // uygulamanın açılışını engellemenin faydası yok.
            if (!await baglam.Database.CanConnectAsync())
            {
                kayit.LogWarning("Kimlik geçişi atlandı: veritabanına bağlanılamadı.");
                return;
            }

            List<Models.Yonetici> eskiler;
            try
            {
                eskiler = await baglam.Yoneticiler.AsNoTracking().ToListAsync();
            }
            catch (Exception hata)
            {
                kayit.LogWarning(hata, "Kimlik geçişi atlandı: eski Yoneticiler tablosu okunamadı.");
                return;
            }

            foreach (var eski in eskiler)
            {
                if (string.IsNullOrWhiteSpace(eski.KullaniciAdi)) continue;

                var mevcut = await kullaniciYoneticisi.FindByNameAsync(eski.KullaniciAdi);
                if (mevcut != null) continue; // zaten taşınmış

                var kullanici = new IdentityUser
                {
                    UserName = eski.KullaniciAdi,
                    Email = eski.Eposta,
                    EmailConfirmed = true,           // panel hesabı; e-posta onay akışı yok
                    SecurityStamp = Guid.NewGuid().ToString(),
                    // BCrypt özeti olduğu gibi taşınıyor; ilk girişte yükseltilecek.
                    PasswordHash = eski.Sifre
                };

                var sonuc = await kullaniciYoneticisi.CreateAsync(kullanici);

                if (sonuc.Succeeded)
                {
                    kayit.LogInformation("Yönetici Identity'ye taşındı: {Kullanici}", eski.KullaniciAdi);
                }
                else
                {
                    kayit.LogError("Yönetici taşınamadı: {Kullanici} — {Hatalar}",
                        eski.KullaniciAdi, string.Join("; ", sonuc.Errors.Select(h => h.Description)));
                }
            }
        }
    }
}
