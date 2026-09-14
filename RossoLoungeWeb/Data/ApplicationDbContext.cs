using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RossoLoungeWeb.Models;

namespace RossoLoungeWeb.Data
{
    /// <summary>
    /// Kimlik doğrulama ASP.NET Core Identity'ye taşındı; bu yüzden context
    /// artık <see cref="IdentityDbContext{TUser}"/> türetiyor (AspNetUsers,
    /// AspNetRoles… tabloları buradan geliyor).
    ///
    /// Eski <c>Yoneticiler</c> tablosu KASITLI olarak duruyor: geçiş sırasında
    /// mevcut yöneticinin kullanıcı adı/e-posta/BCrypt özeti oradan okunup
    /// Identity'ye taşınıyor (bkz. Data/KimlikGecisi.cs). Şema değişiklikleri
    /// yalnızca eklemeli olabildiği için tablo düşürülmedi; geçiş canlıda
    /// doğrulandıktan sonra ayrı bir migration ile kaldırılabilir.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
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
        public DbSet<Iletisim> IletisimMesajlari { get; set; }
        public DbSet<BultenAbone> BultenAboneleri { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- VERİ BÜTÜNLÜĞÜ ---
            // Önceden cascade idi: bir kategori silinince altındaki tüm ürünler
            // (ve dolayısıyla menü kalemleri) sessizce siliniyordu. Restrict ile
            // kategoride ürün varsa silme veritabanı seviyesinde engellenir;
            // kullanıcı önce ürünleri taşımalı ya da silmelidir.
            modelBuilder.Entity<Urun>()
                .HasOne(u => u.Kategori)
                .WithMany(k => k.Urunler)
                .HasForeignKey(u => u.KategoriId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- METİN SÜTUNLARI: TİP SABİTLEME ---
            // Modellerdeki [MaxLength] / [StringLength] nitelikleri bilinçli olarak
            // SADECE DOĞRULAMA amaçlıdır (boş/aşırı uzun form girdisine karşı).
            //
            // Bu sütunlar canlı veritabanında halen nvarchar(max). EF, nitelikleri
            // görüp sütunları nvarchar(100) gibi DARALTAN bir ALTER COLUMN üretir;
            // canlıda sınırı aşan tek bir satır varsa migration "String or binary
            // data would be truncated" hatasıyla yarıda kalır. Proje kuralı gereği
            // şema değişiklikleri yalnızca eklemeli olabilir, bu yüzden sütun tipi
            // açıkça nvarchar(max) olarak sabitleniyor ve migration'da hiçbir
            // daraltma yer almıyor.
            //
            // İleride daraltmak istenirse: önce her sütun için MAX(LEN(...)) kontrol
            // edilmeli, sonra bu satırlar kaldırılıp ayrı bir migration üretilmeli.
            // (Ayrıntılı liste ve kontrol sorgusu VERI-MIMARI raporunda.)
            modelBuilder.Entity<Iletisim>(e =>
            {
                e.Property(i => i.AdSoyad).HasColumnType("nvarchar(max)");
                e.Property(i => i.Email).HasColumnType("nvarchar(max)");
                e.Property(i => i.Telefon).HasColumnType("nvarchar(max)");
                e.Property(i => i.Mesaj).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<Yorum>(e =>
            {
                e.Property(y => y.AdSoyad).HasColumnType("nvarchar(max)");
                e.Property(y => y.Mesaj).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<Rezervasyon>(e =>
            {
                e.Property(r => r.AdSoyad).HasColumnType("nvarchar(max)");
                e.Property(r => r.Telefon).HasColumnType("nvarchar(max)");
                e.Property(r => r.Not).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<Urun>(e =>
            {
                e.Property(u => u.Ad).HasColumnType("nvarchar(max)");
                e.Property(u => u.Aciklama).HasColumnType("nvarchar(max)");
                e.Property(u => u.ResimUrl).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<Yonetici>()
                .Property(y => y.Eposta).HasColumnType("nvarchar(max)");

            modelBuilder.Entity<SistemAyarlari>(e =>
            {
                e.Property(a => a.GonderenMail).HasColumnType("nvarchar(max)");
                e.Property(a => a.GonderenSifre).HasColumnType("nvarchar(max)");
                e.Property(a => a.SmtpSunucu).HasColumnType("nvarchar(max)");
            });

            // --- İNDEKSLER ---
            // Sık filtrelenen / sıralanan kolonlar.
            modelBuilder.Entity<Yorum>().HasIndex(y => y.OnaylandiMi);          // ana sayfa: onaylı yorumlar
            modelBuilder.Entity<Rezervasyon>().HasIndex(r => r.Tarih);          // panel listesi ve 7 günlük grafik
            modelBuilder.Entity<Iletisim>().HasIndex(i => i.OkunduMu);          // panel: okunmamış mesaj sayacı
            modelBuilder.Entity<Urun>().HasIndex(u => u.SiraNo);                // menü sıralaması
        }
    }
}
