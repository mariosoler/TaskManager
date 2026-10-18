-- Migración 011: fotos de las tarjetas.
-- FotosTarjeta.Archivo = nombre generado por la aplicación (<tarjetaId>_<guid>.<ext>) dentro de la carpeta
-- appSettings RutaFotosTarjetas (Web.config). Al borrar una tarjeta, lista o tablero, la aplicación borra los archivos.
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

-- Si ya existe AdjuntosTarjeta (migración 012), las fotos ya son adjuntos: no se vuelve a crear FotosTarjeta.
IF OBJECT_ID('dbo.FotosTarjeta', 'U') IS NULL AND OBJECT_ID('dbo.AdjuntosTarjeta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FotosTarjeta (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        TarjetaId   INT NOT NULL REFERENCES dbo.Tarjetas(Id) ON DELETE CASCADE,
        Archivo     NVARCHAR(100) NOT NULL,
        Tamano      INT NOT NULL,
        SubidaPor   INT NOT NULL REFERENCES dbo.Usuarios(Id),
        FechaSubida DATETIME2 NOT NULL CONSTRAINT DF_FotosTarjeta_FechaSubida DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_FotosTarjeta_Tarjeta ON dbo.FotosTarjeta (TarjetaId);
END
GO
