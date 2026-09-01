/* =============================================================
   ROSSO LOUNGE — HAREKET KATMANI
   -------------------------------------------------------------
   Preloader · fade-up · parallax · manyetik hover · navbar durumu
   Bağımlılık yok: IntersectionObserver + rAF + Web Animations.
   Hareket azaltma tercihine ve JS'siz duruma saygılıdır.
   ============================================================= */
(function () {
    'use strict';

    var azHareket = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var dokunmatik = window.matchMedia('(hover: none)').matches;

    /* ---------------------------------------------------------
       1. PRELOADER
       Sayfa yüklenince perdeyi kaldırır. Hata olsa bile CSS'teki
       güvenlik animasyonu 2.5sn sonra perdeyi kaldırıyor.
       --------------------------------------------------------- */
    function onyukleyiciyiKapat() {
        var perde = document.querySelector('.rosso-onyukleyici');
        if (!perde) return;

        perde.classList.add('rosso-onyukleyici--bitti');
        document.body.classList.add('rosso-hazir');
        setTimeout(function () {
            if (perde.parentNode) perde.parentNode.removeChild(perde);
        }, 700);
    }

    if (document.readyState === 'complete') {
        onyukleyiciyiKapat();
    } else {
        window.addEventListener('load', function () {
            setTimeout(onyukleyiciyiKapat, azHareket ? 0 : 320);
        });
        // Ağ takılırsa yine de aç
        setTimeout(onyukleyiciyiKapat, 3000);
    }

    /* ---------------------------------------------------------
       2. FADE-UP (.rosso-belir)
       --------------------------------------------------------- */
    function belirmeyiBaslat() {
        var hedefler = document.querySelectorAll('.rosso-belir');
        if (!hedefler.length) return;

        if (azHareket || !('IntersectionObserver' in window)) {
            hedefler.forEach(function (e) { e.classList.add('rosso-belir--acik'); });
            return;
        }

        var gozlemci = new IntersectionObserver(function (girisler) {
            girisler.forEach(function (giris) {
                if (!giris.isIntersecting) return;

                var oge = giris.target;
                // Kardeşler sırayla belirsin (kademeli giriş)
                var kardesler = Array.prototype.slice.call(oge.parentNode.children);
                var sira = kardesler.indexOf(oge);
                oge.style.setProperty('--rs-gecikme', Math.min(sira, 6) * 90 + 'ms');

                oge.classList.add('rosso-belir--acik');
                gozlemci.unobserve(oge);
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -8% 0px' });

        hedefler.forEach(function (e) { gozlemci.observe(e); });
    }

    /* ---------------------------------------------------------
       3. NAVBAR DURUMU + PARALLAX
       Tek scroll dinleyici, rAF ile sınırlanmış.
       --------------------------------------------------------- */
    function kaydirmaBagla() {
        var nav = document.querySelector('.rosso-nav');
        var zemin = document.querySelector('.rosso-hero__zemin');
        if (!nav && !zemin) return;

        var bekliyor = false;

        function guncelle() {
            var y = window.scrollY || window.pageYOffset;

            if (nav) nav.classList.toggle('rosso-nav--sabit', y > 40);

            // Parallax: arka plan kaydırmanın %18'i kadar geride kalır
            if (zemin && !azHareket && y < window.innerHeight * 1.2) {
                zemin.style.setProperty('--rs-parallax', (y * 0.18) + 'px');
            }

            bekliyor = false;
        }

        window.addEventListener('scroll', function () {
            if (bekliyor) return;
            bekliyor = true;
            window.requestAnimationFrame(guncelle);
        }, { passive: true });

        guncelle();
    }

    /* ---------------------------------------------------------
       4. MANYETİK HOVER
       İmleç butona yaklaşınca buton hafifçe imlece doğru kayar.
       Dokunmatikte ve hareket azaltmada devre dışı.
       --------------------------------------------------------- */
    function manyetikBagla() {
        if (azHareket || dokunmatik) return;

        var ogeler = document.querySelectorAll('.rosso-btn--manyetik');
        if (!ogeler.length) return;

        ogeler.forEach(function (oge) {
            var guc = 0.28;   // ne kadar takip etsin
            var menzil = 90;  // kaç px yakınlıkta tetiklensin

            function hareket(olay) {
                var k = oge.getBoundingClientRect();
                var mx = olay.clientX - (k.left + k.width / 2);
                var my = olay.clientY - (k.top + k.height / 2);
                var uzaklik = Math.hypot(mx, my);

                if (uzaklik > Math.max(k.width, k.height) / 2 + menzil) return sifirla();

                oge.style.setProperty('--rs-mx', (mx * guc).toFixed(1) + 'px');
                oge.style.setProperty('--rs-my', (my * guc).toFixed(1) + 'px');
            }

            function sifirla() {
                oge.style.setProperty('--rs-mx', '0px');
                oge.style.setProperty('--rs-my', '0px');
            }

            window.addEventListener('mousemove', hareket, { passive: true });
            oge.addEventListener('mouseleave', sifirla);
            oge.addEventListener('blur', sifirla);
        });
    }

    /* ---------------------------------------------------------
       5. MOBİL MENÜ
       --------------------------------------------------------- */
    function menuBagla() {
        var dugme = document.querySelector('.rosso-nav__hamburger');
        var menu = document.querySelector('.rosso-nav__menu');
        var perde = document.querySelector('.rosso-perde');
        if (!dugme || !menu) return;

        function ayarla(ac) {
            menu.classList.toggle('rosso-nav__menu--acik', ac);
            if (perde) perde.classList.toggle('rosso-perde--acik', ac);
            dugme.setAttribute('aria-expanded', ac ? 'true' : 'false');
            document.body.style.overflow = ac ? 'hidden' : '';
        }

        dugme.addEventListener('click', function () {
            ayarla(dugme.getAttribute('aria-expanded') !== 'true');
        });

        if (perde) perde.addEventListener('click', function () { ayarla(false); });

        menu.addEventListener('click', function (olay) {
            if (olay.target.closest('a')) ayarla(false);
        });

        document.addEventListener('keydown', function (olay) {
            if (olay.key === 'Escape') ayarla(false);
        });
    }

    /* ---------------------------------------------------------
       6. AKTİF MENÜ BAĞLANTISI
       --------------------------------------------------------- */
    function aktifBaglantiBagla() {
        var bolumler = document.querySelectorAll('section[id]');
        var linkler = document.querySelectorAll('.rosso-nav__link[href^="#"]');
        if (!bolumler.length || !linkler.length || !('IntersectionObserver' in window)) return;

        var gozlemci = new IntersectionObserver(function (girisler) {
            girisler.forEach(function (giris) {
                if (!giris.isIntersecting) return;
                linkler.forEach(function (l) {
                    l.classList.toggle('rosso-nav__link--aktif',
                        l.getAttribute('href') === '#' + giris.target.id);
                });
            });
        }, { rootMargin: '-45% 0px -50% 0px' });

        bolumler.forEach(function (b) { gozlemci.observe(b); });
    }

    /* ---------------------------------------------------------
       BAŞLATMA
       Her modül kendi try/catch'inde: biri hata verirse diğerleri
       çalışmaya devam eder. İki kez çağrılırsa zarar vermesin diye
       her modül idempotent (tekrar bağlanmayı kendi içinde engeller).
       --------------------------------------------------------- */
    var baslatildi = false;

    function baslat() {
        if (baslatildi) return;
        baslatildi = true;

        [belirmeyiBaslat, kaydirmaBagla, manyetikBagla, menuBagla, aktifBaglantiBagla]
            .forEach(function (modul) {
                try {
                    modul();
                } catch (hata) {
                    // Tek bir modülün hatası sayfayı sessizce felç etmesin
                    if (window.console) console.error('rosso-hareket:', modul.name, hata);
                }
            });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', baslat);
    } else {
        baslat();
    }

    // Emniyet: DOMContentLoaded kaçırılmış olsa bile
    window.addEventListener('load', baslat);
})();
