using System.ComponentModel.DataAnnotations;
using RossoLoungeWeb.Services;

namespace RossoLoungeWeb.Models
{
    // Veritabanındaki "Rezervasyons" tablosunun karşılığı
    public class Rezervasyon
    {
        [Key] // Birincil Anahtar (Primary Key)
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [MaxLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [MaxLength(25, ErrorMessage = "Telefon numarası en fazla 25 karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string Telefon { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rezervasyon tarihi zorunludur.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Rezervasyon Tarihi")]
        public DateTime Tarih { get; set; }

        [Required(ErrorMessage = "Kişi sayısı zorunludur.")]
        [Range(1, 100, ErrorMessage = "Kişi sayısı 1 ile 100 arasında olmalıdır.")]
        [Display(Name = "Kişi Sayısı")]
        public int KisiSayisi { get; set; }

        // Sütun tipi nvarchar(max) kalıyor (bkz. ApplicationDbContext.OnModelCreating).
        [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
        [Display(Name = "Not")]
        public string? Not { get; set; } // Boş bırakılabilir

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime OlusturulmaTarihi { get; set; } = TurkiyeSaati.Simdi;

        [Display(Name = "Onaylandı")]
        public bool OnaylandiMi { get; set; } = false;
    }
}
