/* =============================================================
   ROSSO — YÖNETİM PANELİ ETKİLEŞİMLERİ
   -------------------------------------------------------------
   Ziyaretçi sitesinin sahne motoru (Lenis + GSAP) panelde
   YÜKLENMEZ. Panel bekleyecek bir yer değil: burada yalnızca
   düz DOM işleri var, animasyonu CSS geçişleri yapıyor.

   Bağımlılık: yok.
   ============================================================= */
(function () {
    'use strict';

    /* Giriş ekranında kabuk yok ama şifre göster/gizle var; bu yüzden
       erken return yerine her yetenek kendi varlık kontrolünü yapıyor. */
    var kabuk = document.querySelector('.y-kabuk');

    var DAR = 'y-kabuk--dar';
    var CEKMECE = 'y-kabuk--cekmece';
    var ANAHTAR = 'rosso-panel-kenar-dar';
    var genisSorgu = window.matchMedia('(min-width: 901px)');

    /* ---------------------------------------------------------
       Kenar çubuğu: geniş ekranda daralt/genişlet, dar ekranda
       çekmece. Aynı düğme iki bağlama hizmet ediyor.
       --------------------------------------------------------- */
    var dugme = kabuk ? document.querySelector('[data-kenar-dugme]') : null;
    var perde = kabuk ? document.querySelector('.y-perde') : null;

    // Tercih hatırlanıyor; her sayfa açılışında yeniden ayarlamak
    // panelde gün boyu çalışan biri için can sıkıcı.
    try {
        if (kabuk && localStorage.getItem(ANAHTAR) === '1') kabuk.classList.add(DAR);
    } catch (h) { /* gizli sekmede localStorage kapalı olabilir */ }

    function darYaz(dar) {
        try { localStorage.setItem(ANAHTAR, dar ? '1' : '0'); } catch (h) { /* yoksay */ }
    }

    function cekmeceAyarla(ac) {
        if (!kabuk) return;
        kabuk.classList.toggle(CEKMECE, ac);
        if (dugme) dugme.setAttribute('aria-expanded', ac ? 'true' : 'false');
        document.body.style.overflow = ac ? 'hidden' : '';
    }

    if (dugme) {
        dugme.addEventListener('click', function () {
            if (genisSorgu.matches) {
                var dar = kabuk.classList.toggle(DAR);
                darYaz(dar);
                dugme.setAttribute('aria-pressed', dar ? 'true' : 'false');
            } else {
                cekmeceAyarla(!kabuk.classList.contains(CEKMECE));
            }
        });
    }

    if (perde) perde.addEventListener('click', function () { cekmeceAyarla(false); });

    document.addEventListener('keydown', function (olay) {
        if (olay.key === 'Escape' && kabuk && kabuk.classList.contains(CEKMECE)) cekmeceAyarla(false);
    });

    // Bağlantıya tıklanınca çekmece kapansın (dar ekranda sayfa değişiyor)
    var menu = kabuk ? document.querySelector('.y-menu') : null;
    if (menu) {
        menu.addEventListener('click', function (olay) {
            if (olay.target.closest('a')) cekmeceAyarla(false);
        });
    }

    // Geniş ekrana dönülünce çekmece durumu takılı kalmasın
    var sorguDinle = genisSorgu.addEventListener
        ? genisSorgu.addEventListener.bind(genisSorgu, 'change')
        : genisSorgu.addListener.bind(genisSorgu);

    sorguDinle(function (olay) {
        if (olay.matches) cekmeceAyarla(false);
    });

    /* ---------------------------------------------------------
       Tablo araması — sunucuya gitmeden satır süzme.
       Kaynak: [data-tablo-ara] girdisi, hedef: aria-controls ile
       gösterilen tablonun tbody satırları.
       --------------------------------------------------------- */
    document.querySelectorAll('[data-tablo-ara]').forEach(function (girdi) {
        var tablo = document.getElementById(girdi.getAttribute('aria-controls') || '');
        if (!tablo) return;

        var satirlar = Array.prototype.slice.call(tablo.querySelectorAll('tbody tr'));
        var sayac = document.querySelector('[data-tablo-sayac]');

        girdi.addEventListener('input', function () {
            var arama = girdi.value.trim().toLocaleLowerCase('tr');
            var gorunen = 0;

            satirlar.forEach(function (satir) {
                var uyuyor = !arama || satir.textContent.toLocaleLowerCase('tr').indexOf(arama) !== -1;
                satir.hidden = !uyuyor;
                if (uyuyor) gorunen++;
            });

            if (sayac) sayac.textContent = gorunen;
        });
    });

    /* ---------------------------------------------------------
       Şifre göster/gizle
       --------------------------------------------------------- */
    document.querySelectorAll('[data-sifre-gor]').forEach(function (dugme) {
        var girdi = document.getElementById(dugme.getAttribute('aria-controls') || '');
        if (!girdi) return;

        dugme.addEventListener('click', function () {
            var acik = girdi.type === 'text';
            girdi.type = acik ? 'password' : 'text';
            dugme.setAttribute('aria-label', acik ? 'Şifreyi göster' : 'Şifreyi gizle');
            dugme.innerHTML = acik
                ? '<i class="fas fa-eye" aria-hidden="true"></i>'
                : '<i class="fas fa-eye-slash" aria-hidden="true"></i>';
        });
    });
})();
