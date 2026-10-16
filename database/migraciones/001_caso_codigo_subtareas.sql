-- Migración 001: número de caso, apartado de código (markdown) y subtareas.
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Tarjetas', 'Caso') IS NULL
    ALTER TABLE dbo.Tarjetas ADD Caso NVARCHAR(20) NULL;   -- solo dígitos (conserva ceros a la izquierda)
GO

IF COL_LENGTH('dbo.Tarjetas', 'Codigo') IS NULL
    ALTER TABLE dbo.Tarjetas ADD Codigo NVARCHAR(MAX) NULL; -- texto markdown
GO

IF OBJECT_ID('dbo.CK_Tarjetas_Caso', 'C') IS NULL
    ALTER TABLE dbo.Tarjetas ADD CONSTRAINT CK_Tarjetas_Caso
        CHECK (Caso IS NULL OR (Caso <> N'' AND Caso NOT LIKE N'%[^0-9]%'));
GO

IF OBJECT_ID('dbo.Subtareas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Subtareas (
        Id         INT IDENTITY(1,1) PRIMARY KEY,
        TarjetaId  INT NOT NULL REFERENCES dbo.Tarjetas(Id) ON DELETE CASCADE,
        Texto      NVARCHAR(300) NOT NULL,
        Completada BIT NOT NULL DEFAULT 0,
        Posicion   INT NOT NULL DEFAULT 0
    );
    CREATE INDEX IX_Subtareas_Tarjeta ON dbo.Subtareas(TarjetaId, Posicion);
END
GO
