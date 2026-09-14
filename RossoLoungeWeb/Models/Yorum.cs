using System.ComponentModel.DataAnnotations;
using RossoLoungeWeb.Services;

namespace RossoLoungeWeb.Models
{
    public class Yorum
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [MaxLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        // Sütun tipi nvarchar(max) kalıyor (bkz. ApplicationDbContext.OnModelCreating).
        [Required(ErrorMessage = "Yorum metni zorunludur.")]
        [StringLength(1000, MinimumLength = 3, ErrorMessage = "Yorum 3 ile 1000 karakter arasında olmalıdır.")]
        [Display(Name = "Yorumunuz")]
        public string Mesaj { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")]
        [Display(Name = "Puan")]
        public int Puan { get; set; } // 1 ile 5 arası yıldız

        [Display(Name = "Tarih")]
        public DateTime Tarih { get; set; } = TurkiyeSaati.Simdi;

        [Display(Name = "Onaylandı")]
        public bool OnaylandiMi { get; set; } = false; // Varsayılan: Gizli
    }
}
