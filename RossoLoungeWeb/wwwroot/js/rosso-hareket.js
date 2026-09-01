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

    /* Head'deki satır içi betiğin emniyet zamanlayıcısı bunu görüyor:
       ayarlanmazsa kinetik sınıfını söküp içeriği açığa çıkarıyor. */
    window.__rossoBasladi = true;

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

    if (!kinetik) {
        /* Sınıfı head'deki betik iyimser şekilde eklemiş olabilir;
           GSAP gelmediyse ya da hareket azaltma açıksa geri al. */
        kok.classList.remove('rosso-kinetik');
    }

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

    // Galeri pin'i: ticker olurse emniyet katmani buradan soker
    var galeriPin = null;

    /* Sıvı imlecin halkasına uygulanan manyetik kayma. Yorum defterinin
       kenar bölgeleri buraya yazıyor, imleç döngüsü okuyor. */
    var imlecCekim = { x: 0, y: 0 };

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

        /* Hero girişi perde KAPANDIKTAN sonra değil, paneller yırtılmaya
           başlarken tetiklenir. Arka arkaya çalışınca yazılar ancak
           ~4.5sn'de oturuyordu; iç içe girince ~1.8sn'ye iniyor. */
        var girisBasladi = false;
        function girisiBaslat() {
            if (girisBasladi) return;
            girisBasladi = true;
            if (sonra) sonra();
        }

        var bitti = false;
        function tamamla() {
            if (bitti) return;
            bitti = true;
            perdeyiKaldir();
            girisiBaslat();
            ScrollTrigger.refresh();
        }

        // Emniyet: rAF durursa (arka plan sekmesi, ağır cihaz) perde
        // duvar saatiyle yine de kalkar. Kullanıcı asla siyah ekranda kalmaz.
        setTimeout(tamamla, 1800);

        var ilerleme = { deger: 0 };
        var zc = gsap.timeline({ onComplete: tamamla });

        zc.to(ilerleme, {
            deger: 100,
            duration: 0.35,
            ease: 'power2.inOut',
            onUpdate: function () {
                if (sayac) sayac.textContent = Math.round(ilerleme.deger).toString().padStart(3, '0');
            }
        })
          .to('.sahne-perde__marka', { opacity: 0, y: -14, duration: 0.2, ease: 'power2.in' }, '-=0.1')
          .to(sayac, { opacity: 0, duration: 0.18, ease: 'power2.in' }, '<')
          .set(perde, { autoAlpha: 0 })
          // Perde ortadan yırtılır
          .to(ustPanel, { yPercent: -100, duration: 0.45, ease: 'expo.inOut' }, 'yirt')
          .to(altPanel, { yPercent: 100, duration: 0.45, ease: 'expo.inOut' }, 'yirt')
          // Yazılar paneller açılırken yükselmeye başlasın
          .add(girisiBaslat, 'yirt-=0.12');

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
            zc.from(bolunmus.lines, { yPercent: 115, duration: 0.85, stagger: 0.06 });
        } else if (baslik) {
            zc.fromTo(baslik, { yPercent: 20, autoAlpha: 0 },
                      { yPercent: 0, autoAlpha: 1, duration: 0.8 });
        }

        /* fromTo ŞART, from DEĞİL.
           Başlangıç durumu artık CSS'te (.rosso-kinetik ... { opacity: 0 }),
           çünkü perde yırtılınca hazır sayfa görünüyordu. gsap.from()
           mevcut değeri BİTİŞ olarak okur; CSS 0 dediği için animasyon
           0'dan 0'a giderdi ve hero hiç açılmazdı. Bitişi açıkça yazıyoruz. */
        zc.fromTo('.hero__ustbaslik', { autoAlpha: 0, y: 16 },
                  { autoAlpha: 1, y: 0, duration: 0.55 }, 0.08)
          .fromTo('.hero__alt', { autoAlpha: 0, y: 18 },
                  { autoAlpha: 1, y: 0, duration: 0.55 }, '-=0.42')
          .fromTo('.hero__eylemler > *', { autoAlpha: 0, y: 20 },
                  { autoAlpha: 1, y: 0, duration: 0.5, stagger: 0.07 }, '-=0.38')
          .fromTo('.hero__durum', { autoAlpha: 0 },
                  { autoAlpha: 1, duration: 0.45 }, '-=0.3')
          .fromTo('.nav__marka, .nav__menu > li, .nav__eylem', { autoAlpha: 0, y: -12 },
                  { autoAlpha: 1, y: 0, duration: 0.45, stagger: 0.04 }, 0.1);

        return zc;
    }

    /* =========================================================
       4. FARE DUYARLI 3D PARALLAX (hero)
       ========================================================= */
    function heroParallax() {
        var sahne = document.querySelector('.hero');
        var zemin = document.querySelector('.hero__zemin');
        if (!sahne || !zemin || !kinetik || !inceIsaretci) return;

        /* AYRI KANALLAR — titremenin sebebi buydu.
           Fare parallaxı yüzde kanalına (xPercent/yPercent), kaydırma
           parallaxı PX kanalına (y) yazıyor. İkisi de yPercent'e
           yazarken quickTo tweeni ile scrub her karede birbirinin
           değerini eziyordu; fotoğraf kaydırırken titriyordu.
           GSAP bu iki kanalı ayrı tutup nihai matriste topluyor. */
        var xAyar = gsap.quickTo(zemin, 'xPercent', { duration: 0.9, ease: 'power3.out' });
        var yAyar = gsap.quickTo(zemin, 'yPercent', { duration: 0.9, ease: 'power3.out' });

        /* Hero belgenin başında; kutusu bir kez ölçülüp kaydırma
           konumuyla birlikte hesaplanıyor. Her mousemove'da
           getBoundingClientRect çağırmak, tam da kaydırırken zorunlu
           layout okuması demekti. */
        var kutu = null;
        function kutuyuOlc() {
            var r = sahne.getBoundingClientRect();
            kutu = { sol: r.left, belgeUst: r.top + window.pageYOffset, gen: r.width, yuk: r.height };
        }
        kutuyuOlc();
        window.addEventListener('resize', kutuyuOlc);

        sahne.addEventListener('mousemove', function (olay) {
            if (!kutu || !kutu.gen || !kutu.yuk) return;
            var ust = kutu.belgeUst - window.pageYOffset;
            xAyar(((olay.clientX - kutu.sol) / kutu.gen - 0.5) * -2.4);
            yAyar(((olay.clientY - ust) / kutu.yuk - 0.5) * -2.4);
        }, { passive: true });

        sahne.addEventListener('mouseleave', function () { xAyar(0); yAyar(0); });

        // Kaydırdıkça arka plan geride kalır (px kanalı; yPercent 12 ile aynı mesafe)
        gsap.to(zemin, {
            y: function () { return zemin.offsetHeight * 0.12; },
            ease: 'none',
            invalidateOnRefresh: true,
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
    /* =========================================================
       GLOBAL ÖZEL İMLEÇ

       Tek DOM örneği, tek delege dinleyici. Hover başına öğe
       yaratılmaz, öğe başına listener bağlanmaz — yalnızca sınıf
       değişir. Bütün durumlar (manyetik / metin / görsel) aynı
       halkanın üstünde yaşar.

       Konum gsap.quickTo ile: nokta kısa süreli (ani), halka uzun
       süreli (gecikmeli) → sıvı momentum ve ağırlık hissi.

       Manyetik kutu hover'da BİR KEZ ölçülür. Her mousemove'da
       getBoundingClientRect çağırmak kare başına zorunlu layout
       okuması demek olurdu; kaydırma ve yeniden boyutlandırmada
       tazeleniyor.
       ========================================================= */
    /* =========================================================
       NAVBAR AKTİF BÖLÜM İZLEYİCİSİ

       Eski tasarımın scrollspy'ı .nav-links kancasına bağlıydı ve yeni
       navbarla eşleşmiyordu; sonuç olarak "Ana Sayfa" hangi bölüme
       gidilirse gidilsin altı çizili kalıyordu.

       IntersectionObserver kullanılıyor: kaydırma dinleyicisi yok,
       ölçüm yalnızca kesişim değiştiğinde yapılıyor.
       ========================================================= */
    function navIzleyici() {
        var linkler = Array.prototype.slice.call(document.querySelectorAll('.nav__link'));
        if (!linkler.length) return;

        var hedefler = [];

        /* Eşleme href'ten DEĞİL data-bolum'dan okunuyor: "Menü" bağlantısı
           /Home/Menu'ye gidiyor ama ana sayfadaki vitrin bölümü de ona ait.
           href'e bakıldığında o bölümde hiçbir eşleşme bulunamıyor ve
           "Ana Sayfa" altı çizili kalıyordu. */
        linkler.forEach(function (bag) {
            var bolumId = bag.getAttribute('data-bolum');
            if (!bolumId) return;
            var bolum = document.getElementById(bolumId);
            if (bolum) hedefler.push({ bag: bag, bolum: bolum });
        });

        if (!hedefler.length) return; // menü sayfası: sunucunun verdiği durum kalsın

        function isaretle(etkin) {
            linkler.forEach(function (bag) {
                bag.classList.toggle('nav__link--aktif', bag === etkin);
            });
        }

        var gorunen = [];

        var izleyici = new IntersectionObserver(function (girisler) {
            girisler.forEach(function (giris) {
                var yer = gorunen.indexOf(giris.target);
                if (giris.isIntersecting) {
                    if (yer < 0) gorunen.push(giris.target);
                } else if (yer >= 0) {
                    gorunen.splice(yer, 1);
                }
            });

            /* Hiçbir bölüm bantta değilse (ör. footer) son durum kalsın;
               temizlemek çizginin kaybolup geri gelmesine yol açıyordu. */
            if (!gorunen.length) return;

            // Birden fazla bölüm görünüyorsa en yukarıdaki kazanır
            var enUst = gorunen.slice().sort(function (a, b) {
                return a.getBoundingClientRect().top - b.getBoundingClientRect().top;
            })[0];

            var eslesen = null;
            for (var i = 0; i < hedefler.length; i++) {
                if (hedefler[i].bolum === enUst) { eslesen = hedefler[i].bag; break; }
            }
            if (eslesen) isaretle(eslesen);
        }, {
            /* Ekranın %40'ında SIFIR yükseklikte bir referans çizgisi:
               o çizgiyi hangi bölüm kesiyorsa o aktif. Önceki geniş bant
               (96px - %45) iki bölümün birden kesişmesine yol açıyor ve
               en üstteki kazandığı için, ekranı iletişim bölümü doldurmuşken
               "Yorumlar" altı çizili kalıyordu (ölçüldü: %82 konumunda). */
            rootMargin: '-40% 0px -60% 0px',
            threshold: 0
        });

        hedefler.forEach(function (h) { izleyici.observe(h.bolum); });
    }

    /* =========================================================
       ÇAPA KAYDIRMASI — sayfadaki TEK kaydırma sahibi

       Önceden iki sistem vardı: burada Lenis'e bağlı bir dinleyici,
       script.js'te ise her a[href^="#"] tıklamasını preventDefault edip
       window.scrollTo çağıran ikinci bir dinleyici. İkisi aynı anda
       çalıştığı için hedef tam oturmuyordu. script.js'teki kaldırıldı.

       Delege dinleyici: bağlantılar sonradan eklense de çalışır.
       İki biçimi de kabul eder — "#hedef" ve aynı sayfayı gösteren
       "/#hedef" (navbar partial'ı bu ikinci biçimi kullanıyor).

       Ofset navbarın CANLI yüksekliğinden hesaplanıyor; sabit sayı
       yazılsaydı navbar kompakt duruma geçtiğinde kayardı.
       ========================================================= */
    function capaOfseti() {
        var nav = document.querySelector('.nav');
        var yukseklik = nav ? nav.getBoundingClientRect().height : 0;
        return yukseklik + 12;
    }

    /* 'tepe' döner: aynı sayfaya giden hash'siz bağlantı (Ana Sayfa,
       marka). Bunlar sayfayı yeniden yüklüyordu; artık başa kaydırıyor. */
    function capayiCoz(bag) {
        var ham = bag.getAttribute('href') || '';

        if (ham.charAt(0) !== '#') {
            if (bag.pathname !== window.location.pathname) return null;
            if (!bag.hash) return 'tepe';
            ham = bag.hash;
        }

        if (ham === '#') return 'tepe';
        if (ham.length < 2) return null;

        try {
            return document.querySelector(ham);
        } catch (h) {
            return null; // geçersiz seçici içeren hash
        }
    }

    /* Bölümlerin üst dolgusu ~200px. Kutunun tepesine gidince ekranın
       üst yarısı boş kalıyor, içerik aşağıda başlıyordu. Bölüm hedefiyse
       BAŞLIĞINA hizalıyoruz — tıklayan kişi içeriği görsün. */
    /* Hedefe göre hem hizalanacak öğeyi hem üstte bırakılacak payı verir. */
    function hizaBilgisi(hedef) {
        var varsayilanPay = capaOfseti() + 28;
        if (!hedef || hedef.tagName !== 'SECTION') {
            return { oge: hedef, pay: varsayilanPay };
        }

        /* Pin'li bölüm (galeri): sahne 100vh ve kaydırmayla yatay akıyor.
           Pay bırakmak sahnenin ÖNCESİNE düşürüyor — ölçüldü, tıklayınca
           hâlâ vitrin bölümündeydi. Tam tepeye oturuyoruz. */
        if (hedef.querySelector('.pin-spacer')) {
            return { oge: hedef, pay: 0 };
        }

        /* Başlığa değil, bölümün İÇERİK BLOĞUNA hizalanıyor.
           Hakkımızda'da başlık sağ sütunda, görseller sol sütunda daha
           yukarıdan başlıyor; başlığı hizalayınca görsellerin üstü
           kesiliyordu. .kap sarmalayıcısı iki sütunun da başladığı yer.

           Pay da genişletildi: eski değerle üst başlık navın 29px
           altına sıkışıyor, altta boşluk kalıyordu. */
        var blok = hedef.querySelector('.kap') ||
                   hedef.querySelector('h1, h2, .hero__baslik') || hedef;
        return { oge: blok, pay: capaOfseti() + 56 };
    }

    /* Tek genel kaydırma girişi. script.js (rezervasyon formu ilk hatalı
       alana giderken) buradan çağırıyor — ikinci bir kaydırma mantığı
       yazmasın diye bilerek dışarı açıldı. */
    window.rossoKaydir = function (hedef, aninda) {
        if (!hedef) return;

        var bilgi = hizaBilgisi(hedef);
        var ust = bilgi.oge.getBoundingClientRect().top + window.pageYOffset - bilgi.pay;

        // Sayfanın en başındaki bölüm için tepeye git, araya boşluk girmesin
        if (ust < 140) ust = 0;

        if (lenis && !aninda) {
            lenis.scrollTo(ust, { duration: 1.2 });
            return;
        }

        window.scrollTo({
            top: Math.max(ust, 0),
            behavior: (aninda || azHareket) ? 'auto' : 'smooth'
        });
    };

    function capaBagla() {
        document.addEventListener('click', function (olay) {
            var bag = olay.target.closest ? olay.target.closest('a[href]') : null;
            if (!bag || bag.target === '_blank') return;

            var hedef = capayiCoz(bag);
            if (!hedef) return;

            olay.preventDefault();

            if (hedef === 'tepe') {
                if (lenis) lenis.scrollTo(0, { duration: 1.1 });
                else window.scrollTo({ top: 0, behavior: azHareket ? 'auto' : 'smooth' });
                return;
            }

            window.rossoKaydir(hedef);

            // Klavye odağı da hedefe taşınsın, yoksa Tab başa döner
            if (!hedef.hasAttribute('tabindex')) hedef.setAttribute('tabindex', '-1');
            hedef.focus({ preventScroll: true });
        });

        /* Başka sayfadan #çıpa ile gelindiğinde tarayıcı sabit navbarı
           hesaba katmıyor; yükleme bitince biz hizalıyoruz. */
        window.addEventListener('load', function () {
            if (!window.location.hash) return;
            var hedef = null;
            try { hedef = document.querySelector(window.location.hash); } catch (h) { return; }
            if (hedef && hedef !== 'tepe') setTimeout(function () { window.rossoKaydir(hedef, true); }, 60);
        });
    }

    var ETKILESIM_SECICI = 'a, button, input, textarea, select, [role="button"]';
    var IMLEC_SECICI = '[data-cursor-image], [data-cursor-text], ' + ETKILESIM_SECICI;

    function imlecBaslat() {
        if (!inceIsaretci || azHareket || !gsapVar) return;

        var imlec = document.querySelector('.imlec');
        if (!imlec) return;

        kok.classList.add('rosso-imlec');

        var kadraj = imlec.querySelector('.imlec__kadraj');
        var tik = imlec.querySelector('.imlec__tik');
        var yazi = imlec.querySelector('.imlec__yazi');
        var gorsel = imlec.querySelector('.imlec__gorsel');
        if (!kadraj || !tik) return;

        /* Her ikisi de kendi merkezine oturuyor. margin yerine
           xPercent/yPercent: kadrajın genişliği duruma göre değişiyor,
           margin kullanılsaydı her ölçü değişiminde onu da güncellemek
           gerekirdi. GSAP yüzde kanalını x/y'den ayrı tutup topluyor. */
        gsap.set([kadraj, tik], { xPercent: -50, yPercent: -50 });

        /* İki hız: tik imlecin gerçek noktasında, kadraj gecikmeli.
           Ağırlık ve sıvı akış hissi bu farktan geliyor. */
        var tx = gsap.quickTo(tik, 'x', { duration: 0.10, ease: 'power3.out' });
        var ty = gsap.quickTo(tik, 'y', { duration: 0.10, ease: 'power3.out' });
        var kx = gsap.quickTo(kadraj, 'x', { duration: 0.55, ease: 'power3.out' });
        var ky = gsap.quickTo(kadraj, 'y', { duration: 0.55, ease: 'power3.out' });

        var sonHedef = null;
        var manyetik = null;
        var manyetikKutu = null;
        var sonX = 0, sonY = 0;

        /* Kadrajı hedefin kutusuna oturt: kırpma işaretleri öğeyi
           vizör gibi çerçeveler. Ölçü CSS geçişiyle yumuşuyor. */
        function kadrajaOturt(kutu) {
            kadraj.style.width = Math.round(kutu.width + 18) + 'px';
            kadraj.style.height = Math.round(kutu.height + 18) + 'px';
        }

        function kadrajOlcusunuBirak() {
            kadraj.style.width = '';
            kadraj.style.height = '';
        }

        function kutuyuTazele() {
            if (!manyetik) return;
            manyetikKutu = manyetik.getBoundingClientRect();
            kadrajaOturt(manyetikKutu);
        }

        function durumSifirla() {
            imlec.classList.remove('imlec--yakin', 'imlec--etiketli', 'imlec--gorselli');
            manyetik = null;
            manyetikKutu = null;
            kadrajOlcusunuBirak();
        }

        function durumUygula(hedef) {
            // Aynı hedefin çocukları arasında gezinirken iş yapma
            if (hedef === sonHedef) return;
            sonHedef = hedef;
            durumSifirla();
            if (!hedef) return;

            var kaynak = gorsel && hedef.getAttribute('data-cursor-image');
            if (kaynak) {
                // Aynı görsel tekrar atanırsa tarayıcı yeniden çözmesin
                if (gorsel.getAttribute('src') !== kaynak) gorsel.setAttribute('src', kaynak);
                imlec.classList.add('imlec--gorselli');
                return;
            }

            var metin = yazi && hedef.getAttribute('data-cursor-text');
            if (metin) {
                yazi.textContent = metin;
                imlec.classList.add('imlec--etiketli');
                return;
            }

            imlec.classList.add('imlec--yakin');
            manyetik = hedef;
            manyetikKutu = hedef.getBoundingClientRect();

            /* Çok büyük alanları çerçevelemek anlamsız (tam ekran
               düğmeler, uzun bağlantı blokları); orada kadraj kendi
               ölçüsünde kalıp yalnızca hedefe doğru kayıyor. */
            if (manyetikKutu.width > 420 || manyetikKutu.height > 260) {
                manyetikKutu = null;
                return;
            }
            kadrajaOturt(manyetikKutu);
        }

        window.addEventListener('mousemove', function (olay) {
            sonX = olay.clientX;
            sonY = olay.clientY;

            tx(sonX); ty(sonY);

            /* Kadraj hedefe OTURUYOR: kutunun merkezine gidiyor, imlecin
               kendisine değil. Vizör hissi buradan. Hedef yoksa imleci
               takip ediyor; imlecCekim'i yorum defteri besliyor. */
            if (manyetikKutu) {
                kx(manyetikKutu.left + manyetikKutu.width / 2);
                ky(manyetikKutu.top + manyetikKutu.height / 2);
            } else {
                kx(sonX + imlecCekim.x);
                ky(sonY + imlecCekim.y);
            }
        }, { passive: true });

        // TEK delege dinleyici — öğe başına bağlama yok
        document.addEventListener('mouseover', function (olay) {
            var oge = olay.target;
            if (!oge || oge.nodeType !== 1) return;
            durumUygula(oge.closest(IMLEC_SECICI));
        }, { passive: true });

        // Pencereden çıkınca durum takılı kalmasın
        document.addEventListener('mouseleave', function () {
            sonHedef = null;
            durumSifirla();
        });

        /* TAKILI KALMA ONARIMI
           Menü filtresi, off-canvas panel veya tam ekran görüntüleyici,
           imlecin üzerinde durduğu öğeyi gizleyebiliyor. Öğe gizlenince
           mouseout TETİKLENMİYOR, imleç de o durumda donuyor.
           Tıklamadan hemen sonra imlecin ALTINDA gerçekten ne olduğuna
           bakıp durumu yeniden kuruyoruz — tıklama başına tek ölçüm. */
        document.addEventListener('click', function () {
            setTimeout(function () {
                var altta = document.elementFromPoint(sonX, sonY);
                /* undefined, null DEĞİL: durumUygula ilk satırda
                   hedef === sonHedef diye erken dönüyor ve altta hiçbir
                   şey yokken (elementFromPoint null) sıfırlama atlanıyordu. */
                sonHedef = undefined;
                durumUygula(altta && altta.closest ? altta.closest(IMLEC_SECICI) : null);
            }, 80);
        });

        // Hedef DOM'dan tamamen çıkarsa (isConnected bedava, layout okumaz)
        window.addEventListener('mousemove', function () {
            if (sonHedef && !sonHedef.isConnected) {
                sonHedef = null;
                durumSifirla();
            }
        }, { passive: true });

        // Ölçülen kutu kaydırma/boyut değişiminde bayatlar
        window.addEventListener('scroll', kutuyuTazele, { passive: true });
        window.addEventListener('resize', kutuyuTazele);
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
       - Görsel önizleme: satırlardaki data-cursor-image ile global imleç
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

    }

    /* =========================================================
       12. GALERİ SERGİSİ — yatay akış
       -------------------------------------------------------------
       - ScrollTrigger pin: dikey kaydırma yatay çeviriye dönüşür
       - containerAnimation ile çerçeve içi ZIT YÖNLÜ parallax
       - Kaydırma hızına bağlı skewX, durunca yumuşak düzelme
       - Tıklanınca çerçeve bulunduğu yerden tam ekrana büyür

       TASARIM KARARI — pin YALNIZCA burada kurulursa .galeri--pinli
       eklenir. Sınıf yoksa CSS rayı doğal bir yatay kaydırıcı olarak
       bırakır; JS/GSAP düşse bile kareler gezilebilir kalır.
       ========================================================= */
    function galeriSergisi() {
        var bolum = document.querySelector('.galeri');
        if (!bolum) return;

        var sahne = bolum.querySelector('.galeri__sahne');
        var ray = bolum.querySelector('.galeri__ray');
        var kareler = Array.prototype.slice.call(bolum.querySelectorAll('.galeri__kare'));
        if (!sahne || !ray || !kareler.length) return;

        // Tam ekran her koşulda bağlanır (kinetik olmasa da tıklanabilir)
        tamEkranBagla(bolum, kareler);

        if (!kinetik || typeof gsap.matchMedia !== 'function') return;

        var mm = gsap.matchMedia();

        /* Yatay akış yalnızca geniş ekranda. Dar ekranda parmakla
           kaydırılan doğal ray daha rahat — pin dokunmatikte hantal. */
        mm.add('(min-width: 900px)', function () {
            bolum.classList.add('galeri--pinli');

            var dolgu = bolum.querySelector('.galeri__ilerleme-dolgu');

            // Ray ekran genişliğinden ne kadar taşıyorsa o kadar yol var
            function mesafe() {
                return Math.max(0, ray.scrollWidth - window.innerWidth);
            }

            /* --- Hıza bağlı eğilme ---
               Yatay harekette sürüklenme hissini skewX verir (skewY
               dikey kaydırmanın karşılığı). Kaydırma durduğunda
               ScrollTrigger artık onUpdate yollamaz; bu yüzden
               düzelmeyi zamanlayıcı tetikler. */
            var egimAyar = gsap.quickTo(kareler, 'skewX', { duration: 0.5, ease: 'power3.out' });
            var durakZaman;

            function egimUygula(hiz) {
                egimAyar(gsap.utils.clamp(-7, 7, hiz / -260));
                clearTimeout(durakZaman);
                durakZaman = setTimeout(function () { egimAyar(0); }, 120);
            }

            /* --- Yatay çeviri ---
               scrub: 1 → kaydırmayı bir saniyelik gecikmeyle takip
               eder; momentum hissi buradan geliyor. */
            var yatay = gsap.to(ray, {
                x: function () { return -mesafe(); },
                ease: 'none',
                scrollTrigger: {
                    trigger: sahne,
                    start: 'top top',
                    end: function () { return '+=' + mesafe(); },
                    pin: true,
                    /* anticipatePin KAPALI. Pini hıza göre erken uygulayıp
                       öğeyi yerine "atıyordu": ölçümde pin devreye girerken
                       sahne tek karede 46px zıplıyordu (scroll o karede
                       yalnızca 18px ilerlemişti). Lenis'in yumuşak
                       kaydırmasında zaten flaş riski yok. */
                    anticipatePin: 0,
                    scrub: 1,
                    invalidateOnRefresh: true,
                    onUpdate: function (kendi) {
                        if (dolgu) gsap.set(dolgu, { scaleX: kendi.progress });
                        egimUygula(kendi.getVelocity());
                    }
                }
            });

            galeriPin = yatay.scrollTrigger;

            /* --- Çerçeve içi zıt yönlü parallax ---
               Kare sağdan sola akarken foto çerçeve içinde sola→sağa
               kayar. Foto %124 genişlikte olduğu için ±%8.7'lik kayma
               hiçbir kenarda boşluk açmaz.
               containerAnimation: tetikleyici, sayfanın dikey kaydırması
               değil yukarıdaki yatay tween'dir. */
            kareler.forEach(function (kare) {
                var foto = kare.querySelector('.galeri__foto');
                if (!foto) return;

                gsap.fromTo(foto,
                    { xPercent: -7 },
                    {
                        xPercent: 7,
                        ease: 'none',
                        scrollTrigger: {
                            trigger: kare,
                            containerAnimation: yatay,
                            start: 'left right',
                            end: 'right left',
                            scrub: true
                        }
                    });
            });

            // matchMedia sorgu dışına çıkınca tween'leri kendisi geri alır;
            // sınıfı ve eğimi biz temizliyoruz.
            return function () {
                clearTimeout(durakZaman);
                gsap.set(kareler, { skewX: 0 });
                bolum.classList.remove('galeri--pinli');
                galeriPin = null;
            };
        });
    }

    /* =========================================================
       12b. TAM EKRAN GÖRÜNTÜLEYİCİ
       -------------------------------------------------------------
       Çerçeve, tıklanan karenin çerçevesinin ölçüldüğü dikdörtgenden
       doğal tam ekran yerine doğru büyür ("seamless scale").

       NEDEN Flip DEĞİL: Flip.from öğeyi ya grid akışında bırakıp
       transform yazar (kap `place-items:center` olduğu için genişlik
       değişirken merkez kayar) ya da absolute:true ile akıştan çıkarır
       (bu sefer de kapanışta yerine oturması kırılgan). Ölçülen iki
       dikdörtgen arasında position:fixed ile tweenlemek aynı görüntüyü
       verir ve tamamen belirlenimli — kenar durumu yok.
       ========================================================= */
    function tamEkranBagla(bolum, kareler) {
        var kat = document.getElementById('tamekran');
        if (!kat) return;

        var zemin = kat.querySelector('.tamekran__zemin');
        var cerceve = kat.querySelector('.tamekran__cerceve');
        var foto = kat.querySelector('.tamekran__foto');
        var noEt = kat.querySelector('.tamekran__no');
        var etiketEt = kat.querySelector('.tamekran__etiket');
        var ayak = kat.querySelector('.tamekran__ayak');
        var kapatDugme = kat.querySelector('.tamekran__kapat');
        var oncekiDugme = kat.querySelector('.tamekran__ok--onceki');
        var sonrakiDugme = kat.querySelector('.tamekran__ok--sonraki');
        if (!cerceve || !foto || !kapatDugme) return;

        /* Canlandırma kararı HER ÇAĞRIDA yeniden veriliyor.
           `kinetik` açılışta bir kez hesaplanıyor; emniyet katmanı ticker
           ölü olduğunda rosso-kinetik sınıfını sonradan kaldırıyor. Bayrağı
           dondurursak tween'ler hiç ilerlemez ve katman açık kilitli kalır —
           bir kez yaşandı. Sınıf canlı sağlık sinyali, onu okuyoruz. */
        function canlandirMi() {
            return gsapVar && kok.classList.contains('rosso-kinetik');
        }

        var suSira = 0;
        var acanKare = null;
        var acik = false;

        var arayuz = [ayak, kapatDugme, oncekiDugme, sonrakiDugme].filter(Boolean);

        function karedekiCerceve(kare) { return kare.querySelector('.galeri__cerceve'); }

        function icerikYaz(sira) {
            suSira = (sira + kareler.length) % kareler.length;
            var kare = kareler[suSira];
            var kaynak = kare.querySelector('.galeri__foto');
            if (!kaynak) return;

            foto.src = kaynak.currentSrc || kaynak.src;
            foto.alt = kaynak.alt || '';
            if (noEt) noEt.textContent = kare.dataset.galeriNo || '';
            if (etiketEt) etiketEt.textContent = kare.dataset.galeriEtiket || '';
        }

        /* Pin sırasında sayfa kaymasın. Pinli modda body overflow'una
           dokunmuyoruz — pin-spacer'ın yüksekliğini bozup ScrollTrigger'ı
           şaşırtıyor. Lenis'i durdurmak tekerleği zaten kesiyor. */
        function kaydirmaKilidi(kilit) {
            if (lenis) {
                if (kilit) { lenis.stop(); } else { lenis.start(); }
            }
            if (!bolum.classList.contains('galeri--pinli')) {
                document.body.style.overflow = kilit ? 'hidden' : '';
            }
        }

        function ac(sira) {
            icerikYaz(sira);
            acanKare = kareler[suSira];
            acik = true;

            kat.hidden = false;
            kok.classList.add('tamekran-acik');
            kaydirmaKilidi(true);
            kapatDugme.focus();

            if (!canlandirMi()) {
                // Canlandırmasız açılış: önceki animasyonlu turdan kalmış
                // opaklıklar katmanı görünmez bırakmasın
                if (gsapVar) gsap.set([zemin].concat(arayuz), { clearProps: 'opacity' });
                return;
            }

            // Eğim açıkken ölçüm bozulur; kareleri düz bırak
            gsap.set(kareler, { skewX: 0 });

            var kaynakCerceve = karedekiCerceve(acanKare);
            var k = kaynakCerceve ? kaynakCerceve.getBoundingClientRect() : null;
            var h = cerceve.getBoundingClientRect(); // doğal tam ekran yeri

            /* set + to; fromTo başlangıç değerini bir sonraki kareye
                erteleyebilir ve çerçeve bir kare boyu tam boy görünür. */
            gsap.set(zemin, { opacity: 0 });
            gsap.to(zemin, { opacity: 1, duration: 0.5, ease: 'power2.out' });
            gsap.set(arayuz, { opacity: 0 });

            if (!k || !k.width) {
                gsap.set(cerceve, { opacity: 0, scale: 0.94 });
                gsap.to(cerceve, { opacity: 1, scale: 1, duration: 0.5, ease: 'expo.out' });
                gsap.to(arayuz, { opacity: 1, duration: 0.4, delay: 0.25 });
                return;
            }

            gsap.set(cerceve, { position: 'fixed', margin: 0, left: k.left, top: k.top, width: k.width, height: k.height });
            gsap.to(cerceve, {
                left: h.left, top: h.top, width: h.width, height: h.height,
                duration: 0.85, ease: 'expo.inOut',
                onComplete: function () {
                    gsap.set(cerceve, { clearProps: 'position,margin,left,top,width,height' });
                    gsap.to(arayuz, { opacity: 1, duration: 0.4, ease: 'power2.out' });
                }
            });
        }

        function kapat() {
            if (!acik) return;
            acik = false;
            kok.classList.remove('tamekran-acik');

            function bitir() {
                kat.hidden = true;
                if (gsap.set) {
                    gsap.set(cerceve, { clearProps: 'position,margin,left,top,width,height,opacity,scale' });
                    // Kare değiştirme tween'i yarıda kalmışsa foto opak 0 kalmasın
                    gsap.set(foto, { clearProps: 'opacity,scale' });
                }
                foto.removeAttribute('src');
                kaydirmaKilidi(false);
                if (acanKare) acanKare.focus();
            }

            var kaynakCerceve = acanKare ? karedekiCerceve(acanKare) : null;
            var k = kaynakCerceve ? kaynakCerceve.getBoundingClientRect() : null;

            if (!canlandirMi() || !k || !k.width) { bitir(); return; }

            var h = cerceve.getBoundingClientRect();

            gsap.to(arayuz, { opacity: 0, duration: 0.2, ease: 'power2.in' });
            gsap.to(zemin, { opacity: 0, duration: 0.45, ease: 'power2.in', delay: 0.15 });

            gsap.set(cerceve, { position: 'fixed', margin: 0, left: h.left, top: h.top, width: h.width, height: h.height });
            gsap.to(cerceve, {
                left: k.left, top: k.top, width: k.width, height: k.height,
                duration: 0.6, ease: 'expo.inOut', onComplete: bitir
            });
        }

        function goster(sira) {
            icerikYaz(sira);
            acanKare = kareler[suSira];
            if (!canlandirMi()) return;
            gsap.set(foto, { opacity: 0, scale: 1.04 });
            gsap.to(foto, { opacity: 1, scale: 1, duration: 0.45, ease: 'power2.out' });
        }

        kareler.forEach(function (kare, sira) {
            kare.addEventListener('click', function () { ac(sira); });
        });

        kapatDugme.addEventListener('click', kapat);
        if (oncekiDugme) oncekiDugme.addEventListener('click', function () { goster(suSira - 1); });
        if (sonrakiDugme) sonrakiDugme.addEventListener('click', function () { goster(suSira + 1); });

        // Boşluğa tıklayınca kapansın (çerçevenin ve butonların dışı)
        kat.addEventListener('click', function (olay) {
            if (olay.target === kat || olay.target === zemin) kapat();
        });

        document.addEventListener('keydown', function (olay) {
            if (!acik) return;

            if (olay.key === 'Escape') {
                kapat();
            } else if (olay.key === 'ArrowLeft') {
                goster(suSira - 1);
            } else if (olay.key === 'ArrowRight') {
                goster(suSira + 1);
            } else if (olay.key === 'Tab') {
                // Odak tuzağı: sekme katman içinde dönsün
                var odaklanabilir = [kapatDugme, oncekiDugme, sonrakiDugme].filter(Boolean);
                var su = odaklanabilir.indexOf(document.activeElement);
                olay.preventDefault();
                var yon = olay.shiftKey ? -1 : 1;
                odaklanabilir[(su + yon + odaklanabilir.length) % odaklanabilir.length].focus();
            }
        });
    }

    /* =========================================================
       13. ZİYARETÇİ DEFTERİ — tipografik yorum sergisi
       -------------------------------------------------------------
       - Sürükleyerek geçiş (pointer events), sönümlü takip + eşik
       - Kenar bölgelerinde sıvı imleç "Geri/İleri" etiketine dönüşür
         ve halka bölgeye doğru manyetik olarak çekilir
       - Geçiş: eski yorum satır satır bulanıklaşıp dağılır, yeni
         yorum maskeden yükselerek gelir (SplitText)
       - İnce ilerleme çizgisi + sayaç

       Galerideki desenin aynısı: .defter--sahnede sınıfı yalnızca
       sergi gerçekten devralındığında ekleniyor. Yoksa yapraklar
       CSS'te alt alta okunur bir liste olarak kalıyor.
       ========================================================= */
    function yorumDefteri() {
        var bolum = document.querySelector('.defter');
        if (!bolum) return;

        var sahne = bolum.querySelector('.defter__sahne');
        var yapraklar = Array.prototype.slice.call(bolum.querySelectorAll('.defter__yaprak'));
        // Tek yorumda sergiye gerek yok; sıfırda zaten boş durum var
        if (!sahne || yapraklar.length < 2 || !kinetik) return;

        var geri = bolum.querySelector('.defter__yon--geri');
        var ileri = bolum.querySelector('.defter__yon--ileri');
        var dolgu = bolum.querySelector('.defter__ilerleme-dolgu');
        var suEt = bolum.querySelector('.defter__su');
        var bolmeVar = typeof window.SplitText !== 'undefined';

        var suSira = 0;
        var mesgul = false;

        bolum.classList.add('defter--sahnede');

        // Tek döngü: ilk yaprak daha ilk adımda açılıyor, arada bir
        // hata olsa bile "hepsi gizli" durumu oluşmuyor.
        yapraklar.forEach(function (yaprak, i) {
            gsap.set(yaprak, { autoAlpha: i === 0 ? 1 : 0 });
            yaprak.setAttribute('aria-hidden', i === 0 ? 'false' : 'true');
        });
        durumYaz(0);

        function durumYaz(i) {
            if (dolgu) {
                gsap.to(dolgu, {
                    scaleX: (i + 1) / yapraklar.length,
                    duration: 0.7, ease: 'expo.out'
                });
            }
            if (suEt) suEt.textContent = ('0' + (i + 1)).slice(-2);
        }

        /* ---------- Satır bölme ----------
           Bölme her geçişte yapılıp sonra geri alınıyor. Satır kutuları
           böylece HER ZAMAN o anki genişliğe göre hesaplanır; yeniden
           boyutlandırmada bayat bölme kalmaz, resize dinleyicisi
           gerekmez. Birkaç satır için maliyeti yok denecek kadar az. */
        function satirlaraBol(yaprak) {
            var hedef = yaprak.querySelector('.defter__metin');
            if (!bolmeVar || !hedef) return null;

            var bol = new SplitText(hedef, { type: 'lines', linesClass: 'defter__satir' });

            // Her satırı taşan bir kaba al → maske etkisi
            var kaplar = bol.lines.map(function (satir) {
                var kap = document.createElement('span');
                kap.className = 'defter__satir-kap';
                satir.parentNode.insertBefore(kap, satir);
                kap.appendChild(satir);
                return kap;
            });

            return {
                satirlar: bol.lines,
                geriAl: function () {
                    // Önce kapları söküyoruz; revert() sarmalanmış
                    // düğümleri geride bırakabiliyor.
                    kaplar.forEach(function (kap) {
                        if (kap.firstChild) kap.parentNode.insertBefore(kap.firstChild, kap);
                        if (kap.parentNode) kap.parentNode.removeChild(kap);
                    });
                    bol.revert();
                }
            };
        }

        function suslerOf(yaprak) {
            return yaprak.querySelectorAll('.defter__isaret, .defter__imza');
        }

        /* ---------- Geçiş ---------- */
        function gecis(hedef, yon) {
            if (mesgul || hedef === suSira || hedef < 0 || hedef >= yapraklar.length) return;
            mesgul = true;

            var eski = yapraklar[suSira];
            var yeni = yapraklar[hedef];

            eski.setAttribute('aria-hidden', 'true');
            yeni.setAttribute('aria-hidden', 'false');
            suSira = hedef;
            durumYaz(hedef);

            var eskiBol = satirlaraBol(eski);
            var yeniBol = satirlaraBol(yeni);

            var zc = gsap.timeline({
                onComplete: function () {
                    if (eskiBol) eskiBol.geriAl();
                    if (yeniBol) yeniBol.geriAl();
                    gsap.set(eski, { autoAlpha: 0, x: 0, rotate: 0, filter: 'none' });
                    gsap.set(yeni, { clearProps: 'transform,filter' });
                    mesgul = false;
                }
            });

            // Yeni yaprağın süsleri girişten önce kapalı olsun
            zc.set(suslerOf(yeni), { autoAlpha: 0 }, 0);

            /* ÇIKIŞ — satırlar bulanıklaşıp sürükleme yönünde dağılır */
            if (eskiBol) {
                zc.to(eskiBol.satirlar, {
                    autoAlpha: 0, filter: 'blur(7px)', yPercent: -14 * yon,
                    duration: 0.42, ease: 'power2.in', stagger: 0.03
                }, 0);
            } else {
                zc.to(eski, { autoAlpha: 0, filter: 'blur(7px)', duration: 0.4, ease: 'power2.in' }, 0);
            }

            zc.to(suslerOf(eski), { autoAlpha: 0, duration: 0.3, ease: 'power2.in' }, 0);

            // Sahneyi devret
            zc.set(eski, { autoAlpha: 0 }, 0.44);
            zc.set(yeni, { autoAlpha: 1 }, 0.44);

            /* GİRİŞ — satırlar maskeden yükselir */
            if (yeniBol) {
                zc.fromTo(yeniBol.satirlar,
                    { yPercent: 112, autoAlpha: 0, filter: 'blur(9px)' },
                    {
                        yPercent: 0, autoAlpha: 1, filter: 'blur(0px)',
                        duration: 0.85, ease: 'expo.out', stagger: 0.055
                    }, 0.46);
            } else {
                zc.fromTo(yeni,
                    { autoAlpha: 0, filter: 'blur(9px)' },
                    { autoAlpha: 1, filter: 'blur(0px)', duration: 0.7, ease: 'expo.out' }, 0.46);
            }

            zc.fromTo(suslerOf(yeni),
                { autoAlpha: 0, y: 14 },
                { autoAlpha: 1, y: 0, duration: 0.7, ease: 'expo.out', stagger: 0.08 }, 0.62);
        }

        function git(yon) {
            gecis((suSira + yon + yapraklar.length) % yapraklar.length, yon);
        }

        /* ---------- Sürükleme ----------
           Yaprak imleci sönümlü takip eder (1 px → 0.34 px); eşiği
           geçerse geçiş yapılır, geçmezse yaylanarak yerine döner. */
        var basX = 0, basY = 0, kayma = 0, tutuluyor = false, niyet = null;

        sahne.addEventListener('pointerdown', function (olay) {
            if (mesgul) return;
            if (olay.pointerType === 'mouse' && olay.button !== 0) return;
            tutuluyor = true;
            niyet = null;
            kayma = 0;
            basX = olay.clientX;
            basY = olay.clientY;
        });

        sahne.addEventListener('pointermove', function (olay) {
            if (!tutuluyor) return;

            var gx = olay.clientX - basX;
            var gy = olay.clientY - basY;

            /* İlk 10 px'te niyeti belirle. Dikey ise sürüklemeyi bırak:
               dokunmatikte sayfanın kendi kaydırması bloke olmasın. */
            if (niyet === null) {
                if (Math.abs(gx) < 10 && Math.abs(gy) < 10) return;
                niyet = Math.abs(gx) > Math.abs(gy) ? 'yatay' : 'dikey';
                if (niyet === 'yatay') {
                    bolum.classList.add('defter--tutuluyor');
                    try { sahne.setPointerCapture(olay.pointerId); } catch (h) { /* yoksay */ }
                } else {
                    tutuluyor = false;
                    return;
                }
            }

            kayma = gx;
            gsap.set(yapraklar[suSira], { x: kayma * 0.34, rotate: kayma * 0.0035 });
        });

        function birak(olay) {
            if (!tutuluyor) return;
            tutuluyor = false;
            bolum.classList.remove('defter--tutuluyor');

            if (olay && olay.pointerId != null) {
                try { sahne.releasePointerCapture(olay.pointerId); } catch (h) { /* yoksay */ }
            }

            var esik = Math.min(110, sahne.offsetWidth * 0.11);
            var yeter = Math.abs(kayma) >= esik;
            var yon = kayma < 0 ? 1 : -1;
            kayma = 0;

            if (yeter) {
                // Yaprak sürüklendiği yerden çıkışa devam etsin diye
                // x sıfırlanmıyor; geçiş sonunda temizleniyor.
                git(yon);
            } else {
                gsap.to(yapraklar[suSira], {
                    x: 0, rotate: 0, duration: 0.7, ease: 'elastic.out(1, 0.55)'
                });
            }
        }

        sahne.addEventListener('pointerup', birak);
        sahne.addEventListener('pointercancel', birak);

        /* ---------- Klavye ---------- */
        sahne.addEventListener('keydown', function (olay) {
            if (olay.key === 'ArrowLeft') { olay.preventDefault(); git(-1); }
            else if (olay.key === 'ArrowRight') { olay.preventDefault(); git(1); }
        });

        /* ---------- Kenar bölgeleri + manyetik imleç ---------- */
        [[geri, 'Geri', -1], [ileri, 'İleri', 1]].forEach(function (uc) {
            var dugme = uc[0];
            if (!dugme) return;

            dugme.addEventListener('click', function () { git(uc[2]); });

            if (!inceIsaretci || azHareket) return;

            // Metin imlecini artık global imleç nitelikten okuyor
            dugme.setAttribute('data-cursor-text', uc[1]);

            // Manyetik çekim: halka bölgenin eksenine doğru kayar,
            // nokta imlecin gerçek yerinde kalır → "çekiliyor" hissi
            dugme.addEventListener('mousemove', function (olay) {
                var r = dugme.getBoundingClientRect();
                imlecCekim.x = ((r.left + r.width / 2) - olay.clientX) * 0.38;
            }, { passive: true });

            // Bölgeden çıkınca çekim sıfırlanmalı, yoksa halka kayık kalır
            dugme.addEventListener('mouseleave', function () { imlecCekim.x = 0; });
        });
    }

    /* =========================================================
       14. YORUM GÖNDERME PANELİ
       -------------------------------------------------------------
       - Puan sözcüğü + karakter sayacı: JS varsa her koşulda çalışır
       - Katman modu (sağdan kayan off-canvas) yalnızca kinetik modda
       - Gönderim durumu: buton "İletiliyor" + belirsiz ilerleme çizgisi

       Katman moduna geçilmezse panel akışın içinde normal bir form
       olarak kalır; ziyaretçi yorumunu her hâlükârda gönderebilir.
       ========================================================= */
    function paylasPaneli() {
        var kat = document.getElementById('paylas');
        if (!kat) return;

        var panel = kat.querySelector('.paylas__panel');
        var form = kat.querySelector('.paylas__form');
        if (!panel || !form) return;

        formYardimcilari(form);
        gonderimDurumu(form);

        // Akışta kalsın: GSAP yoksa katmanı açacak bir şey de yok
        if (!kinetik) return;
        katmanKur(kat, panel);
    }

    /* ---------- Puan sözcüğü + karakter sayacı ---------- */
    function formYardimcilari(form) {
        var sozcukler = {
            '1': 'Geliştirilmeli', '2': 'Orta', '3': 'İyi',
            '4': 'Çok iyi', '5': 'Mükemmel'
        };

        var puanYazi = form.querySelector('.puan__yazi');
        var radyolar = Array.prototype.slice.call(form.querySelectorAll('.puan__radyo'));

        if (puanYazi) {
            radyolar.forEach(function (radyo) {
                radyo.addEventListener('change', function () {
                    puanYazi.textContent = sozcukler[radyo.value] || '';
                });
            });
        }

        var govde = form.querySelector('.alan--govde .alan__girdi');
        var sayac = form.querySelector('.alan__sayac');

        if (govde && sayac) {
            var sinir = govde.getAttribute('maxlength') || '1000';
            var yaz = function () { sayac.textContent = govde.value.length + ' / ' + sinir; };
            govde.addEventListener('input', yaz);
            yaz();
        }

        manyetikDugme(form.querySelector('.paylas__gonder'));
    }

    /* ---------- Manyetik gönder butonu ---------- */
    function manyetikDugme(dugme) {
        if (!dugme || !inceIsaretci || azHareket || !gsapVar) return;

        var xAyar = gsap.quickTo(dugme, 'x', { duration: 0.45, ease: 'power3.out' });
        var yAyar = gsap.quickTo(dugme, 'y', { duration: 0.45, ease: 'power3.out' });

        dugme.addEventListener('mousemove', function (olay) {
            var r = dugme.getBoundingClientRect();
            xAyar((olay.clientX - (r.left + r.width / 2)) * 0.28);
            yAyar((olay.clientY - (r.top + r.height / 2)) * 0.34);
        }, { passive: true });

        dugme.addEventListener('mouseleave', function () { xAyar(0); yAyar(0); });
    }

    /* ---------- Gönderim durumu ----------
       ortak.js formu kilitleyip butonun textContent'ini değiştiriyor;
       bu yüzden görünen yazıyı CSS ::before üstleniyor, biz yalnızca
       sınıfı ekliyoruz. Tarayıcı doğrulaması gönderimi engellerse
       submit olayı hiç tetiklenmez, buton da durum değiştirmez. */
    function gonderimDurumu(form, dugme) {
        dugme = dugme || form.querySelector('.paylas__gonder');
        if (!dugme) return;

        form.addEventListener('submit', function () {
            dugme.classList.add('rosso-iletiliyor');
        });

        // Geri tuşuyla önbellekten dönülünce buton takılı kalmasın
        window.addEventListener('pageshow', function (olay) {
            if (olay.persisted) dugme.classList.remove('rosso-iletiliyor');
        });
    }

    /* =========================================================
       İMZA + BAŞA DÖN

       Devasa ROSSO LOUNGE yazısı artık footer'da değil, iletişim
       bölümünde haritanın sağında. Eski "sabit footer perdesi"
       (fixed footer + boşluk ölçme) tamamen kaldırıldı: footer iki
       satıra indi, perdeye gerek kalmadı.

       İmza görünürken NAVBAR gizleniyor — aynı anda iki ROSSO LOUNGE
       yazısı ekranda durmasın. IntersectionObserver kullanılıyor,
       kaydırma dinleyicisi yok.
       ========================================================= */
    function imzaBolumu() {
        var imza = document.querySelector('.imza');
        var nav = document.querySelector('.nav');

        /* --- Navbarı gizle/göster --- */
        if (imza && nav && 'IntersectionObserver' in window) {
            new IntersectionObserver(function (girisler) {
                girisler.forEach(function (giris) {
                    nav.classList.toggle('nav--gizli', giris.isIntersecting);
                });
            }, {
                /* Üstte navbar yüksekliği kadar pay: imza gerçekten
                   navbarın hizasına gelmeden gizlemeye gerek yok. */
                rootMargin: '-72px 0px -25% 0px',
                threshold: 0
            }).observe(imza);
        }

        /* --- Harflerin maskeden yükselmesi --- */
        if (imza && kinetik) {
            var harfler = imza.querySelectorAll('.imza__harf > span');
            if (harfler.length) {
                /* y: 0 ŞART. GSAP, CSS'teki translateY(105%) değerini kendi
                   PX kanalına emiyor; yPercent ayrı kanal olduğu için ikisi
                   toplanıyor ve harfler bitişte hâlâ aşağıda kalıyordu. */
                gsap.fromTo(harfler,
                    { yPercent: 105, y: 0 },
                    {
                        yPercent: 0,
                        duration: 1.05,
                        ease: 'expo.out',
                        stagger: 0.035,
                        scrollTrigger: { trigger: imza, start: 'top 88%' }
                    });
            }
        }

        /* --- Footer şeridi --- */
        var serit = document.querySelector('.finale__alt');
        if (serit && kinetik) {
            gsap.fromTo(serit,
                { autoAlpha: 0, y: 20 },
                {
                    autoAlpha: 1, y: 0, duration: 0.8, ease: 'power2.out',
                    /* 'top bottom': şerit görünmeye başlar başlamaz.
                       'top 95%' ULAŞILAMIYORDU — ölçüldü: tetik başlangıcı
                       9275, sayfanın gidebildiği son nokta 9274. Sayfanın
                       en dibindeki öğede yüzdeli başlangıç bu yüzden riskli. */
                    scrollTrigger: { trigger: serit, start: 'top bottom' }
                });
        }

        /* --- Başa dön --- */
        var dugme = document.querySelector('.basa-don');
        if (!dugme) return;

        dugme.hidden = false;

        dugme.addEventListener('click', function () {
            if (lenis) lenis.scrollTo(0, { duration: 1.1 });
            else window.scrollTo({ top: 0, behavior: azHareket ? 'auto' : 'smooth' });
        });

        var esik = document.querySelector('.finale') || serit;
        if (esik && 'IntersectionObserver' in window) {
            new IntersectionObserver(function (girisler) {
                girisler.forEach(function (giris) {
                    dugme.classList.toggle('basa-don--gorunur', giris.isIntersecting);
                });
            }, { rootMargin: '0px 0px 20% 0px', threshold: 0 }).observe(esik);
        } else {
            dugme.classList.add('basa-don--gorunur');
        }
    }


    /* ---------- Katman modu ---------- */
    function katmanKur(kat, panel) {
        var acDugme = document.getElementById('paylas-ac');
        var kapatDugme = kat.querySelector('.paylas__kapat');
        var zemin = kat.querySelector('.paylas__zemin');
        if (!acDugme || !kapatDugme) return;

        kat.classList.add('paylas--katman');
        kat.hidden = true;
        panel.setAttribute('role', 'dialog');
        panel.setAttribute('aria-modal', 'true');
        acDugme.hidden = false;
        kapatDugme.hidden = false;

        var acik = false;

        /* Kinetik mod açılışta doğrulanıyor ama emniyet katmanı ticker
           ölüyse sınıfı sonradan kaldırıyor. Bayrağı dondurursak panel
           ekran dışında (xPercent 100) kilitli kalır — galeri büyütecinde
           bir kez yaşandı. Karar her çağrıda canlı sinyalden okunuyor. */
        function canlandirMi() {
            return gsapVar && kok.classList.contains('rosso-kinetik');
        }

        /* Panelin arkasındaki sayfa kaymasın. Galeri pinliyken body
           overflow'una dokunmuyoruz — pin-spacer'ın yüksekliğini bozup
           ScrollTrigger'ı şaşırtıyor. Lenis'i durdurmak tekerleği kesiyor. */
        function kilit(kapali) {
            if (lenis) {
                if (kapali) { lenis.stop(); } else { lenis.start(); }
            }
            var galeri = document.querySelector('.galeri');
            if (!galeri || !galeri.classList.contains('galeri--pinli')) {
                document.body.style.overflow = kapali ? 'hidden' : '';
            }
        }

        function odaklanabilirler() {
            return Array.prototype.slice.call(panel.querySelectorAll(
                'button:not([hidden]):not(:disabled), input:not([type="hidden"]),' +
                ' select, textarea, [href], [tabindex]:not([tabindex="-1"])'
            ));
        }

        function ac() {
            if (acik) return;
            acik = true;

            kat.hidden = false;
            kilit(true);
            panel.focus();

            if (!canlandirMi()) {
                gsap.set([zemin, panel], { clearProps: 'opacity,visibility,transform,filter' });
                return;
            }

            gsap.set(zemin, { opacity: 0 });
            gsap.to(zemin, { opacity: 1, duration: 0.45, ease: 'power2.out' });

            gsap.set(panel, { xPercent: 100, filter: 'blur(14px)' });
            gsap.to(panel, {
                xPercent: 0, filter: 'blur(0px)',
                duration: 0.8, ease: 'expo.out'
            });

            gsap.fromTo(icerikler(),
                { autoAlpha: 0, y: 24 },
                {
                    autoAlpha: 1, y: 0, duration: 0.7, ease: 'expo.out',
                    stagger: 0.05, delay: 0.18
                });
        }

        function icerikler() {
            return panel.querySelectorAll(
                '.paylas__ustbaslik, .paylas__baslik, .paylas__alt, .paylas__form > *'
            );
        }

        function kapat() {
            if (!acik) return;
            acik = false;

            function bitir() {
                kat.hidden = true;
                if (gsapVar) {
                    gsap.set([panel, zemin], { clearProps: 'transform,filter,opacity' });
                    gsap.set(icerikler(), { clearProps: 'opacity,visibility,transform' });
                }
                kilit(false);
                acDugme.focus();
            }

            if (!canlandirMi()) { bitir(); return; }

            gsap.to(zemin, { opacity: 0, duration: 0.4, ease: 'power2.in' });
            gsap.to(panel, {
                xPercent: 100, filter: 'blur(10px)',
                duration: 0.5, ease: 'power3.in', onComplete: bitir
            });
        }

        acDugme.addEventListener('click', ac);
        kapatDugme.addEventListener('click', kapat);
        if (zemin) zemin.addEventListener('click', kapat);

        document.addEventListener('keydown', function (olay) {
            if (!acik) return;

            if (olay.key === 'Escape') {
                kapat();
                return;
            }

            if (olay.key !== 'Tab') return;

            // Odak tuzağı: sekme panel içinde dönsün
            var liste = odaklanabilirler();
            if (!liste.length) return;

            var ilk = liste[0];
            var son = liste[liste.length - 1];

            if (olay.shiftKey && (document.activeElement === ilk || document.activeElement === panel)) {
                olay.preventDefault();
                son.focus();
            } else if (!olay.shiftKey && document.activeElement === son) {
                olay.preventDefault();
                ilk.focus();
            }
        });
    }

    /* =========================================================
       15. KONSİYER — iletişim bölümü
       -------------------------------------------------------------
       - Başlık kelime kelime maskeden yükseliyor (SplitText)
       - Bilgi satırları kaydırmada sırayla, aşağıdan yukarı
         maskelenerek açılıyor (.kn-maske → clip-path)
       - İki formun gönder butonu manyetik + "İletiliyor" durumlu

       .kn-maske gizlemesi yalnızca html.rosso-kinetik altında
       geçerli ve gorunurlukEmniyeti onu zaten temizliyor; GSAP
       düşerse bilgiler olduğu gibi açık gelir.
       ========================================================= */
    function konsiyerBolumu() {
        var bolum = document.querySelector('.konsiyer');
        if (!bolum) return;

        // Formlar kinetik moddan bağımsız çalışmalı
        Array.prototype.slice.call(bolum.querySelectorAll('.konsiyer__form')).forEach(function (form) {
            var dugme = form.querySelector('.konsiyer__gonder');
            manyetikDugme(dugme);
            gonderimDurumu(form, dugme);
        });

        if (!kinetik) return;

        var sol = bolum.querySelector('.konsiyer__sol');
        if (!sol) return;

        /* --- Başlık: kelime kelime maskeden --- */
        var baslik = bolum.querySelector('[data-kelime-acilis]');
        if (baslik && typeof window.SplitText !== 'undefined') {
            var bol = new SplitText(baslik, { type: 'words' });

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
                stagger: 0.05,
                scrollTrigger: { trigger: baslik, start: 'top 86%' }
            });
        }

        /* --- Bilgi satırları: kademeli maske ---
           Tek bir ritim olsun diye soldaki tüm .kn-maske öğeleri
           aynı zaman çizgisinde, yukarıdan aşağı sırayla açılıyor. */
        var maskeliler = sol.querySelectorAll('.kn-maske');
        if (maskeliler.length) {
            gsap.to(maskeliler, {
                clipPath: 'inset(0 0 0% 0)',
                duration: 1,
                ease: 'expo.out',
                stagger: 0.085,
                scrollTrigger: { trigger: sol, start: 'top 74%' }
            });
        }
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

            /* ÖNCE tween'leri öldür. gsap.from(...) immediateRender ile
               "gizli" başlangıç durumunu geri yazıyor: inline stilleri
               temizlesek bile maskeli başlık kelimeleri yPercent 118'de
               kalıp taşan kabın dışında görünmez oluyordu. Ölçümle
               yakalandı — hem konsiyer hem hakkımızda başlığı etkiliyordu. */
            var kinetikOgeler = document.querySelectorAll('.kelime-kap > *, .kn-satir-ic, .imza__harf > span');
            if (kinetikOgeler.length) {
                gsap.killTweensOf(kinetikOgeler);
                gsap.set(kinetikOgeler, { clearProps: 'all' });
            }

            // GSAP'in inline yazdığı gizlemeleri de temizle
            document.querySelectorAll(
                '.hakkinda__govde, .hakkinda__olcut, .hakkinda__yil,' +
                ' .hakkinda__alinti, .hakkinda__cerceve, .kn-maske, .kn-kaydir, .kn-solgun,' +
                ' .hero__baslik, .hero__ustbaslik, .hero__alt, .hero__eylemler,' +
                ' .hero__eylemler > *, .hero__durum,' +
                ' .nav__marka, .nav__menu > li, .nav__eylem,' +
                ' .menu__kalem, .menu__bas > *,' +
                ' .vitrin__kart, .vitrin__bas > *, .vitrin__eylem,' +
                ' .defter__yaprak, .defter__satir, .defter__isaret, .defter__imza,' +
                ' .paylas__panel, .paylas__zemin, .paylas__ustbaslik, .paylas__baslik,' +
                ' .paylas__alt, .paylas__form > *,' +
                ' .kelime-kap > *, .konsiyer__satir, .konsiyer__baslik,' +
                ' .finale__alt, .imza__harf > span'
            ).forEach(function (oge) {
                oge.style.opacity = '';
                oge.style.visibility = '';
                oge.style.clipPath = '';
                oge.style.transform = '';
            });

            /* Galeri pin'i tickersız ilerleyemez: kullanıcı 100vh'lik
               kıpırdamayan bir bölümde sıkışır. Pin sökülür, ray CSS'teki
               doğal yatay kaydırıcı hâline geri döner. */
            if (galeriPin) {
                galeriPin.kill(true);
                galeriPin = null;
            }
            var galeri = document.querySelector('.galeri');
            if (galeri) galeri.classList.remove('galeri--pinli');

            /* Defter sergisi de tickersız ilerlemez: yapraklar alt alta
               okunur listeye dönsün, hiçbir yorum gizli kalmasın. */
            var defter = document.querySelector('.defter');
            if (defter) defter.classList.remove('defter--sahnede');

            /* Yorum gönderme paneli katman modunda ekran dışında
               (xPercent 100) duruyor olabilir; akıştaki normal form
               hâline döndürülüyor ki gönderim yolu kapanmasın. */
            var paylas = document.getElementById('paylas');
            if (paylas) {
                paylas.classList.remove('paylas--katman');
                paylas.hidden = false;
                document.body.style.overflow = '';

                var paylasAc = document.getElementById('paylas-ac');
                if (paylasAc) paylasAc.hidden = true;

                var paylasKapat = paylas.querySelector('.paylas__kapat');
                if (paylasKapat) paylasKapat.hidden = true;

                var paylasPanel = paylas.querySelector('.paylas__panel');
                if (paylasPanel) {
                    paylasPanel.removeAttribute('role');
                    paylasPanel.removeAttribute('aria-modal');
                }
            }

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

        [lenisBaslat, capaBagla, navIzleyici, imlecBaslat, navDurumu, menuBagla, heroParallax, kaydirmaGirisleri, hakkindaBolumu, vitrinBolumu, menuSergisi, galeriSergisi, yorumDefteri, paylasPaneli, konsiyerBolumu, imzaBolumu, gorunurlukEmniyeti]
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
