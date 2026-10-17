# TaskManager

Gestor de tareas tipo Trello (tableros, listas y tarjetas) en ASP.NET Web Forms (VB.NET, .NET Framework 4.8.1) y SQL Server.

## Publicar en un IIS

1. **Base de datos.** En el SQL Server de destino, crear la base `TaskManager` y ejecutar `database/TaskManager.sql`.
   Si la base ya existía, ejecutar en orden las migraciones de `database/migraciones/` que falten
   (son idempotentes: se pueden repetir sin problema).
2. **Publicar.** En Visual Studio: clic derecho en el proyecto → *Publicar* → perfil de carpeta.
   La primera vez se crean en la carpeta de destino, a partir de las plantillas `*.example.config`:
   - `connectionStrings.config` — conexión a la base de datos.
   - `appSettings.config` — ajustes del servidor, como la carpeta de las imágenes de fondo (`RutaFondosTableros`).

   Edítalos con los datos del servidor. En las siguientes publicaciones **no se sobrescriben**
   (el perfil debe mantener *Eliminar los archivos existentes* desactivado). Al copiar a mano la carpeta
   publicada a otro servidor, no sustituyas esos dos archivos si allí ya están configurados.
3. **IIS.**
   - Grupo de aplicaciones con .NET CLR v4.0 y canalización integrada (requiere ASP.NET 4.8 instalado).
   - Enlace **HTTPS**: la cookie de sesión solo viaja por HTTPS, sin él no se puede iniciar sesión.
   - Con autenticación de Windows en la cadena de conexión, crear en SQL Server un inicio de sesión para la
     identidad del grupo de aplicaciones (p. ej. `IIS APPPOOL\TaskManager`) con permisos de lectura y escritura
     sobre la base.
   - Dar a esa identidad permiso de **modificación** sobre la carpeta de fondos. Se recomienda una ruta absoluta
     fuera de la carpeta del sitio (p. ej. `D:\Datos\TaskManager\Fondos`) para no perder las imágenes al publicar.

Los archivos `connectionStrings.config` y `appSettings.config` son locales: están excluidos de git y nunca se
publican los del equipo de desarrollo. No guardes contraseñas en el repositorio.
