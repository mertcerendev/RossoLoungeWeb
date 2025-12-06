using Microsoft.EntityFrameworkCore;
using RossoLoungeWeb.Data;

var builder = WebApplication.CreateBuilder(args);

// Servisler
builder.Services.AddControllersWithViews();

// 1. SESSION SERVÝSÝNÝ EKLE (BURAYI EKLE)
builder.Services.AddSession();

// Veritabaný
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 2. SESSION'I KULLAN (BURAYI EKLE - UseRouting'den sonra, MapControllerRoute'dan önce)
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();