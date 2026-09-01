using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using RossoLoungeWeb.Binders;
using RossoLoungeWeb.Data;

// Kültürü sabitle. Form verileri (tarih, fiyat) sunucunun bölge ayarına göre
// yorumlanır; sabitlemezsek aynı kod yerelde ve sunucuda farklı davranır.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Servisler
builder.Services.AddControllersWithViews(secenekler =>
{
    // Fiyat alanlarında virgül de kabul edilsin. Sunucu invariant kültürle
    // çalıştığı için "12,50" varsayılan bağlayıcıda 1250 olarak okunuyordu.
    secenekler.ModelBinderProviders.Insert(0, new OndalikModelBinderProvider());
});

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// DataProtection anahtarlarını diske yaz. Paylaşımlı hostingde varsayılan konum
// uygulama havuzu geri döndüğünde kaybolur; bu da oturumların düşmesine yol açar.
var anahtarDizini = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
Directory.CreateDirectory(anahtarDizini);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(anahtarDizini))
    .SetApplicationName("RossoLoungeWeb");

// Veritabanı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 404 gibi durum kodlarında boş beyaz sayfa yerine hata görünümü gösterilsin.
app.UseStatusCodePagesWithReExecute("/Home/Error", "?kod={0}");

app.UseHttpsRedirection();

// Statik dosyalar için tarayıcı önbelleği.
// Görseller/fontlar nadiren değişir ve çok yer kaplar (125 ürün fotoğrafı) -> 30 gün.
// CSS/JS ise sık güncelleniyor ve dosya adlarında sürüm damgası YOK; uzun süre
// önbelleğe alınırsa kullanıcılar eski tasarımda kalır -> 1 saat.
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var uzanti = Path.GetExtension(ctx.File.Name).ToLowerInvariant();
        var uzunOmurlu = uzanti is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif"
                                or ".svg" or ".ico" or ".woff" or ".woff2" or ".ttf";

        ctx.Context.Response.Headers.CacheControl = uzunOmurlu
            ? "public,max-age=2592000"      // 30 gün
            : "public,max-age=3600";        // 1 saat
    }
});

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
