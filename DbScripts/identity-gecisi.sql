IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118223128_IlkKurulum'
)
BEGIN
    CREATE TABLE [Rezervasyons] (
        [Id] int NOT NULL IDENTITY,
        [AdSoyad] nvarchar(max) NOT NULL,
        [Telefon] nvarchar(max) NOT NULL,
        [Tarih] datetime2 NOT NULL,
        [KisiSayisi] int NOT NULL,
        [Not] nvarchar(max) NULL,
        [OlusturulmaTarihi] datetime2 NOT NULL,
        [OnaylandiMi] bit NOT NULL,
        CONSTRAINT [PK_Rezervasyons] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118223128_IlkKurulum'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118223128_IlkKurulum', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118233627_YoneticiTablosu'
)
BEGIN
    CREATE TABLE [Yoneticiler] (
        [Id] int NOT NULL IDENTITY,
        [KullaniciAdi] nvarchar(max) NOT NULL,
        [Sifre] nvarchar(max) NOT NULL,
        [Eposta] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Yoneticiler] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118233627_YoneticiTablosu'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118233627_YoneticiTablosu', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118235132_AyarlarTablosu'
)
BEGIN
    CREATE TABLE [Ayarlar] (
        [Id] int NOT NULL IDENTITY,
        [GonderenMail] nvarchar(max) NOT NULL,
        [GonderenSifre] nvarchar(max) NOT NULL,
        [SmtpSunucu] nvarchar(max) NOT NULL,
        [SmtpPort] int NOT NULL,
        CONSTRAINT [PK_Ayarlar] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251118235132_AyarlarTablosu'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251118235132_AyarlarTablosu', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201171601_MenuTablolari'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Yoneticiler]') AND [c].[name] = N'Sifre');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Yoneticiler] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Yoneticiler] ALTER COLUMN [Sifre] nvarchar(250) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201171601_MenuTablolari'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Yoneticiler]') AND [c].[name] = N'KullaniciAdi');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Yoneticiler] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Yoneticiler] ALTER COLUMN [KullaniciAdi] nvarchar(50) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201171601_MenuTablolari'
)
BEGIN
    CREATE TABLE [Kategoriler] (
        [Id] int NOT NULL IDENTITY,
        [Ad] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Kategoriler] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201171601_MenuTablolari'
)
BEGIN
    CREATE TABLE [Urunler] (
        [Id] int NOT NULL IDENTITY,
        [Ad] nvarchar(max) NOT NULL,
        [Aciklama] nvarchar(max) NULL,
        [Fiyat] decimal(18,2) NOT NULL,
        [ResimUrl] nvarchar(max) NULL,
        [KategoriId] int NOT NULL,
        CONSTRAINT [PK_Urunler] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Urunler_Kategoriler_KategoriId] FOREIGN KEY ([KategoriId]) REFERENCES [Kategoriler] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201171601_MenuTablolari'
)
BEGIN
    CREATE INDEX [IX_Urunler_KategoriId] ON [Urunler] ([KategoriId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201171601_MenuTablolari'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251201171601_MenuTablolari', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201183427_UrunSiraNoEkle'
)
BEGIN
    ALTER TABLE [Urunler] ADD [SiraNo] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201183427_UrunSiraNoEkle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251201183427_UrunSiraNoEkle', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201185109_KategoriSiraNoEkle'
)
BEGIN
    ALTER TABLE [Kategoriler] ADD [SiraNo] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201185109_KategoriSiraNoEkle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251201185109_KategoriSiraNoEkle', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201203248_IletisimTablosu'
)
BEGIN
    CREATE TABLE [IletisimMesajlari] (
        [Id] int NOT NULL IDENTITY,
        [AdSoyad] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Telefon] nvarchar(max) NULL,
        [Mesaj] nvarchar(max) NOT NULL,
        [Tarih] datetime2 NOT NULL,
        [OkunduMu] bit NOT NULL,
        CONSTRAINT [PK_IletisimMesajlari] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251201203248_IletisimTablosu'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251201203248_IletisimTablosu', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202105617_YorumTablosu'
)
BEGIN
    DROP TABLE [IletisimMesajlari];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202105617_YorumTablosu'
)
BEGIN
    CREATE TABLE [Yorumlar] (
        [Id] int NOT NULL IDENTITY,
        [AdSoyad] nvarchar(max) NOT NULL,
        [Mesaj] nvarchar(max) NOT NULL,
        [Puan] int NOT NULL,
        [Tarih] datetime2 NOT NULL,
        [OnaylandiMi] bit NOT NULL,
        CONSTRAINT [PK_Yorumlar] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202105617_YorumTablosu'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251202105617_YorumTablosu', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202221908_UrunFiyatBuyukEkle'
)
BEGIN
    ALTER TABLE [Urunler] ADD [FiyatBuyuk] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202221908_UrunFiyatBuyukEkle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251202221908_UrunFiyatBuyukEkle', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202223915_UrunFiyatEtiketleri'
)
BEGIN
    ALTER TABLE [Urunler] ADD [FiyatBuyukTur] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202223915_UrunFiyatEtiketleri'
)
BEGIN
    ALTER TABLE [Urunler] ADD [FiyatTur] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202223915_UrunFiyatEtiketleri'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251202223915_UrunFiyatEtiketleri', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831131425_IletisimMesajlariGeriEkle'
)
BEGIN
    CREATE TABLE [IletisimMesajlari] (
        [Id] int NOT NULL IDENTITY,
        [AdSoyad] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Telefon] nvarchar(max) NULL,
        [Mesaj] nvarchar(max) NOT NULL,
        [Tarih] datetime2 NOT NULL,
        [OkunduMu] bit NOT NULL,
        CONSTRAINT [PK_IletisimMesajlari] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831131425_IletisimMesajlariGeriEkle'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831131425_IletisimMesajlariGeriEkle', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831154440_IndeksVeKategoriSilmeKisitlamasi'
)
BEGIN
    ALTER TABLE [Urunler] DROP CONSTRAINT [FK_Urunler_Kategoriler_KategoriId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831154440_IndeksVeKategoriSilmeKisitlamasi'
)
BEGIN
    CREATE INDEX [IX_Yorumlar_OnaylandiMi] ON [Yorumlar] ([OnaylandiMi]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831154440_IndeksVeKategoriSilmeKisitlamasi'
)
BEGIN
    CREATE INDEX [IX_Urunler_SiraNo] ON [Urunler] ([SiraNo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831154440_IndeksVeKategoriSilmeKisitlamasi'
)
BEGIN
    CREATE INDEX [IX_Rezervasyons_Tarih] ON [Rezervasyons] ([Tarih]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831154440_IndeksVeKategoriSilmeKisitlamasi'
)
BEGIN
    CREATE INDEX [IX_IletisimMesajlari_OkunduMu] ON [IletisimMesajlari] ([OkunduMu]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831154440_IndeksVeKategoriSilmeKisitlamasi'
)
BEGIN
    ALTER TABLE [Urunler] ADD CONSTRAINT [FK_Urunler_Kategoriler_KategoriId] FOREIGN KEY ([KategoriId]) REFERENCES [Kategoriler] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831154440_IndeksVeKategoriSilmeKisitlamasi'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831154440_IndeksVeKategoriSilmeKisitlamasi', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901165912_BultenAbonesiEklendi'
)
BEGIN
    CREATE TABLE [BultenAboneleri] (
        [Id] int NOT NULL IDENTITY,
        [Email] nvarchar(150) NOT NULL,
        [Tarih] datetime2 NOT NULL,
        [AktifMi] bit NOT NULL,
        CONSTRAINT [PK_BultenAboneleri] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901165912_BultenAbonesiEklendi'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901165912_BultenAbonesiEklendi', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902085502_IdentityKimlikDogrulama'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902085502_IdentityKimlikDogrulama', N'8.0.0');
END;
GO

COMMIT;
GO

