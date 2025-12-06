using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // Yeni eklendi

namespace RossoLoungeWeb.Models
{
    public class Yonetici
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required, MaxLength(250)] // Hashing sonrası şifre uzayacağı için 250 yaptık
        [JsonIgnore] // Şifrenin tarayıcıya JSON olarak gitmesini engeller (Ek Güvenlik)
        public string Sifre { get; set; } = string.Empty;

        [Required]
        public string Eposta { get; set; } = string.Empty;
    }
}