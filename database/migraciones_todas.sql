-- =====================================================================================
-- TODAS LAS MIGRACIONES de la base TaskManager, en orden, en un solo script.
-- GENERADO por database\GenerarMigracionesTodas.ps1: no editar a mano (los cambios se perderían);
-- se edita cada migración en database\migraciones y se vuelve a generar.
-- Todas son idempotentes: se puede ejecutar entero sobre una base existente aunque ya tenga
-- algunas aplicadas. Para pasarlas una a una, usar los archivos de database\migraciones.
--
--   sqlcmd -S <servidor> -d TaskManager -E -b -i migraciones_todas.sql
--
-- Incluye:
--   001_caso_codigo_subtareas.sql
--   002_modificacion_historial.sql
--   003_prioridad.sql
--   004_fondo_tableros.sql
--   005_busquedas_guardadas.sql
--   006_etiquetas.sql
--   007_perfil_usuario.sql
--   008_subtareas_enlace.sql
--   009_preferencias_listas.sql
--   010_etiquetas_degradados.sql
--   011_fotos_tarjetas.sql
--   012_adjuntos_tarjetas.sql
-- =====================================================================================
SET NOCOUNT ON;
GO

-- -------------------------------------------------------------------------------------
-- 001_caso_codigo_subtareas.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 001_caso_codigo_subtareas.sql';
GO
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

-- -------------------------------------------------------------------------------------
-- 002_modificacion_historial.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 002_modificacion_historial.sql';
GO
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

-- -------------------------------------------------------------------------------------
-- 003_prioridad.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 003_prioridad.sql';
GO
-- Migración 003: prioridad de las tarjetas (1 = Alta, 2 = Media, 3 = Baja; NULL = sin prioridad).
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Tarjetas', 'Prioridad') IS NULL
    ALTER TABLE dbo.Tarjetas ADD Prioridad TINYINT NULL;
GO

IF OBJECT_ID('dbo.CK_Tarjetas_Prioridad', 'C') IS NULL
    ALTER TABLE dbo.Tarjetas ADD CONSTRAINT CK_Tarjetas_Prioridad
        CHECK (Prioridad IS NULL OR Prioridad BETWEEN 1 AND 3);
GO

-- -------------------------------------------------------------------------------------
-- 004_fondo_tableros.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 004_fondo_tableros.sql';
GO
-- Migración 004: imagen de fondo de cada tablero (nombre del archivo en la carpeta RutaFondosTableros).
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Tableros', 'Fondo') IS NULL
    ALTER TABLE dbo.Tableros ADD Fondo NVARCHAR(100) NULL;
GO

-- -------------------------------------------------------------------------------------
-- 005_busquedas_guardadas.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 005_busquedas_guardadas.sql';
GO
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

-- -------------------------------------------------------------------------------------
-- 006_etiquetas.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 006_etiquetas.sql';
GO
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

-- -------------------------------------------------------------------------------------
-- 007_perfil_usuario.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 007_perfil_usuario.sql';
GO
-- Migración 007: preferencias del perfil de usuario («Mi perfil»).
-- Usuarios.FormatoFecha guarda la clave del formato de fecha elegido (lista blanca en Models/FormatoFecha.vb);
-- NULL = formato predeterminado.
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Usuarios', 'FormatoFecha') IS NULL
    ALTER TABLE dbo.Usuarios ADD FormatoFecha VARCHAR(20) NULL;
GO

-- -------------------------------------------------------------------------------------
-- 008_subtareas_enlace.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 008_subtareas_enlace.sql';
GO
-- Migración 008: subtareas enlazadas a otra tarjeta del mismo tablero.
-- Subtareas.TarjetaEnlazadaId (opcional) = Id de la tarjeta enlazada. Sin FK: una segunda FK a Tarjetas con
-- ON DELETE SET NULL daría varias rutas de cascada; la aplicación comprueba al mostrarla que la tarjeta sigue
-- existiendo en el mismo tablero (si no, muestra Texto, que guarda el título al enlazarla).
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Subtareas', 'TarjetaEnlazadaId') IS NULL
    ALTER TABLE dbo.Subtareas ADD TarjetaEnlazadaId INT NULL;
GO

-- -------------------------------------------------------------------------------------
-- 009_preferencias_listas.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 009_preferencias_listas.sql';
GO
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

-- -------------------------------------------------------------------------------------
-- 010_etiquetas_degradados.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 010_etiquetas_degradados.sql';
GO
-- Migración 010: más colores de etiqueta y degradados.
-- Etiquetas.Color pasa a admitir "grad:<clave>" (degradados de la lista blanca Models/ColorEtiqueta.vb) además de
-- "#rrggbb": se amplía la columna a NVARCHAR(30) y se cambia la restricción CK_Etiquetas_Color.
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Etiquetas', 'Color') < 60   -- en bytes: NVARCHAR(30) = 60
BEGIN
    -- La restricción y el valor por defecto dependen de la columna: se quitan para ampliarla y se vuelven a crear.
    IF OBJECT_ID('dbo.CK_Etiquetas_Color', 'C') IS NOT NULL
        ALTER TABLE dbo.Etiquetas DROP CONSTRAINT CK_Etiquetas_Color;

    DECLARE @Predeterminado SYSNAME = (
        SELECT dc.name FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.Etiquetas') AND c.name = 'Color');
    IF @Predeterminado IS NOT NULL
    BEGIN
        -- El nombre sale del catálogo (no de datos del usuario) y va entre corchetes con QUOTENAME.
        DECLARE @Sql NVARCHAR(400) = N'ALTER TABLE dbo.Etiquetas DROP CONSTRAINT ' + QUOTENAME(@Predeterminado);
        EXEC sp_executesql @Sql;
    END

    ALTER TABLE dbo.Etiquetas ALTER COLUMN Color NVARCHAR(30) NOT NULL;
    ALTER TABLE dbo.Etiquetas ADD CONSTRAINT DF_Etiquetas_Color DEFAULT '#1f6fc5' FOR Color;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = 'CK_Etiquetas_Color' AND definition LIKE '%grad:%')
BEGIN
    IF OBJECT_ID('dbo.CK_Etiquetas_Color', 'C') IS NOT NULL
        ALTER TABLE dbo.Etiquetas DROP CONSTRAINT CK_Etiquetas_Color;
    ALTER TABLE dbo.Etiquetas ADD CONSTRAINT CK_Etiquetas_Color CHECK (
        Color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'
        OR (Color LIKE 'grad:_%' AND Color NOT LIKE 'grad:%[^a-z0-9-]%'));
END
GO

-- -------------------------------------------------------------------------------------
-- 011_fotos_tarjetas.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 011_fotos_tarjetas.sql';
GO
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

-- -------------------------------------------------------------------------------------
-- 012_adjuntos_tarjetas.sql
-- -------------------------------------------------------------------------------------
PRINT N'Migración 012_adjuntos_tarjetas.sql';
GO
-- Migración 012: las fotos de las tarjetas pasan a ser adjuntos de cualquier tipo.
-- Crea AdjuntosTarjeta (Nombre = nombre original; EsImagen = se muestra en miniatura) y, si existe FotosTarjeta
-- (migración 011), copia sus fotos como adjuntos de imagen (mismo archivo, con nombre "foto-<Id>.<ext>") y la elimina.
-- Los archivos no se mueven: la carpeta sigue siendo la de Web.config (ahora appSettings RutaAdjuntosTarjetas).
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF OBJECT_ID('dbo.AdjuntosTarjeta', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdjuntosTarjeta (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        TarjetaId   INT NOT NULL REFERENCES dbo.Tarjetas(Id) ON DELETE CASCADE,
        Archivo     NVARCHAR(100) NOT NULL,
        Nombre      NVARCHAR(255) NOT NULL,
        EsImagen    BIT NOT NULL,
        Tamano      INT NOT NULL,
        SubidoPor   INT NOT NULL REFERENCES dbo.Usuarios(Id),
        FechaSubida DATETIME2 NOT NULL CONSTRAINT DF_AdjuntosTarjeta_FechaSubida DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_AdjuntosTarjeta_Tarjeta ON dbo.AdjuntosTarjeta (TarjetaId);
END
GO

IF OBJECT_ID('dbo.FotosTarjeta', 'U') IS NOT NULL
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRAN;
    -- SQL dinámico: FotosTarjeta puede no existir al compilar el lote (sin valores del usuario).
    EXEC (N'INSERT INTO dbo.AdjuntosTarjeta (TarjetaId, Archivo, Nombre, EsImagen, Tamano, SubidoPor, FechaSubida)
            SELECT f.TarjetaId, f.Archivo,
                   CONCAT(N''foto-'', f.Id, RIGHT(f.Archivo, CHARINDEX(N''.'', REVERSE(f.Archivo)))),
                   1, f.Tamano, f.SubidaPor, f.FechaSubida
            FROM dbo.FotosTarjeta f
            WHERE NOT EXISTS (SELECT 1 FROM dbo.AdjuntosTarjeta a WHERE a.Archivo = f.Archivo);');
    DROP TABLE dbo.FotosTarjeta;
    COMMIT;
END
GO

PRINT N'Migraciones aplicadas.';
GO
