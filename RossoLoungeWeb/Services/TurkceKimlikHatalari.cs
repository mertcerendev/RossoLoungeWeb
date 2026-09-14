using Microsoft.AspNetCore.Identity;

namespace RossoLoungeWeb.Services
{
    /// <summary>
    /// Identity'nin hata metinleri varsayılan olarak İngilizce gelir
    /// ("Passwords must be at least 8 characters."). Panelin geri kalanı
    /// Türkçe olduğu için kullanıcıya karışık dilde bir ekran çıkıyordu.
    ///
    /// Yalnızca panelde gerçekten karşılaşılabilecek hatalar çevrildi;
    /// çevrilmeyenler taban sınıftan İngilizce gelmeye devam eder
    /// (iki faktörlü doğrulama, harici giriş sağlayıcıları gibi bu
    /// projede kullanılmayan akışlar).
    /// </summary>
    public class TurkceKimlikHatalari : IdentityErrorDescriber
    {
        public override IdentityError PasswordTooShort(int length) => new()
        {
            Code = nameof(PasswordTooShort),
            Description = $"Şifre en az {length} karakter olmalıdır."
        };

        public override IdentityError PasswordRequiresDigit() => new()
        {
            Code = nameof(PasswordRequiresDigit),
            Description = "Şifre en az bir rakam içermelidir."
        };

        public override IdentityError PasswordRequiresUpper() => new()
        {
            Code = nameof(PasswordRequiresUpper),
            Description = "Şifre en az bir büyük harf içermelidir."
        };

        public override IdentityError PasswordRequiresLower() => new()
        {
            Code = nameof(PasswordRequiresLower),
            Description = "Şifre en az bir küçük harf içermelidir."
        };

        public override IdentityError PasswordRequiresNonAlphanumeric() => new()
        {
            Code = nameof(PasswordRequiresNonAlphanumeric),
            Description = "Şifre en az bir noktalama işareti içermelidir."
        };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new()
        {
            Code = nameof(PasswordRequiresUniqueChars),
            Description = $"Şifre en az {uniqueChars} farklı karakter içermelidir."
        };

        public override IdentityError DuplicateUserName(string userName) => new()
        {
            Code = nameof(DuplicateUserName),
            Description = $"'{userName}' kullanıcı adı zaten kullanılıyor."
        };

        public override IdentityError DuplicateEmail(string email) => new()
        {
            Code = nameof(DuplicateEmail),
            Description = $"'{email}' e-posta adresi zaten kayıtlı."
        };

        public override IdentityError InvalidUserName(string? userName) => new()
        {
            Code = nameof(InvalidUserName),
            Description = $"'{userName}' geçerli bir kullanıcı adı değil. Yalnızca harf, rakam ve - . _ @ + kullanılabilir."
        };

        public override IdentityError InvalidEmail(string? email) => new()
        {
            Code = nameof(InvalidEmail),
            Description = $"'{email}' geçerli bir e-posta adresi değil."
        };

        public override IdentityError PasswordMismatch() => new()
        {
            Code = nameof(PasswordMismatch),
            Description = "Mevcut şifre hatalı."
        };

        public override IdentityError InvalidToken() => new()
        {
            Code = nameof(InvalidToken),
            Description = "Doğrulama bağlantısı geçersiz ya da süresi dolmuş."
        };

        public override IdentityError UserAlreadyHasPassword() => new()
        {
            Code = nameof(UserAlreadyHasPassword),
            Description = "Kullanıcının zaten bir şifresi var."
        };

        public override IdentityError UserLockoutNotEnabled() => new()
        {
            Code = nameof(UserLockoutNotEnabled),
            Description = "Bu hesap için kilitleme özelliği kapalı."
        };

        public override IdentityError ConcurrencyFailure() => new()
        {
            Code = nameof(ConcurrencyFailure),
            Description = "Kayıt başka bir yerden değiştirilmiş. Sayfayı yenileyip tekrar deneyin."
        };

        public override IdentityError DefaultError() => new()
        {
            Code = nameof(DefaultError),
            Description = "Beklenmeyen bir hata oluştu."
        };
    }
}
