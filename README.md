# TaskManager

Gestor de tareas tipo Trello (tableros, listas y tarjetas) en ASP.NET Web Forms (VB.NET, .NET Framework 4.8.1) y SQL Server.

## Publicar en un IIS

1. **Base de datos.** En el SQL Server de destino, crear la base `TaskManager` y ejecutar `database/TaskManager.sql`.
   Si la base ya existía, ejecutar en orden las migraciones de `database/migraciones/` que falten
   (son idempotentes: se pueden repetir sin problema).
2. **Publicar.** En Visual Studio: clic derecho en el proyecto → *Publicar* → perfil de carpeta (configuración Release).
   El `Web.config` publicado lleva dentro, con su explicación, dos marcadores para sustituir en el servidor:
   - `[SERVIDOR_SQL]` en la cadena de conexión `TaskManager` (conexión a la base de datos).
   - `[RUTA_CARPETA_FONDOS]` en `RutaFondosTableros` (carpeta de las imágenes de fondo que suben los usuarios).

   Si se olvida alguno, la aplicación muestra un error que indica qué falta. Cada publicación genera el `Web.config`
   otra vez con los marcadores: al actualizar un servidor ya configurado, conserva su `Web.config` (no lo sustituyas
   al copiar) o vuelve a poner los valores.
3. **IIS.**
   - Grupo de aplicaciones con .NET CLR v4.0 y canalización integrada (requiere ASP.NET 4.8 instalado).
   - Enlace **HTTPS**: la cookie de sesión solo viaja por HTTPS, sin él no se puede iniciar sesión.
   - Con autenticación de Windows en la cadena de conexión, crear en SQL Server un inicio de sesión para la
     identidad del grupo de aplicaciones (p. ej. `IIS APPPOOL\TaskManager`) con permisos de lectura y escritura
     sobre la base.
   - Dar a esa identidad permiso de **modificación** sobre la carpeta de fondos. Se recomienda una ruta absoluta
     fuera de la carpeta del sitio (p. ej. `D:\Datos\TaskManager\Fondos`) para no perder las imágenes al publicar.

En desarrollo, la cadena de conexión está en `connectionStrings.config` (local, excluido de git; plantilla
`connectionStrings.example.config`). No guardes contraseñas en el repositorio.
