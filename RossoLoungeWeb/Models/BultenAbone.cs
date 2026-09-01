using System.ComponentModel.DataAnnotations;
using RossoLoungeWeb.Services;

namespace RossoLoungeWeb.Models
{
    // Alt bilgideki e-bülten formundan gelen kayıtlar
    public class BultenAbone
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [MaxLength(150, ErrorMessage = "E-posta adresi en fazla 150 karakter olabilir.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Kayıt Tarihi")]
        public DateTime Tarih { get; set; } = TurkiyeSaati.Simdi;

        // Abonelikten çıkanlar silinmez, bu bayrak false olur
        [Display(Name = "Aktif")]
        public bool AktifMi { get; set; } = true;
    }
}
