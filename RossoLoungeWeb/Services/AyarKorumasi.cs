using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using RossoLoungeWeb.Data;

namespace RossoLoungeWeb.Services
{
    /// <summary>
    /// SMTP uygulama şifresini veritabanında ŞİFRELİ tutar.
    ///
    /// Önceden düz metindi: bir veritabanı yedeği ya da SQL sızıntısı doğrudan
    /// mail hesabının ele geçirilmesi demekti. Artık DataProtection ile
    /// şifreleniyor; anahtarlar Program.cs'te App_Data/keys'e kalıcı yazılıyor
    /// ve uygulama adı sabitlenmiş durumda.
    ///
    /// GEÇİŞ: <see cref="Coz"/> çözemediği değeri ESKİ DÜZ METİN kabul edip
    /// olduğu gibi döner. Böylece şifreleme devreye girdiği anda mail gönderimi
    /// kesilmez. <see cref="DuzMetniSifreleAsync"/> açılışta bir kez çalışıp
    /// kalan düz metni yerinde şifreler.
    ///
    /// DİKKAT: App_Data/keys silinirse saklanan şifre çözülemez hâle gelir ve
    /// panelden yeniden girilmesi gerekir. Anahtar klasörü yedeklenmeli.
    /// </summary>
    public class AyarKorumasi
    {
        // Amaç dizesi anahtarın kapsamını belirler; değiştirilirse eski
        // değerler çözülemez. Bilerek sabit.
        private const string Amac = "RossoLoungeWeb.SistemAyarlari.GonderenSifre.v1";

        private readonly IDataProtector _koruyucu;
        private readonly ILogger<AyarKorumasi> _kayit;

        public AyarKorumasi(IDataProtectionProvider saglayici, ILogger<AyarKorumasi> kayit)
        {
            _koruyucu = saglayici.CreateProtector(Amac);
            _kayit = kayit;
        }

        /// <summary>Düz metni saklanacak biçime çevirir. Boş girdi boş döner.</summary>
        public string Koru(string? acikMetin)
        {
            return string.IsNullOrEmpty(acikMetin) ? "" : _koruyucu.Protect(acikMetin);
        }

        /// <summary>
        /// Saklanan değeri çözer. Çözülemiyorsa değer henüz şifrelenmemiş
        /// eski bir kayıttır; olduğu gibi döner.
        /// </summary>
        public string Coz(string? saklanan)
        {
            if (string.IsNullOrEmpty(saklanan)) return "";

            try
            {
                return _koruyucu.Unprotect(saklanan);
            }
            catch (CryptographicException)
            {
                return saklanan; // eski düz metin
            }
        }

        /// <summary>Değer şifrelenmiş mi? Geçiş kontrolü için.</summary>
        public bool Sifreli(string? saklanan)
        {
            if (string.IsNullOrEmpty(saklanan)) return false;

            try
            {
                _koruyucu.Unprotect(saklanan);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        /// <summary>
        /// Veritabanında kalmış düz metin şifreyi şifreler. İdempotent:
        /// zaten şifrelenmiş kaydı tekrar sarmalamaz, bu yüzden her açılışta
        /// güvenle çağrılabilir.
        /// </summary>
        public async Task DuzMetniSifreleAsync(ApplicationDbContext baglam)
        {
            var ayar = await baglam.Ayarlar.FirstOrDefaultAsync();
            if (ayar == null || string.IsNullOrEmpty(ayar.GonderenSifre)) return;
            if (Sifreli(ayar.GonderenSifre)) return;

            ayar.GonderenSifre = Koru(ayar.GonderenSifre);
            await baglam.SaveChangesAsync();

            _kayit.LogInformation("SMTP şifresi düz metinden şifreli biçime taşındı.");
        }
    }
}
