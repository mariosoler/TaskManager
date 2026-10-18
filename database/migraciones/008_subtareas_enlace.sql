-- Migración 008: subtareas enlazadas a otra tarjeta del mismo tablero.
-- Subtareas.TarjetaEnlazadaId (opcional) = Id de la tarjeta enlazada. Sin FK: una segunda FK a Tarjetas con
-- ON DELETE SET NULL daría varias rutas de cascada; la aplicación comprueba al mostrarla que la tarjeta sigue
-- existiendo en el mismo tablero (si no, muestra Texto, que guarda el título al enlazarla).
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Subtareas', 'TarjetaEnlazadaId') IS NULL
    ALTER TABLE dbo.Subtareas ADD TarjetaEnlazadaId INT NULL;
GO
