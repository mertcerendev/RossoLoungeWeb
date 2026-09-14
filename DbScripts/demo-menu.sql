SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

/* ---------- 1) KATEGORILER ---------- */
-- Yeni kategoriler yalnizca yoksa eklenir; sira herkeste guncellenir.
IF NOT EXISTS (SELECT 1 FROM Kategoriler WHERE Ad = N'Başlangıçlar')
    INSERT INTO Kategoriler (Ad, SiraNo) VALUES (N'Başlangıçlar', 1);
UPDATE Kategoriler SET SiraNo = 1 WHERE Ad = N'Başlangıçlar';
IF NOT EXISTS (SELECT 1 FROM Kategoriler WHERE Ad = N'Çorbalar')
    INSERT INTO Kategoriler (Ad, SiraNo) VALUES (N'Çorbalar', 2);
UPDATE Kategoriler SET SiraNo = 2 WHERE Ad = N'Çorbalar';
IF NOT EXISTS (SELECT 1 FROM Kategoriler WHERE Ad = N'Makarnalar')
    INSERT INTO Kategoriler (Ad, SiraNo) VALUES (N'Makarnalar', 3);
UPDATE Kategoriler SET SiraNo = 3 WHERE Ad = N'Makarnalar';
IF NOT EXISTS (SELECT 1 FROM Kategoriler WHERE Ad = N'Pizzalar')
    INSERT INTO Kategoriler (Ad, SiraNo) VALUES (N'Pizzalar', 4);
UPDATE Kategoriler SET SiraNo = 4 WHERE Ad = N'Pizzalar';
IF NOT EXISTS (SELECT 1 FROM Kategoriler WHERE Ad = N'Burgerler')
    INSERT INTO Kategoriler (Ad, SiraNo) VALUES (N'Burgerler', 5);
UPDATE Kategoriler SET SiraNo = 5 WHERE Ad = N'Burgerler';
IF NOT EXISTS (SELECT 1 FROM Kategoriler WHERE Ad = N'Ana Yemekler')
    INSERT INTO Kategoriler (Ad, SiraNo) VALUES (N'Ana Yemekler', 6);
UPDATE Kategoriler SET SiraNo = 6 WHERE Ad = N'Ana Yemekler';
IF NOT EXISTS (SELECT 1 FROM Kategoriler WHERE Ad = N'Tatlılar')
    INSERT INTO Kategoriler (Ad, SiraNo) VALUES (N'Tatlılar', 7);
UPDATE Kategoriler SET SiraNo = 7 WHERE Ad = N'Tatlılar';
IF NOT EXISTS (SELECT 1 FROM Kategoriler WHERE Ad = N'Sıcak İçecekler')
    INSERT INTO Kategoriler (Ad, SiraNo) VALUES (N'Sıcak İçecekler', 8);
UPDATE Kategoriler SET SiraNo = 8 WHERE Ad = N'Sıcak İçecekler';
IF NOT EXISTS (SELECT 1 FROM Kategoriler WHERE Ad = N'Soğuk İçecekler')
    INSERT INTO Kategoriler (Ad, SiraNo) VALUES (N'Soğuk İçecekler', 9);
UPDATE Kategoriler SET SiraNo = 9 WHERE Ad = N'Soğuk İçecekler';

/* ---------- 2) URUNLER ---------- */
UPDATE Urunler SET SiraNo = 1, ResimUrl = N'/img/urunler/menu-sezar-salata.jpg' WHERE Id = 4;
UPDATE Urunler SET SiraNo = 2, ResimUrl = N'/img/urunler/menu-kasarli-sigara-boregi.jpg' WHERE Id = 5;
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Humus Tabağı')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Humus Tabağı', N'Nohut ezmesi, tahin ve zeytinyağı; közlenmiş biber ve sıcak pide ile.', 165.00, N'/img/urunler/menu-humus-tabagi.jpg', k.Id, 3, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Başlangıçlar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Fırın Karides')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Fırın Karides', N'Tereyağı ve sarımsakla fırınlanmış karides; limon ve taze maydanoz.', 320.00, N'/img/urunler/menu-firin-karides.jpg', k.Id, 4, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Başlangıçlar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Akdeniz Salatası')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Akdeniz Salatası', N'Roka, kiraz domates, avokado ve beyaz peynir; nar ekşili sos.', 210.00, N'/img/urunler/menu-akdeniz-salatasi.jpg', k.Id, 5, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Başlangıçlar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Mozzarella Çubukları')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Mozzarella Çubukları', N'Çıtır kaplamalı mozzarella; ev yapımı domates sosuyla.', 185.00, N'/img/urunler/menu-mozzarella-cubuklari.jpg', k.Id, 6, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Başlangıçlar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Baharatlı Patates')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Baharatlı Patates', N'Kabuklu patates, özel baharat karışımı ve cheddar sos.', 130.00, N'/img/urunler/menu-baharatli-patates.jpg', k.Id, 7, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Başlangıçlar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Mercimek Çorbası')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Mercimek Çorbası', N'Kırmızı mercimek, tereyağı ve pul biber; limon dilimiyle.', 110.00, N'/img/urunler/menu-mercimek-corbasi.jpg', k.Id, 1, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Çorbalar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Ezogelin Çorbası')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Ezogelin Çorbası', N'Mercimek, bulgur ve nane; geleneksel tarif.', 115.00, N'/img/urunler/menu-ezogelin-corbasi.jpg', k.Id, 2, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Çorbalar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Mantar Çorbası')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Mantar Çorbası', N'Kremalı kültür mantarı çorbası; kıtır ekmek eşliğinde.', 140.00, N'/img/urunler/menu-mantar-corbasi.jpg', k.Id, 3, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Çorbalar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Penne Arrabbiata')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Penne Arrabbiata', N'Acılı domates sos, sarımsak ve taze fesleğen.', 280.00, N'/img/urunler/menu-penne-arrabbiata.jpg', k.Id, 1, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Makarnalar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Fettuccine Alfredo')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Fettuccine Alfredo', N'Krema, tereyağı ve parmesanla hazırlanmış klasik alfredo.', 310.00, N'/img/urunler/menu-fettuccine-alfredo.jpg', k.Id, 2, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Makarnalar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Spaghetti Bolognese')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Spaghetti Bolognese', N'Uzun sürede pişirilmiş dana kıymalı bolonez sos.', 320.00, N'/img/urunler/menu-spaghetti-bolognese.jpg', k.Id, 3, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Makarnalar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Deniz Ürünlü Linguine')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Deniz Ürünlü Linguine', N'Karides, midye ve kalamar; beyaz şarap soslu linguine.', 420.00, N'/img/urunler/menu-deniz-urunlu-linguine.jpg', k.Id, 4, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Makarnalar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Truffle Tagliatelle')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Truffle Tagliatelle', N'Trüf mantarı yağı, mantar ve parmesan; ev yapımı tagliatelle.', 380.00, N'/img/urunler/menu-truffle-tagliatelle.jpg', k.Id, 5, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Makarnalar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Pesto Soslu Penne')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Pesto Soslu Penne', N'Fesleğen pesto, çam fıstığı ve kurutulmuş domates.', 290.00, N'/img/urunler/menu-pesto-soslu-penne.jpg', k.Id, 6, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Makarnalar';
UPDATE Urunler SET SiraNo = 1 WHERE Id = 2;
UPDATE Urunler SET SiraNo = 2 WHERE Id = 8;
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Margherita')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Margherita', N'Domates sos, mozzarella ve taze fesleğen.', 260.00, N'/img/urunler/menu-margherita.jpg', k.Id, 3, 340.00, N'Orta', N'Büyük', 0
    FROM Kategoriler k WHERE k.Ad = N'Pizzalar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Sucuklu Pizza')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Sucuklu Pizza', N'Kangal sucuk, mozzarella ve közlenmiş biber.', 300.00, N'/img/urunler/menu-sucuklu-pizza.jpg', k.Id, 4, 390.00, N'Orta', N'Büyük', 0
    FROM Kategoriler k WHERE k.Ad = N'Pizzalar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Dört Peynirli')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Dört Peynirli', N'Mozzarella, gorgonzola, parmesan ve keçi peyniri.', 330.00, N'/img/urunler/menu-dort-peynirli.jpg', k.Id, 5, 430.00, N'Orta', N'Büyük', 0
    FROM Kategoriler k WHERE k.Ad = N'Pizzalar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Akdeniz Pizza')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Akdeniz Pizza', N'Kurutulmuş domates, zeytin, roka ve beyaz peynir.', 310.00, N'/img/urunler/menu-akdeniz-pizza.jpg', k.Id, 6, 400.00, N'Orta', N'Büyük', 0
    FROM Kategoriler k WHERE k.Ad = N'Pizzalar';
UPDATE Urunler SET SiraNo = 1 WHERE Id = 1;
UPDATE Urunler SET SiraNo = 2 WHERE Id = 9;
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Truffle Burger')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Truffle Burger', N'Dana köfte, trüf mayonez, mantar ve gruyere peyniri.', 380.00, N'/img/urunler/menu-truffle-burger.jpg', k.Id, 3, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Burgerler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Tavuk Burger')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Tavuk Burger', N'Çıtır tavuk göğsü, marul ve ranch sos.', 275.00, N'/img/urunler/menu-tavuk-burger.jpg', k.Id, 4, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Burgerler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'BBQ Bacon Burger')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'BBQ Bacon Burger', N'Dana köfte, dana füme, barbekü sos ve soğan halkası.', 350.00, N'/img/urunler/menu-bbq-bacon-burger.jpg', k.Id, 5, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Burgerler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Mantarlı Burger')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Mantarlı Burger', N'Dana köfte, sotelenmiş mantar ve İsviçre peyniri.', 330.00, N'/img/urunler/menu-mantarli-burger.jpg', k.Id, 6, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Burgerler';
UPDATE Urunler SET SiraNo = 1, ResimUrl = N'/img/urunler/menu-izgara-somon.jpg' WHERE Id = 6;
UPDATE Urunler SET SiraNo = 2, ResimUrl = N'/img/urunler/menu-kuzu-sis.jpg' WHERE Id = 7;
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Dana Antrikot')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Dana Antrikot', N'300 gr dinlendirilmiş antrikot; tereyağlı patates püresi.', 720.00, N'/img/urunler/menu-dana-antrikot.jpg', k.Id, 3, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Ana Yemekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Tavuk Şiş')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Tavuk Şiş', N'Yoğurtta marine edilmiş tavuk göğsü; közlenmiş sebzelerle.', 380.00, N'/img/urunler/menu-tavuk-sis.jpg', k.Id, 4, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Ana Yemekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Karışık Izgara')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Karışık Izgara', N'Kuzu pirzola, dana köfte, tavuk şiş ve kanat; iki kişilik.', 650.00, N'/img/urunler/menu-karisik-izgara.jpg', k.Id, 5, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Ana Yemekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Levrek Izgara')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Levrek Izgara', N'Bütün levrek, zeytinyağı ve limon; roka salatasıyla.', 540.00, N'/img/urunler/menu-levrek-izgara.jpg', k.Id, 6, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Ana Yemekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Kuzu Pirzola')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Kuzu Pirzola', N'Kekikli kuzu pirzola; kızarmış patates ve közlenmiş domates.', 690.00, N'/img/urunler/menu-kuzu-pirzola.jpg', k.Id, 7, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Ana Yemekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Tavuk Fajita')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Tavuk Fajita', N'Baharatlı tavuk, renkli biber ve soğan; sıcak tortilla ile.', 420.00, N'/img/urunler/menu-tavuk-fajita.jpg', k.Id, 8, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Ana Yemekler';
UPDATE Urunler SET SiraNo = 1, ResimUrl = N'/img/urunler/menu-san-sebastian.jpg' WHERE Id = 3;
UPDATE Urunler SET SiraNo = 2, ResimUrl = N'/img/urunler/menu-tiramisu.jpg' WHERE Id = 10;
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Çikolatalı Sufle')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Çikolatalı Sufle', N'Sıcak akışkan çikolata; yanında vanilyalı dondurma.', 175.00, N'/img/urunler/menu-cikolatali-sufle.jpg', k.Id, 3, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Tatlılar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Elmalı Tart')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Elmalı Tart', N'İnce tart hamuru üzerine dilimlenmiş elma; tarçın ve kaymak ile.', 170.00, N'/img/urunler/menu-elmali-tart.jpg', k.Id, 4, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Tatlılar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Künefe')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Künefe', N'Tel kadayıf ve Hatay peyniri; üzerine antep fıstığı.', 190.00, N'/img/urunler/menu-kunefe.jpg', k.Id, 5, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Tatlılar';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Profiterol')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Profiterol', N'Kremalı choux hamuru; sıcak çikolata sosu.', 160.00, N'/img/urunler/menu-profiterol.jpg', k.Id, 6, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Tatlılar';
UPDATE Urunler SET SiraNo = 1 WHERE Id = 11;
UPDATE Urunler SET SiraNo = 2 WHERE Id = 12;
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Espresso')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Espresso', N'Tek shot; yoğun ve kısa.', 85.00, N'/img/urunler/menu-espresso.jpg', k.Id, 3, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Sıcak İçecekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Latte')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Latte', N'Espresso ve buharla ısıtılmış süt; latte art ile.', 125.00, N'/img/urunler/menu-latte.jpg', k.Id, 4, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Sıcak İçecekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Cappuccino')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Cappuccino', N'Espresso, süt ve bol köpük; üzerine tarçın.', 120.00, N'/img/urunler/menu-cappuccino.jpg', k.Id, 5, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Sıcak İçecekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Sıcak Çikolata')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Sıcak Çikolata', N'Bitter çikolata ve süt; üzerine krema.', 140.00, N'/img/urunler/menu-sicak-cikolata.jpg', k.Id, 6, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Sıcak İçecekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Bitki Çayı')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Bitki Çayı', N'Ihlamur, papatya ya da nane-limon; cam demlikte.', 95.00, N'/img/urunler/menu-bitki-cayi.jpg', k.Id, 7, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Sıcak İçecekler';
UPDATE Urunler SET SiraNo = 1, ResimUrl = N'/img/urunler/menu-limonata.jpg' WHERE Id = 13;
UPDATE Urunler SET SiraNo = 2, ResimUrl = N'/img/urunler/menu-ev-yapimi-ice-tea.jpg' WHERE Id = 14;
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Portakal Suyu')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Portakal Suyu', N'Taze sıkılmış; katkısız.', 150.00, N'/img/urunler/menu-portakal-suyu.jpg', k.Id, 3, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Soğuk İçecekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Milkshake')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Milkshake', N'Çikolata, çilek ya da muz; üzerine krema.', 165.00, N'/img/urunler/menu-milkshake.jpg', k.Id, 4, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Soğuk İçecekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Ayran')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Ayran', N'Ev yapımı, köpüklü; bakır maşrapada.', 60.00, N'/img/urunler/menu-ayran.jpg', k.Id, 5, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Soğuk İçecekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Soğuk Kahve')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Soğuk Kahve', N'Espresso, süt ve buz; şuruplu ya da sade.', 145.00, N'/img/urunler/menu-soguk-kahve.jpg', k.Id, 6, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Soğuk İçecekler';
IF NOT EXISTS (SELECT 1 FROM Urunler WHERE Ad = N'Meyveli Soda')
    INSERT INTO Urunler (Ad, Aciklama, Fiyat, ResimUrl, KategoriId, SiraNo, FiyatBuyuk, FiyatTur, FiyatBuyukTur, OneCikan)
    SELECT N'Meyveli Soda', N'Limon, şeftali ya da vişne aromalı.', 90.00, N'/img/urunler/menu-meyveli-soda.jpg', k.Id, 7, NULL, NULL, NULL, 0
    FROM Kategoriler k WHERE k.Ad = N'Soğuk İçecekler';

/* ---------- 3) VITRIN ---------- */
-- Ana sayfadaki "bu haftanin seckileri" icin uc urun.
UPDATE Urunler SET OneCikan = 0;
UPDATE Urunler SET OneCikan = 1 WHERE Ad = N'Dana Antrikot';
UPDATE Urunler SET OneCikan = 1 WHERE Ad = N'Truffle Pizza';
UPDATE Urunler SET OneCikan = 1 WHERE Ad = N'San Sebastian';

COMMIT;

SELECT 'kategori=' + CAST(COUNT(*) AS VARCHAR) FROM Kategoriler;
SELECT 'urun=' + CAST(COUNT(*) AS VARCHAR) + ' gorselsiz=' + CAST(SUM(CASE WHEN ResimUrl IS NULL THEN 1 ELSE 0 END) AS VARCHAR) FROM Urunler;