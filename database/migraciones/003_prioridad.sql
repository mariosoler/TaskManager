-- Migración 003: prioridad de las tarjetas (1 = Alta, 2 = Media, 3 = Baja; NULL = sin prioridad).
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Tarjetas', 'Prioridad') IS NULL
    ALTER TABLE dbo.Tarjetas ADD Prioridad TINYINT NULL;
GO

IF OBJECT_ID('dbo.CK_Tarjetas_Prioridad', 'C') IS NULL
    ALTER TABLE dbo.Tarjetas ADD CONSTRAINT CK_Tarjetas_Prioridad
        CHECK (Prioridad IS NULL OR Prioridad BETWEEN 1 AND 3);
GO
