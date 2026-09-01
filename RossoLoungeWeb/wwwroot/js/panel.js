/* =========================================================
   ROSSO LOUNGE BISTRO — YÖNETİM PANELİ
   ---------------------------------------------------------
   Yüklendiği sayfalar: Views/Admin/*, Views/Kategori/*, Views/Urun/*
   Ziyaretçi sitesi script.js kullanır; ortak.js ikisinde de yüklüdür.

   Her blok kendi elemanını arar ve bulamazsa sessizce çıkar,
   çünkü aynı dosya 15 panel sayfasında birden çalışıyor.

   panel.css'in bağlı olduğu kancalar (DEĞİŞTİRMEYİN):
   .toggle-password  .form-input  .mesaj-govde

   NOT: Silme onayları görünümlerdeki onsubmit="return confirm(...)"
   ile, yorum modalı ise data-ad / data-mesaj öznitelikleriyle
   çalışıyor. Kullanıcı verisi satır içi JS metnine gömülmesin diye
   bilinçli olarak böyle; onclick'e geri döndürmeyin.
   ========================================================= */
(function () {
    'use strict';

    // ============================================================
    // 1. ŞİFRE GÖSTER / GİZLE — Admin/Login.cshtml
    //    Göz simgesi <div role="button">; klavye desteğini elle veriyoruz.
    // ============================================================
    (function () {
        var girdi = document.getElementById('loginPassword');
        var dugme = document.getElementById('loginToggle');
        var ikon = document.getElementById('loginEyeIcon');

        if (!girdi || !dugme) return;

        function degistir() {
            var gizli = girdi.type === 'password';
            girdi.type = gizli ? 'text' : 'password';

            if (ikon) {
                ikon.classList.toggle('fa-eye-slash', !gizli);
                ikon.classList.toggle('fa-eye', gizli);
            }

            dugme.setAttribute('aria-pressed', gizli ? 'true' : 'false');
            dugme.setAttribute('aria-label', gizli ? 'Şifreyi gizle' : 'Şifreyi göster');
        }

        dugme.addEventListener('click', degistir);
        dugme.addEventListener('keydown', function (olay) {
            if (olay.key === 'Enter' || olay.key === ' ') {
                olay.preventDefault();
                degistir();
            }
        });
    })();

    // ============================================================
    // 2. ŞİFRE GÖSTER / GİZLE — Admin/Profil.cshtml (onay kutusu)
    // ============================================================
    (function () {
        var kutu = document.getElementById('sifreGoster');
        var girdi = document.getElementById('yeniSifre');

        if (!kutu || !girdi) return;

        kutu.addEventListener('change', function () {
            girdi.type = kutu.checked ? 'text' : 'password';
        });
    })();

    // ============================================================
    // 3. FİYAT ALANLARINDA VİRGÜL → NOKTA — Urun/Ekle + Urun/Duzenle
    //    Sunucu sayıları InvariantCulture ile okuyor; '12,50'
    //    yazılırsa 1250 olarak kaydediliyordu.
    // ============================================================
    (function () {
        var form = document.getElementById('urunForm');
        if (!form) return;

        form.addEventListener('submit', function () {
            var alanlar = form.querySelectorAll('input[name="Fiyat"], input[name="FiyatBuyuk"]');
            Array.prototype.forEach.call(alanlar, function (alan) {
                alan.value = alan.value.trim().replace(',', '.');
            });
        });
    })();

    // ============================================================
    // 4. YORUM DETAY MODALI — Admin/Yorumlar.cshtml
    //    Ziyaretçi adı/mesajı data-* özniteliğinden okunuyor ve
    //    textContent ile yazılıyor: kesme işaretli isimler
    //    (O'Brien) ve HTML içeren mesajlar sorun çıkarmıyor.
    // ============================================================
    (function () {
        var modalEl = document.getElementById('readModal');
        var baslik = document.getElementById('modalTitle');
        var govde = document.getElementById('modalMessage');

        if (!modalEl || !baslik || !govde) return;
        if (typeof bootstrap === 'undefined') return;

        var modal = new bootstrap.Modal(modalEl);

        document.addEventListener('click', function (olay) {
            var dugme = olay.target.closest('.yorum-oku');
            if (!dugme) return;

            baslik.textContent = (dugme.dataset.ad || 'Ziyaretçi') + ' diyor ki:';
            govde.textContent = dugme.dataset.mesaj || '';
            modal.show();
        });
    })();

    // ============================================================
    // 5. SÜRÜKLE-BIRAK SIRALAMA (kategoriler ve ürünler)
    // ============================================================
    // Not: Sunucu yalnızca gönderilen kayıtların sıra numaralarını kendi
    // aralarında yeniden dağıtır. Bu yüzden ürün listesinde kategori filtresi
    // açıkken sıralama yapmak, listede görünmeyen ürünleri etkilemez.
    const siraGovdesi = document.querySelector('.siralanabilir');

    if (siraGovdesi) {
        const kart = siraGovdesi.closest('.custom-card') || document;
        const durumKutusu = kart.querySelector('.sira-durum');
        const tokenAlani = kart.querySelector('input[name="__RequestVerificationToken"]');
        const url = siraGovdesi.dataset.siraUrl;
        let kaydediliyor = false;

        const durum = (metin, tur) => {
            if (!durumKutusu) return;
            durumKutusu.textContent = metin;
            durumKutusu.className = 'sira-durum' + (tur ? ' ' + tur : '');
            if (tur === 'basarili') {
                setTimeout(() => {
                    if (durumKutusu.textContent === metin) {
                        durumKutusu.textContent = '';
                        durumKutusu.className = 'sira-durum';
                    }
                }, 3000);
            }
        };

        // Satırlardaki sıra rozetlerini görsel olarak tazeler.
        // Sunucu mevcut numara havuzunu yeniden dağıttığı için, ekrandaki
        // numaralar da aynı havuzun sıralı hâli olmalı.
        const rozetleriTazele = () => {
            const rozetler = [...siraGovdesi.querySelectorAll('tr .badge-rank')];
            const numaralar = rozetler
                .map(r => parseInt(r.textContent, 10))
                .filter(n => !Number.isNaN(n))
                .sort((a, b) => a - b);
            rozetler.forEach((r, i) => {
                if (numaralar[i] !== undefined) r.textContent = numaralar[i];
            });
        };

        const kaydet = async () => {
            if (kaydediliyor) return;
            kaydediliyor = true;
            durum('Sıralama kaydediliyor…');

            const govde = new URLSearchParams();
            if (tokenAlani) govde.append('__RequestVerificationToken', tokenAlani.value);
            siraGovdesi.querySelectorAll('tr[data-id]').forEach(tr => govde.append('sira', tr.dataset.id));

            try {
                const yanit = await fetch(url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: govde
                });
                if (!yanit.ok) throw new Error('HTTP ' + yanit.status);
                rozetleriTazele();
                durum('Sıralama kaydedildi.', 'basarili');
            } catch (hata) {
                durum('Sıralama kaydedilemedi. Sayfayı yenileyip tekrar deneyin.', 'hata');
            } finally {
                kaydediliyor = false;
            }
        };

        // --- Fare ve dokunmatik: SortableJS ---
        if (window.Sortable) {
            Sortable.create(siraGovdesi, {
                handle: '.sira-tut',
                animation: 150,
                ghostClass: 'suruklenen',
                onEnd: (olay) => {
                    if (olay.oldIndex !== olay.newIndex) kaydet();
                }
            });
        } else {
            // Kütüphane yüklenemediyse klavye yolu yine de çalışır
            durum('Sürükleme kütüphanesi yüklenemedi. Ok tuşlarıyla sıralayabilirsiniz.', 'hata');
        }

        // --- Klavye: tutamak odaktayken yukarı/aşağı ok ---
        siraGovdesi.addEventListener('keydown', (olay) => {
            const tutamak = olay.target.closest('.sira-tut');
            if (!tutamak) return;
            if (olay.key !== 'ArrowUp' && olay.key !== 'ArrowDown') return;

            olay.preventDefault();
            const satir = tutamak.closest('tr');
            const hedef = olay.key === 'ArrowUp'
                ? satir.previousElementSibling
                : satir.nextElementSibling;
            if (!hedef) return;

            if (olay.key === 'ArrowUp') satir.parentNode.insertBefore(satir, hedef);
            else satir.parentNode.insertBefore(hedef, satir);

            tutamak.focus(); // DOM taşınınca odak kaybolmasın
            kaydet();
        });
    }

})();
