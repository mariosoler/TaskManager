-- Migración 002: fecha/usuario de última modificación de cada tarjeta e historial de cambios.
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Tarjetas', 'FechaModificacion') IS NULL
    ALTER TABLE dbo.Tarjetas ADD FechaModificacion DATETIME2 NULL;
GO

-- Las tarjetas existentes se consideran modificadas por última vez al crearse.
UPDATE dbo.Tarjetas SET FechaModificacion = FechaCreacion WHERE FechaModificacion IS NULL;
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.Tarjetas') AND name = 'FechaModificacion' AND is_nullable = 1)
    ALTER TABLE dbo.Tarjetas ALTER COLUMN FechaModificacion DATETIME2 NOT NULL;
GO

IF OBJECT_ID('dbo.DF_Tarjetas_FechaModificacion', 'D') IS NULL
    ALTER TABLE dbo.Tarjetas ADD CONSTRAINT DF_Tarjetas_FechaModificacion
        DEFAULT SYSUTCDATETIME() FOR FechaModificacion;
GO

IF COL_LENGTH('dbo.Tarjetas', 'ModificadoPor') IS NULL
    ALTER TABLE dbo.Tarjetas ADD ModificadoPor INT NULL
        CONSTRAINT FK_Tarjetas_ModificadoPor REFERENCES dbo.Usuarios(Id);
GO

IF OBJECT_ID('dbo.HistorialTarjeta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistorialTarjeta (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        TarjetaId     INT NOT NULL REFERENCES dbo.Tarjetas(Id) ON DELETE CASCADE,
        UsuarioId     INT NOT NULL REFERENCES dbo.Usuarios(Id),
        Fecha         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Accion        NVARCHAR(30)  NOT NULL,  -- Creada, Editada, Movida, Subtarea*, Comentario
        Campo         NVARCHAR(30)  NULL,      -- solo en Editada: Titulo, Caso, Descripcion, ...
        ValorAnterior NVARCHAR(500) NULL,
        ValorNuevo    NVARCHAR(500) NULL
    );
    CREATE INDEX IX_HistorialTarjeta_Tarjeta ON dbo.HistorialTarjeta(TarjetaId, Fecha DESC);
END
GO
