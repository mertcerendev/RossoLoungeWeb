using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RossoLoungeWeb.Binders
{
    /// <summary>
    /// Ondalık alanları hem virgülle hem noktayla kabul eder.
    /// Uygulama invariant kültürle çalıştığı için "12,50" varsayılan bağlayıcıda
    /// 1250 olarak okunuyordu (yüz katı fiyat). JavaScript tarafında düzeltme
    /// vardı ama JS devre dışıysa ya da istek doğrudan gönderildiyse hata
    /// geri geliyordu; koruma bu yüzden sunucu tarafına alındı.
    /// </summary>
    public class OndalikModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var deger = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (deger == ValueProviderResult.None) return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, deger);

            var ham = deger.FirstValue;
            bool bosBirakilabilir = Nullable.GetUnderlyingType(bindingContext.ModelType) != null;

            if (string.IsNullOrWhiteSpace(ham))
            {
                if (bosBirakilabilir) bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            if (decimal.TryParse(Normallestir(ham), NumberStyles.Number,
                                 CultureInfo.InvariantCulture, out var sonuc))
            {
                bindingContext.Result = ModelBindingResult.Success(sonuc);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName, "Geçerli bir sayı girin. Örnek: 12,50");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Son ayıracı ondalık kabul eder — ama yalnızca ardından 1-2 rakam
        /// geliyorsa. Böylece "12,50" ve "12.50" → 12.50 olurken, binlik ayıracı
        /// olan "1.234" → 1234 olarak okunur.
        /// </summary>
        private static string Normallestir(string ham)
        {
            var metin = ham.Trim().Replace(" ", "");
            int ayirac = Math.Max(metin.LastIndexOf(','), metin.LastIndexOf('.'));

            if (ayirac < 0) return metin;

            var tamKisim = metin[..ayirac].Replace(",", "").Replace(".", "");
            var kesirKisim = metin[(ayirac + 1)..];

            // 1-2 hane: ondalık. Diğer her durumda ayıraçlar binlik sayılır.
            return kesirKisim.Length is 1 or 2
                ? $"{tamKisim}.{kesirKisim}"
                : tamKisim + kesirKisim;
        }
    }

    /// <summary>Tüm decimal / decimal? alanlarını <see cref="OndalikModelBinder"/>'a yönlendirir.</summary>
    public class OndalikModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var tur = context.Metadata.UnderlyingOrModelType;
            return tur == typeof(decimal) ? new OndalikModelBinder() : null;
        }
    }
}
