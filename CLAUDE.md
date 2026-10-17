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
  - `Buscar.aspx[?tablero=N | ?busqueda=N]` — buscador con filtros básicos y búsqueda avanzada; exporta a Excel. Las búsquedas se pueden guardar (y compartir); `?busqueda=N` aplica una guardada.
  - `Estadisticas.aspx?id=N[&dias=7|30|90]` — indicadores y gráficas del tablero; exporta a Excel.
  - `Tablero.aspx?id=N&tarjeta=M` abre directamente la tarjeta M (lo usan los enlaces del buscador).
  - `Chat.ashx` — endpoint JSON del chat de cada tarjeta (GET mensajes nuevos / POST publicar).
  - `Fondo.ashx?id=N` — sirve la imagen de fondo del tablero N (solo a propietario/miembros).
  - `Default.aspx` — redirige a `Tableros.aspx`.
- `Datos/` — acceso a datos ADO.NET (`Db`, `UsuariosDatos`, `TablerosDatos`, `ListasDatos`, `TarjetasDatos`, `SubtareasDatos`, `ComentariosDatos`, `HistorialDatos`, `BusquedaDatos`, `BusquedasGuardadasDatos`, `EstadisticasDatos`). Las páginas no contienen SQL.
- `Models/` — clases de modelo que reflejan las tablas (Usuario, Tablero, Lista, Tarjeta, Subtarea, Comentario, Miembro, HistorialTarjeta, FiltroBusqueda, BusquedaGuardada, Estadisticas).
- `Seguridad/` — `PasswordHasher` (PBKDF2), `Autenticacion` (cookie de Forms Auth), `PaginaBase` (base de todas las páginas).
- `Utilidades/` — `Formato` (presentación), `LibroExcel` (genera .xlsx sin librerías, con `System.IO.Compression`), `ExportacionExcel` (hojas comunes de las exportaciones).
- `Content/site.css` — estilos. **Colores de GE Vernova** (variables en `:root`): base evergreen `--primario` `#005e60` (hover `#004746`, fondo claro `#e4f1f1`), acento verde `--acento` `#c8ff08` solo sobre fondo evergreen (barra superior), tinta `#212121`. `Content/logo.svg` — icono del logo (barra superior, login/registro y favicon); junto a él va el texto «Task**Manager**» (`.logo-texto`) con la fuente **Outfit** incluida en `Content/fuentes/` (licencia OFL, solo para la marca; `Web.config` declara el MIME de `.woff2`).
- `Scripts/tablero.js` — arrastrar/soltar de tarjetas, abrir detalle, Esc, edición del nombre del tablero al pulsar el título.
- `Scripts/chat.js` — chat del detalle de tarjeta: envío por fetch (Enter) y sondeo cada 4 s de los mensajes nuevos; si falla, recurre al postback del botón Enviar.
- `Scripts/graficas.js` — gráficas de estadísticas en HTML/CSS, sin librerías (ver «Gráficas» más abajo).
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
- Migraciones aplicadas: `001_caso_codigo_subtareas.sql`, `002_modificacion_historial.sql`, `003_prioridad.sql`, `004_fondo_tableros.sql`, `005_busquedas_guardadas.sql`.

### Esquema (nombres en español)

| Tabla | Descripción | Relaciones |
|---|---|---|
| `Usuarios` | Cuentas (`Email` único, `PasswordHash`) | — |
| `Tableros` | Tableros; `Fondo` (opcional) = archivo de imagen subido o `predef:<clave>` de un fondo predeterminado | `PropietarioId` → Usuarios |
| `MiembrosTablero` | Miembros de un tablero; `Rol` = `Admin` / `Miembro` | PK (TableroId, UsuarioId); cascada desde Tableros |
| `Listas` | Columnas de un tablero, ordenadas por `Posicion` | → Tableros (cascada) |
| `Tarjetas` | Tarjetas de una lista, ordenadas por `Posicion`; `FechaLimite`, `AsignadoA` opcionales; `Caso` (NVARCHAR(20), solo dígitos, `CK_Tarjetas_Caso`); `Codigo` (markdown, NVARCHAR(MAX)); `FechaModificacion` + `ModificadoPor` (última modificación) | → Listas (cascada), `AsignadoA` → Usuarios |
| `Subtareas` | Checklist de una tarjeta (`Texto`, `Completada`), ordenada por `Posicion` | → Tarjetas (cascada) |
| `HistorialTarjeta` | Un cambio por fila: `Accion`, `Campo`, `ValorAnterior`, `ValorNuevo`, quién y cuándo | → Tarjetas (cascada), → Usuarios |
| `Etiquetas` | Etiquetas por tablero; `Color` hex `#rrggbb` | → Tableros (cascada) |
| `TarjetaEtiquetas` | N:M tarjetas–etiquetas | → Tarjetas (cascada), → Etiquetas (sin cascada) |
| `Comentarios` | Mensajes del chat de cada tarjeta | → Tarjetas (cascada), → Usuarios |
| `BusquedasGuardadas` | Búsquedas del buscador: `Nombre` (único por usuario), `Criterios` (filtro serializado), `Compartida` | `UsuarioId` → Usuarios (cascada) |

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
- **Fondos de tablero**: la carpeta se configura en `Web.config` → `appSettings` `RutaFondosTableros` (ruta absoluta o `~/...`; por defecto `~/App_Data/Fondos`, ignorada por git). `Utilidades/FondosTablero` guarda el archivo con un nombre generado (`<tableroId>_<guid>.<ext>`), acepta solo JPG/PNG/GIF/WebP comprobando la firma del contenido (nunca SVG) y hasta 5 MB. Nunca construir rutas con texto del usuario; `Fondo.ashx` busca el archivo por el nombre guardado en la base de datos. Cambiar el fondo: propietario o `Admin`. Al cambiarlo, quitarlo o eliminar el tablero se borra el archivo anterior. **Fondos predeterminados** (15: 7 degradados y 8 fotos de `Content/fondos/`, licencia Unsplash en `LICENCIA.txt`): se guardan como `predef:<clave>` en `Tableros.Fondo` y solo se aceptan las claves de la lista blanca `FondosTablero.Predefinidos`; el CSS lo genera `FondosTablero.ImagenCss` (las fichas de «Mis tableros» usan la miniatura `*-mini.jpg`). Para añadir uno: entrada en `Predefinidos` y, si es foto, `<clave>.jpg` (1920×1080) + `<clave>-mini.jpg` (480×270) registrados en el `.vbproj`.
- **Nombre del tablero**: lo cambian (con la descripción) el propietario o un `Admin` desde «✏ Editar» en la cabecera (`TablerosDatos.CambiarNombre`). El nombre también se edita pulsando el título: `Scripts/tablero.js` lo cambia por un cuadro de texto (Enter o salir del cuadro guarda con el botón oculto `btnGuardarTitulo`; Esc cancela). El servidor solo envía el cuadro de texto a propietario/`Admin` y vuelve a comprobar el permiso al guardar.
- **Búsquedas guardadas**: `FiltroBusqueda.ACriterios`/`DesdeCriterios` serializan el filtro (formato clave=valor con claves fijas). Cada usuario ve las suyas y las compartidas por sus compañeros (usuarios con algún tablero en común); solo el autor las borra, y guardar con un nombre existente la sustituye. Al aplicarla, cada valor se valida contra las opciones del usuario (sus tableros, listas, compañeros) y se avisa si alguno no se aplica; los resultados siguen limitados a los tableros de quien busca, así que compartir no da acceso a nada.
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

### 6. SQL dinámico (buscador)

- `BusquedaDatos` compone el `WHERE` solo con **fragmentos fijos escritos en el código**; los valores del usuario van siempre como parámetros (sus nombres, `@Palabra0`…, los genera el código). El `ORDER BY` sale de una lista blanca (`Ordenes`).
- En `LIKE` escapar los comodines con `BusquedaDatos.EscaparLike` y `ESCAPE ''`.
- La búsqueda siempre restringe a tableros donde el usuario es propietario o miembro, aunque se fuerce otro `TableroId`.

### 7. Exportación a Excel

- Usar `LibroExcel` (.xlsx real). El texto va como `inlineStr`: Excel no lo evalúa como fórmula (no generar CSV, que sí es vulnerable a la inyección de fórmulas).
- Fechas como `DateTime` (sin hora = fecha; con hora = fecha y hora); las UTC se convierten a hora local antes de exportar.
- `PaginaBase.EnviarArchivo` envía la descarga; el nombre de archivo lo genera el código (ASCII), nunca el usuario.

### 8. Gráficas

- Siguen la guía de visualización: una serie = color de serie 1 (`#00949a`, evergreen saturado validado contra el blanco de las tarjetas; el evergreen `#005e60` puro no pasa el mínimo de croma como color de serie); "Sin asignar" en gris de baja énfasis; estados (vencidas, esta semana…) solo con la paleta de estado y siempre con icono + etiqueta; barras de ≤ 24 px con punta redondeada; rejilla en línea fina; texto siempre en colores de tinta, nunca en el color de la serie.
- Cada gráfica tiene tooltip (ratón y teclado) y vista de tabla. Los datos llegan como JSON en `<script type="application/json">` con `<`, `>` y `&` escapados; en JS los textos se insertan con `textContent`/`setAttribute`, nunca con `innerHTML`.
- Prefijo `graf-` para las clases CSS de las gráficas: `.barra` ya es la barra superior de la aplicación.

### 9. Chat y peticiones AJAX

- El chat de la tarjeta usa la tabla `Comentarios`. Los mensajes **no** se registran en el historial ni cambian la fecha de modificación (son conversación). Las entradas "comentó" antiguas del historial se siguen mostrando.
- `Chat.ashx` comprueba en cada petición que el usuario pertenece al tablero; el POST exige el token `Seguridad.TokenAntiCsrf` en la cabecera `X-Chat-Token` y que `Origin` (si viene) sea la propia web. Lee el texto con `Request.Unvalidated.Form` (admite `<`) y los parámetros solo de la URL o del cuerpo, nunca de `Request(...)` (incluye cookies).
- Cualquier endpoint AJAX nuevo que modifique datos debe seguir el mismo patrón (token + comprobación de acceso).

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
