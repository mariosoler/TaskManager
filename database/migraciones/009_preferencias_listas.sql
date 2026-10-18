-- Migración 009: ocultar listas y ordenar sus tarjetas (preferencias de cada usuario) e ID/fecha de entrada.
-- Tarjetas.FechaEnLista = cuándo entró la tarjeta en su lista actual (al crearla o al moverla a otra lista).
--   Al añadirla se rellena con el último movimiento del historial o, si no hay, con la fecha de creación.
-- PreferenciasLista = qué listas oculta cada usuario y cómo ordena sus tarjetas (Orden: clave de Models/OrdenLista).
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Tarjetas', 'FechaEnLista') IS NULL
BEGIN
    ALTER TABLE dbo.Tarjetas ADD FechaEnLista DATETIME2 NOT NULL
        CONSTRAINT DF_Tarjetas_FechaEnLista DEFAULT SYSUTCDATETIME();
    -- La columna aún no existe al compilar el lote: el relleno va como SQL dinámico (sin valores del usuario).
    EXEC (N'UPDATE c SET FechaEnLista = COALESCE(
                 (SELECT MAX(h.Fecha) FROM dbo.HistorialTarjeta h WHERE h.TarjetaId = c.Id AND h.Accion = N''Movida''),
                 c.FechaCreacion)
             FROM dbo.Tarjetas c;');
END
GO

IF OBJECT_ID('dbo.PreferenciasLista', 'U') IS NULL
    CREATE TABLE dbo.PreferenciasLista (
        UsuarioId INT NOT NULL REFERENCES dbo.Usuarios(Id),
        ListaId   INT NOT NULL REFERENCES dbo.Listas(Id) ON DELETE CASCADE,
        Oculta    BIT NOT NULL CONSTRAINT DF_PreferenciasLista_Oculta DEFAULT 0,
        Orden     VARCHAR(20) NULL,
        CONSTRAINT PK_PreferenciasLista PRIMARY KEY (UsuarioId, ListaId)
    );
GO
