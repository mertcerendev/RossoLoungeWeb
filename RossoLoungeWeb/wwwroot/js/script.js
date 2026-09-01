/* =========================================================
   ROSSO LOUNGE BISTRO — ZİYARETÇİ SİTESİ
   ---------------------------------------------------------
   Yüklendiği sayfalar: Views/Home/Index.cshtml, Views/Home/Menu.cshtml
   Panel sayfaları panel.js kullanır; ortak.js ikisinde de yüklüdür.

   style.css'in bağlı olduğu kancalar (DEĞİŞTİRMEYİN):
   #navbar  .kaydirildi  .hamburger  .nav-links  .active  .scroll-ipucu
   .toast-bildirim  .toast-kapat  .kapaniyor  .basarili  .hata
   .reveal  .gorunur  --reveal-gecikme
   #reservation-form  #datePicker  #phoneInput
   .menu-filtre-btn  .menu-category  .menu-item

   Her blok kendi elemanını arar ve bulamazsa sessizce çıkar,
   çünkü aynı dosya iki farklı sayfada çalışıyor.
   ========================================================= */
(function () {
    'use strict';

    var azHareketTercihi = window.matchMedia('(prefers-reduced-motion: reduce)');

    function azHareket() {
        return azHareketTercihi.matches;
    }

    // 'auto' CSS'teki scroll-behavior'a uyar; style.css'te html { smooth }
    // olduğu için hareket azaltma tercihinde 'instant' diyoruz.
    function kaydirmaDavranisi() {
        return azHareket() ? 'instant' : 'smooth';
    }

    function navbarYuksekligi() {
        var bar = document.getElementById('navbar');
        return bar ? bar.getBoundingClientRect().height : 0;
    }

    // Sabit navbar başlığı örtmesin diye hedefin biraz üstüne kaydırıyoruz.
    function hedefeKaydir(hedef, aninda) {
        var ust = hedef.getBoundingClientRect().top + window.pageYOffset
            - navbarYuksekligi() - 12;

        window.scrollTo({
            top: Math.max(ust, 0),
            behavior: aninda ? 'instant' : kaydirmaDavranisi()
        });
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
    // 2. NAVİGASYON — hamburger, sayfa içi bağlantılar
    // ============================================================
    (function () {
        var hamburger = document.querySelector('.hamburger');
        var navLinks = document.querySelector('.nav-links');

        if (hamburger && navLinks) {
            hamburger.setAttribute('aria-expanded', 'false');

            function menuyuKapat() {
                if (!navLinks.classList.contains('active')) return;
                navLinks.classList.remove('active');
                hamburger.setAttribute('aria-expanded', 'false');
            }

            hamburger.addEventListener('click', function (olay) {
                olay.stopPropagation();
                var acik = navLinks.classList.toggle('active');
                hamburger.setAttribute('aria-expanded', acik ? 'true' : 'false');
            });

            // Menüden bir bağlantıya tıklanınca kapansın
            navLinks.addEventListener('click', function (olay) {
                if (olay.target.closest('a')) menuyuKapat();
            });

            // Boşluğa tıklayınca kapansın
            document.addEventListener('click', function (olay) {
                if (navLinks.contains(olay.target) || hamburger.contains(olay.target)) return;
                menuyuKapat();
            });

            // Esc ile kapansın (klavye kullanıcıları için)
            document.addEventListener('keydown', function (olay) {
                if (olay.key === 'Escape') menuyuKapat();
            });

            // Kaydırınca kapansın
            window.addEventListener('scroll', menuyuKapat, { passive: true });
        }

        // Sayfa içi bağlantılar: navbar yüksekliği kadar ofsetle kaydır.
        document.addEventListener('click', function (olay) {
            var baglanti = olay.target.closest('a[href^="#"]');
            if (!baglanti) return;

            var hedefId = baglanti.getAttribute('href');

            if (hedefId === '#') {
                olay.preventDefault();
                window.scrollTo({ top: 0, behavior: kaydirmaDavranisi() });
                return;
            }

            var hedef = document.querySelector(hedefId);
            if (!hedef) return;

            olay.preventDefault();
            hedefeKaydir(hedef);

            // Klavye odağı da hedefe taşınsın, yoksa Tab başa döner.
            if (!hedef.hasAttribute('tabindex')) hedef.setAttribute('tabindex', '-1');
            hedef.focus({ preventScroll: true });
        });

        // Başka sayfadan #çıpa ile gelindiğinde tarayıcı navbar'ı hesaba katmıyor.
        window.addEventListener('load', function () {
            if (!location.hash) return;

            var hedef = null;
            try {
                hedef = document.querySelector(location.hash);
            } catch (e) {
                return; // geçersiz seçici içeren hash
            }

            if (hedef) hedefeKaydir(hedef, true);
        });
    })();

    // ============================================================
    // 3. NAVBAR KAYDIRMA DURUMU (.kaydirildi)
    // ============================================================
    (function () {
        var ustBar = document.getElementById('navbar');
        if (!ustBar) return;

        var bekleyen = false;

        function guncelle() {
            bekleyen = false;
            ustBar.classList.toggle('kaydirildi', window.scrollY > 50);
        }

        guncelle();

        window.addEventListener('scroll', function () {
            if (bekleyen) return;
            bekleyen = true;
            window.requestAnimationFrame(guncelle);
        }, { passive: true });
    })();

    // ============================================================
    // 4. KAYDIRMA ANİMASYONLARI (.reveal → .gorunur)
    //    IntersectionObserver yoksa ya da kullanıcı hareket azaltma
    //    istiyorsa .reveal hiç eklenmez; içerik olduğu gibi görünür.
    // ============================================================
    (function () {
        var hedefler = document.querySelectorAll(
            '.section-title, .bolum-ustbaslik, .menu-card, .menu-category,' +
            ' .hakkimizda-gorseller, .hakkimizda-metin, .rakam-serit,' +
            ' .mozaik-kare, .review-card, .review-form-container,' +
            ' .bilgi-kart, .harita-kutu, .form-panel'
        );

        if (!hedefler.length) return;
        if (azHareket() || !('IntersectionObserver' in window)) return;

        Array.prototype.forEach.call(hedefler, function (el) {
            el.classList.add('reveal');
        });

        var gozlemci = new IntersectionObserver(function (girisler) {
            girisler.forEach(function (giris) {
                if (!giris.isIntersecting) return;

                // Yan yana duran kartlar sırayla belirsin
                var kardesler = Array.prototype.slice.call(giris.target.parentElement.children);
                var sira = kardesler.indexOf(giris.target);
                giris.target.style.setProperty('--reveal-gecikme', Math.min(sira, 5) * 0.08 + 's');

                giris.target.classList.add('gorunur');
                gozlemci.unobserve(giris.target);
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -60px 0px' });

        Array.prototype.forEach.call(hedefler, function (el) {
            gozlemci.observe(el);
        });
    })();

    // ============================================================
    // 5. AKTİF MENÜ BAĞLANTISI (sadece tek sayfalık ana sayfada)
    // ============================================================
    (function () {
        var bolumler = document.querySelectorAll('section[id]');
        var baglantilar = document.querySelectorAll('.nav-links a[href^="#"]');

        if (!bolumler.length || !baglantilar.length) return;
        if (!('IntersectionObserver' in window)) return;

        var gozlemci = new IntersectionObserver(function (girisler) {
            girisler.forEach(function (giris) {
                if (!giris.isIntersecting) return;

                Array.prototype.forEach.call(baglantilar, function (baglanti) {
                    baglanti.classList.toggle(
                        'active',
                        baglanti.getAttribute('href') === '#' + giris.target.id
                    );
                });
            });
        }, { rootMargin: '-45% 0px -50% 0px' });

        Array.prototype.forEach.call(bolumler, function (bolum) {
            gozlemci.observe(bolum);
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
            var grup = girdi.closest('.form-group') || girdi.parentElement;
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
            hedefeKaydir(ilkHatali);
            ilkHatali.focus({ preventScroll: true });
        });
    })();

    // ============================================================
    // 7. MENÜ SAYFASI KATEGORİ FİLTRESİ
    //    JS kapalıysa hiçbir bölüm gizlenmez; tüm menü görünür kalır.
    // ============================================================
    (function () {
        var dugmeler = document.querySelectorAll('.menu-filtre-btn');
        var bolumler = document.querySelectorAll('.menu-category');

        if (!dugmeler.length || !bolumler.length) return;

        function gorunurYap(bolum) {
            // Bölüm kaydırma animasyonu için .reveal almış olabilir; filtreyle
            // sonradan gösterilen bölüm saydam kalmasın diye elle açıyoruz.
            bolum.classList.add('gorunur');
            Array.prototype.forEach.call(bolum.querySelectorAll('.reveal'), function (el) {
                el.classList.add('gorunur');
            });
        }

        Array.prototype.forEach.call(dugmeler, function (dugme) {
            dugme.addEventListener('click', function () {
                var secili = dugme.getAttribute('data-kategori');

                Array.prototype.forEach.call(dugmeler, function (d) {
                    var aktif = (d === dugme);
                    d.setAttribute('aria-pressed', aktif ? 'true' : 'false');
                    d.classList.toggle('rs-btn-birincil', aktif);
                    d.classList.toggle('rs-btn-ikincil', !aktif);
                });

                Array.prototype.forEach.call(bolumler, function (bolum) {
                    var goster = (secili === 'tumu') ||
                        (bolum.getAttribute('data-kategori') === secili);

                    bolum.hidden = !goster;
                    if (goster) gorunurYap(bolum);
                });
            });
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


    // ============================================================
    // 9. GALERİ BÜYÜTECİ (lightbox)
    // ============================================================
    const buyutec = document.getElementById('buyutec');

    if (buyutec) {
        const kareler = [...document.querySelectorAll('.mozaik-kare')];
        const gorsel = buyutec.querySelector('.buyutec-gorsel');
        const kapatDugmesi = buyutec.querySelector('.buyutec-kapat');
        const oncekiDugme = buyutec.querySelector('.buyutec-onceki');
        const sonrakiDugme = buyutec.querySelector('.buyutec-sonraki');
        let suSira = 0;
        let acanKare = null;

        const goster = (sira) => {
            suSira = (sira + kareler.length) % kareler.length;
            const kare = kareler[suSira];
            gorsel.src = kare.dataset.buyuk;
            gorsel.alt = kare.querySelector('img')?.alt || '';
        };

        const ac = (sira) => {
            acanKare = kareler[sira];
            goster(sira);
            buyutec.hidden = false;
            document.body.style.overflow = 'hidden'; // arkadaki sayfa kaymasın
            kapatDugmesi.focus();
        };

        const kapat = () => {
            buyutec.hidden = true;
            gorsel.src = '';
            document.body.style.overflow = '';
            if (acanKare) acanKare.focus(); // odak geldiği yere dönsün
        };

        kareler.forEach((kare, sira) => {
            kare.addEventListener('click', () => ac(sira));
        });

        kapatDugmesi.addEventListener('click', kapat);
        oncekiDugme.addEventListener('click', () => goster(suSira - 1));
        sonrakiDugme.addEventListener('click', () => goster(suSira + 1));

        // Boşluğa tıklayınca kapansın (görselin veya butonların üstü hariç)
        buyutec.addEventListener('click', (olay) => {
            if (olay.target === buyutec) kapat();
        });

        document.addEventListener('keydown', (olay) => {
            if (buyutec.hidden) return;

            if (olay.key === 'Escape') {
                kapat();
            } else if (olay.key === 'ArrowLeft') {
                goster(suSira - 1);
            } else if (olay.key === 'ArrowRight') {
                goster(suSira + 1);
            } else if (olay.key === 'Tab') {
                // Odak tuzağı: sekme büyüteç içinde dönsün, arkadaki sayfaya kaçmasın
                const odaklanabilir = [kapatDugmesi, oncekiDugme, sonrakiDugme];
                const su = odaklanabilir.indexOf(document.activeElement);
                olay.preventDefault();
                const yon = olay.shiftKey ? -1 : 1;
                const yeni = (su + yon + odaklanabilir.length) % odaklanabilir.length;
                odaklanabilir[yeni].focus();
            }
        });
    }


    // ============================================================
    // 10. KATLANABİLİR YORUM FORMU
    // ============================================================
    const yorumAc = document.querySelector('.yorum-ac');

    if (yorumAc) {
        const govde = document.getElementById(yorumAc.getAttribute('aria-controls'));

        yorumAc.addEventListener('click', () => {
            const acik = yorumAc.getAttribute('aria-expanded') === 'true';
            yorumAc.setAttribute('aria-expanded', acik ? 'false' : 'true');
            if (govde) govde.hidden = acik;
        });

        // Doğrulama hatası döndüyse form kapalı kalmasın
        if (govde && govde.querySelector('.rs-hata-metin, [aria-invalid="true"]')) {
            yorumAc.click();
        }
    }

})();
