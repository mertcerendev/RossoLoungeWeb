using System.ComponentModel.DataAnnotations;

namespace RossoLoungeWeb.Models
{
    public class Kategori
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur.")]
        [MaxLength(50)]
        public string Ad { get; set; } // Örn: "Pizzalar", "İçecekler"
        public int SiraNo { get; set; }

        // İLİŞKİ (Navigation Property):
        // Bir kategorinin birden fazla ürünü olabilir.
        // Bu satır sayesinde veritabanında ilişki kurulacak.
        public virtual ICollection<Urun> Urunler { get; set; }
    }
}