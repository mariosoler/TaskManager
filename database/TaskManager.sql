-- Esquema de la base de datos TaskManager (SQL Server).
-- Ejecutar sobre la base de datos TaskManager ya creada.

CREATE TABLE Usuarios (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    Nombre        NVARCHAR(100)  NOT NULL,
    Email         NVARCHAR(200)  NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(500)  NOT NULL,
    FechaCreacion DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    FormatoFecha  VARCHAR(20)    NULL  -- clave de Models/FormatoFecha (lista blanca); NULL = predeterminado
);

CREATE TABLE Tableros (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    Nombre        NVARCHAR(150) NOT NULL,
    Descripcion   NVARCHAR(500) NULL,
    PropietarioId INT NOT NULL REFERENCES Usuarios(Id),
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Fondo         NVARCHAR(100) NULL -- archivo subido (carpeta RutaFondosTableros) o "predef:<clave>" (FondosTablero.Predefinidos)
);

CREATE TABLE MiembrosTablero (
    TableroId INT NOT NULL REFERENCES Tableros(Id) ON DELETE CASCADE,
    UsuarioId INT NOT NULL REFERENCES Usuarios(Id),
    Rol       NVARCHAR(20) NOT NULL DEFAULT 'Miembro', -- Admin / Miembro
    PRIMARY KEY (TableroId, UsuarioId)
);

CREATE TABLE Listas (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    TableroId INT NOT NULL REFERENCES Tableros(Id) ON DELETE CASCADE,
    Titulo    NVARCHAR(150) NOT NULL,
    Posicion  INT NOT NULL DEFAULT 0
);

CREATE TABLE Tarjetas (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    ListaId       INT NOT NULL REFERENCES Listas(Id) ON DELETE CASCADE,
    Titulo        NVARCHAR(200) NOT NULL,
    Descripcion   NVARCHAR(MAX) NULL,
    Posicion      INT NOT NULL DEFAULT 0,
    FechaLimite   DATETIME2 NULL,
    AsignadoA     INT NULL REFERENCES Usuarios(Id),
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Caso          NVARCHAR(20) NULL,      -- número de caso, solo dígitos
    Codigo        NVARCHAR(MAX) NULL,     -- apartado de código (markdown)
    FechaModificacion DATETIME2 NOT NULL CONSTRAINT DF_Tarjetas_FechaModificacion DEFAULT SYSUTCDATETIME(),
    ModificadoPor INT NULL CONSTRAINT FK_Tarjetas_ModificadoPor REFERENCES Usuarios(Id),
    Prioridad     TINYINT NULL,           -- 1 = Alta, 2 = Media, 3 = Baja
    FechaEnLista  DATETIME2 NOT NULL CONSTRAINT DF_Tarjetas_FechaEnLista DEFAULT SYSUTCDATETIME(), -- entrada en su lista actual
    CONSTRAINT CK_Tarjetas_Caso CHECK (Caso IS NULL OR (Caso <> N'' AND Caso NOT LIKE N'%[^0-9]%')),
    CONSTRAINT CK_Tarjetas_Prioridad CHECK (Prioridad IS NULL OR Prioridad BETWEEN 1 AND 3)
);

-- Una fila por cambio en una tarjeta (quién, cuándo, qué).
CREATE TABLE HistorialTarjeta (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    TarjetaId     INT NOT NULL REFERENCES Tarjetas(Id) ON DELETE CASCADE,
    UsuarioId     INT NOT NULL REFERENCES Usuarios(Id),
    Fecha         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Accion        NVARCHAR(30)  NOT NULL,  -- Creada, Editada, Movida, Subtarea*, Comentario
    Campo         NVARCHAR(30)  NULL,      -- solo en Editada: Titulo, Caso, Descripcion, ...
    ValorAnterior NVARCHAR(500) NULL,
    ValorNuevo    NVARCHAR(500) NULL
);

-- Archivos adjuntos de las tarjetas (de cualquier tipo). Archivo = nombre generado por la aplicación
-- (<tarjetaId>_<guid>.<ext>; .bin si no es una imagen) dentro de la carpeta appSettings RutaAdjuntosTarjetas;
-- Nombre = nombre original, solo para mostrarlo y como nombre de descarga. EsImagen = JPG/PNG/GIF/WebP comprobada
-- por su contenido (se muestra en miniatura). Al borrar una tarjeta, lista o tablero, la aplicación borra los archivos.
CREATE TABLE AdjuntosTarjeta (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    TarjetaId   INT NOT NULL REFERENCES Tarjetas(Id) ON DELETE CASCADE,
    Archivo     NVARCHAR(100) NOT NULL,
    Nombre      NVARCHAR(255) NOT NULL,
    EsImagen    BIT NOT NULL,
    Tamano      INT NOT NULL,                       -- bytes
    SubidoPor   INT NOT NULL REFERENCES Usuarios(Id),
    FechaSubida DATETIME2 NOT NULL CONSTRAINT DF_AdjuntosTarjeta_FechaSubida DEFAULT SYSUTCDATETIME()
);
CREATE INDEX IX_AdjuntosTarjeta_Tarjeta ON AdjuntosTarjeta (TarjetaId);

-- Preferencias de vista de cada usuario en cada lista (solo para él): ocultarla y ordenar sus tarjetas.
-- Orden = clave de Models/OrdenLista (lista blanca); NULL = orden manual. Sin fila = visible y en orden manual.
-- Sin cascada desde Usuarios (Listas ya llega en cascada desde Usuarios a través de Tableros).
CREATE TABLE PreferenciasLista (
    UsuarioId INT NOT NULL REFERENCES Usuarios(Id),
    ListaId   INT NOT NULL REFERENCES Listas(Id) ON DELETE CASCADE,
    Oculta    BIT NOT NULL CONSTRAINT DF_PreferenciasLista_Oculta DEFAULT 0,
    Orden     VARCHAR(20) NULL,
    CONSTRAINT PK_PreferenciasLista PRIMARY KEY (UsuarioId, ListaId)
);

CREATE TABLE Subtareas (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    TarjetaId  INT NOT NULL REFERENCES Tarjetas(Id) ON DELETE CASCADE,
    Texto      NVARCHAR(300) NOT NULL,
    Completada BIT NOT NULL DEFAULT 0,
    Posicion   INT NOT NULL DEFAULT 0,
    -- Tarjeta del mismo tablero a la que enlaza la subtarea (opcional). Sin FK: una segunda FK a Tarjetas con
    -- ON DELETE SET NULL daría varias rutas de cascada (SQL Server no lo permite); al mostrarla se comprueba
    -- que la tarjeta sigue existiendo en el tablero y, si no, se muestra Texto (título al enlazarla).
    TarjetaEnlazadaId INT NULL
);

-- Etiquetas (#tags) de un tablero. Nombre sin "#", único por tablero (sin distinguir mayúsculas).
-- Color de la lista blanca Models/ColorEtiqueta (sólido o degradado).
CREATE TABLE Etiquetas (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    TableroId INT NOT NULL REFERENCES Tableros(Id) ON DELETE CASCADE,
    Nombre    NVARCHAR(50) NOT NULL,
    -- "#rrggbb" o "grad:<clave>" de un degradado (lista blanca en Models/ColorEtiqueta.vb).
    Color     NVARCHAR(30) NOT NULL CONSTRAINT DF_Etiquetas_Color DEFAULT '#1f6fc5',
    CONSTRAINT CK_Etiquetas_Color CHECK (
        Color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'
        OR (Color LIKE 'grad:_%' AND Color NOT LIKE 'grad:%[^a-z0-9-]%'))
);
CREATE UNIQUE INDEX UX_Etiquetas_Tablero_Nombre ON Etiquetas (TableroId, Nombre);

CREATE TABLE TarjetaEtiquetas (
    TarjetaId  INT NOT NULL REFERENCES Tarjetas(Id) ON DELETE CASCADE,
    EtiquetaId INT NOT NULL REFERENCES Etiquetas(Id),
    PRIMARY KEY (TarjetaId, EtiquetaId)
);

CREATE TABLE Comentarios (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    TarjetaId INT NOT NULL REFERENCES Tarjetas(Id) ON DELETE CASCADE,
    UsuarioId INT NOT NULL REFERENCES Usuarios(Id),
    Texto     NVARCHAR(MAX) NOT NULL,
    Fecha     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Búsquedas guardadas del buscador. Criterios = filtro serializado (FiltroBusqueda.ACriterios).
-- Compartida = 1: la ven también los compañeros del autor (usuarios con algún tablero en común).
CREATE TABLE BusquedasGuardadas (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId     INT NOT NULL REFERENCES Usuarios(Id) ON DELETE CASCADE,
    Nombre        NVARCHAR(100) NOT NULL,
    Criterios     NVARCHAR(4000) NOT NULL,
    Compartida    BIT NOT NULL DEFAULT 0,
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_BusquedasGuardadas_Nombre UNIQUE (UsuarioId, Nombre)
);

CREATE INDEX IX_Listas_Tablero  ON Listas(TableroId, Posicion);
CREATE INDEX IX_Tarjetas_Lista   ON Tarjetas(ListaId, Posicion);
CREATE INDEX IX_Subtareas_Tarjeta ON Subtareas(TarjetaId, Posicion);
CREATE INDEX IX_HistorialTarjeta_Tarjeta ON HistorialTarjeta(TarjetaId, Fecha DESC);
CREATE INDEX IX_TarjetaEtiquetas_Etiqueta ON TarjetaEtiquetas(EtiquetaId);
GO
