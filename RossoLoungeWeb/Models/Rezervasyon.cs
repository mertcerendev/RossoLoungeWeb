using System.ComponentModel.DataAnnotations;

namespace RossoLoungeWeb.Models
{
    // Veritabanındaki "Rezervasyons" tablosunun karşılığı
    public class Rezervasyon
    {
        [Key] // Birincil Anahtar (Primary Key)
        public int Id { get; set; }

        [Required] // Zorunlu alan
        public string AdSoyad { get; set; } = string.Empty;

        [Required]
        public string Telefon { get; set; } = string.Empty;

        [Required]
        public DateTime Tarih { get; set; }

        [Required]
        public int KisiSayisi { get; set; }

        public string? Not { get; set; } // Boş bırakılabilir

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;

        public bool OnaylandiMi { get; set; } = false;
    }
}