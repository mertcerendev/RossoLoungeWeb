# Ön Yüz Mimarisi

Bu dosya, ziyaretçi sitesinin varlık (asset) düzenini ve neden böyle olduğunu anlatır.
Sayılar tarayıcıda ölçüldü, tahmin değil.

## Kabuk

Ziyaretçi sayfaları `Views/Shared/_Layout.cshtml` üzerinden render olur.

```
Views/Shared/
  _Layout.cshtml     Ziyaretçi kabuğu: head, CSS, script sırası, ortak parçalar
  _Nav.cshtml        Navbar (aktif bağlantı rotadan çıkar)
  _Imlec.cshtml      Global özel imleç — tek DOM örneği
```

Panel sayfaları (`Views/Admin`, `Views/Kategori`, `Views/Urun`) bilerek `Layout = null`
kullanmaya devam ediyor: arayüzleri tamamen farklı, ortak kabuk onlara zarar verir.

### Script sırası

`_Layout.cshtml` içinde sıra kritik ve şöyle:

1. Lenis + GSAP çekirdeği ve eklentileri (ScrollTrigger, SplitText, Flip)
2. `@section Scriptler` — sayfaya özel (ör. yalnızca ana sayfada flatpickr)
3. Uygulama: `rosso-hareket.js`, `ortak.js`, `script.js`

Hepsi `defer`. Ayrıştırmayı bloklamazlar ve **belge sırasında** çalışırlar, bu yüzden
3. adımdaki dosyalar 1. ve 2. adımı hazır bulur. `DOMContentLoaded` beklemeye gerek yok;
`rosso-hareket.js` yine de hem `DOMContentLoaded` hem `load` üzerine bağlanır ve
`acildi` bayrağıyla ikinci kez çalışmayı reddeder.

## Varlık düzeni

```
wwwroot/
  css/
    theme.css              (eski) tasarım jetonları — AĞIRLIKLA PANEL İÇİN
    style.css              (eski) genel stiller     — AĞIRLIKLA PANEL İÇİN
    rosso-tema.css         jetonlar, temel tipografi, imleç, hareket azaltma
    rosso-bilesenler.css   bileşenler (nav, hero, menü, galeri, finale, imleç durumları)
  js/
    rosso-hareket.js       sahne: Lenis, GSAP, imleç, çapa kaydırması, bölüm animasyonları
    script.js              ziyaretçi mantığı: toast, rezervasyon formu, iletişim sekmeleri
    ortak.js               her iki tarafta ortak yardımcılar
    panel.js               yalnızca yönetim paneli
```

### Neden `global.css` / `components/` değil

- **İngilizce yeniden adlandırma yapılmadı.** Projedeki *her* dosya, sınıf ve
  değişken adı Türkçe (`rosso-bilesenler`, `imlec__kap`, `menuSergisi`). Birkaç
  dosyayı İngilizceye çevirmek tutarlılığı bozar, kazanç getirmez.
- **`components/` altına bölünmedi.** Projede bir paketleyici (bundler) yok.
  `rosso-bilesenler.css`'i 12 parçaya bölmek 12 ayrı HTTP isteği demek olurdu.
  Bölme, ancak bir derleme adımı eklenirse anlamlı.

### Ölçülen borç

Ana sayfada seçici eşleşme oranı:

| Dosya | Kural | Eşleşen | Eşleşmeyen |
|---|---|---|---|
| `theme.css` | 80 | 13 | **%84** |
| `style.css` | 272 | 41 | **%85** |
| `rosso-tema.css` | 45 | 25 | %44 |
| `rosso-bilesenler.css` | 394 | 311 | %21 |

Eşleşmeyenlerin bir kısmı çalışma anında eklenen durum sınıfları (`.nav--sabit`,
`.nav__menu--acik`) — yani ölü değil. Yine de `theme.css` + `style.css` ağırlıkla
panel bileşenlerinden (`.rs-btn`, `.icerige-atla`, `.container`) oluşuyor ve
ziyaretçi sitesi bunları boşuna indiriyor.

**Sıradaki iş:** bu iki dosyanın ziyaretçi tarafında gereken ~%15'ini ayırıp
`rosso-*` katmanına taşımak, kalanını panel kabuğuna bırakmak. Körlemesine
yapılamaz: durum sınıfları statik ölçümde "eşleşmiyor" göründüğü için silinirse
canlı sitede stil kaybı olur. Sınıf sınıf doğrulama gerekir.

## Kaydırmanın tek sahibi

Sayfa içi çapa kaydırmasını **yalnızca** `rosso-hareket.js` yapar (`capaBagla`).
Dışarıya tek giriş açılmıştır:

```js
window.rossoKaydir(hedef, aninda)
```

`script.js` rezervasyon formunda ilk hatalı alana giderken bunu çağırır. İkinci bir
kaydırma mantığı yazılmamalı: eskiden `script.js` her `a[href^="#"]` tıklamasını
`preventDefault` edip `window.scrollTo` çağırıyordu ve Lenis aynı anda kendi
kaydırmasını yürüttüğü için hedef tam oturmuyordu.

Ofset, navbarın **canlı** yüksekliğinden hesaplanır (sabit sayı değil), çünkü navbar
kaydırınca kompakt duruma geçip kısalıyor. CSS'teki `scroll-margin-top` JS'siz yedek
olarak duruyor.
