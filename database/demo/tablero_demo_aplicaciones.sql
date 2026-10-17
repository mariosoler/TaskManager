-- Tablero de demostración de las etiquetas (#tags) con las aplicaciones PLANIFICADOR_CABINAS,
-- GESTOR_CABINAS e IT_CASES. Solo datos de ejemplo: no forma parte del esquema.
--
-- Uso (el email es el de una cuenta ya registrada, que será la propietaria del tablero):
--   sqlcmd -S <servidor> -d TaskManager -E -i database\demo\tablero_demo_aplicaciones.sql -v EmailPropietario="usuario@ejemplo.com"
--
-- Si ese usuario ya tiene un tablero con el mismo nombre, no hace nada (bórralo antes para regenerarlo).
-- Requiere SQL Server 2022 o posterior (STRING_SPLIT con ordinal).

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Email NVARCHAR(200) = N'$(EmailPropietario)';
DECLARE @NombreTablero NVARCHAR(150) = N'Demo · Aplicaciones';
DECLARE @UsuarioId INT = (SELECT Id FROM Usuarios WHERE Email = @Email);

IF @UsuarioId IS NULL
BEGIN
    RAISERROR(N'No existe ningún usuario con ese email.', 16, 1);
    RETURN;
END
IF EXISTS (SELECT 1 FROM Tableros WHERE PropietarioId = @UsuarioId AND Nombre = @NombreTablero)
BEGIN
    PRINT N'El tablero de demostración ya existe para ese usuario; no se ha cambiado nada.';
    RETURN;
END

BEGIN TRAN;

DECLARE @TableroId INT;
INSERT INTO Tableros (Nombre, Descripcion, PropietarioId, Fondo)
VALUES (@NombreTablero, N'Tareas de ejemplo de PLANIFICADOR_CABINAS, GESTOR_CABINAS e IT_CASES, organizadas con #etiquetas.',
        @UsuarioId, N'predef:evergreen');
SET @TableroId = SCOPE_IDENTITY();
INSERT INTO MiembrosTablero (TableroId, UsuarioId, Rol) VALUES (@TableroId, @UsuarioId, N'Admin');

INSERT INTO Listas (TableroId, Titulo, Posicion)
VALUES (@TableroId, N'Por hacer', 0), (@TableroId, N'En curso', 1), (@TableroId, N'En revisión', 2), (@TableroId, N'Hecho', 3);

-- Etiquetas con los colores de Etiqueta.Paleta en orden (las aplicaciones, las primeras).
INSERT INTO Etiquetas (TableroId, Nombre, Color)
VALUES (@TableroId, N'PLANIFICADOR_CABINAS', '#1f6fc5'),
       (@TableroId, N'GESTOR_CABINAS',       '#e8710a'),
       (@TableroId, N'IT_CASES',             '#1f845a'),
       (@TableroId, N'bug',                  '#7c4dcc'),
       (@TableroId, N'mejora',               '#c2185b'),
       (@TableroId, N'urgente',              '#f2b705');

-- Tarjetas. Dias = fecha límite relativa a hoy (NULL = sin fecha); Etiquetas separadas por espacios;
-- Subtareas separadas por "|" (con "+" delante = completada).
DECLARE @Datos TABLE (
    Orden INT IDENTITY(1,1), Lista NVARCHAR(150), Titulo NVARCHAR(200), Descripcion NVARCHAR(MAX),
    Prioridad TINYINT NULL, Dias INT NULL, Caso NVARCHAR(20) NULL, Asignada BIT, Codigo NVARCHAR(MAX) NULL,
    Etiquetas NVARCHAR(400), Subtareas NVARCHAR(MAX) NULL);

INSERT INTO @Datos (Lista, Titulo, Descripcion, Prioridad, Dias, Caso, Asignada, Codigo, Etiquetas, Subtareas) VALUES
(N'Por hacer', N'Vista semanal en el calendario de cabinas',
 N'Los supervisores quieren ver la ocupación de todas las cabinas de la semana en una sola pantalla.',
 2, 10, NULL, 0, NULL, N'PLANIFICADOR_CABINAS mejora', N'Maqueta de la vista semanal|Consulta de reservas por semana|Prueba con dos supervisores'),
(N'Por hacer', N'Error al exportar el inventario de cabinas a Excel',
 N'Con más de 1.000 cabinas la exportación se corta y el archivo sale vacío.',
 1, 2, N'104512', 1, NULL, N'GESTOR_CABINAS bug urgente', NULL),
(N'Por hacer', N'Revisar los permisos de los técnicos',
 N'Los técnicos de segundo nivel no pueden reasignar casos de otros grupos.',
 3, 14, N'104530', 0, NULL, N'IT_CASES', NULL),
(N'Por hacer', N'Avisar por correo de los cambios de turno de cabina',
 N'Cuando el planificador cambia un turno, el gestor de cabinas debe avisar al responsable.',
 NULL, NULL, NULL, 0, NULL, N'PLANIFICADOR_CABINAS GESTOR_CABINAS mejora', NULL),
(N'En curso', N'Acelerar la carga del planificador con más de 500 reservas',
 N'La pantalla principal tarda más de 8 segundos en abrir en las semanas de más actividad.',
 1, 1, NULL, 1, NULL, N'PLANIFICADOR_CABINAS urgente', N'+Medir los tiempos actuales|Paginar la consulta de reservas|Guardar en caché la lista de cabinas'),
(N'En curso', N'Alta de las cabinas nuevas de la planta 2',
 N'Dar de alta las 12 cabinas nuevas con su equipamiento y responsable.',
 2, 5, NULL, 1, NULL, N'GESTOR_CABINAS', N'+Recoger los datos de las cabinas|Darlas de alta|Validar con mantenimiento'),
(N'En curso', N'El formulario de alta no guarda los adjuntos',
 N'Al crear un caso con adjuntos de más de 2 MB, el caso se guarda sin ellos.',
 1, -1, N'104498', 1, NULL, N'IT_CASES bug', NULL),
(N'En revisión', N'Filtro por estado en el listado de casos',
 N'Permitir filtrar por Abierto / En curso / Pendiente del usuario / Cerrado.',
 3, 3, NULL, 0, NULL, N'IT_CASES mejora', NULL),
(N'En revisión', N'Reservas solapadas al cambiar de turno',
 N'Dos reservas de la misma cabina pueden solaparse si el turno termina a medianoche.',
 2, 4, N'104477', 1,
 N'Consulta para detectar solapes:' + CHAR(10) + CHAR(10) + N'```sql' + CHAR(10) +
 N'SELECT a.Id, b.Id' + CHAR(10) + N'FROM Reservas a' + CHAR(10) +
 N'JOIN Reservas b ON b.CabinaId = a.CabinaId AND b.Id > a.Id' + CHAR(10) +
 N' AND b.Inicio < a.Fin AND a.Inicio < b.Fin;' + CHAR(10) + N'```',
 N'PLANIFICADOR_CABINAS bug', NULL),
(N'Hecho', N'Migrar el gestor de cabinas a la nueva base de datos',
 N'Migración completada y validada con los datos de producción.',
 2, NULL, NULL, 1, NULL, N'GESTOR_CABINAS', N'+Copia de seguridad|+Migración de datos|+Validación'),
(N'Hecho', N'Respuesta automática al cerrar un caso',
 N'Plantilla de correo que se envía al usuario cuando se cierra su caso.',
 NULL, NULL, NULL, 0, NULL, N'IT_CASES', NULL),
(N'Hecho', N'Colores por tipo de cabina en el planificador',
 N'Cada tipo de cabina se muestra con su color en el calendario.',
 3, NULL, NULL, 0, NULL, N'PLANIFICADOR_CABINAS mejora', NULL);

DECLARE @Orden INT = 1, @Total INT = (SELECT COUNT(*) FROM @Datos);
DECLARE @TarjetaId INT, @ListaId INT, @NombreLista NVARCHAR(150), @Ahora DATETIME2 = SYSUTCDATETIME();

WHILE @Orden <= @Total
BEGIN
    SELECT @ListaId = l.Id, @NombreLista = l.Titulo
    FROM @Datos d JOIN Listas l ON l.TableroId = @TableroId AND l.Titulo = d.Lista
    WHERE d.Orden = @Orden;

    INSERT INTO Tarjetas (ListaId, Titulo, Descripcion, Posicion, FechaLimite, AsignadoA, Caso, Codigo, Prioridad,
                          FechaCreacion, FechaModificacion, ModificadoPor)
    SELECT @ListaId, d.Titulo, d.Descripcion,
           (SELECT COUNT(*) FROM Tarjetas WHERE ListaId = @ListaId),
           CASE WHEN d.Dias IS NULL THEN NULL ELSE CAST(DATEADD(DAY, d.Dias, CAST(GETDATE() AS DATE)) AS DATETIME2) END,
           CASE WHEN d.Asignada = 1 THEN @UsuarioId END, d.Caso, d.Codigo, d.Prioridad,
           @Ahora, @Ahora, @UsuarioId
    FROM @Datos d WHERE d.Orden = @Orden;
    SET @TarjetaId = SCOPE_IDENTITY();

    INSERT INTO HistorialTarjeta (TarjetaId, UsuarioId, Fecha, Accion, ValorNuevo)
    VALUES (@TarjetaId, @UsuarioId, @Ahora, N'Creada', @NombreLista);

    INSERT INTO TarjetaEtiquetas (TarjetaId, EtiquetaId)
    SELECT DISTINCT @TarjetaId, e.Id
    FROM @Datos d CROSS APPLY STRING_SPLIT(d.Etiquetas, N' ') s
    JOIN Etiquetas e ON e.TableroId = @TableroId AND e.Nombre = s.value
    WHERE d.Orden = @Orden AND s.value <> N'';

    INSERT INTO Subtareas (TarjetaId, Texto, Completada, Posicion)
    SELECT @TarjetaId,
           CASE WHEN LEFT(s.value, 1) = N'+' THEN SUBSTRING(s.value, 2, 300) ELSE s.value END,
           CASE WHEN LEFT(s.value, 1) = N'+' THEN 1 ELSE 0 END,
           s.ordinal - 1
    FROM @Datos d CROSS APPLY STRING_SPLIT(d.Subtareas, N'|', 1) s
    WHERE d.Orden = @Orden AND d.Subtareas IS NOT NULL;

    SET @Orden += 1;
END

COMMIT;
PRINT N'Tablero de demostración creado (Id ' + CAST(@TableroId AS NVARCHAR(20)) + N').';
