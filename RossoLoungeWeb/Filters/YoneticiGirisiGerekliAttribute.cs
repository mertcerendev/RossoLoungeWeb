using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RossoLoungeWeb.Filters
{
    /// <summary>
    /// Panel (yönetim) sayfaları için oturum kontrolü.
    ///
    /// Neden gerekli: Kontrol şu an her action'ın ilk satırında elle yapılıyor
    /// (<c>if (HttpContext.Session.GetString("User") == null) ...</c>). Bir action'da
    /// unutulursa o sayfa herkese açık kalır. Bu filtre kontrolü tek yere toplar.
    ///
    /// Kullanımı — controller'ın tamamı için:
    /// <code>
    /// [YoneticiGirisiGerekli]
    /// public class KategoriController : Controller { ... }
    /// </code>
    /// Tek action için <c>[YoneticiGirisiGerekli]</c> metodun üstüne konur.
    ///
    /// DİKKAT: <c>AdminController</c> sınıf düzeyinde işaretlenirse
    /// <c>Login</c> ve <c>LoginPost</c> action'larına <c>[GirisSerbest]</c>
    /// eklenmelidir, aksi halde giriş sayfası sonsuz yönlendirmeye girer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class YoneticiGirisiGerekliAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Action ya da controller [GirisSerbest] ile işaretliyse kontrolü atla.
            var serbestMi = context.ActionDescriptor.EndpointMetadata
                                   .OfType<GirisSerbestAttribute>()
                                   .Any();

            if (!serbestMi && string.IsNullOrEmpty(context.HttpContext.Session.GetString("User")))
            {
                context.Result = new RedirectToActionResult("Login", "Admin", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// <see cref="YoneticiGirisiGerekliAttribute"/> ile işaretlenmiş bir controller'da
    /// oturum kontrolünden muaf tutulacak action'lar için (giriş sayfası gibi).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class GirisSerbestAttribute : Attribute
    {
    }
}
