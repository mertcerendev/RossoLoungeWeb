/* =========================================================
   ROSSO LOUNGE BISTRO — ZİYARETÇİ SİTESİ
   ---------------------------------------------------------
   _Layout.cshtml üzerinden ziyaretçi sayfalarında yüklenir.
   Panel sayfaları panel.js kullanır; ortak.js ikisinde de yüklüdür.

   NOT: Sayfa içi çapa kaydırması, navbar kaydırma durumu, hamburger ve
   menü filtresi BURADAN KALDIRILDI. Hepsinin karşılığı rosso-hareket.js'te
   (Lenis ile uyumlu). Eski sürümde bu dosya her a[href^="#"] tıklamasını
   preventDefault edip window.scrollTo çağırıyordu; Lenis aynı anda kendi
   kaydırmasını yürüttüğü için hedef tam oturmuyordu.

   style.css'in bağlı olduğu kancalar (DEĞİŞTİRMEYİN):
   .toast-bildirim  .toast-kapat  .kapaniyor  .basarili  .hata
   #reservation-form  #datePicker  #phoneInput  .form-sekmeler  .form-sekme

   Her blok kendi elemanını arar ve bulamazsa sessizce çıkar,
   çünkü aynı dosya iki farklı sayfada çalışıyor.
   ========================================================= */
(function () {
    'use strict';

    var azHareketTercihi = window.matchMedia('(prefers-reduced-motion: reduce)');

    function azHareket() {
        return azHareketTercihi.matches;
    }

    // ============================================================
    // 1. BİLDİRİM (TOAST) KAPATMA
    // ============================================================
    (function () {
        var toastlar = document.querySelectorAll('.toast-bildirim');
        if (!toastlar.length) return;

        Array.prototype.forEach.call(toastlar, function (toast) {
            var kapandi = false;

            function kapat() {
                if (kapandi) return;
                kapandi = true;
                toast.classList.add('kapaniyor');
                setTimeout(function () { toast.remove(); }, 350);
            }

            var kapatDugmesi = toast.querySelector('.toast-kapat');
            if (kapatDugmesi) kapatDugmesi.addEventListener('click', kapat);

            // Hata mesajları okunacak kadar dursun, başarı mesajı erken kapansın.
            setTimeout(kapat, toast.classList.contains('hata') ? 8000 : 5000);
        });
    })();

    // ============================================================
    // 6. REZERVASYON FORMU — flatpickr, telefon, doğrulama
    // ============================================================
    (function () {
        var form = document.getElementById('reservation-form');
        var tarihGirdisi = document.getElementById('datePicker');

        if (!form || !tarihGirdisi) return;
        if (typeof flatpickr !== 'function') return;

        // ---- Takvim ------------------------------------------------
        // altInput: kullanıcı "31.08.2026 20:00" görür, sunucuya
        // dateFormat'taki ISO değeri ("2026-08-31 20:00") gider.
        // Sunucu kültüründen bağımsız parse edilsin diye bu düzen ŞART.
        var fp = flatpickr(tarihGirdisi, {
            enableTime: true,
            altInput: true,
            altFormat: 'd.m.Y H:i',
            altInputClass: 'flatpickr-alt-input',
            dateFormat: 'Y-m-d H:i',
            minDate: 'today',
            time_24hr: true,
            locale: 'tr',
            disableMobile: 'true',
            theme: 'dark'
        });

        // altInput görünen alan olduğu için <label for="datePicker">
        // artık gizli inputu gösteriyordu; etiketi görünen alana bağlıyoruz.
        var gorunenTarih = fp.altInput || tarihGirdisi;
        if (fp.altInput) {
            fp.altInput.id = 'datePickerGorunen';
            var etiket = document.querySelector('label[for="datePicker"]');
            if (etiket) etiket.setAttribute('for', 'datePickerGorunen');

            // Yüzen etiket görünen alana taşınıyor. Asıl girdi type=hidden'a
            // dönüyor ve :placeholder-shown ile eşleşmiyor; üzerinde
            // .alan__girdi kalırsa :not(:placeholder-shown) tutuyor ve etiket
            // kalıcı olarak yukarıda takılı kalıyor.
            tarihGirdisi.classList.remove('alan__girdi');
            fp.altInput.classList.add('alan__girdi');
            fp.altInput.setAttribute('placeholder', ' ');
        }

        // ---- Telefon ------------------------------------------------
        var telefon = document.getElementById('phoneInput');
        if (telefon) {
            telefon.addEventListener('input', function () {
                var temiz = telefon.value.replace(/[^0-9]/g, '');
                if (temiz.charAt(0) === '0') temiz = temiz.substring(1);
                if (temiz.length > 10) temiz = temiz.slice(0, 10);
                telefon.value = temiz;
            });
        }

        // ---- Satır içi hata mesajları -------------------------------
        // alert() yerine alanın altında kırmızı metin + aria-invalid.
        var HATA_ISARETI = 'data-rez-hata';

        function hataKutusu(girdi, olustur) {
            var grup = girdi.closest('.alan') || girdi.parentElement;
            if (!grup) return null;

            var kutu = grup.querySelector('[' + HATA_ISARETI + ']');
            if (kutu || !olustur) return kutu;

            // .rs-koyu sarmalayıcı: rezervasyon kutusu koyu zeminli olduğu için
            // hata metni okunaklı açık kırmızıya (--renk-marka-acik) dönsün.
            kutu = document.createElement('div');
            kutu.setAttribute(HATA_ISARETI, '');
            kutu.className = 'rs-koyu';

            var metin = document.createElement('p');
            metin.className = 'rs-hata-metin';
            metin.id = girdi.id + '-hata';
            metin.setAttribute('role', 'alert');
            kutu.appendChild(metin);

            grup.appendChild(kutu);
            return kutu;
        }

        function aciklamaEkle(el, hataId) {
            if (!el) return;
            el.setAttribute('aria-invalid', 'true');

            if (typeof el.dataset.eskiAciklama !== 'string') {
                el.dataset.eskiAciklama = el.getAttribute('aria-describedby') || '';
            }

            var liste = el.dataset.eskiAciklama ? el.dataset.eskiAciklama.split(' ') : [];
            if (liste.indexOf(hataId) === -1) liste.push(hataId);
            el.setAttribute('aria-describedby', liste.join(' '));
        }

        function aciklamaSil(el) {
            if (!el) return;
            el.removeAttribute('aria-invalid');

            if (typeof el.dataset.eskiAciklama !== 'string') return;

            if (el.dataset.eskiAciklama) {
                el.setAttribute('aria-describedby', el.dataset.eskiAciklama);
            } else {
                el.removeAttribute('aria-describedby');
            }
        }

        function hataYaz(girdi, mesaj, gorunen) {
            var kutu = hataKutusu(girdi, true);
            if (!kutu) return;

            var metin = kutu.querySelector('p');
            metin.textContent = mesaj;
            kutu.hidden = false;

            aciklamaEkle(girdi, metin.id);
            if (gorunen && gorunen !== girdi) aciklamaEkle(gorunen, metin.id);
        }

        function hataSil(girdi, gorunen) {
            var kutu = hataKutusu(girdi, false);
            if (kutu) kutu.hidden = true;

            aciklamaSil(girdi);
            if (gorunen && gorunen !== girdi) aciklamaSil(gorunen);
        }

        // Kullanıcı düzeltmeye başlayınca hata kaybolsun
        var ad = document.getElementById('rez-ad');
        if (ad) ad.addEventListener('input', function () { hataSil(ad); });
        if (telefon) telefon.addEventListener('input', function () { hataSil(telefon); });
        tarihGirdisi.addEventListener('change', function () {
            hataSil(tarihGirdisi, gorunenTarih);
        });

        // ---- Gönderim -----------------------------------------------
        form.addEventListener('submit', function (olay) {
            var ilkHatali = null;

            function hata(girdi, mesaj, gorunen) {
                hataYaz(girdi, mesaj, gorunen);
                if (!ilkHatali) ilkHatali = gorunen || girdi;
            }

            // 1) Ad Soyad
            if (ad) {
                if (ad.value.trim().length < 3) {
                    hata(ad, 'Lütfen isminizi tam giriniz (en az 3 harf).');
                } else {
                    hataSil(ad);
                }
            }

            // 2) Telefon
            if (telefon) {
                if (telefon.value.trim().length < 10) {
                    hata(telefon, 'Telefon numarası 10 hane olmalı (başında 0 olmadan).');
                } else {
                    hataSil(telefon);
                }
            }

            // 3) Tarih ve saat
            if (!tarihGirdisi.value) {
                hata(tarihGirdisi, 'Lütfen tarih ve saat seçiniz.', gorunenTarih);
            } else if (fp.selectedDates.length) {
                var secilen = new Date(fp.selectedDates[0]);

                // Saat seçilmediyse flatpickr 00:00 verir; o günün tamamı geçmiş sayılmasın.
                if (secilen.getHours() === 0 && secilen.getMinutes() === 0) {
                    secilen.setDate(secilen.getDate() + 1);
                }

                if (secilen < new Date()) {
                    hata(tarihGirdisi, 'Geçmiş bir saate rezervasyon yapamazsınız.', gorunenTarih);
                } else {
                    hataSil(tarihGirdisi, gorunenTarih);
                }
            } else {
                hataSil(tarihGirdisi, gorunenTarih);
            }

            if (!ilkHatali) return;

            olay.preventDefault();
            // Kaydırmanın tek sahibi rosso-hareket.js (Lenis uyumlu)
            if (typeof window.rossoKaydir === 'function') window.rossoKaydir(ilkHatali);
            else ilkHatali.scrollIntoView({ block: 'center' });
            ilkHatali.focus({ preventScroll: true });
        });
    })();

    // ============================================================
    // 8. İLETİŞİM FORMU SEKMELERİ
    // ============================================================
    const sekmeListesi = document.querySelector('.form-sekmeler');

    if (sekmeListesi) {
        const sekmeler = [...sekmeListesi.querySelectorAll('.form-sekme')];
        const panelBul = (sekme) => document.getElementById(sekme.getAttribute('aria-controls'));

        const sekmeAc = (hedef, odakla = true) => {
            sekmeler.forEach((s) => {
                const aktif = s === hedef;
                s.classList.toggle('aktif', aktif);
                s.setAttribute('aria-selected', aktif ? 'true' : 'false');
                s.tabIndex = aktif ? 0 : -1;

                const panel = panelBul(s);
                if (panel) panel.hidden = !aktif;
            });
            if (odakla) hedef.focus();
        };

        sekmeler.forEach((sekme) => {
            sekme.addEventListener('click', () => sekmeAc(sekme, false));
        });

        // Ok tuşlarıyla sekmeler arasında gezinme (WAI-ARIA tab deseni)
        sekmeListesi.addEventListener('keydown', (olay) => {
            const su = sekmeler.indexOf(document.activeElement);
            if (su < 0) return;

            let hedef = null;
            if (olay.key === 'ArrowRight') hedef = sekmeler[(su + 1) % sekmeler.length];
            else if (olay.key === 'ArrowLeft') hedef = sekmeler[(su - 1 + sekmeler.length) % sekmeler.length];
            else if (olay.key === 'Home') hedef = sekmeler[0];
            else if (olay.key === 'End') hedef = sekmeler[sekmeler.length - 1];

            if (hedef) {
                olay.preventDefault();
                sekmeAc(hedef);
            }
        });

        // "Mesaj gönder" bağlantıları doğru sekmeyi açsın.
        // #rezervasyon çıpası panelin kendisinde olduğu için varsayılan sekme
        // zaten rezervasyon; sadece mesaj tarafını ele alıyoruz.
        const mesajSekmesi = document.getElementById('sekme-mesaj');
        document.querySelectorAll('a[href$="#iletisim-form"], a[href$="#mesaj"]').forEach((baglanti) => {
            baglanti.addEventListener('click', () => {
                if (mesajSekmesi) sekmeAc(mesajSekmesi, false);
            });
        });

        // Sunucudan hata/başarı dönerse ilgili sekme açık gelsin
        if (location.hash === '#mesaj' && mesajSekmesi) sekmeAc(mesajSekmesi, false);
    }

})();
