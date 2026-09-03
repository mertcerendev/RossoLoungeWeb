using System.ComponentModel.DataAnnotations;

namespace RossoLoungeWeb.Models
{
    /// <summary>
    /// Panel giriş formu.
    ///
    /// Identity'nin hazır arayüzü (Razor Pages) kendi <c>Input.Email</c> /
    /// <c>Input.Password</c> modelini kullanır. Bu proje girişi kendi MVC
    /// controller'ıyla yönetiyor ve yönetici e-posta ile değil KULLANICI ADI
    /// ile giriyor; o yüzden alan adları buradaki modele göre. Giriş yine de
    /// her ikisini kabul ediyor: yazılan değer önce kullanıcı adı, bulunamazsa
    /// e-posta olarak aranıyor.
    /// </summary>
    public class GirisModeli
    {
        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [Display(Name = "Kullanıcı adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Sifre { get; set; } = string.Empty;

        [Display(Name = "Beni hatırla")]
        public bool BeniHatirla { get; set; }
    }

    /// <summary>
    /// Panel hesap ayarları formu. Identity kullanıcısını doğrudan görünüme
    /// göndermemek için ara model: <c>PasswordHash</c> gibi alanların
    /// yanlışlıkla ekrana basılması mümkün olmasın.
    /// </summary>
    public class ProfilModeli
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [MaxLength(50, ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir.")]
        [Display(Name = "Kullanıcı adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(150, ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
        [Display(Name = "E-posta")]
        public string Eposta { get; set; } = string.Empty;

        /// <summary>Boş bırakılırsa şifre değişmez.</summary>
        [DataType(DataType.Password)]
        [Display(Name = "Yeni şifre")]
        public string? Sifre { get; set; }
    }

    /// <summary>
    /// Ayarlar ekranının verisi — iki bölüm tek sayfada.
    ///
    /// NEDEN BİRLEŞTİ: hesap bilgileri /Admin/Profil'de, SMTP ayarları
    /// /Admin/MailAyarlari'ndaydı ve aralarında birbirine işaret eden
    /// iki düğme vardı ("Mail ayarları" ↔ "Hesap ayarları"). İkisi de
    /// aynı şeyin parçası: panele nasıl giriliyor ve şifre sıfırlama
    /// e-postası nereden gönderiliyor. Menüde tek "Ayarlar" öğesi var,
    /// sayfası da tek olmalı.
    ///
    /// İki AYRI FORM olarak duruyorlar: uçları ve doğrulama kuralları
    /// farklı, biri kaydedilirken diğerinin alanları gönderilmemeli.
    /// </summary>
    public class AyarlarModeli
    {
        public ProfilModeli Hesap { get; set; } = new();
        public SistemAyarlari Mail { get; set; } = new();

        /// <summary>
        /// Kayıtlı bir SMTP şifresi var mı? Şifrenin KENDİSİ görünüme
        /// hiç gitmiyor; alan yalnızca yer tutucu metnini ve "boş
        /// bırakırsanız değişmez" notunu belirliyor.
        /// </summary>
        public bool MailSifresiKayitli { get; set; }
    }
}
