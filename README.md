# Rosso Lounge Bistro — Web Platformu

Rosso Lounge Bistro için hazırlanmış tanıtım sitesi ve yönetim paneli.
Menü, rezervasyon, yorum ve iletişim mesajları koda dokunmadan panelden
yönetilir — işletmenin kendi içeriğini kendi güncelleyebilmesi hedeflendi.

**Yığın:** ASP.NET Core MVC · Entity Framework Core · SQL Server LocalDB ·
Bootstrap

---

## Ne yapar

**Ziyaretçi tarafı**
- Ana sayfa ve kategorilere ayrılmış menü
- Rezervasyon talebi
- Yorum bırakma
- İletişim formu

**Yönetim paneli** (`/Admin`)
- Kategori ve ürün ekleme, düzenleme, sıralama
- Gelen rezervasyonları görüntüleme
- Yorumları onaylama/yönetme
- İletişim mesajlarını okuma
- Mail ayarları ve yönetici profili
- Parola sıfırlama akışı

## Proje yapısı

```
RossoLoungeWeb/
├── Controllers/
│   ├── HomeController.cs        # Ana sayfa, menü
│   ├── AdminController.cs       # Panel, giriş, mesajlar, rezervasyonlar
│   ├── KategoriController.cs    # Kategori CRUD
│   └── UrunController.cs        # Ürün CRUD
├── Models/
│   ├── Kategori.cs  Urun.cs     # Menü yapısı
│   ├── Rezervasyon.cs  Yorum.cs
│   ├── Iletisim.cs              # İletişim formu kayıtları
│   ├── Yonetici.cs              # Panel kullanıcısı
│   └── SistemAyarlari.cs        # Mail vb. ayarlar
├── Data/ApplicationDbContext.cs
├── Migrations/                  # 11 migration
├── Views/                       # Razor sayfaları
└── wwwroot/                     # CSS, JS, ürün görselleri, Bootstrap
```

## Çalıştırma

Gereken: .NET SDK ve SQL Server LocalDB (Visual Studio ile birlikte gelir).

```bash
# 1. Bağımlılıklar
dotnet restore

# 2. Veritabanını oluştur
dotnet ef database update

# 3. Çalıştır
dotnet run
```

Bağlantı dizesi `appsettings.json` içinde:

```
Server=(localdb)\mssqllocaldb;Database=RossoLoungeDB;Trusted_Connection=True
```

## Veri modelinin gelişimi

Migration adları projenin nasıl büyüdüğünü sırayla gösteriyor:
ilk kurulum → yönetici tablosu → ayarlar → menü tabloları → ürün ve kategori
sıra numaraları → iletişim → yorumlar → ürün fiyat etiketleri.

Fiyat tarafındaki son iki migration, tek fiyat alanının yetmediği anlaşılınca
eklendi: aynı ürünün büyük/küçük gibi farklı porsiyonları ayrı etiketlerle
tutuluyor.

## Notlar

- Ürün görselleri `wwwroot/img/urunler/` altında GUID isimlerle saklanır;
  yükleme panelden yapılır.
- `DbScripts/TemporaryQuery.sql` geliştirme sırasında kullanılmış geçici
  sorgular içerir, uygulamanın çalışması için gerekli değildir.
