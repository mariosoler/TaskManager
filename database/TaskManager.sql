-- Esquema de la base de datos TaskManager (SQL Server).
-- Ejecutar sobre la base de datos TaskManager ya creada.

CREATE TABLE Usuarios (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    Nombre        NVARCHAR(100)  NOT NULL,
    Email         NVARCHAR(200)  NOT NULL UNIQUE,
    PasswordHash  NVARCHAR(500)  NOT NULL,
    FechaCreacion DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME()
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

CREATE TABLE Subtareas (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    TarjetaId  INT NOT NULL REFERENCES Tarjetas(Id) ON DELETE CASCADE,
    Texto      NVARCHAR(300) NOT NULL,
    Completada BIT NOT NULL DEFAULT 0,
    Posicion   INT NOT NULL DEFAULT 0
);

CREATE TABLE Etiquetas (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    TableroId INT NOT NULL REFERENCES Tableros(Id) ON DELETE CASCADE,
    Nombre    NVARCHAR(50) NOT NULL,
    Color     NVARCHAR(7)  NOT NULL DEFAULT '#3b82f6'
);

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
GO
