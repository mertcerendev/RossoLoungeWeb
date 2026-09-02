BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902162629_RezervasyonDurumu'
)
BEGIN
    ALTER TABLE [Rezervasyons] ADD [Durum] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902162629_RezervasyonDurumu'
)
BEGIN
    UPDATE Rezervasyons SET Durum = 1 WHERE OnaylandiMi = 1;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902162629_RezervasyonDurumu'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Rezervasyons]') AND [c].[name] = N'OnaylandiMi');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Rezervasyons] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Rezervasyons] DROP COLUMN [OnaylandiMi];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902162629_RezervasyonDurumu'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902162629_RezervasyonDurumu', N'8.0.0');
END;
GO

COMMIT;
GO

