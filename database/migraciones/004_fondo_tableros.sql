-- Migración 004: imagen de fondo de cada tablero (nombre del archivo en la carpeta RutaFondosTableros).
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Tableros', 'Fondo') IS NULL
    ALTER TABLE dbo.Tableros ADD Fondo NVARCHAR(100) NULL;
GO
