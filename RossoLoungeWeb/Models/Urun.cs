using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // ForeignKey için gerekli

namespace RossoLoungeWeb.Models
{
    public class Urun
    {
        [Key]
        public int Id { get; set; }

        public int SiraNo { get; set; } // Sıralama

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        public string Ad { get; set; } // Örn: "Margherita Pizza"

        public string? Aciklama { get; set; } // Örn: "Mozzarella, domates sos..."

        [Required]
        public decimal Fiyat { get; set; } // Varsayılan (veya Orta Boy) Fiyat

        // Büyük Boy Fiyatı (Boş bırakılabilir)
        public decimal? FiyatBuyuk { get; set; }

        // --- YENİ EKLENEN ETİKETLER ---
        [MaxLength(50)]
        public string? FiyatTur { get; set; } // Örn: "Orta", "Kutu", "Kadeh"

        [MaxLength(50)]
        public string? FiyatBuyukTur { get; set; } // Örn: "Büyük", "Şişe", "Sürahi"
        // ------------------------------

        public string? ResimUrl { get; set; } // Resmin dosya yolu

        // --- İLİŞKİ AYARLARI ---

        // Hangi kategoriye ait? (Foreign Key)
        [Display(Name = "Kategori")]
        public int KategoriId { get; set; }

        // Bağlantı nesnesi
        [ForeignKey("KategoriId")]
        public virtual Kategori Kategori { get; set; }
    }
}