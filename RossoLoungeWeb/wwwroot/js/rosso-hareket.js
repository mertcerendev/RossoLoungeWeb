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
       9. HAKKIMIZDA BÖLÜMÜ
       - Çerçeveler maskeyle açılır (clip-path)
       - İçlerindeki fotoğraflar ZIT yönde, farklı hızda kayar
       - Başlık kelime kelime maskeden yükselir
       - Gövde metni okuma hizasına gelince yumuşak açılır
       ========================================================= */
    function hakkindaBolumu() {
        var bolum = document.querySelector('.hakkinda');
        if (!bolum || !kinetik) return;

        /* --- Çerçevelerin maskeli açılışı --- */
        gsap.utils.toArray('.hakkinda__cerceve').forEach(function (cerceve, i) {
            gsap.to(cerceve, {
                clipPath: 'inset(0 0 0% 0)',
                duration: 1.4,
                ease: 'expo.out',
                delay: i * 0.12,
                scrollTrigger: { trigger: cerceve, start: 'top 85%' }
            });
        });

        /* --- Zıt yönlü parallax ---
           data-parallax değeri yPercent hedefi. Geniş çerçeve negatif
           (yukarı), dar çerçeve pozitif (aşağı) → ters akış. */
        gsap.utils.toArray('.hakkinda__cerceve').forEach(function (cerceve) {
            var foto = cerceve.querySelector('.hakkinda__foto');
            var mesafe = parseFloat(cerceve.dataset.parallax || '0');
            if (!foto || !mesafe) return;

            gsap.fromTo(foto,
                { yPercent: -mesafe },
                {
                    yPercent: mesafe,
                    ease: 'none',
                    scrollTrigger: {
                        trigger: cerceve,
                        start: 'top bottom',
                        end: 'bottom top',
                        scrub: 1.1
                    }
                });
        });

        /* --- Başlık: kelime kelime maskeden yükselir --- */
        var baslik = bolum.querySelector('[data-kelime-acilis]');
        if (baslik && typeof window.SplitText !== 'undefined') {
            var bol = new SplitText(baslik, { type: 'words' });

            // Her kelimeyi taşan bir kaba al → maske etkisi
            bol.words.forEach(function (kelime) {
                var kap = document.createElement('span');
                kap.className = 'kelime-kap';
                kelime.parentNode.insertBefore(kap, kelime);
                kap.appendChild(kelime);
            });

            gsap.from(bol.words, {
                yPercent: 118,
                duration: 1.05,
                ease: 'expo.out',
                stagger: 0.045,
                scrollTrigger: { trigger: baslik, start: 'top 84%' }
            });
        }

        /* --- Gövde metni: okuma hizasına gelince --- */
        gsap.to('.hakkinda__govde', {
            autoAlpha: 1,
            duration: 1.2,
            ease: 'power2.out',
            scrollTrigger: { trigger: '.hakkinda__govde', start: 'top 78%' }
        });

        /* --- Ölçütler, buton, yıl rozeti --- */
        gsap.to('.hakkinda__olcut', {
            autoAlpha: 1, duration: 1, ease: 'power2.out',
            scrollTrigger: { trigger: '.hakkinda__olcut', start: 'top 88%' }
        });

        gsap.to('.hakkinda__eylemler', {
            autoAlpha: 1, duration: 1, ease: 'power2.out',
            scrollTrigger: { trigger: '.hakkinda__eylemler', start: 'top 92%' }
        });

        gsap.to('.hakkinda__yil', {
            autoAlpha: 1, duration: 0.9, ease: 'power2.out', delay: 0.5,
            scrollTrigger: { trigger: '.hakkinda__gorseller', start: 'top 70%' }
        });

        /* --- İmza alıntısı --- */
        gsap.to('.hakkinda__alinti', {
            autoAlpha: 1,
            duration: 1.5,
            ease: 'power2.out',
            scrollTrigger: { trigger: '.hakkinda__alinti', start: 'top 86%' }
        });
    }


    /* =========================================================
       11. MENÜ SERGİSİ
       - Kategori filtresi: GSAP Flip ile pürüzsüz yer değiştirme,
         girenlerde blur + yukarıdan kayma
       - İmleç görsel önizlemesi: hıza bağlı skew ile fareyi takip
       ========================================================= */
    /* Ana sayfadaki vitrin (seçilmiş birkaç tabak) */
    function vitrinBolumu() {
        var bolum = document.querySelector('.vitrin');
        if (!bolum || !kinetik) return;

        gsap.to(bolum.querySelectorAll('.vitrin__bas > *'), {
            autoAlpha: 1, y: 0, duration: 0.9, ease: 'expo.out', stagger: 0.08,
            scrollTrigger: { trigger: bolum, start: 'top 78%' }
        });

        gsap.to(bolum.querySelectorAll('.vitrin__kart'), {
            autoAlpha: 1, y: 0, duration: 1, ease: 'expo.out', stagger: 0.12,
            scrollTrigger: { trigger: bolum.querySelector('.vitrin__izgara'), start: 'top 84%' }
        });

        gsap.to(bolum.querySelector('.vitrin__eylem'), {
            autoAlpha: 1, duration: 0.9, ease: 'power2.out',
            scrollTrigger: { trigger: bolum.querySelector('.vitrin__eylem'), start: 'top 92%' }
        });
    }

    function menuSergisi() {
        var bolum = document.querySelector('.menu');
        if (!bolum) return;

        var liste = bolum.querySelector('#menu-liste');
        var kalemler = Array.prototype.slice.call(bolum.querySelectorAll('.menu__kalem'));
        var sekmeler = Array.prototype.slice.call(bolum.querySelectorAll('.menu__sekme'));
        var bosMesaj = bolum.querySelector('.menu__bos');
        if (!liste || !kalemler.length) return;

        /* ---------- Giriş animasyonu ---------- */
        if (kinetik) {
            gsap.to(bolum.querySelectorAll('.menu__bas > *'), {
                autoAlpha: 1, y: 0, duration: 0.9, ease: 'expo.out', stagger: 0.08,
                scrollTrigger: { trigger: bolum, start: 'top 78%' }
            });

            gsap.to(kalemler, {
                autoAlpha: 1, duration: 0.7, ease: 'power2.out', stagger: 0.04,
                scrollTrigger: { trigger: liste, start: 'top 85%' }
            });
        }

        /* ---------- Kategori filtresi ---------- */
        function filtrele(deger) {
            var flipVar = kinetik && typeof window.Flip !== 'undefined';
            var durum = flipVar ? Flip.getState(kalemler) : null;

            var gorunen = 0;
            kalemler.forEach(function (kalem) {
                var uygun = deger === 'tumu' || kalem.dataset.kategori === deger;
                kalem.hidden = !uygun;
                if (uygun) gorunen++;
            });

            liste.classList.toggle('menu__liste--filtreli', deger !== 'tumu');
            if (bosMesaj) bosMesaj.hidden = gorunen > 0;

            if (!flipVar) return;

            Flip.from(durum, {
                duration: 0.62,
                ease: 'power2.inOut',
                absolute: true,
                // Ayrılanlar bulanıklaşarak çıkar
                onLeave: function (ogeler) {
                    gsap.to(ogeler, { autoAlpha: 0, filter: 'blur(8px)', duration: 0.32, ease: 'power2.in' });
                },
                // Girenler yukarıdan, bulanıklıktan netleşerek yerleşir
                onEnter: function (ogeler) {
                    gsap.fromTo(ogeler,
                        { autoAlpha: 0, y: -26, filter: 'blur(10px)' },
                        { autoAlpha: 1, y: 0, filter: 'blur(0px)', duration: 0.55, ease: 'expo.out', stagger: 0.035 });
                }
            });
        }

        sekmeler.forEach(function (sekme) {
            sekme.addEventListener('click', function () {
                if (sekme.classList.contains('menu__sekme--aktif')) return;

                sekmeler.forEach(function (s) {
                    var aktif = s === sekme;
                    s.classList.toggle('menu__sekme--aktif', aktif);
                    s.setAttribute('aria-selected', aktif ? 'true' : 'false');
                    s.tabIndex = aktif ? 0 : -1;
                });

                filtrele(sekme.dataset.filtre);
            });
        });

        // Ok tuşlarıyla sekmeler arası gezinme (WAI-ARIA tab deseni)
        var sekmeKabi = bolum.querySelector('.menu__filtre-ic');
        if (sekmeKabi) {
            sekmeKabi.addEventListener('keydown', function (olay) {
                var su = sekmeler.indexOf(document.activeElement);
                if (su < 0) return;
                var hedef = null;
                if (olay.key === 'ArrowRight') hedef = sekmeler[(su + 1) % sekmeler.length];
                else if (olay.key === 'ArrowLeft') hedef = sekmeler[(su - 1 + sekmeler.length) % sekmeler.length];
                else if (olay.key === 'Home') hedef = sekmeler[0];
                else if (olay.key === 'End') hedef = sekmeler[sekmeler.length - 1];
                if (!hedef) return;
                olay.preventDefault();
                hedef.focus();
                hedef.click();
            });
        }

        /* ---------- İmleç görsel önizlemesi ---------- */
        var onizleme = bolum.querySelector('.menu__onizleme');
        var foto = onizleme && onizleme.querySelector('.menu__onizleme-foto');

        // Yalnızca gerçek fare + GSAP varken; dokunmatikte hiç açılmaz
        if (!onizleme || !foto || !inceIsaretci || azHareket || !gsapVar) return;

        onizleme.style.display = 'block';

        var xAyar = gsap.quickTo(onizleme, 'x', { duration: 0.55, ease: 'power3.out' });
        var yAyar = gsap.quickTo(onizleme, 'y', { duration: 0.55, ease: 'power3.out' });
        var egimAyar = gsap.quickTo(onizleme, 'skewY', { duration: 0.5, ease: 'power3.out' });

        var sonX = 0, sonZaman = 0, aktifKalem = null;

        window.addEventListener('mousemove', function (olay) {
            if (!aktifKalem) return;

            // Görsel imlecin sağ-altında dursun, ekran dışına taşmasın
            var g = onizleme.offsetWidth, y = onizleme.offsetHeight;
            var hx = Math.min(olay.clientX + 28, window.innerWidth - g - 16);
            var hy = Math.min(Math.max(olay.clientY - y / 2, 16), window.innerHeight - y - 16);
            xAyar(hx); yAyar(hy);

            // Yatay hıza göre hafif eğilme
            var simdi = performance.now();
            var dt = simdi - sonZaman;
            if (dt > 0) {
                var hiz = (olay.clientX - sonX) / dt;
                egimAyar(Math.max(-9, Math.min(9, hiz * -5)));
            }
            sonX = olay.clientX;
            sonZaman = simdi;
        }, { passive: true });

        kalemler.forEach(function (kalem) {
            kalem.addEventListener('mouseenter', function () {
                var kaynak = kalem.dataset.gorsel;
                if (!kaynak) return;

                aktifKalem = kalem;
                foto.src = kaynak;
                foto.alt = '';

                gsap.killTweensOf(onizleme);
                gsap.fromTo(onizleme,
                    { autoAlpha: 0, scale: 0.9, clipPath: 'inset(100% 0 0 0)' },
                    { autoAlpha: 1, scale: 1, clipPath: 'inset(0% 0 0 0)', duration: 0.55, ease: 'expo.out' });
            });

            kalem.addEventListener('mouseleave', function () {
                if (aktifKalem !== kalem) return;
                aktifKalem = null;
                gsap.to(onizleme, {
                    autoAlpha: 0, scale: 0.94, duration: 0.3, ease: 'power2.in',
                    clipPath: 'inset(100% 0 0 0)'
                });
            });
        });
    }

    /* =========================================================
       10. GÖRÜNÜRLÜK EMNİYETİ
       -------------------------------------------------------------
       rosso-kinetik sınıfı GSAP'in YÜKLENDİĞİNİ doğrular, ÇALIŞTIĞINI
       değil. Ticker ilerlemezse (arka plan sekmesi, rAF kısıtlı ortam,
       beklenmedik hata) clip-path ve opacity:0 kalır → içerik kalıcı
       olarak görünmez olur. Burada tickerın gerçekten ilerlediğini
       ölçüyoruz; ilerlemiyorsa kinetik mod tamamen bırakılır ve tüm
       içerik açılır. İçerik hiçbir koşulda gizli kalmaz.
       ========================================================= */
    function gorunurlukEmniyeti() {
        if (!kinetik) return;

        var kareSayisi = 0;
        function say() { kareSayisi++; }
        gsap.ticker.add(say);

        setTimeout(function () {
            gsap.ticker.remove(say);
            if (kareSayisi > 0) return; // ticker sağlıklı, dokunma

            if (window.console) {
                console.warn('rosso: GSAP tickerı ilerlemiyor — kinetik mod bırakıldı, içerik açılıyor.');
            }

            kok.classList.remove('rosso-kinetik', 'rosso-kilit');

            // GSAP'in inline yazdığı gizlemeleri de temizle
            document.querySelectorAll(
                '.hakkinda__govde, .hakkinda__olcut, .hakkinda__eylemler, .hakkinda__yil,' +
                ' .hakkinda__alinti, .hakkinda__cerceve, .kn-maske, .kn-kaydir, .kn-solgun,' +
                ' .hero__baslik, .hero__ustbaslik, .hero__alt, .hero__eylemler, .hero__durum,' +
                ' .menu__kalem, .menu__bas > *,' +
                ' .vitrin__kart, .vitrin__bas > *, .vitrin__eylem'
            ).forEach(function (oge) {
                oge.style.opacity = '';
                oge.style.visibility = '';
                oge.style.clipPath = '';
                oge.style.transform = '';
            });

            var perde = document.querySelector('.sahne-perde');
            if (perde) perde.remove();
            document.querySelectorAll('.sahne-perde__panel').forEach(function (p) { p.remove(); });
        }, 2600);
    }

    /* =========================================================
       BAŞLAT
       Her modül kendi try/catch'inde — biri patlarsa sahne durmaz.
       ========================================================= */
    var acildi = false;

    function baslat() {
        if (acildi) return;
        acildi = true;

        [lenisBaslat, imlecBaslat, navDurumu, menuBagla, heroParallax, kaydirmaGirisleri, hakkindaBolumu, vitrinBolumu, menuSergisi, gorunurlukEmniyeti]
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
