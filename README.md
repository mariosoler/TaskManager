# TaskManager

Gestor de tareas tipo Trello (tableros, listas y tarjetas) en ASP.NET Web Forms (VB.NET, .NET Framework 4.8.1) y SQL Server.

## Publicar en un IIS

1. **Base de datos.** En el SQL Server de destino, crear la base `TaskManager` y ejecutar `database/TaskManager.sql`.
   Si la base ya existía, ejecutar `database/migraciones_todas.sql` (todas las migraciones en orden, en un solo
   script) o, una a una y en orden, las de `database/migraciones/` que falten (son idempotentes: se pueden repetir
   sin problema).
   La carpeta `database/` (con su `LEEME.txt`) también se copia en la publicación, para tener los scripts a mano
   en el servidor; `Web.config` la oculta, así que no se puede descargar desde el navegador.
2. **Publicar.** En Visual Studio: clic derecho en el proyecto → *Publicar* → perfil de carpeta (configuración Release).
   El `Web.config` publicado lleva dentro, con su explicación, tres marcadores para sustituir en el servidor:
   - `[SERVIDOR_SQL]` en la cadena de conexión `TaskManager` (conexión a la base de datos).
   - `[RUTA_CARPETA_FONDOS]` en `RutaFondosTableros` (carpeta de las imágenes de fondo que suben los usuarios).
   - `[RUTA_CARPETA_ADJUNTOS]` en `RutaAdjuntosTarjetas` (carpeta de los archivos adjuntos de las tarjetas).

   Si se olvida alguno, la aplicación muestra un error que indica qué falta. Cada publicación genera el `Web.config`
   otra vez con los marcadores: al actualizar un servidor ya configurado, conserva su `Web.config` (no lo sustituyas
   al copiar) o vuelve a poner los valores.
3. **IIS.**
   - Grupo de aplicaciones con .NET CLR v4.0 y canalización integrada (requiere ASP.NET 4.8 instalado).
   - Enlace **HTTPS**: la cookie de sesión solo viaja por HTTPS, sin él no se puede iniciar sesión.
   - Con autenticación de Windows en la cadena de conexión, crear en SQL Server un inicio de sesión para la
     identidad del grupo de aplicaciones (p. ej. `IIS APPPOOL\TaskManager`) con permisos de lectura y escritura
     sobre la base.
   - Dar a esa identidad permiso de **modificación** sobre las carpetas de fondos y de adjuntos. Se recomiendan rutas
     absolutas fuera de la carpeta del sitio (p. ej. `D:\Datos\TaskManager\Fondos` y `D:\Datos\TaskManager\Adjuntos`)
     para no perder las imágenes al publicar.

En desarrollo, la cadena de conexión está en `connectionStrings.config` (local, excluido de git; plantilla
`connectionStrings.example.config`). No guardes contraseñas en el repositorio.
