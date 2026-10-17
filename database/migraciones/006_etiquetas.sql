-- Migración 006: etiquetas (#tags) de las tarjetas. Las tablas Etiquetas y TarjetaEtiquetas ya existían;
-- se añade nombre único por tablero (sin distinguir mayúsculas con la intercalación de la base),
-- validación del color y un índice para buscar tarjetas por etiqueta.
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Etiquetas_Tablero_Nombre' AND object_id = OBJECT_ID('dbo.Etiquetas'))
    CREATE UNIQUE INDEX UX_Etiquetas_Tablero_Nombre ON dbo.Etiquetas (TableroId, Nombre);
GO

IF OBJECT_ID('dbo.CK_Etiquetas_Color', 'C') IS NULL
    ALTER TABLE dbo.Etiquetas ADD CONSTRAINT CK_Etiquetas_Color
        CHECK (Color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TarjetaEtiquetas_Etiqueta' AND object_id = OBJECT_ID('dbo.TarjetaEtiquetas'))
    CREATE INDEX IX_TarjetaEtiquetas_Etiqueta ON dbo.TarjetaEtiquetas (EtiquetaId);
GO
