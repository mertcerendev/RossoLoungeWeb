using System.ComponentModel.DataAnnotations;

namespace RossoLoungeWeb.Models
{
    public class Kategori
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [MaxLength(50, ErrorMessage = "Kategori adı en fazla 50 karakter olabilir.")]
        [Display(Name = "Kategori Adı")]
        public string Ad { get; set; } = string.Empty; // Örn: "Pizzalar", "İçecekler"

        [Range(0, 9999, ErrorMessage = "Sıra numarası 0 ile 9999 arasında olmalıdır.")]
        [Display(Name = "Sıra No")]
        public int SiraNo { get; set; }

        // İLİŞKİ (Navigation Property):
        // Bir kategorinin birden fazla ürünü olabilir.
        public virtual ICollection<Urun> Urunler { get; set; } = new List<Urun>();
    }
}
