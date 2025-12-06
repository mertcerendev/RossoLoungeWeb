using System.ComponentModel.DataAnnotations;

namespace RossoLoungeWeb.Models
{
    public class SistemAyarlari
    {
        [Key]
        public int Id { get; set; }

        public string GonderenMail { get; set; } = ""; // Örn: rosso@gmail.com
        public string GonderenSifre { get; set; } = ""; // 16 haneli Google şifresi
        public string SmtpSunucu { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
    }
}