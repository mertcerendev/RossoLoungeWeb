using Microsoft.EntityFrameworkCore;
using RossoLoungeWeb.Models;

namespace RossoLoungeWeb.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Rezervasyon> Rezervasyons { get; set; }
        public DbSet<Yonetici> Yoneticiler { get; set; }
        public DbSet<SistemAyarlari> Ayarlar { get; set; }
        public DbSet<Kategori> Kategoriler { get; set; }
        public DbSet<Urun> Urunler { get; set; }
        public DbSet<Yorum> Yorumlar { get; set; }

        
    }
}