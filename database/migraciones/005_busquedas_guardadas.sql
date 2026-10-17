-- Migración 005: búsquedas guardadas del buscador (propias y compartidas con los compañeros).
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF OBJECT_ID('dbo.BusquedasGuardadas', 'U') IS NULL
    CREATE TABLE dbo.BusquedasGuardadas (
        Id            INT IDENTITY(1,1) PRIMARY KEY,
        UsuarioId     INT NOT NULL REFERENCES dbo.Usuarios(Id) ON DELETE CASCADE,
        Nombre        NVARCHAR(100) NOT NULL,
        Criterios     NVARCHAR(4000) NOT NULL,
        Compartida    BIT NOT NULL DEFAULT 0,
        FechaCreacion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_BusquedasGuardadas_Nombre UNIQUE (UsuarioId, Nombre)
    );
GO
