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
       Metin kutusu (native <dialog>)
       Uzun metnin tamamını göstermek için. Odak tuzağı, Escape ve
       arka planın etkisizleşmesi tarayıcının işi — modal kütüphanesi
       yüklemeye gerek yok.
       --------------------------------------------------------- */
    (function () {
        var kutu = document.getElementById('y-metin-kutusu');
        if (!kutu || typeof kutu.showModal !== 'function') return;

        var baslik = kutu.querySelector('.y-kutu__baslik');
        var govde = kutu.querySelector('.y-kutu__govde');

        document.addEventListener('click', function (olay) {
            var acan = olay.target.closest('[data-metin-goster]');
            if (!acan) return;

            baslik.textContent = acan.dataset.baslik || 'Detay';
            govde.textContent = acan.dataset.govde || '';
            kutu.showModal();
        });
    })();

    /* ---------------------------------------------------------
       SÜRÜKLE-BIRAK SIRALAMA (kategoriler ve ürünler)
       Sunucu yalnızca gönderilen kayıtların mevcut sıra numaralarını
       kendi aralarında yeniden dağıtır; bu yüzden ürün listesinde
       kategori filtresi açıkken sıralamak, listede görünmeyen
       ürünleri etkilemez.
       --------------------------------------------------------- */
    (function () {
        var govde = document.querySelector('.y-siralanabilir');
        if (!govde) return;

        var durumKutusu = document.querySelector('.y-sira-durum');
        var tokenAlani = document.querySelector('input[name="__RequestVerificationToken"]');
        var url = govde.dataset.siraUrl;
        var kaydediliyor = false;

        function durum(metin, tur) {
            if (!durumKutusu) return;
            durumKutusu.textContent = metin;
            durumKutusu.className = 'y-sira-durum' + (tur ? ' y-sira-durum--' + tur : '');

            if (tur === 'basarili') {
                setTimeout(function () {
                    if (durumKutusu.textContent === metin) {
                        durumKutusu.textContent = '';
                        durumKutusu.className = 'y-sira-durum';
                    }
                }, 3000);
            }
        }

        // Satırlardaki sıra numaralarını görsel olarak tazeler: sunucu
        // mevcut numara havuzunu yeniden dağıttığı için ekrandaki
        // numaralar da aynı havuzun sıralı hâli olmalı.
        function numaralariTazele() {
            var kutular = Array.prototype.slice.call(govde.querySelectorAll('tr .y-sira__no'));
            var numaralar = kutular
                .map(function (k) { return parseInt(k.textContent, 10); })
                .filter(function (n) { return !isNaN(n); })
                .sort(function (a, b) { return a - b; });

            kutular.forEach(function (k, i) {
                if (numaralar[i] !== undefined) k.textContent = numaralar[i];
            });
        }

        function kaydet() {
            if (kaydediliyor) return;
            kaydediliyor = true;
            durum('Sıralama kaydediliyor…');

            var govdeVeri = new URLSearchParams();
            if (tokenAlani) govdeVeri.append('__RequestVerificationToken', tokenAlani.value);
            govde.querySelectorAll('tr[data-id]').forEach(function (tr) {
                govdeVeri.append('sira', tr.dataset.id);
            });

            fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: govdeVeri
            })
                .then(function (yanit) {
                    if (!yanit.ok) throw new Error('HTTP ' + yanit.status);
                    numaralariTazele();
                    durum('Sıralama kaydedildi.', 'basarili');
                })
                .catch(function () {
                    durum('Sıralama kaydedilemedi. Sayfayı yenileyip tekrar deneyin.', 'hata');
                })
                .then(function () { kaydediliyor = false; });
        }

        // --- Fare ve dokunmatik ---
        if (window.Sortable) {
            window.Sortable.create(govde, {
                handle: '.y-sira__tut',
                animation: 150,
                ghostClass: 'y-suruklenen',
                onEnd: function (olay) {
                    if (olay.oldIndex !== olay.newIndex) kaydet();
                }
            });
        } else {
            durum('Sürükleme kütüphanesi yüklenemedi. Ok tuşlarıyla sıralayabilirsiniz.', 'hata');
        }

        // --- Klavye: tutamak odaktayken yukarı/aşağı ok ---
        govde.addEventListener('keydown', function (olay) {
            var tutamak = olay.target.closest('.y-sira__tut');
            if (!tutamak) return;
            if (olay.key !== 'ArrowUp' && olay.key !== 'ArrowDown') return;

            olay.preventDefault();
            var satir = tutamak.closest('tr');
            var hedef = olay.key === 'ArrowUp' ? satir.previousElementSibling : satir.nextElementSibling;
            if (!hedef) return;

            if (olay.key === 'ArrowUp') satir.parentNode.insertBefore(satir, hedef);
            else satir.parentNode.insertBefore(hedef, satir);

            tutamak.focus(); // DOM taşınınca odak kaybolmasın
            kaydet();
        });
    })();

    /* ---------------------------------------------------------
       FİYAT ALANLARINDA VİRGÜL → NOKTA
       Sunucu sayıları InvariantCulture ile okuyor; '12,50' yazılırsa
       1250 olarak kaydediliyordu (100 kat). Sunucu tarafında
       OndalikModelBinder de var — bu, JS varken hatayı kullanıcı
       göndermeden düzelten ikinci katman.
       --------------------------------------------------------- */
    document.querySelectorAll('[data-ondalik-form]').forEach(function (form) {
        form.addEventListener('submit', function () {
            form.querySelectorAll('[data-ondalik]').forEach(function (alan) {
                alan.value = alan.value.trim().replace(',', '.');
            });
        });
    });

    /* ---------------------------------------------------------
       ÖZET GRAFİĞİ — dönem seçici
       Dört dönemin verisi (7 gün / 30 gün / aylık / yıllık) sunucudan
       tek seferde geliyor; dönem değiştirmek sunucuya gitmiyor, sütunlar
       yeniden çiziliyor. Nokta sayısı küçük olduğu için maliyeti yok.

       Grafik kütüphanesi YÜKLENMİYOR: sütunlar div + yükseklik yüzdesi.
       Otuz sayı için 60 KB'lık bir kütüphane taşımak gereksiz.
       --------------------------------------------------------- */
    (function () {
        var kap = document.querySelector('[data-grafik]');
        var kaynak = document.getElementById('grafik-verisi');
        if (!kap || !kaynak) return;

        var veri;
        try {
            veri = JSON.parse(kaynak.textContent);
        } catch (h) {
            return; // Bozuk veri: <noscript> tablosu yerinde kalır
        }

        var dugmeler = Array.prototype.slice.call(document.querySelectorAll('[data-donem]'));
        var yilSecici = document.querySelector('[data-yil-secici]');
        var yilKutusu = document.getElementById('grafikYil');
        var ozet = document.querySelector('[data-grafik-ozet]');
        var ANAHTAR = 'rosso-panel-grafik-donem';

        function seriGetir(donem) {
            if (donem === 'gun30') return veri.Gun30 || [];
            if (donem === 'yillik') return veri.Yillik || [];
            if (donem === 'aylik') {
                var yil = yilKutusu ? yilKutusu.value : veri.VarsayilanYil;
                return (veri.Aylik && veri.Aylik[yil]) || [];
            }
            return veri.Gun7 || [];
        }

        function ciz(donem) {
            var seri = seriGetir(donem);
            kap.innerHTML = '';

            if (!seri.length) {
                var bos = document.createElement('p');
                bos.className = 'y-bos__alt';
                bos.textContent = 'Bu dönem için kayıt yok.';
                kap.appendChild(bos);
                if (ozet) ozet.textContent = '';
                return;
            }

            var enYuksek = Math.max.apply(null, seri.map(function (n) { return n.Rezervasyon; }));
            if (enYuksek < 1) enYuksek = 1;

            var liste = document.createElement('ul');
            liste.className = 'y-sutunlar';
            // 30 sütun dar olur; kap sınıfı sütun genişliğini ayarlıyor
            liste.classList.add(seri.length > 15 ? 'y-sutunlar--sik' : 'y-sutunlar--genis');

            seri.forEach(function (nokta) {
                var oran = Math.round(nokta.Rezervasyon * 100 / enYuksek);

                var oge = document.createElement('li');
                oge.className = 'y-sutun';

                var deger = document.createElement('span');
                deger.className = 'y-sutun__deger y-sayi';
                deger.textContent = nokta.Rezervasyon;

                var cubuk = document.createElement('span');
                cubuk.className = 'y-sutun__cubuk';
                // Sıfır bile ince bir iz bıraksın: "veri yok" ile "gün boş"
                // ayrımı görünür olmalı
                cubuk.style.height = (nokta.Rezervasyon > 0 ? Math.max(oran, 4) : 2) + '%';
                cubuk.setAttribute('role', 'img');
                cubuk.setAttribute('aria-label',
                    nokta.TamEtiket + ': ' + nokta.Rezervasyon + ' rezervasyon, ' + nokta.Kisi + ' kişi');
                cubuk.title = nokta.TamEtiket + ' — ' + nokta.Rezervasyon + ' rezervasyon · ' + nokta.Kisi + ' kişi';

                if (nokta.Rezervasyon === enYuksek && nokta.Rezervasyon > 0) {
                    cubuk.classList.add('y-sutun__cubuk--zirve');
                }

                var etiket = document.createElement('span');
                etiket.className = 'y-sutun__etiket';
                etiket.textContent = nokta.Etiket;

                oge.appendChild(deger);
                oge.appendChild(cubuk);
                oge.appendChild(etiket);
                liste.appendChild(oge);
            });

            kap.appendChild(liste);

            if (ozet) {
                var toplam = seri.reduce(function (t, n) { return t + n.Rezervasyon; }, 0);
                var kisi = seri.reduce(function (t, n) { return t + n.Kisi; }, 0);
                var zirve = seri.reduce(function (e, n) { return n.Rezervasyon > e.Rezervasyon ? n : e; }, seri[0]);

                ozet.textContent = toplam > 0
                    ? 'Dönem toplamı: ' + toplam + ' rezervasyon · ' + kisi + ' kişi · en yoğun ' + zirve.TamEtiket
                    : 'Bu dönemde rezervasyon yok.';
            }
        }

        function donemSec(donem, odakla) {
            dugmeler.forEach(function (d) {
                var aktif = d.dataset.donem === donem;
                d.classList.toggle('y-donem__dugme--aktif', aktif);
                d.setAttribute('aria-selected', aktif ? 'true' : 'false');
                d.tabIndex = aktif ? 0 : -1;
                if (aktif && odakla) d.focus();
            });

            if (yilSecici) yilSecici.hidden = donem !== 'aylik';

            try { localStorage.setItem(ANAHTAR, donem); } catch (h) { /* yoksay */ }
            ciz(donem);
        }

        dugmeler.forEach(function (d) {
            d.addEventListener('click', function () { donemSec(d.dataset.donem, false); });
        });

        // Ok tuşlarıyla gezinme (WAI-ARIA sekme deseni)
        var sekmeKabi = dugmeler.length ? dugmeler[0].parentNode : null;
        if (sekmeKabi) {
            sekmeKabi.addEventListener('keydown', function (olay) {
                var su = dugmeler.indexOf(document.activeElement);
                if (su === -1) return;

                var hedef = null;
                if (olay.key === 'ArrowRight') hedef = dugmeler[(su + 1) % dugmeler.length];
                else if (olay.key === 'ArrowLeft') hedef = dugmeler[(su - 1 + dugmeler.length) % dugmeler.length];
                else if (olay.key === 'Home') hedef = dugmeler[0];
                else if (olay.key === 'End') hedef = dugmeler[dugmeler.length - 1];

                if (hedef) {
                    olay.preventDefault();
                    donemSec(hedef.dataset.donem, true);
                }
            });
        }

        if (yilKutusu) {
            yilKutusu.addEventListener('change', function () { ciz('aylik'); });
        }

        // Son seçilen dönem hatırlanıyor: panelde gün boyu çalışan biri
        // her açılışta aynı tıklamayı tekrarlamasın.
        var baslangic = 'gun7';
        try {
            var kayitli = localStorage.getItem(ANAHTAR);
            if (kayitli && dugmeler.some(function (d) { return d.dataset.donem === kayitli; })) {
                baslangic = kayitli;
            }
        } catch (h) { /* yoksay */ }

        donemSec(baslangic, false);
    })();

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
