# CLAUDE.md

Guía para Claude Code al trabajar en este repositorio.

## Proyecto

**TaskManager**: aplicación web tipo Trello (tableros, listas y tarjetas) construida con:

- **ASP.NET Web Forms** en **VB.NET**, .NET Framework **4.8.1** (`TaskManager.vbproj`, solución `TaskManager.slnx`).
- **SQL Server** como base de datos, accedida con ADO.NET (`System.Data.SqlClient`).
- Paquetes NuGet en `packages.config` (carpeta `packages/`).

## Estructura

- `Site.Master` — página maestra (barra superior, cerrar sesión).
- Páginas (`*.aspx` + `.aspx.vb` + `.aspx.designer.vb`; las clases se llaman `XxxPagina`):
  - `Login.aspx`, `Registro.aspx` — acceso (únicas páginas públicas).
  - `Tableros.aspx` — "Mis tableros": listar, crear y eliminar.
  - `Tablero.aspx?id=N` — vista Kanban: listas, tarjetas, comentarios y miembros. Un único modal (`pnlDetalle`) sirve para crear tarjetas ("+ Añadir una tarjeta", estado en `ListaNuevaId`) y para editarlas (`TarjetaAbiertaId`).
  - `Default.aspx` — redirige a `Tableros.aspx`.
- `Datos/` — acceso a datos ADO.NET (`Db`, `UsuariosDatos`, `TablerosDatos`, `ListasDatos`, `TarjetasDatos`, `SubtareasDatos`, `ComentariosDatos`, `HistorialDatos`). Las páginas no contienen SQL.
- `Models/` — clases de modelo que reflejan las tablas (Usuario, Tablero, Lista, Tarjeta, Subtarea, Comentario, Miembro, HistorialTarjeta).
- `Seguridad/` — `PasswordHasher` (PBKDF2), `Autenticacion` (cookie de Forms Auth), `PaginaBase` (base de todas las páginas).
- `Utilidades/` — helpers de presentación (`Formato`).
- `Content/site.css` — estilos.
- `Scripts/tablero.js` — arrastrar/soltar de tarjetas, abrir detalle, Esc.
- `Scripts/markdown.js` — editor markdown propio sin dependencias (barra, Escribir/Vista previa). El renderizador escapa todo el HTML antes de aplicar marcas y solo permite enlaces `http(s)`/`mailto`; mantener esa garantía si se amplía.
- CSS/JS se enlazan con `Formato.UrlConVersion("~/...")` (añade `?v=<fecha del archivo>`) para que el navegador no use versiones en caché tras un cambio.
- **Codificación**: `.aspx`/`.Master` en UTF-8 **con BOM** (y `fileEncoding="utf-8"` en Web.config); sin ello ASP.NET los lee como Windows-1252 y los acentos salen mal.
- **No usar `App_Code/`**: en un proyecto *Web Application* el código ahí se compila dos veces (en la DLL y en tiempo de ejecución).
- Al añadir archivos, registrarlos en `TaskManager.vbproj` (`Compile` / `Content`) y mantener los `.designer.vb` al día con los controles con `ID`.
- `App_Data/` — datos locales; los `.mdf`/`.ldf` están ignorados por git.
- `database/` — script(s) SQL de la base de datos **TaskManager** (creación de esquema y datos iniciales). Cualquier cambio de esquema debe reflejarse aquí.
- `Web.config` — configuración versionada (sin secretos).

## Base de datos

- Nombre: **TaskManager** (SQL Server).
- Script del esquema: `database/TaskManager.sql`. Para montar el entorno local, crear la base TaskManager y ejecutarlo sobre ella (SSMS o `sqlcmd -S <servidor> -d TaskManager -E -i database\TaskManager.sql`).
- Al modificar tablas, columnas o procedimientos, en el mismo cambio:
  1. Actualizar `database/TaskManager.sql` (esquema completo para instalaciones nuevas).
  2. Añadir una migración **idempotente** en `database/migraciones/NNN_descripcion.sql` (`IF COL_LENGTH(...) IS NULL`, `IF OBJECT_ID(...) IS NULL`) para bases ya existentes.
- Migraciones aplicadas: `001_caso_codigo_subtareas.sql`, `002_modificacion_historial.sql`.

### Esquema (nombres en español)

| Tabla | Descripción | Relaciones |
|---|---|---|
| `Usuarios` | Cuentas (`Email` único, `PasswordHash`) | — |
| `Tableros` | Tableros | `PropietarioId` → Usuarios |
| `MiembrosTablero` | Miembros de un tablero; `Rol` = `Admin` / `Miembro` | PK (TableroId, UsuarioId); cascada desde Tableros |
| `Listas` | Columnas de un tablero, ordenadas por `Posicion` | → Tableros (cascada) |
| `Tarjetas` | Tarjetas de una lista, ordenadas por `Posicion`; `FechaLimite`, `AsignadoA` opcionales; `Caso` (NVARCHAR(20), solo dígitos, `CK_Tarjetas_Caso`); `Codigo` (markdown, NVARCHAR(MAX)); `FechaModificacion` + `ModificadoPor` (última modificación) | → Listas (cascada), `AsignadoA` → Usuarios |
| `Subtareas` | Checklist de una tarjeta (`Texto`, `Completada`), ordenada por `Posicion` | → Tarjetas (cascada) |
| `HistorialTarjeta` | Un cambio por fila: `Accion`, `Campo`, `ValorAnterior`, `ValorNuevo`, quién y cuándo | → Tarjetas (cascada), → Usuarios |
| `Etiquetas` | Etiquetas por tablero; `Color` hex `#rrggbb` | → Tableros (cascada) |
| `TarjetaEtiquetas` | N:M tarjetas–etiquetas | → Tarjetas (cascada), → Etiquetas (sin cascada) |
| `Comentarios` | Comentarios en tarjetas | → Tarjetas (cascada), → Usuarios |

Notas:
- Las fechas se guardan en **UTC** (`SYSUTCDATETIME()`); convertir a hora local solo al mostrar.
- `Usuarios.PasswordHash` guarda solo el hash (p. ej. PBKDF2 con `Rfc2898DeriveBytes` y sal); nunca contraseñas en claro.
- Borrar una etiqueta asignada a tarjetas falla por la FK sin cascada: eliminar antes sus filas en `TarjetaEtiquetas`.
- Al mover tarjetas/listas, actualizar `Posicion` de los elementos afectados dentro de una transacción.
- Comprobar en cada operación que el usuario es propietario o miembro del tablero.
- `Caso` es texto para conservar ceros a la izquierda; validar con `[0-9]` (no `\d`, que en .NET acepta dígitos de otros alfabetos y la restricción CHECK los rechaza).
- `Codigo` no se recorta al guardar (la sangría importa). En el tablero solo se carga `TieneCodigo`; el texto completo, al abrir la tarjeta.
- **Historial**: toda operación que modifique una tarjeta debe llamar a `HistorialDatos.Registrar` dentro de su misma transacción; eso inserta las filas y actualiza `FechaModificacion`/`ModificadoPor`. Solo se registran cambios reales (`TarjetasDatos.Actualizar` compara campo a campo; guardar sin cambios no deja rastro). Reordenar dentro de la misma lista no se registra. Descripción y código se normalizan a `
` antes de guardar para no detectar cambios falsos por `
`. Las acciones/campos están en `Models/HistorialTarjeta.vb` y el texto se genera en `TableroPagina.DescribirCambio` (que codifica los valores).
- Subtareas: al editar una tarjeta se guardan al momento; al crearla se acumulan en ViewState (`SubtareasPendientes`) y se insertan con la tarjeta en una transacción.

## Reglas obligatorias

### 1. Consultas siempre parametrizadas

Nunca concatenar ni interpolar valores en SQL. Usar siempre `SqlParameter`:

```vb
Using cn As New SqlConnection(ConfigurationManager.ConnectionStrings("TaskManager").ConnectionString)
    Using cmd As New SqlCommand("SELECT Id, Title FROM Cards WHERE ListId = @ListId", cn)
        cmd.Parameters.Add("@ListId", SqlDbType.Int).Value = listId
        cn.Open()
        Using rd = cmd.ExecuteReader()
            ' ...
        End Using
    End Using
End Using
```

- Preferir `Parameters.Add(nombre, SqlDbType)` con tipo explícito frente a `AddWithValue`.
- Esto aplica también a `ORDER BY`, nombres de columna, etc.: si deben ser dinámicos, validarlos contra una lista blanca, nunca pasarlos tal cual desde la entrada del usuario.
- Envolver conexiones, comandos y readers en `Using`.

### 2. Sin secretos en el repositorio

- No escribir contraseñas, cadenas de conexión reales, claves de API ni certificados en ningún archivo versionado (código, `Web.config`, `Web.*.config`, scripts SQL, README, commits).
- Los scripts de `database/` no deben crear logins/usuarios con contraseñas reales; usar marcadores (p. ej. `<PASSWORD>`) si es imprescindible.
- Antes de hacer commit, revisar el diff en busca de credenciales.

### 3. Cadena de conexión en archivo local ignorado

La cadena de conexión se guarda en **`connectionStrings.config`** en la raíz del proyecto, archivo ya excluido en `.gitignore` (junto con `Web.Local.config`, `secrets.json`, `.env`, etc.).

`Web.config` solo referencia el archivo externo:

```xml
<connectionStrings configSource="connectionStrings.config" />
```

Contenido de `connectionStrings.config` (local, **no versionar**):

```xml
<?xml version="1.0" encoding="utf-8"?>
<connectionStrings>
  <add name="TaskManager"
       connectionString="Server=.;Database=TaskManager;Integrated Security=True;TrustServerCertificate=True"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

- Preferir autenticación integrada de Windows en local.
- Si se necesita una plantilla para otros desarrolladores, versionar `connectionStrings.example.config` con valores ficticios, nunca el archivo real.
- Leer la cadena siempre mediante `ConfigurationManager.ConnectionStrings("TaskManager")`; no codificarla en el código.
- No eliminar entradas de seguridad del `.gitignore`.

### 4. Autenticación y autorización

- Forms Authentication (`Web.config`): todo exige sesión salvo `Login.aspx`, `Registro.aspx`, `Content/` y `Scripts/`. Cookie solo HTTPS y HttpOnly.
- El nombre del ticket es el **Id del usuario** (`PaginaBase.UsuarioId`); `UserData` guarda su nombre.
- Contraseñas: solo `PasswordHasher.Hash` / `Verificar` (PBKDF2-SHA256, sal aleatoria, comparación en tiempo constante).
- Todas las páginas heredan de `Seguridad.PaginaBase`, que fija `ViewStateUserKey` (protección CSRF de los postbacks).
- `Tablero.aspx` carga el tablero con `TablerosDatos.Obtener(id, UsuarioId)` en **cada** petición; si devuelve `Nothing` no hay acceso.
- Los métodos de `Datos/` reciben el `tableroId` autorizado y lo incluyen en el `WHERE` (vía `Listas.TableroId`), así un Id manipulado de otro tablero no tiene efecto. Mantener este patrón en métodos nuevos.
- Gestionar miembros: propietario o rol `Admin`. Eliminar tablero: solo el propietario.

### 5. Codificar siempre la salida

- La validación de peticiones de ASP.NET está desactivada en las páginas (`ValidateRequestMode="Disabled"`) para permitir textos con `<` en títulos, comentarios o contraseñas. Por tanto **toda** salida de datos del usuario debe codificarse: `<%#: %>` / `<%: %>`, `Literal Mode="Encode"` o `HttpUtility.HtmlEncode`. `Label.Text` y `Page.Title` **no** codifican solos.

## Convenciones de código

- `Option Explicit On` (ya activo en `Web.config`); escribir código compatible con `Option Strict On` cuando sea posible.
- Separar acceso a datos (clases en `Datos/`) de la lógica de las páginas; el code-behind no debe contener SQL.
- Operaciones de varias sentencias (mover/eliminar con renumeración de `Posicion`) en un único lote con `SET XACT_ABORT ON; BEGIN TRAN; … COMMIT;`.
- En VB, para leer columnas nulas usar `Db.Texto/Fecha/Entero` (evita el `If(cond, Nothing, valor)` que convierte `Nothing` en `DateTime.MinValue`/`0`).
- Comentarios y textos de la interfaz en español.

## Compilar y ejecutar

- Abrir `TaskManager.slnx` en Visual Studio y ejecutar con IIS Express (F5).
- Desde línea de comandos: `msbuild TaskManager.vbproj` (restaurar antes los paquetes NuGet con `nuget restore`).
- Requiere que exista `connectionStrings.config` local y la base de datos TaskManager creada con el script de `database/`.
