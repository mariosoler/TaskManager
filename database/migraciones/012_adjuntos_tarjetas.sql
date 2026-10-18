-- Migración 012: las fotos de las tarjetas pasan a ser adjuntos de cualquier tipo.
-- Crea AdjuntosTarjeta (Nombre = nombre original; EsImagen = se muestra en miniatura) y, si existe FotosTarjeta
-- (migración 011), copia sus fotos como adjuntos de imagen (mismo archivo, con nombre "foto-<Id>.<ext>") y la elimina.
-- Los archivos no se mueven: la carpeta sigue siendo la de Web.config (ahora appSettings RutaAdjuntosTarjetas).
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF OBJECT_ID('dbo.AdjuntosTarjeta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdjuntosTarjeta (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        TarjetaId   INT NOT NULL REFERENCES dbo.Tarjetas(Id) ON DELETE CASCADE,
        Archivo     NVARCHAR(100) NOT NULL,
        Nombre      NVARCHAR(255) NOT NULL,
        EsImagen    BIT NOT NULL,
        Tamano      INT NOT NULL,
        SubidoPor   INT NOT NULL REFERENCES dbo.Usuarios(Id),
        FechaSubida DATETIME2 NOT NULL CONSTRAINT DF_AdjuntosTarjeta_FechaSubida DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_AdjuntosTarjeta_Tarjeta ON dbo.AdjuntosTarjeta (TarjetaId);
END
GO

IF OBJECT_ID('dbo.FotosTarjeta', 'U') IS NOT NULL
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRAN;
    -- SQL dinámico: FotosTarjeta puede no existir al compilar el lote (sin valores del usuario).
    EXEC (N'INSERT INTO dbo.AdjuntosTarjeta (TarjetaId, Archivo, Nombre, EsImagen, Tamano, SubidoPor, FechaSubida)
            SELECT f.TarjetaId, f.Archivo,
                   CONCAT(N''foto-'', f.Id, RIGHT(f.Archivo, CHARINDEX(N''.'', REVERSE(f.Archivo)))),
                   1, f.Tamano, f.SubidaPor, f.FechaSubida
            FROM dbo.FotosTarjeta f
            WHERE NOT EXISTS (SELECT 1 FROM dbo.AdjuntosTarjeta a WHERE a.Archivo = f.Archivo);');
    DROP TABLE dbo.FotosTarjeta;
    COMMIT;
END
GO
