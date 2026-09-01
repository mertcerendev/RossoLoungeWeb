/* =============================================================
   ROSSO LOUNGE — SAHNE MOTORU
   -------------------------------------------------------------
   Lenis (momentumlu kaydırma) + GSAP/ScrollTrigger/SplitText/Flip
   + sıvı imleç + sinematik perde.

   TASARIM KARARI — BOZULMAYA DAYANIKLILIK
   Kinetik girişler öğeleri CSS'te gizler. Bu gizleme yalnızca
   <html class="rosso-kinetik"> altında geçerlidir ve bu sınıfı
   AŞAĞIDAKİ kod, kütüphanelerin gerçekten yüklendiğini doğruladıktan
   sonra ekler. CDN düşerse / JS kapalıysa / kullanıcı hareket
   azaltma istiyorsa: sınıf hiç eklenmez, sayfa tam okunur açılır.
   ============================================================= */
(function () {
    'use strict';

    var kok = document.documentElement;
    var azHareket = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var inceIsaretci = window.matchMedia('(hover: hover) and (pointer: fine)').matches;

    var gsapVar = typeof window.gsap !== 'undefined';
    var stVar = gsapVar && typeof window.ScrollTrigger !== 'undefined';
    var lenisVar = typeof window.Lenis !== 'undefined';

    /* =========================================================
       0. YETENEK KAPISI
       Kinetik moda ancak her şey hazırsa geçilir.
       ========================================================= */
    var kinetik = gsapVar && stVar && !azHareket;

    if (kinetik) {
        kok.classList.add('rosso-kinetik');
        gsap.registerPlugin(ScrollTrigger);
        if (typeof window.SplitText !== 'undefined') gsap.registerPlugin(SplitText);
        if (typeof window.Flip !== 'undefined') gsap.registerPlugin(Flip);
    }

    /* =========================================================
       1. LENIS — momentumlu kaydırma
       ScrollTrigger ile senkron çalışması için tek rAF döngüsü.
       ========================================================= */
    var lenis = null;

    function lenisBaslat() {
        if (!lenisVar || azHareket) return;

        lenis = new Lenis({
            duration: 1.15,
            easing: function (t) { return Math.min(1, 1.001 - Math.pow(2, -10 * t)); },
            smoothWheel: true,
            syncTouch: false // dokunmatikte native kaydırma daha doğal
        });

        if (kinetik) {
            lenis.on('scroll', ScrollTrigger.update);
            gsap.ticker.add(function (zaman) { lenis.raf(zaman * 1000); });
            gsap.ticker.lagSmoothing(0);
        } else {
            var dongu = function (z) { lenis.raf(z); requestAnimationFrame(dongu); };
            requestAnimationFrame(dongu);
        }

        // Sayfa içi bağlantılar Lenis üzerinden aksın
        document.querySelectorAll('a[href^="#"]').forEach(function (bag) {
            bag.addEventListener('click', function (olay) {
                var hedefId = bag.getAttribute('href');
                if (!hedefId || hedefId === '#') return;
                var hedef = document.querySelector(hedefId);
                if (!hedef) return;
                olay.preventDefault();
                lenis.scrollTo(hedef, { offset: -84, duration: 1.3 });
            });
        });
    }

    /* =========================================================
       2. SİNEMATİK PERDE
       Sayaç 0→100, ardından perde iki panel halinde yırtılır ve
       hero içeriği zincirleme girer.
       ========================================================= */
    function perdeAc(sonra) {
        var perde = document.querySelector('.sahne-perde');
        var ustPanel = document.querySelector('.sahne-perde__panel--ust');
        var altPanel = document.querySelector('.sahne-perde__panel--alt');
        var sayac = document.querySelector('.sahne-perde__sayac');

        function perdeyiKaldir() {
            kok.classList.remove('rosso-kilit');
            if (lenis) lenis.start();
            if (perde && perde.parentNode) perde.remove();
            if (ustPanel && ustPanel.parentNode) ustPanel.remove();
            if (altPanel && altPanel.parentNode) altPanel.remove();
        }

        // Sayfa arka plan sekmesinde açıldıysa sinematik girişi hiç oynatma:
        // kullanıcı izlemiyor, üstelik gizli sekmede rAF durduğu için
        // zaman çizelgesi ilerlemez ve perde asla kalkmazdı.
        if (document.visibilityState === 'hidden') {
            perdeyiKaldir();
            if (sonra) sonra();
            return;
        }

        if (!perde || !kinetik) {
            if (perde) perde.remove();
            if (ustPanel) ustPanel.remove();
            if (altPanel) altPanel.remove();
            kok.classList.remove('rosso-kilit');
            if (sonra) sonra();
            return;
        }

        kok.classList.add('rosso-kilit');
        if (lenis) lenis.stop();

        var bitti = false;
        function tamamla() {
            if (bitti) return;
            bitti = true;
            perdeyiKaldir();
            if (sonra) sonra();
            ScrollTrigger.refresh();
        }

        // Emniyet: rAF durursa (arka plan sekmesi, ağır cihaz) perde
        // duvar saatiyle yine de kalkar. Kullanıcı asla siyah ekranda kalmaz.
        setTimeout(tamamla, 4200);

        var ilerleme = { deger: 0 };
        var zc = gsap.timeline({ onComplete: tamamla });

        zc.to(ilerleme, {
            deger: 100,
            duration: 1.1,
            ease: 'power2.inOut',
            onUpdate: function () {
                if (sayac) sayac.textContent = Math.round(ilerleme.deger).toString().padStart(3, '0');
            }
        })
          .to('.sahne-perde__marka', { opacity: 0, y: -18, duration: 0.45, ease: 'power2.in' }, '-=0.15')
          .to(sayac, { opacity: 0, duration: 0.35, ease: 'power2.in' }, '<')
          .set(perde, { autoAlpha: 0 })
          // Perde ortadan yırtılır
          .to(ustPanel, { yPercent: -100, duration: 1.0, ease: 'expo.inOut' }, 'yirt')
          .to(altPanel, { yPercent: 100, duration: 1.0, ease: 'expo.inOut' }, 'yirt');

        return zc;
    }

    /* =========================================================
       3. HERO GİRİŞİ
       Başlık satır satır (SplitText), diğerleri kayarak.
       ========================================================= */
    function heroGirisi() {
        if (!kinetik) return;

        var baslik = document.querySelector('.hero__baslik');
        var zc = gsap.timeline({ defaults: { ease: 'expo.out' } });

        if (baslik && typeof window.SplitText !== 'undefined') {
            var bolunmus = new SplitText(baslik, { type: 'lines', linesClass: 'kn-satir-ic' });
            // Her satırı taşan bir kaba al ki alttan yukarı "yükselsin"
            bolunmus.lines.forEach(function (satir) {
                var kap = document.createElement('span');
                kap.className = 'kn-satir';
                kap.style.display = 'block';
                satir.parentNode.insertBefore(kap, satir);
                kap.appendChild(satir);
            });
            gsap.set(baslik, { autoAlpha: 1 });
            zc.from(bolunmus.lines, { yPercent: 115, duration: 1.25, stagger: 0.09 });
        } else if (baslik) {
            zc.from(baslik, { yPercent: 20, autoAlpha: 0, duration: 1.1 });
        }

        zc.from('.hero__ustbaslik', { autoAlpha: 0, y: 18, duration: 0.9 }, 0.15)
          .from('.hero__alt', { autoAlpha: 0, y: 22, duration: 0.9 }, '-=0.85')
          .from('.hero__eylemler > *', { autoAlpha: 0, y: 24, duration: 0.8, stagger: 0.1 }, '-=0.7')
          .from('.hero__durum', { autoAlpha: 0, duration: 0.7 }, '-=0.5')
          .from('.nav__marka, .nav__menu > li, .nav__eylem', { autoAlpha: 0, y: -14, duration: 0.7, stagger: 0.06 }, 0.3);

        return zc;
    }

    /* =========================================================
       4. FARE DUYARLI 3D PARALLAX (hero)
       ========================================================= */
    function heroParallax() {
        var sahne = document.querySelector('.hero');
        var zemin = document.querySelector('.hero__zemin');
        if (!sahne || !zemin || !kinetik || !inceIsaretci) return;

        var xAyar = gsap.quickTo(zemin, 'xPercent', { duration: 0.9, ease: 'power3.out' });
        var yAyar = gsap.quickTo(zemin, 'yPercent', { duration: 0.9, ease: 'power3.out' });

        sahne.addEventListener('mousemove', function (olay) {
            var k = sahne.getBoundingClientRect();
            xAyar(((olay.clientX - k.left) / k.width - 0.5) * -2.4);
            yAyar(((olay.clientY - k.top) / k.height - 0.5) * -2.4);
        });

        sahne.addEventListener('mouseleave', function () { xAyar(0); yAyar(0); });

        // Kaydırdıkça arka plan geride kalır
        gsap.to(zemin, {
            yPercent: 12,
            ease: 'none',
            scrollTrigger: { trigger: sahne, start: 'top top', end: 'bottom top', scrub: true }
        });
    }

    /* =========================================================
       5. KAYDIRMA TETİKLİ GİRİŞLER
       ========================================================= */
    function kaydirmaGirisleri() {
        if (!kinetik) return;

        // Maskeli açılış
        gsap.utils.toArray('.kn-maske').forEach(function (oge) {
            gsap.to(oge, {
                clipPath: 'inset(0 0 0% 0)',
                duration: 1.3,
                ease: 'expo.out',
                scrollTrigger: { trigger: oge, start: 'top 82%' }
            });
        });

        // Hafif eğilmeyle yerleşen bloklar
        gsap.utils.toArray('.kn-kaydir').forEach(function (oge, i) {
            gsap.to(oge, {
                autoAlpha: 1, y: 0, skewY: 0,
                duration: 1.0, ease: 'expo.out', delay: (i % 4) * 0.05,
                scrollTrigger: { trigger: oge, start: 'top 88%' }
            });
        });

        gsap.utils.toArray('.kn-solgun').forEach(function (oge) {
            gsap.to(oge, {
                autoAlpha: 1, duration: 1.1, ease: 'power2.out',
                scrollTrigger: { trigger: oge, start: 'top 90%' }
            });
        });
    }

    /* =========================================================
       6. SIVI İMLEÇ
       ========================================================= */
    function imlecBaslat() {
        if (!inceIsaretci || azHareket || !gsapVar) return;

        var imlec = document.querySelector('.imlec');
        if (!imlec) return;

        kok.classList.add('rosso-imlec');

        var nokta = imlec.querySelector('.imlec__nokta');
        var halka = imlec.querySelector('.imlec__halka');

        // Nokta ani, halka gecikmeli → sıvı/ağırlık hissi
        var nx = gsap.quickTo(nokta, 'x', { duration: 0.12, ease: 'power3.out' });
        var ny = gsap.quickTo(nokta, 'y', { duration: 0.12, ease: 'power3.out' });
        var hx = gsap.quickTo(halka, 'x', { duration: 0.55, ease: 'power3.out' });
        var hy = gsap.quickTo(halka, 'y', { duration: 0.55, ease: 'power3.out' });

        window.addEventListener('mousemove', function (olay) {
            nx(olay.clientX); ny(olay.clientY);
            hx(olay.clientX); hy(olay.clientY);
        }, { passive: true });

        // Tıklanabilir öğelerde halka büyür
        document.addEventListener('mouseover', function (olay) {
            if (olay.target.closest('a, button, input, textarea, select, [role="button"]')) {
                imlec.classList.add('imlec--yakin');
            }
        });

        document.addEventListener('mouseout', function (olay) {
            if (olay.target.closest('a, button, input, textarea, select, [role="button"]')) {
                imlec.classList.remove('imlec--yakin');
            }
        });
    }

    /* =========================================================
       7. NAVBAR DURUMU
       ========================================================= */
    function navDurumu() {
        var nav = document.querySelector('.nav');
        if (!nav) return;

        var bekle = false;
        function guncelle() {
            nav.classList.toggle('nav--sabit', (window.scrollY || window.pageYOffset) > 40);
            bekle = false;
        }

        window.addEventListener('scroll', function () {
            if (bekle) return;
            bekle = true;
            requestAnimationFrame(guncelle);
        }, { passive: true });

        guncelle();
    }

    /* =========================================================
       8. MOBİL MENÜ
       ========================================================= */
    function menuBagla() {
        var dugme = document.querySelector('.nav__hamburger');
        var menu = document.querySelector('.nav__menu');
        var perde = document.querySelector('.nav-perde');
        if (!dugme || !menu) return;

        function ayarla(ac) {
            menu.classList.toggle('nav__menu--acik', ac);
            if (perde) perde.classList.toggle('nav-perde--acik', ac);
            dugme.setAttribute('aria-expanded', ac ? 'true' : 'false');
            if (lenis) ac ? lenis.stop() : lenis.start();
            document.body.style.overflow = ac ? 'hidden' : '';
        }

        dugme.addEventListener('click', function () {
            ayarla(dugme.getAttribute('aria-expanded') !== 'true');
        });
        if (perde) perde.addEventListener('click', function () { ayarla(false); });
        menu.addEventListener('click', function (o) { if (o.target.closest('a')) ayarla(false); });
        document.addEventListener('keydown', function (o) { if (o.key === 'Escape') ayarla(false); });
    }

    /* =========================================================
       BAŞLAT
       Her modül kendi try/catch'inde — biri patlarsa sahne durmaz.
       ========================================================= */
    var acildi = false;

    function baslat() {
        if (acildi) return;
        acildi = true;

        [lenisBaslat, imlecBaslat, navDurumu, menuBagla, heroParallax, kaydirmaGirisleri]
            .forEach(function (modul) {
                try { modul(); } catch (h) {
                    if (window.console) console.error('rosso:', modul.name, h);
                }
            });

        try { perdeAc(heroGirisi); } catch (h) {
            // Perde açılamazsa sahneyi kilitli bırakma
            kok.classList.remove('rosso-kilit', 'rosso-kinetik');
            var p = document.querySelector('.sahne-perde');
            if (p) p.remove();
            if (window.console) console.error('rosso: perde', h);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', baslat);
    } else {
        baslat();
    }

    window.addEventListener('load', baslat);
})();
