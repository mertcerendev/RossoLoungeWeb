using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RossoLoungeWeb.Binders;
using RossoLoungeWeb.Data;
using RossoLoungeWeb.Services;

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


// DataProtection anahtarlarını diske yaz. Paylaşımlı hostingde varsayılan konum
// uygulama havuzu geri döndüğünde kaybolur; bu da oturumların düşmesine yol açar.
var anahtarDizini = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
Directory.CreateDirectory(anahtarDizini);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(anahtarDizini))
    .SetApplicationName("RossoLoungeWeb");

// SMTP uygulama şifresini veritabanında şifreli tutar (bkz. Services/AyarKorumasi).
builder.Services.AddScoped<RossoLoungeWeb.Services.AyarKorumasi>();

// Veritabanı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

/* -------------------------------------------------------------
   KİMLİK DOĞRULAMA — ASP.NET Core Identity

   Önceden elle yazılmış bir oturum (session) kontrolü vardı:
   giriş bilgisi Session["User"]'a yazılıyor, her panel isteğinde
   bir action filtresi bunu kontrol ediyordu. Identity'ye geçildi;
   böylece kilitlenme (lockout), güvenlik damgası, şifre sıfırlama
   jetonları ve standart [Authorize] altyapısı hazır geliyor.

   Rol tabloları da kuruluyor: bugün tek bir yönetici var ve
   [Authorize] yetiyor, ama ileride "mutfak" / "garson" gibi
   sınırlı hesaplar açmak istenirse şema hazır olsun.
   ------------------------------------------------------------- */
builder.Services.AddIdentity<IdentityUser, IdentityRole>(secenekler =>
{
    // Şifre kuralları. Panel hesabı tek kişilik ama zayıf şifre
    // burada tüm siteyi açar; varsayılanların çoğu korunuyor.
    secenekler.Password.RequiredLength = 8;
    secenekler.Password.RequireNonAlphanumeric = false;
    secenekler.Password.RequireUppercase = true;
    secenekler.Password.RequireLowercase = true;
    secenekler.Password.RequireDigit = true;

    // KABA KUVVET KORUMASI — daha önce hiç yoktu (DEVAM.md'de açık madde).
    // 5 hatalı denemeden sonra hesap 15 dakika kilitlenir.
    secenekler.Lockout.MaxFailedAccessAttempts = 5;
    secenekler.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    secenekler.Lockout.AllowedForNewUsers = true;

    secenekler.User.RequireUniqueEmail = false; // tek hesap; e-posta zorunlu benzersizlik gereksiz
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddErrorDescriber<TurkceKimlikHatalari>()   // hata metinleri Türkçe
    .AddDefaultTokenProviders();

// Eski BCrypt özetleri geçerli kalsın diye (bkz. sınıfın açıklaması).
builder.Services.AddScoped<IPasswordHasher<IdentityUser>, BcryptGecisliSifreleyici>();

builder.Services.ConfigureApplicationCookie(secenekler =>
{
    secenekler.Cookie.Name = "RossoPanel";
    secenekler.Cookie.HttpOnly = true;
    secenekler.Cookie.SameSite = SameSiteMode.Lax;
    secenekler.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

    // Eski oturum süresiyle aynı: 2 saat, hareket ettikçe uzuyor.
    secenekler.ExpireTimeSpan = TimeSpan.FromHours(2);
    secenekler.SlidingExpiration = true;

    secenekler.LoginPath = "/Admin/Login";
    secenekler.LogoutPath = "/Admin/CikisYap";
    secenekler.AccessDeniedPath = "/Admin/Login";
});

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

// Sıra önemli: kimlik önce çözülür, yetki sonra denetlenir.
app.UseAuthentication();
app.UseAuthorization();

/* -------------------------------------------------------------
   KISA ADRESLER

   /menu gerçek bir sayfa: rotası HomeController.Menu'de [Route("menu")]
   olarak duruyor. Aşağıdakiler yönlendirme:

   - /Home/Menu   → /menu     Eski adres. Nitelikli rota gelince
                              {controller}/{action} kalıbından çıktığı
                              için artık 404 olurdu; yer imleri ve
                              dizine girmiş bağlantılar kırılmasın.
   - /hakkimizda, /galeri, /yorumlar, /iletisim, /rezervasyon
                              → ana sayfadaki karşılığı (/#hakkimizda …)
                              Bunlar AYRI SAYFA DEĞİL, ana sayfanın
                              bölümleri. Kendi adreslerinde ayrıca
                              render edilselerdi aynı içerik iki adreste
                              görünüp kopya sayılırdı; o yüzden kalıcı
                              (301) yönlendiriyorlar.

   Navbar bilerek hâlâ /#rezervasyon kullanıyor: kısa adres sunucuya
   gidip dönerdi, çapa ise sayfayı hiç terk etmeden kaydırıyor.
   ------------------------------------------------------------- */
static Task KaliciYonlendir(HttpContext baglam, string hedef)
{
    baglam.Response.Redirect(hedef, permanent: true);
    return Task.CompletedTask;
}

app.MapGet("/Home/Index", baglam => KaliciYonlendir(baglam, "/"));
app.MapGet("/Home/Menu", baglam => KaliciYonlendir(baglam, "/menu"));

// Ana sayfa bölümlerinin kısa adresleri. Bölüm kimlikleri Türkçe
// (#hakkimizda, #galeri, #yorumlar, #iletisim) — adres çubuğunda
// yarısı İngilizce yarısı Türkçe bir liste durmasın diye.
foreach (var bolum in new[] { "hakkimizda", "galeri", "yorumlar", "iletisim", "rezervasyon" })
{
    var hedef = "/#" + bolum;
    app.MapGet("/" + bolum, baglam => KaliciYonlendir(baglam, hedef));
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

/* -------------------------------------------------------------
   KİMLİK GEÇİŞİ
   Eski Yoneticiler tablosundaki hesapları Identity'ye taşır.
   İdempotent: taşınmış hesabı tekrar oluşturmaz, bu yüzden her
   açılışta güvenle çalışır ve canlıya çıkarken elle bir adım
   atlanmış olmaz.
   ------------------------------------------------------------- */
using (var kapsam = app.Services.CreateScope())
{
    var kayit = kapsam.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("KimlikGecisi");

    try
    {
        await KimlikGecisi.TasiAsync(kapsam.ServiceProvider, kayit);
    }
    catch (Exception hata)
    {
        // Geçiş başarısız olsa bile site ayakta kalmalı: ziyaretçi
        // tarafı kimlik doğrulamaya bağlı değil.
        kayit.LogError(hata, "Kimlik geçişi sırasında beklenmeyen hata.");
    }

    /* Veritabanında kalmış DÜZ METİN SMTP şifresini şifreler.
       İdempotent; şifrelenmiş kaydı tekrar sarmalamaz. */
    try
    {
        var koruma = kapsam.ServiceProvider.GetRequiredService<RossoLoungeWeb.Services.AyarKorumasi>();
        var baglam = kapsam.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await koruma.DuzMetniSifreleAsync(baglam);
    }
    catch (Exception hata)
    {
        kayit.LogError(hata, "SMTP şifresi şifrelenirken hata oluştu.");
    }
}

app.Run();
