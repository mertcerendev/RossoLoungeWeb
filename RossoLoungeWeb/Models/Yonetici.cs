using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RossoLoungeWeb.Models
{
    public class Yonetici
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [MaxLength(50, ErrorMessage = "Kullanıcı adı en fazla 50 karakter olabilir.")]
        [Display(Name = "Kullanıcı Adı")]
        public string KullaniciAdi { get; set; } = string.Empty;

        // BCrypt hash'i saklanır, düz metin şifre asla veritabanına yazılmaz.
        [Required(ErrorMessage = "Şifre zorunludur.")]
        [MaxLength(250)] // Hashing sonrası şifre uzayacağı için 250
        [JsonIgnore] // Şifrenin tarayıcıya JSON olarak gitmesini engeller (Ek Güvenlik)
        [Display(Name = "Şifre")]
        public string Sifre { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(150, ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
        [Display(Name = "E-posta")]
        public string Eposta { get; set; } = string.Empty;
    }
}
