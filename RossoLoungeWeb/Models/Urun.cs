using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // ForeignKey / Column için gerekli

namespace RossoLoungeWeb.Models
{
    public class Urun
    {
        [Key]
        public int Id { get; set; }

        [Range(0, 9999, ErrorMessage = "Sıra numarası 0 ile 9999 arasında olmalıdır.")]
        [Display(Name = "Sıra No")]
        public int SiraNo { get; set; } // Sıralama

        [Required(ErrorMessage = "Ürün adı zorunludur.")]
        [MaxLength(100, ErrorMessage = "Ürün adı en fazla 100 karakter olabilir.")]
        [Display(Name = "Ürün Adı")]
        public string Ad { get; set; } = string.Empty; // Örn: "Margherita Pizza"

        // Sütun tipi nvarchar(max) kalıyor (bkz. ApplicationDbContext.OnModelCreating).
        [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; } // Örn: "Mozzarella, domates sos..."

        // decimal için sütun tipi açıkça belirtilmezse EF uyarı verir ve fiyatlar
        // sessizce yuvarlanabilir ("No store type was specified for the decimal property").
        [Required(ErrorMessage = "Fiyat zorunludur.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100000, ErrorMessage = "Fiyat 0 ile 100000 arasında olmalıdır.")]
        [Display(Name = "Fiyat")]
        public decimal Fiyat { get; set; } // Varsayılan (veya Orta Boy) Fiyat

        // Büyük Boy Fiyatı (Boş bırakılabilir)
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100000, ErrorMessage = "Fiyat 0 ile 100000 arasında olmalıdır.")]
        [Display(Name = "Büyük Boy Fiyatı")]
        public decimal? FiyatBuyuk { get; set; }

        // --- FİYAT ETİKETLERİ ---
        [MaxLength(50, ErrorMessage = "Fiyat etiketi en fazla 50 karakter olabilir.")]
        [Display(Name = "Fiyat Etiketi")]
        public string? FiyatTur { get; set; } // Örn: "Orta", "Kutu", "Kadeh"

        [MaxLength(50, ErrorMessage = "Fiyat etiketi en fazla 50 karakter olabilir.")]
        [Display(Name = "Büyük Boy Etiketi")]
        public string? FiyatBuyukTur { get; set; } // Örn: "Büyük", "Şişe", "Sürahi"
        // ------------------------------

        [MaxLength(300, ErrorMessage = "Görsel yolu en fazla 300 karakter olabilir.")]
        [Display(Name = "Görsel")]
        public string? ResimUrl { get; set; } // Resmin dosya yolu

        /// <summary>
        /// Ana sayfadaki vitrinde ("Bu haftanın seçkileri") gösterilsin mi?
        ///
        /// Öncesinde vitrin, SiraNo'su en küçük 3 ürünü alıyordu: seçim
        /// yapmanın tek yolu o tabakları bütün menünün en tepesine
        /// sürüklemekti — yani vitrin ile menü sırası aynı düğmeye bağlıydı.
        /// Artık seçim ayrı ve açık.
        ///
        /// Vitrin ızgarası CSS'te repeat(3, 1fr): en fazla ÜÇ tabak.
        /// Sınır UrunController'da uygulanıyor.
        /// </summary>
        [Display(Name = "Öne çıkan")]
        public bool OneCikan { get; set; }

        // --- İLİŞKİ AYARLARI ---

        // Hangi kategoriye ait? (Foreign Key)
        [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
        [Display(Name = "Kategori")]
        public int KategoriId { get; set; }

        // Bağlantı nesnesi.
        // DİKKAT: Burası bilinçli olarak `required` DEĞİL. Form gönderiminde bu nesne
        // dolmaz; `required` yapılırsa model binding kırılır. Controller'lar zaten
        // ModelState.Remove("Kategori") çağırıyor.
        [ForeignKey("KategoriId")]
        public virtual Kategori Kategori { get; set; } = null!;
    }
}
