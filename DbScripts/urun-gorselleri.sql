/* ============================================================
   ÜRÜN GÖRSELLERİNİ ADA GÖRE EŞLE

   Sorun: ürünlere görsel atanırken img/urunler/ klasöründeki
   dosyalar ALFABETİK sırayla dağıtılmış. İçerikle hiçbir ilgisi
   yok; "Sezar Salata"nın fotoğrafı Red Bull + viski bardağıydı.

   Bu betik eşlemeyi ÜRÜN ADINA göre yapar, Id'ye göre değil —
   böylece yereldeki test verisinde de canlı veritabanında da
   aynı şekilde çalışır. Adı tutmayan satıra dokunmaz.

   Görselleri gözle tek tek eşleştirdim. Eşleşmeyenler NULL
   bırakıldı: yanlış fotoğraf göstermektense görselsiz kalsın,
   view'lar zaten nötr bir yer tutucuya düşüyor.

   Çalıştırma:
     sqlcmd -S <sunucu> -d RossoLoungeDB -U <kullanici> -P <parola> \
            -i DbScripts/urun-gorselleri.sql
   ============================================================ */

SET NOCOUNT ON;

DECLARE @eslesme TABLE (Ad NVARCHAR(100), Gorsel NVARCHAR(300), Not_ NVARCHAR(200));

INSERT INTO @eslesme (Ad, Gorsel, Not_) VALUES
-- ---- Tam eşleşenler --------------------------------------------------
(N'Kuzu Şiş',              N'/img/urunler/cee19c77-3e45-4ef7-81f6-6d79930eecab.jpg', N'ızgara köfte + tavuk şiş tabağı, 1200x857'),
(N'İtalyan Pizza',         N'/img/urunler/db1909da-3b58-498a-b1a1-8eded241992d.jpg', N'jambon, roka, mozzarella; 1368x1824'),
(N'Tiramisu',              N'/img/urunler/df2a7d72-a5a0-4d3f-bd26-598a42d3b012.jpg', N'kahveli kremalı tatlı, kakao serpme; 1500x1000'),
(N'Türk Kahvesi',          N'/img/urunler/2184022b-5cc2-49af-b492-d42952de409b.jpg', N'siyah fincanda türk kahvesi; 1920x1080'),
(N'Filtre Kahve',          N'/img/urunler/f37c7d34-544a-405c-9cc8-4cc451b1f5e0.jpg', N'sade siyah kahve; 1311x1600'),
(N'Limonata',              N'/img/urunler/ae88bbe1-b6df-4c35-aafe-92d86b47872f.jpg', N'limon + nane, buzlu; 678x452'),
(N'Rosso Burger',          N'/img/site/burger.jpg',                                  N'paylasim.jpg kırpımı (bira kadraj dışı); 1050x810'),
(N'Cheeseburger',          N'/img/site/cheeseburger.jpg',                            N'paylasim.jpg kırpımı; 1260x930'),

-- ---- Doğru kategori, birebir değil (fotoğraf gelince değiştirilmeli) --
(N'Sezar Salata',          N'/img/urunler/unnamed255421232.webp',                    N'salata — ama yalnızca 125x125'),
(N'Kaşarlı Sigara Böreği', N'/img/urunler/unnamed252127065.webp',                    N'sigara böreği — ama yalnızca 125x125'),
(N'Truffle Pizza',         N'/img/urunler/45967a51-cb51-40a2-ac58-dd7b8cfbc8c9.jpg', N'pizza ama üstünde midye var, trüf değil'),
(N'San Sebastian',         N'/img/urunler/075ee9cf-5616-42f1-af26-c0222ee66b48.jpg', N'tatlı ama çikolatalı krep, cheesecake değil'),

-- ---- Eşleşme YOK: klasörde bu ürünün fotoğrafı bulunmuyor ------------
(N'Izgara Somon',          NULL, N'somon fotoğrafı yok'),
(N'Ev Yapımı Ice Tea',     NULL, N'yalnızca markalı Lipton kutusu var, ev yapımı değil');

UPDATE u
   SET u.ResimUrl = e.Gorsel
  FROM Urunler u
  JOIN @eslesme e ON e.Ad = u.Ad;

PRINT CONCAT(N'Güncellenen ürün: ', @@ROWCOUNT);

-- Sonuç
SELECT u.Ad, ISNULL(u.ResimUrl, N'(görsel yok)') AS Gorsel
  FROM Urunler u
  JOIN @eslesme e ON e.Ad = u.Ad
 ORDER BY u.SiraNo;
