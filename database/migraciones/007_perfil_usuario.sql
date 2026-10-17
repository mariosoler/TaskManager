-- Migración 007: preferencias del perfil de usuario («Mi perfil»).
-- Usuarios.FormatoFecha guarda la clave del formato de fecha elegido (lista blanca en Models/FormatoFecha.vb);
-- NULL = formato predeterminado.
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Usuarios', 'FormatoFecha') IS NULL
    ALTER TABLE dbo.Usuarios ADD FormatoFecha VARCHAR(20) NULL;
GO
