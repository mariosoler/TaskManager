# CLAUDE.md

Guía para Claude Code al trabajar en este repositorio.

## Proyecto

**TaskManager**: aplicación web tipo Trello (tableros, listas y tarjetas) construida con:

- **ASP.NET Web Forms** en **VB.NET**, .NET Framework **4.8.1** (`TaskManager.vbproj`, solución `TaskManager.slnx`).
- **SQL Server** como base de datos, accedida con ADO.NET (`System.Data.SqlClient`).
- Paquetes NuGet en `packages.config` (carpeta `packages/`).

## Estructura

- `Site.Master` — página maestra (barra superior con «Tableros», «Buscar» y, dentro de un tablero, «Estadísticas»: `Tablero.aspx` y `Estadisticas.aspx` fijan `SiteMaster.TableroActual` y entonces «Buscar» abre el buscador filtrado por ese tablero; cerrar sesión; el avatar y el nombre enlazan a «Mi perfil» y se rellenan en `Page_PreRender` para mostrar al momento un nombre recién cambiado). La pestaña del navegador muestra **siempre «TaskManager»** (lo fija `SiteMaster.Page_PreRender`; las páginas no ponen título propio). El favicon y el logo de la barra se enlazan con versión (`Formato.UrlConVersion`) para que el navegador no use un icono antiguo en caché. Junto al logo se muestra la versión de la app (`.version-app`, en todas las páginas): `Formato.VersionApp` lee `AssemblyInformationalVersion` de `My Project/AssemblyInfo.vb` (cambiarla solo ahí; `-beta` hasta la 1.0).
- Páginas (`*.aspx` + `.aspx.vb` + `.aspx.designer.vb`; las clases se llaman `XxxPagina`):
  - `Login.aspx`, `Registro.aspx` — acceso (únicas páginas públicas).
  - `Tableros.aspx` — "Mis tableros": listar, crear y eliminar.
  - `Perfil.aspx` — «Mi perfil»: cambiar el nombre y el formato de las fechas (`Usuarios.FormatoFecha`). El email no se cambia.
  - `Tablero.aspx?id=N` — vista Kanban: listas, tarjetas (con su ID, solo el número, delante del nº de caso; la prioridad solo como franja de color en el lateral, con el texto para lectores de pantalla: `Formato.HtmlPrioridadLectores`), comentarios y miembros. Menú «⋯» de cada lista: ordenar sus tarjetas por fecha (creación, modificación o entrada en la lista; `Models/OrdenLista`) y ocultarla; ambas cosas son **preferencias de cada usuario** (`PreferenciasLista`), no cambian lo que ven los demás. Las listas ocultas se vuelven a mostrar desde «👁 Listas ocultas» en la cabecera. Cabecera compacta: título con «Editar», «Fondo» y «Listas ocultas» a su lado, búsqueda rápida en la misma franja y miembros a la derecha. Un único modal (`pnlDetalle`) sirve para crear tarjetas ("+ Añadir una tarjeta", estado en `ListaNuevaId`) y para editarlas (`TarjetaAbiertaId`). Arriba del detalle: el ID de la tarjeta y el título (cuadro de texto con aspecto de título, se edita al pulsarlo), el resumen (lista, creación, modificación) y la fila caso / prioridad / fecha límite / asignada a; `detalle-tarjeta.js` marca con icono y texto «sin prioridad», la fecha límite superada y «sin asignar», y señala la prioridad elegida solo con el borde del selector, de su color (franja gruesa a la izquierda), sin colorear el fondo ni la lista. Colores de prioridad en toda la app: Alta rojo, Media amarillo, Baja azul (`--prioridad-baja`).
  - `Buscar.aspx[?tablero=N[&etiqueta=NOMBRE] | ?busqueda=N]` — buscador con filtros básicos y búsqueda avanzada (incluidas etiquetas: todas / alguna / sin etiquetas, y texto en etiquetas); exporta a Excel. Las búsquedas se pueden guardar (y compartir); `?busqueda=N` aplica una guardada.
  - `Estadisticas.aspx?id=N[&dias=7|30|90]` — indicadores (incluidos prioridad alta, sin cambios en 30 días y tarjetas creadas frente al periodo anterior) y gráficas del tablero: anillos por lista, por prioridad y cambios por tipo; barras por persona, antigüedad y etiqueta («Tarjetas por etiqueta» enlaza al buscador filtrado); fechas límite; tabla «Próximos vencimientos» con enlace a cada tarjeta. Exporta a Excel.
  - `Tablero.aspx?id=N&tarjeta=M` abre directamente la tarjeta M (lo usan los enlaces del buscador).
  - `Chat.ashx` — endpoint JSON del chat de cada tarjeta (GET mensajes nuevos / POST publicar).
  - `Tarjeta.ashx` — endpoint JSON del guardado automático del detalle de tarjeta (POST; mismo patrón de seguridad que el chat); también marca subtareas (`accion=subtarea`) y crea etiquetas con el color elegido (`accion=etiqueta`).
  - `Usuarios.ashx?tablero=N&q=texto` — sugerencias (≤ 4) para «Añadir miembro»: usuarios cuyo nombre o email contiene el texto (sin distinguir mayúsculas ni acentos), excluidos propietario y miembros. Solo lectura; solo responde a propietario/`Admin` del tablero y con ≥ 2 caracteres.
  - `Fondo.ashx?id=N` — sirve la imagen de fondo del tablero N (solo a propietario/miembros).
  - `Adjuntos.ashx` — archivos adjuntos de las tarjetas: GET `?id=N` sirve el archivo (solo a propietario/miembros de su tablero; las imágenes comprobadas `inline`, cualquier otro tipo como descarga `application/octet-stream` con `nosniff` y `sandbox`); POST `accion=subir` (multipart, un archivo) y `accion=eliminar` (mismo patrón de seguridad que el chat: token `X-Chat-Token`, origen y acceso al tablero) devuelven la lista de adjuntos de la tarjeta.
  - `Default.aspx` — redirige a `Tableros.aspx`.
- `Datos/` — acceso a datos ADO.NET (`Db`, `UsuariosDatos`, `TablerosDatos`, `ListasDatos`, `TarjetasDatos`, `SubtareasDatos`, `ComentariosDatos`, `HistorialDatos`, `EtiquetasDatos`, `BusquedaDatos`, `BusquedasGuardadasDatos`, `EstadisticasDatos`, `AdjuntosDatos`). Las páginas no contienen SQL.
- `Models/` — clases de modelo que reflejan las tablas (Usuario, Tablero, Lista, Tarjeta, Subtarea, Comentario, Miembro, HistorialTarjeta, Etiqueta, FiltroBusqueda, BusquedaGuardada, Estadisticas) y `FormatoFecha` (lista blanca de formatos de fecha del perfil).
- `Seguridad/` — `PasswordHasher` (PBKDF2), `Autenticacion` (cookie de Forms Auth), `PaginaBase` (base de todas las páginas), `UsuarioActual` (perfil del usuario leído de la base de datos una vez por petición, cacheado en `HttpContext.Items`).
- `Utilidades/` — `Formato` (presentación), `EdicionTarjeta` (lee y valida los campos del detalle; la usan el botón Guardar y `Tarjeta.ashx`), `LibroExcel` (genera .xlsx sin librerías, con `System.IO.Compression`), `ExportacionExcel` (hojas comunes de las exportaciones), `FondosTablero` y `AdjuntosTarjeta` (archivos subidos; comprobaciones comunes de imágenes en `ImagenSubida`).
- `Content/site.css` — estilos. **Colores de GE Vernova** (variables en `:root`): base evergreen `--primario` `#005e60` (hover `#004746`, fondo claro `#e4f1f1`), acento verde `--acento` `#c8ff08` solo sobre fondo evergreen (barra superior), tinta `#212121`. `Content/logo.svg` — icono del logo (barra superior, login/registro y favicon); junto a él va el texto «Task**Manager**» (`.logo-texto`) con la fuente **Outfit** incluida en `Content/fuentes/` (licencia OFL, solo para la marca; `Web.config` declara el MIME de `.woff2`).
- `Scripts/tablero.js` — arrastrar/soltar de tarjetas, abrir detalle, Esc, edición del nombre del tablero al pulsar el título.
- `Scripts/detalle-tarjeta.js` — detalle de tarjeta: al editar una existente guarda solo tras una pausa al escribir, al salir de un campo y antes de cerrar (POST a `Tarjeta.ashx`; oculta el botón Guardar y muestra el estado en `#estadoGuardado`); pulsar fuera del modal lo cierra (como la ✕ o Esc). Al crear una tarjeta no hay guardado automático: sigue el botón «Crear tarjeta», y cerrar con datos escritos pide confirmación. Marcar una subtarea no recarga: al editar se guarda con `Tarjeta.ashx` (`accion=subtarea`); al crear, el estado viaja con el siguiente envío (`chkSubtarea_CheckedChanged`); sin JS sigue el `AutoPostBack`. Tras un postback con el mismo detalle abierto, el servidor pone `sin-animacion` en `pnlDetalle` (no se anima de nuevo) y el script recupera el desplazamiento. Solo se carga con el detalle abierto (`phScriptDetalle`).
- `Scripts/subtareas-enlace.js` — subtareas enlazadas: el botón «🔗 Enlazar tarjeta» (junto a «Añadir») pone el cuadro de nueva subtarea en modo búsqueda de tarjetas del tablero (ID, título, nº de caso o lista; el ID exacto sale primero; datos en `TarjetasEnlazablesJson`); elegir una pone su Id en `hfSubtareaEnlace` y pulsa «Añadir». Esc vuelve al modo normal. Solo con el detalle abierto (`phScriptDetalle`).
- `Scripts/adjuntos.js` — adjuntos del detalle (sección bajo las subtareas, solo en tarjetas ya creadas), de cualquier tipo: subir arrastrándolos a cualquier parte del detalle, pegando del portapapeles (captura o archivos copiados, Ctrl+V; se conserva el nombre original, salvo las capturas pegadas sin nombre propio —«image.png»—, que se llaman `Screenshot_AAAA-MM-DD_HH-MM-SS`) o con «Elegir archivos…» (una petición por archivo a `Adjuntos.ashx`). Las imágenes en miniatura; al pulsar una, visor emergente (anterior/siguiente ← →, con animación de deslizamiento que respeta «reducir movimiento», Esc o ✕, abrir original, eliminar; foco atrapado). El resto, en lista con nombre (descarga), tamaño y autor. ✕ arriba a la derecha de cada adjunto para eliminarlo (con confirmación). Datos iniciales en `TableroPagina.AdjuntosJson`. En el tablero, cada tarjeta muestra un clip de línea (SVG `.icono-clip`, color del texto) con el número de adjuntos (`Tarjeta.NumAdjuntos`).
- `Scripts/chat.js` — chat del detalle de tarjeta: envío por fetch (Enter) y sondeo cada 4 s de los mensajes nuevos; si falla, recurre al postback del botón Enviar.
- `Scripts/busqueda-rapida.js` — búsqueda rápida del tablero (cuadro sobre las listas): oculta al escribir las tarjetas que no coinciden. `#texto` busca en nº de caso y etiquetas; sin `#`, en título, descripción, asignado, caso y etiquetas; varias palabras = todas; sin distinguir mayúsculas ni acentos. Lee `data-caso`, `data-etiquetas` y `data-texto` de cada tarjeta (codificados con `<%#: %>`), recuerda la búsqueda en `sessionStorage` por tablero; atajos `/` y Esc. Es solo de cliente: el buscador completo sigue en `Buscar.aspx`.
- `Scripts/miembros.js` — cuadro «Añadir miembro» de la cabecera (aspecto de la búsqueda rápida): pide sugerencias a `Usuarios.ashx` al escribir; ↑/↓, Enter o clic eligen (pone el email y pulsa «Añadir», que es un postback normal); Esc cierra. Solo se carga para propietario/`Admin` (`phScriptMiembros`).
- `Scripts/etiquetas.js` — cuadro de etiquetas (detalle de tarjeta y buscador): pinta las etiquetas con su color **dentro** del cuadro (✕ o Retroceso para quitarlas) y sugiere al escribir (solo las del tablero: las etiquetas **no se comparten entre tableros**); espacio, coma, Enter, Tab o salir del cuadro añaden lo escrito. En el detalle, una etiqueta que no existe abre un cuadro para confirmarla y elegir su color, sólido o degradado, de `ColorEtiqueta.Todos` (`PaletaJson`); se crea en el tablero con `Tarjeta.ashx` (`accion=etiqueta`, `EtiquetasDatos.Crear`, solo colores de la paleta) y se asigna al guardar la tarjeta. El `TextBox` original queda oculto con `#A #B ` (lo leen el servidor y el guardado automático); sin JS se escribe en él y las nuevas toman el siguiente color de la paleta. En el buscador (`modo: 'filtro'`) no se crean: las que no existen se marcan «no existe».
- `Scripts/graficas.js` — gráficas de estadísticas en HTML/CSS, sin librerías (ver «Gráficas» más abajo).
- `Scripts/markdown.js` — editor markdown propio sin dependencias (barra, Escribir/Vista previa). El renderizador escapa todo el HTML antes de aplicar marcas y solo permite enlaces `http(s)`/`mailto`; mantener esa garantía si se amplía.
- CSS/JS se enlazan con `Formato.UrlConVersion("~/...")` (añade `?v=<fecha del archivo>`) para que el navegador no use versiones en caché tras un cambio.
- **Codificación**: `.aspx`/`.Master` en UTF-8 **con BOM** (y `fileEncoding="utf-8"` en Web.config); sin ello ASP.NET los lee como Windows-1252 y los acentos salen mal.
- **No usar `App_Code/`**: en un proyecto *Web Application* el código ahí se compila dos veces (en la DLL y en tiempo de ejecución).
- Al añadir archivos, registrarlos en `TaskManager.vbproj` (`Compile` / `Content`) y mantener los `.designer.vb` al día con los controles con `ID`.
- `App_Data/` — datos locales; los `.mdf`/`.ldf` están ignorados por git.
- `database/` — script(s) SQL de la base de datos **TaskManager** (creación de esquema y datos iniciales). Cualquier cambio de esquema debe reflejarse aquí. La carpeta se **publica** (cada `.sql` y `LEEME.txt` registrados como `Content` en el `.vbproj`: al añadir una migración, registrarla también) y `Web.config` la oculta por HTTP (`hiddenSegments`).
- `Web.config` — configuración versionada (sin secretos).

## Base de datos

- Nombre: **TaskManager** (SQL Server).
- Script del esquema: `database/TaskManager.sql`. Para montar el entorno local, crear la base TaskManager y ejecutarlo sobre ella (SSMS o `sqlcmd -S <servidor> -d TaskManager -E -i database\TaskManager.sql`).
- Al modificar tablas, columnas o procedimientos, en el mismo cambio:
  1. Actualizar `database/TaskManager.sql` (esquema completo para instalaciones nuevas).
  2. Añadir una migración **idempotente** en `database/migraciones/NNN_descripcion.sql` (`IF COL_LENGTH(...) IS NULL`, `IF OBJECT_ID(...) IS NULL`) para bases ya existentes. Debe terminar en `GO`.
  3. Regenerar `database/migraciones_todas.sql` (todas las migraciones en orden en un solo script; no se edita a mano) con `powershell -ExecutionPolicy Bypass -File database\GenerarMigracionesTodas.ps1`. Las migraciones siguen sueltas para poder pasarlas una a una.
- Migraciones aplicadas: `001_caso_codigo_subtareas.sql`, `002_modificacion_historial.sql`, `003_prioridad.sql`, `004_fondo_tableros.sql`, `005_busquedas_guardadas.sql`, `006_etiquetas.sql`, `007_perfil_usuario.sql`, `008_subtareas_enlace.sql`, `009_preferencias_listas.sql`, `010_etiquetas_degradados.sql`, `011_fotos_tarjetas.sql`, `012_adjuntos_tarjetas.sql` (convierte las fotos de la 011 en adjuntos y elimina `FotosTarjeta`).
- `database/demo/tablero_demo_aplicaciones.sql` — tablero de demostración (etiquetas PLANIFICADOR_CABINAS, GESTOR_CABINAS, IT_CASES); no es parte del esquema. Uso: `sqlcmd ... -i database\demo\tablero_demo_aplicaciones.sql -v EmailPropietario="usuario@ejemplo.com"`.

### Esquema (nombres en español)

| Tabla | Descripción | Relaciones |
|---|---|---|
| `Usuarios` | Cuentas (`Email` único, `PasswordHash`); `FormatoFecha` (opcional) = clave de `Models/FormatoFecha`, NULL = predeterminado | — |
| `Tableros` | Tableros; `Fondo` (opcional) = archivo de imagen subido o `predef:<clave>` de un fondo predeterminado | `PropietarioId` → Usuarios |
| `MiembrosTablero` | Miembros de un tablero; `Rol` = `Admin` / `Miembro` | PK (TableroId, UsuarioId); cascada desde Tableros |
| `Listas` | Columnas de un tablero, ordenadas por `Posicion` | → Tableros (cascada) |
| `Tarjetas` | Tarjetas de una lista, ordenadas por `Posicion`; `FechaLimite`, `AsignadoA` opcionales; `Caso` (NVARCHAR(20), solo dígitos, `CK_Tarjetas_Caso`); `Codigo` (markdown, NVARCHAR(MAX)); `FechaModificacion` + `ModificadoPor` (última modificación); `FechaEnLista` (entrada en su lista actual) | → Listas (cascada), `AsignadoA` → Usuarios |
| `PreferenciasLista` | Vista de cada usuario en cada lista: `Oculta`, `Orden` (clave de `Models/OrdenLista`, NULL = manual); sin fila = visible y manual | PK (UsuarioId, ListaId); cascada desde Listas; `UsuarioId` → Usuarios (sin cascada) |
| `Subtareas` | Checklist de una tarjeta (`Texto`, `Completada`), ordenada por `Posicion`; `TarjetaEnlazadaId` (opcional) = tarjeta del mismo tablero a la que enlaza | → Tarjetas (cascada); `TarjetaEnlazadaId` sin FK (ver notas) |
| `HistorialTarjeta` | Un cambio por fila: `Accion`, `Campo`, `ValorAnterior`, `ValorNuevo`, quién y cuándo | → Tarjetas (cascada), → Usuarios |
| `Etiquetas` | Etiquetas (#tags) por tablero; `Nombre` sin `#`, único por tablero sin distinguir mayúsculas (`UX_Etiquetas_Tablero_Nombre`); `Color` = `#rrggbb` o `grad:<clave>` de un degradado (`CK_Etiquetas_Color`; lista blanca `Models/ColorEtiqueta`) | → Tableros (cascada) |
| `TarjetaEtiquetas` | N:M tarjetas–etiquetas | → Tarjetas (cascada), → Etiquetas (sin cascada) |
| `AdjuntosTarjeta` | Archivos adjuntos de una tarjeta: `Archivo` (nombre generado en la carpeta `RutaAdjuntosTarjetas`), `Nombre` (original, solo para mostrar/descargar), `EsImagen`, `Tamano`, `SubidoPor`, `FechaSubida` | → Tarjetas (cascada), `SubidoPor` → Usuarios |
| `Comentarios` | Mensajes del chat de cada tarjeta | → Tarjetas (cascada), → Usuarios |
| `BusquedasGuardadas` | Búsquedas del buscador: `Nombre` (único por usuario), `Criterios` (filtro serializado), `Compartida` | `UsuarioId` → Usuarios (cascada) |

Notas:
- Las fechas se guardan en **UTC** (`SYSUTCDATETIME()`); convertir a hora local solo al mostrar.
- **Formato de las fechas**: cada usuario lo elige en «Mi perfil». Mostrar siempre las fechas con `Formato.FechaLocal` (UTC → local, con hora), `Formato.FechaCorta` (hora si es de hoy; si no, día sin año), `Formato.FechaDia` / `FechaDiaCorta` (fechas sin hora, como `FechaLimite`) o `HtmlFechaLimite`; no usar patrones fijos (`"d MMM"`, `"g"`...) al mostrar. Los patrones salen de la lista blanca `FormatoFecha` (claves en `Usuarios.FormatoFecha`; una clave desconocida = predeterminado). No afecta a los `yyyy-MM-dd` internos (campos de fecha, filtros guardados, historial) ni a las fechas del Excel.
- `Usuarios.PasswordHash` guarda solo el hash (p. ej. PBKDF2 con `Rfc2898DeriveBytes` y sal); nunca contraseñas en claro.
- Borrar una etiqueta asignada a tarjetas falla por la FK sin cascada: eliminar antes sus filas en `TarjetaEtiquetas`.
- Al mover tarjetas/listas, actualizar `Posicion` de los elementos afectados dentro de una transacción. Mover una tarjeta a **otra** lista actualiza `Tarjetas.FechaEnLista` (`TarjetasDatos.Mover`).
- **Orden y ocultar listas** (preferencias por usuario, `ListasDatos.Ocultar` / `CambiarOrden`, que comprueban que la lista es del tablero): el orden por fecha es solo una vista que se aplica al cargar (`OrdenLista.Aplicar`); la `Posicion` manual no cambia. En una lista ordenada (`data-orden` en `.tarjetas`) `tablero.js` no reordena al arrastrar dentro de ella, pero sí permite mover tarjetas a otras listas. Las claves de orden son una lista blanca (`OrdenLista.EsClaveValida`).
- Comprobar en cada operación que el usuario es propietario o miembro del tablero.
- `Caso` es texto para conservar ceros a la izquierda; validar con `[0-9]` (no `\d`, que en .NET acepta dígitos de otros alfabetos y la restricción CHECK los rechaza).
- `Codigo` no se recorta al guardar (la sangría importa). En el tablero solo se carga `TieneCodigo`; el texto completo, al abrir la tarjeta.
- **Historial**: toda operación que modifique una tarjeta debe llamar a `HistorialDatos.Registrar` dentro de su misma transacción; eso inserta las filas y actualiza `FechaModificacion`/`ModificadoPor`. Solo se registran cambios reales (`TarjetasDatos.Actualizar` compara campo a campo; guardar sin cambios no deja rastro). Reordenar dentro de la misma lista no se registra. Las ediciones seguidas de un mismo campo por el mismo usuario (≤ 10 min, sin cambios de otros entre medias) se **agrupan** en una fila (`HistorialDatos.Registrar`): se conserva el valor anterior original; si el campo vuelve a su valor original, la fila se borra (salvo descripción y código, que guardan extracto). Descripción y código se normalizan a `
` antes de guardar para no detectar cambios falsos por `
`. Las acciones/campos están en `Models/HistorialTarjeta.vb` y el texto se genera en `TableroPagina.DescribirCambio` (que codifica los valores).
- **Fondos de tablero**: la carpeta se configura en `Web.config` → `appSettings` `RutaFondosTableros` (ruta absoluta o `~/...`; por defecto `~/App_Data/Fondos`, ignorada por git). `Utilidades/FondosTablero` guarda el archivo con un nombre generado (`<tableroId>_<guid>.<ext>`), acepta solo JPG/PNG/GIF/WebP comprobando la firma del contenido (nunca SVG) y hasta 5 MB. Nunca construir rutas con texto del usuario; `Fondo.ashx` busca el archivo por el nombre guardado en la base de datos. Cambiar el fondo: propietario o `Admin`. Al cambiarlo, quitarlo o eliminar el tablero se borra el archivo anterior. **Fondos predeterminados** (15: 7 degradados y 8 fotos de `Content/fondos/`, licencia Unsplash en `LICENCIA.txt`): se guardan como `predef:<clave>` en `Tableros.Fondo` y solo se aceptan las claves de la lista blanca `FondosTablero.Predefinidos`; el CSS lo genera `FondosTablero.ImagenCss` (las fichas de «Mis tableros» usan la miniatura `*-mini.jpg`). El fondo del tablero y las muestras se pasan en la variable CSS `--fondo-imagen` (el CSS la aplica como `background-image`): **no usar `Control.Style("background-image")`**, porque ASP.NET envuelve en `url(...)` todo valor que no empiece por `url(` y rompe los degradados. Para añadir uno: entrada en `Predefinidos` y, si es foto, `<clave>.jpg` (1920×1080) + `<clave>-mini.jpg` (480×270) registrados en el `.vbproj`.
- **Adjuntos de las tarjetas** (cualquier tipo de archivo): la carpeta se configura en `Web.config` → `appSettings` `RutaAdjuntosTarjetas` (en desarrollo `~/App_Data/Adjuntos`, ignorada por git; al publicar, marcador `[RUTA_CARPETA_ADJUNTOS]` en `Web.Release.config`). `Utilidades/AdjuntosTarjeta` guarda con nombre generado (`<tarjetaId>_<guid>.<ext>`: la extensión de imagen si la firma del contenido es JPG/PNG/GIF/WebP —`Utilidades/ImagenSubida`, compartida con los fondos— y `.bin` para todo lo demás), hasta 10 MB (`maxRequestLength` 11264 KB) y 30 por tarjeta. El nombre original (`LimpiarNombre`: sin ruta ni caracteres de control) solo se muestra codificado y como nombre de descarga (`ContentDisposition`, ASCII + RFC 5987); nunca forma rutas. Solo las imágenes comprobadas se muestran en el navegador; lo demás siempre se descarga. Añadir y eliminar se registran en el historial (`AdjuntoAgregado`/`AdjuntoEliminado`, con el nombre; las antiguas `FotoAgregada`/`FotoEliminada` se siguen mostrando). Al eliminar una tarjeta, lista o tablero, `TarjetasDatos`/`ListasDatos`/`TablerosDatos.Eliminar` devuelven los archivos de sus adjuntos y la página los borra (`AdjuntosTarjeta.EliminarArchivos`).
- **Nombre del tablero**: lo cambian (con la descripción) el propietario o un `Admin` desde «✏ Editar» en la cabecera (`TablerosDatos.CambiarNombre`). El nombre también se edita pulsando el título: `Scripts/tablero.js` lo cambia por un cuadro de texto (Enter o salir del cuadro guarda con el botón oculto `btnGuardarTitulo`; Esc cancela). El servidor solo envía el cuadro de texto a propietario/`Admin` y vuelve a comprobar el permiso al guardar.
- **Búsquedas guardadas**: `FiltroBusqueda.ACriterios`/`DesdeCriterios` serializan el filtro (formato clave=valor con claves fijas). Cada usuario ve las suyas y las compartidas por sus compañeros (usuarios con algún tablero en común); solo el autor las borra, y guardar con un nombre existente la sustituye. Al aplicarla, cada valor se valida contra las opciones del usuario (sus tableros, listas, compañeros) y se avisa si alguno no se aplica; los resultados siguen limitados a los tableros de quien busca, así que compartir no da acceso a nada.
- **Etiquetas (#tags)**: se escriben en el detalle de la tarjeta (`#PLANIFICADOR_CABINAS #urgente`; `#` opcional, separadas por espacios/comas; letras, números, `_` y `-`, ≤ 50 caracteres, ≤ 10 por tarjeta: `Etiqueta.Analizar`). Al guardar, `EtiquetasDatos.Asignar` (en la transacción de `TarjetasDatos.Crear/Actualizar`) crea las que no existan en el tablero con el siguiente color **sólido** de `ColorEtiqueta.Solidos` (con JS, las nuevas ya se han creado antes con el color elegido en el cuadro de etiquetas) (**colores de etiqueta**: lista blanca `Models/ColorEtiqueta`, 28 sólidos y 14 degradados de dos colores a 135°, todos con contraste AA frente a su tinta, en los degradados frente a los dos extremos; `ColorEtiqueta.TintaPara`. Si se añade uno, validarlo así) y deja en la tarjeta exactamente las indicadas; el cambio va al historial (`CampoEtiquetas`, valores `#A #B`). Las etiquetas sin uso se conservan como sugerencias. En el buscador las etiquetas se filtran **por nombre** en todos los tableros del usuario (`BusquedaDatos.AgregarEtiquetas`; sugerencias de `EtiquetasDatos.ListarParaBuscador`, con `etiquetas.js` en `modo: 'filtro'`), se muestran en los resultados y van en la columna «Etiquetas» del Excel. Las etiquetas de colores (tarjetas, sugerencias, vista previa) se muestran **sin `#`**; el `#` solo se usa al escribirlas y en el historial. En el HTML los colores salen de `Etiqueta.EstiloCss`/`FondoCss`/`ColorSeguro`/`Tinta` (generados con la lista blanca), nunca tal cual de la base de datos; donde solo cabe un color (muestras de las gráficas) se usa `ColorSeguro` (en un degradado, su primer color).
- **Subtareas enlazadas**: una subtarea puede enlazar a otra tarjeta **del mismo tablero** (`Subtareas.TarjetaEnlazadaId`); se muestra con el título **actual** de esa tarjeta, subrayado y con enlace a `Tablero.aspx?id=N&tarjeta=M` (`TableroPagina.HtmlSubtarea`, que codifica el texto; al crear una tarjeta el enlace abre otra pestaña). `Texto` guarda el título al enlazarla y se muestra si la tarjeta enlazada se elimina. Sin FK (otra FK a `Tarjetas` con `SET NULL` daría varias rutas de cascada): `SubtareasDatos` valida el enlace contra el `TableroId` al guardarlo (`SqlEnlaceValido`; no puede ser la propia tarjeta) y al mostrarlo (`LEFT JOIN` filtrado por tablero). La casilla de la subtarea es independiente del estado de la tarjeta enlazada.
- Subtareas: al editar una tarjeta se guardan al momento (marcar/desmarcar, sin recargar la página); al crearla se acumulan en ViewState (`SubtareasPendientes`) y se insertan con la tarjeta en una transacción.

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
- **Publicación**: `Web.Release.config` escribe dentro del `Web.config` publicado la cadena de conexión, `RutaFondosTableros` y `RutaAdjuntosTarjetas` con marcadores (`[SERVIDOR_SQL]`, `[RUTA_CARPETA_FONDOS]`, `[RUTA_CARPETA_ADJUNTOS]`) y explicaciones, para rellenarlos en el servidor. Solo marcadores: nunca datos ni contraseñas reales en `Web.Release.config`. Si se publica sin rellenarlos, `Db.CrearConexion`, `FondosTablero.Carpeta` y `AdjuntosTarjeta.Carpeta` lanzan un error que dice qué configurar. Cada publicación regenera el `Web.config` con los marcadores. Pasos de despliegue en `README.md`.
- Leer la cadena siempre mediante `ConfigurationManager.ConnectionStrings("TaskManager")`; no codificarla en el código.
- No eliminar entradas de seguridad del `.gitignore`.

### 4. Autenticación y autorización

- Forms Authentication (`Web.config`): todo exige sesión salvo `Login.aspx`, `Registro.aspx`, `Content/` y `Scripts/`. Cookie solo HTTPS y HttpOnly.
- El nombre del ticket es el **Id del usuario** (`PaginaBase.UsuarioId`); `UserData` guarda su nombre al iniciar sesión, pero la barra muestra el de la base de datos (`UsuarioActual`), que puede haber cambiado en «Mi perfil».
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
- **Anillos («quesos»)**, `anillo()`: solo para parte del total, **≤ 6 porciones** (con más, las 5 mayores y el resto en «Otras», gris); siempre leyenda con valor y %, centro con el total (al señalar: % y nombre), leyenda navegable con teclado y vista de tabla con todas las filas. Colores categóricos `--cat-1…5` en **orden fijo** (`.viz-root`), validados con el script de la guía **en ciclo** (la última porción toca a la primera) para 2–5 colores con y sin «Otras»; si se cambian, volver a validarlos así. El anillo de prioridad usa los colores del tablero (Alta `#d03b3b`, Media `#fab219`, Baja azul `--prioridad-baja` `#1f6fc5`) con icono ▲ ◆ ▼.
- `barras()` admite por fila `color` (muestra `.graf-muestra` junto al nombre, validada `#rrggbb`) y `url` (solo rutas locales `/...`). En «Tarjetas por etiqueta» la barra sigue en color de serie 1 y el color de la etiqueta solo va en la muestra.

### 9. Chat y peticiones AJAX

- El chat de la tarjeta usa la tabla `Comentarios`. Los mensajes **no** se registran en el historial ni cambian la fecha de modificación (son conversación). Las entradas "comentó" antiguas del historial se siguen mostrando.
- `Chat.ashx` comprueba en cada petición que el usuario pertenece al tablero; el POST exige el token `Seguridad.TokenAntiCsrf` en la cabecera `X-Chat-Token` y que `Origin` (si viene) sea la propia web. Lee el texto con `Request.Unvalidated.Form` (admite `<`) y los parámetros solo de la URL o del cuerpo, nunca de `Request(...)` (incluye cookies).
- Cualquier endpoint AJAX nuevo que modifique datos debe seguir el mismo patrón (token + comprobación de acceso), como `Tarjeta.ashx` (reutiliza el token y la cabecera `X-Chat-Token`).

## Convenciones de código

- `Option Explicit On` (ya activo en `Web.config`); escribir código compatible con `Option Strict On` cuando sea posible.
- Separar acceso a datos (clases en `Datos/`) de la lógica de las páginas; el code-behind no debe contener SQL.
- Operaciones de varias sentencias (mover/eliminar con renumeración de `Posicion`) en un único lote con `SET XACT_ABORT ON; BEGIN TRAN; … COMMIT;`.
- En VB, para leer columnas nulas usar `Db.Texto/Fecha/Entero` (evita el `If(cond, Nothing, valor)` que convierte `Nothing` en `DateTime.MinValue`/`0`).
- Comentarios y textos de la interfaz en español.
- **Versión**: en cada cambio, subir la versión en `My Project/AssemblyInfo.vb` (las tres: `AssemblyVersion`, `AssemblyFileVersion`, `AssemblyInformationalVersion` con `-beta`). Funcionalidad nueva → menor (0.9.0 → 0.10.0); corrección o ajuste → parche (0.10.0 → 0.10.1). No pasar a 1.0 sin decisión expresa.

## Compilar y ejecutar

- Abrir `TaskManager.slnx` en Visual Studio y ejecutar con IIS Express (F5).
- Desde línea de comandos: `msbuild TaskManager.vbproj` (restaurar antes los paquetes NuGet con `nuget restore`).
- Requiere que exista `connectionStrings.config` local y la base de datos TaskManager creada con el script de `database/`.
