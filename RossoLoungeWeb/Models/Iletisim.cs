using System.ComponentModel.DataAnnotations;
using RossoLoungeWeb.Services;

namespace RossoLoungeWeb.Models
{
    // İletişim formundan gelen mesajlar
    public class Iletisim
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [MaxLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(150, ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [MaxLength(25, ErrorMessage = "Telefon numarası en fazla 25 karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        // Sütun tipi nvarchar(max) kalıyor (bkz. ApplicationDbContext.OnModelCreating);
        // uzunluk sınırı sadece doğrulama katmanında, kötüye kullanıma karşı.
        [Required(ErrorMessage = "Mesaj alanı zorunludur.")]
        [StringLength(2000, MinimumLength = 5, ErrorMessage = "Mesaj 5 ile 2000 karakter arasında olmalıdır.")]
        [Display(Name = "Mesajınız")]
        public string Mesaj { get; set; } = string.Empty;

        [Display(Name = "Gönderilme Tarihi")]
        public DateTime Tarih { get; set; } = TurkiyeSaati.Simdi;

        [Display(Name = "Okundu")]
        public bool OkunduMu { get; set; } = false;
    }
}
