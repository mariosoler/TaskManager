# CLAUDE.md

Guía para Claude Code al trabajar en este repositorio.

## Proyecto

**TaskManager**: aplicación web tipo Trello (tableros, listas y tarjetas) construida con:

- **ASP.NET Web Forms** en **VB.NET**, .NET Framework **4.8.1** (`TaskManager.vbproj`, solución `TaskManager.slnx`).
- **SQL Server** como base de datos, accedida con ADO.NET (`System.Data.SqlClient`).
- Paquetes NuGet en `packages.config` (carpeta `packages/`).

## Estructura

- `Default.aspx` / `*.aspx.vb` — páginas Web Forms y su code-behind.
- `App_Code/` — clases compartidas (acceso a datos, utilidades).
- `Models/` — clases de modelo que reflejan las tablas (Usuario, Tablero, Lista, Tarjeta, Etiqueta, Comentario…).
- `App_Data/` — datos locales; los `.mdf`/`.ldf` están ignorados por git.
- `database/` — script(s) SQL de la base de datos **TaskManager** (creación de esquema y datos iniciales). Cualquier cambio de esquema debe reflejarse aquí.
- `Web.config` — configuración versionada (sin secretos).

## Base de datos

- Nombre: **TaskManager** (SQL Server).
- Script del esquema: `database/TaskManager.sql`. Para montar el entorno local, crear la base TaskManager y ejecutarlo sobre ella (SSMS o `sqlcmd -S <servidor> -d TaskManager -E -i database\TaskManager.sql`).
- Al modificar tablas, columnas o procedimientos, actualizar el script en `database/` en el mismo cambio.

### Esquema (nombres en español)

| Tabla | Descripción | Relaciones |
|---|---|---|
| `Usuarios` | Cuentas (`Email` único, `PasswordHash`) | — |
| `Tableros` | Tableros | `PropietarioId` → Usuarios |
| `MiembrosTablero` | Miembros de un tablero; `Rol` = `Admin` / `Miembro` | PK (TableroId, UsuarioId); cascada desde Tableros |
| `Listas` | Columnas de un tablero, ordenadas por `Posicion` | → Tableros (cascada) |
| `Tarjetas` | Tarjetas de una lista, ordenadas por `Posicion`; `FechaLimite`, `AsignadoA` opcionales | → Listas (cascada), `AsignadoA` → Usuarios |
| `Etiquetas` | Etiquetas por tablero; `Color` hex `#rrggbb` | → Tableros (cascada) |
| `TarjetaEtiquetas` | N:M tarjetas–etiquetas | → Tarjetas (cascada), → Etiquetas (sin cascada) |
| `Comentarios` | Comentarios en tarjetas | → Tarjetas (cascada), → Usuarios |

Notas:
- Las fechas se guardan en **UTC** (`SYSUTCDATETIME()`); convertir a hora local solo al mostrar.
- `Usuarios.PasswordHash` guarda solo el hash (p. ej. PBKDF2 con `Rfc2898DeriveBytes` y sal); nunca contraseñas en claro.
- Borrar una etiqueta asignada a tarjetas falla por la FK sin cascada: eliminar antes sus filas en `TarjetaEtiquetas`.
- Al mover tarjetas/listas, actualizar `Posicion` de los elementos afectados dentro de una transacción.
- Comprobar en cada operación que el usuario es propietario o miembro del tablero.

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

## Convenciones de código

- `Option Explicit On` (ya activo en `Web.config`); escribir código compatible con `Option Strict On` cuando sea posible.
- Separar acceso a datos (clases en `App_Code/`) de la lógica de las páginas; el code-behind no debe contener SQL.
- Codificar la salida HTML de datos del usuario (`HttpUtility.HtmlEncode` / `<%: %>`) para evitar XSS.
- Comentarios y textos de la interfaz en español.

## Compilar y ejecutar

- Abrir `TaskManager.slnx` en Visual Studio y ejecutar con IIS Express (F5).
- Desde línea de comandos: `msbuild TaskManager.vbproj` (restaurar antes los paquetes NuGet con `nuget restore`).
- Requiere que exista `connectionStrings.config` local y la base de datos TaskManager creada con el script de `database/`.
