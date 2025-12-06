using System.ComponentModel.DataAnnotations;

namespace RossoLoungeWeb.Models
{
    public class Yorum
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AdSoyad { get; set; }

        [Required]
        public string Mesaj { get; set; }

        [Range(1, 5)]
        public int Puan { get; set; } // 1 ile 5 arası yıldız

        public DateTime Tarih { get; set; } = DateTime.Now;

        public bool OnaylandiMi { get; set; } = false; // Varsayılan: Gizli
    }
}