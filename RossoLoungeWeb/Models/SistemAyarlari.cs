using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RossoLoungeWeb.Models
{
    public class SistemAyarlari
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Gönderen e-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(150, ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
        [Display(Name = "Gönderen E-posta")]
        public string GonderenMail { get; set; } = ""; // Örn: rosso@gmail.com

        // GÜVENLİK NOTU: Bu alan SMTP uygulama şifresini veritabanında DÜZ METİN
        // olarak tutuyor. Veritabanı yedeği veya bir SQL injection sızıntısı doğrudan
        // mail hesabının ele geçirilmesi demektir.
        // Kalıcı çözüm: değeri IDataProtector ile şifreleyip saklamak
        // (DataProtection Program.cs'te zaten kayıtlı ve anahtarlar App_Data/keys'te
        // kalıcı) ya da şifreyi hiç veritabanında tutmayıp appsettings/ortam
        // değişkeninden okumak. Bu faz için sadece işaretlendi; şema değişmedi.
        [MaxLength(200)]
        [DataType(DataType.Password)]
        [JsonIgnore] // Şifrenin JSON yanıtlarına sızmasını engeller.
        [Display(Name = "Uygulama Şifresi")]
        public string GonderenSifre { get; set; } = ""; // 16 haneli Google uygulama şifresi

        [Required(ErrorMessage = "SMTP sunucusu zorunludur.")]
        [MaxLength(100, ErrorMessage = "SMTP sunucusu en fazla 100 karakter olabilir.")]
        [Display(Name = "SMTP Sunucusu")]
        public string SmtpSunucu { get; set; } = "smtp.gmail.com";

        [Range(1, 65535, ErrorMessage = "Port 1 ile 65535 arasında olmalıdır.")]
        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; } = 587;
    }
}
