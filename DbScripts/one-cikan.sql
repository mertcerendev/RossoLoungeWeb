/*
    ÜRÜN: "Öne çıkan" işareti  —  migration 20260903060235_UrunOneCikan

    Ana sayfadaki vitrin ("Bu haftanın seçkileri") eskiden SiraNo'su en
    küçük 3 ürünü alıyordu; seçim yapmanın tek yolu o tabakları bütün
    menünün en tepesine sürüklemekti. Bu sütunla seçim ayrı ve açık.

    YALNIZCA EKLEMELİ: tek bir sütun ekleniyor, hiçbir veri silinmiyor
    veya değiştirilmiyor. Varsayılan 0 — yani script çalıştıktan hemen
    sonra hiçbir ürün işaretli değil ve HomeController eski davranışa
    (SiraNo'ya göre ilk 3) düşerek vitrini boş bırakmıyor. İşaretleme
    panelden yapılınca vitrin seçime geçer.

    Idempotent: iki kez çalıştırılabilir, tek transaction.

    Canlıda çalıştırma sırası (bu oturumdaki bekleyen scriptler):
      identity-gecisi.sql → rezervasyon-durumu.sql → one-cikan.sql
      (urun-gorselleri.sql sıradan bağımsız)
*/

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903060235_UrunOneCikan'
)
BEGIN
    ALTER TABLE [Urunler] ADD [OneCikan] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260903060235_UrunOneCikan'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260903060235_UrunOneCikan', N'8.0.0');
END;
GO

COMMIT;
GO
