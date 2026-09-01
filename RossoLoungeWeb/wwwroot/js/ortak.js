/* =========================================================
   ROSSO LOUNGE BISTRO — ORTAK YARDIMCILAR
   ---------------------------------------------------------
   Hem ziyaretçi sitesinde (script.js ile birlikte) hem de
   yönetim panelinde (panel.js ile birlikte) yükleniyor.

   Tek işi: POST formlarında çift gönderimi engellemek.
   Sayfaya özel hiçbir şey bilmez, her yerde güvenle çalışır.
   ========================================================= */
(function () {
    'use strict';

    var KILIT = 'data-gonderiliyor';
    var BEKLEME_METNI = 'Gönderiliyor…';

    // Kilitlenen butonları saklıyoruz ki geri gelindiğinde (bfcache) açılabilsinler.
    var kilitliDugmeler = [];

    function dugmeMetni(dugme) {
        return dugme.tagName === 'INPUT' ? dugme.value : dugme.textContent;
    }

    function dugmeMetniYaz(dugme, metin) {
        if (dugme.tagName === 'INPUT') {
            dugme.value = metin;
        } else {
            dugme.textContent = metin;
        }
    }

    function kilitle(dugme) {
        if (!dugme || dugme.disabled) return;

        dugme.disabled = true;
        dugme.setAttribute('aria-busy', 'true');

        // İkon-only butonlarda metin yazmak düzeni bozar; onları sadece kilitliyoruz.
        var eski = dugmeMetni(dugme);
        if (eski.trim() !== '') {
            dugme.dataset.eskiMetin = eski;
            dugmeMetniYaz(dugme, BEKLEME_METNI);
        }

        kilitliDugmeler.push(dugme);
    }

    function kilidiAc(dugme) {
        dugme.disabled = false;
        dugme.removeAttribute('aria-busy');

        if (typeof dugme.dataset.eskiMetin === 'string') {
            dugmeMetniYaz(dugme, dugme.dataset.eskiMetin);
            delete dugme.dataset.eskiMetin;
        }
    }

    function gonderimDugmesi(form, olay) {
        return olay.submitter ||
            form.querySelector('button[type="submit"], input[type="submit"]');
    }

    var formlar = document.querySelectorAll('form');

    Array.prototype.forEach.call(formlar, function (form) {
        // GET formları (ör. ürün listesindeki kategori filtresi) veri değiştirmez.
        if (form.method.toLowerCase() !== 'post') return;

        form.addEventListener('submit', function (olay) {
            if (form.hasAttribute(KILIT)) {
                olay.preventDefault();
                return;
            }

            var dugme = gonderimDugmesi(form, olay);

            // Kilidi bir sonraki tur'a bırakıyoruz: böylece bizden SONRA çalışan
            // doğrulama kodu (script.js rezervasyon kontrolü) ya da satır içi
            // onsubmit="return confirm(...)" gönderimi iptal ederse form kilitli
            // kalmaz. Ayrıca butonun name/value değeri isteğe dahil olur.
            setTimeout(function () {
                if (olay.defaultPrevented) return;

                form.setAttribute(KILIT, '');
                kilitle(dugme);
            }, 0);
        });
    });

    // Tarayıcıda "geri" ile dönülünce sayfa önbellekten gelir; formu tekrar aç.
    window.addEventListener('pageshow', function (olay) {
        if (!olay.persisted) return;

        Array.prototype.forEach.call(formlar, function (form) {
            form.removeAttribute(KILIT);
        });

        kilitliDugmeler.forEach(kilidiAc);
        kilitliDugmeler = [];
    });
})();
