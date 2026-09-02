using Microsoft.AspNetCore.Identity;

namespace RossoLoungeWeb.Services
{
    /// <summary>
    /// Identity'ye geçişte ŞİFRELERİ KORUYAN özet doğrulayıcı.
    ///
    /// SORUN: Eski panel şifreleri BCrypt ile saklıyordu; Identity ise kendi
    /// PBKDF2 biçimini kullanır. İki biçim birbirine çevrilemez (özet tek
    /// yönlüdür), dolayısıyla düz geçişte mevcut yönetici şifresiyle
    /// giremezdi — herkese şifre sıfırlatmak gerekirdi.
    ///
    /// ÇÖZÜM: Bu doğrulayıcı özetin biçimine bakar.
    ///   • "$2..." ile başlıyorsa (BCrypt) → BCrypt ile doğrular ve
    ///     <see cref="PasswordVerificationResult.SuccessRehashNeeded"/> döner.
    ///     Identity bunu görünce şifreyi geçerli sayar VE kendi biçimiyle
    ///     yeniden özetleyip kaydeder. Yani kullanıcı hiçbir şey fark etmeden,
    ///     ilk girişinde PBKDF2'ye taşınmış olur.
    ///   • Değilse → Identity'nin kendi doğrulayıcısına devreder.
    ///
    /// Yeni şifreler her zaman Identity biçimiyle yazılır (HashPassword
    /// doğrudan varsayılana devrediyor); BCrypt yalnızca okuma yönünde,
    /// geçiş süresince yaşıyor.
    /// </summary>
    public class BcryptGecisliSifreleyici : IPasswordHasher<IdentityUser>
    {
        private readonly PasswordHasher<IdentityUser> _varsayilan = new();

        public string HashPassword(IdentityUser user, string password)
            => _varsayilan.HashPassword(user, password);

        public PasswordVerificationResult VerifyHashedPassword(
            IdentityUser user, string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
                return PasswordVerificationResult.Failed;

            // BCrypt özetleri "$2a$", "$2b$" veya "$2y$" ile başlar.
            if (hashedPassword.StartsWith("$2", StringComparison.Ordinal))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword)
                        ? PasswordVerificationResult.SuccessRehashNeeded
                        : PasswordVerificationResult.Failed;
                }
                catch (Exception)
                {
                    // Bozuk/eksik özet: doğrulama başarısız sayılır.
                    // Girişi çökertmek, saldırgana bilgi vermekten başka işe yaramaz.
                    return PasswordVerificationResult.Failed;
                }
            }

            return _varsayilan.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }
    }
}
