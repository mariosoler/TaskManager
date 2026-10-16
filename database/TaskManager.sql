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
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
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
    FechaCreacion DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
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

CREATE INDEX IX_Listas_Tablero   ON Listas(TableroId, Posicion);
CREATE INDEX IX_Tarjetas_Lista   ON Tarjetas(ListaId, Posicion);
GO
