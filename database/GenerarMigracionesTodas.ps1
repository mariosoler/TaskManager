# Genera migraciones_todas.sql: todas las migraciones de la carpeta "migraciones" en orden (001, 002...) en un solo
# script, para actualizar una base existente de una vez. Las migraciones siguen sueltas para pasarlas una a una.
# Volver a ejecutarlo cada vez que se añada una migración:
#
#   powershell -ExecutionPolicy Bypass -File database\GenerarMigracionesTodas.ps1
#
# Todas las migraciones son idempotentes, así que el script conjunto se puede ejecutar entero sobre cualquier base
# (las que ya estén aplicadas no cambian nada).

$ErrorActionPreference = 'Stop'
$carpeta = Split-Path -Parent $MyInvocation.MyCommand.Path
$salida = Join-Path $carpeta 'migraciones_todas.sql'
$utf8 = New-Object System.Text.UTF8Encoding($true)   # con BOM: sqlcmd y SSMS leen bien los acentos

$migraciones = Get-ChildItem -Path (Join-Path $carpeta 'migraciones') -Filter '*.sql' |
    Where-Object { $_.Name -match '^\d{3}_' } | Sort-Object Name

$texto = New-Object System.Text.StringBuilder
[void]$texto.AppendLine('-- =====================================================================================')
[void]$texto.AppendLine('-- TODAS LAS MIGRACIONES de la base TaskManager, en orden, en un solo script.')
[void]$texto.AppendLine('-- GENERADO por database\GenerarMigracionesTodas.ps1: no editar a mano (los cambios se perderían);')
[void]$texto.AppendLine('-- se edita cada migración en database\migraciones y se vuelve a generar.')
[void]$texto.AppendLine('-- Todas son idempotentes: se puede ejecutar entero sobre una base existente aunque ya tenga')
[void]$texto.AppendLine('-- algunas aplicadas. Para pasarlas una a una, usar los archivos de database\migraciones.')
[void]$texto.AppendLine('--')
[void]$texto.AppendLine('--   sqlcmd -S <servidor> -d TaskManager -E -b -i migraciones_todas.sql')
[void]$texto.AppendLine('--')
[void]$texto.AppendLine('-- Incluye:')
foreach ($m in $migraciones) { [void]$texto.AppendLine('--   ' + $m.Name) }
[void]$texto.AppendLine('-- =====================================================================================')
[void]$texto.AppendLine('SET NOCOUNT ON;')
[void]$texto.AppendLine('GO')

foreach ($m in $migraciones) {
    $contenido = [System.IO.File]::ReadAllText($m.FullName, [System.Text.Encoding]::UTF8).TrimStart([char]0xFEFF).TrimEnd()
    if ($contenido -notmatch '(?m)^GO\s*$' -or $contenido -notmatch 'GO$') {
        throw "La migración $($m.Name) debe terminar en GO para poder unirla a las demás."
    }
    [void]$texto.AppendLine('')
    [void]$texto.AppendLine('-- -------------------------------------------------------------------------------------')
    [void]$texto.AppendLine('-- ' + $m.Name)
    [void]$texto.AppendLine('-- -------------------------------------------------------------------------------------')
    [void]$texto.AppendLine("PRINT N'Migración " + $m.Name + "';")
    [void]$texto.AppendLine('GO')
    [void]$texto.AppendLine($contenido)
}
[void]$texto.AppendLine('')
[void]$texto.AppendLine("PRINT N'Migraciones aplicadas.';")
[void]$texto.AppendLine('GO')

# Saltos de línea de Windows (CRLF), como el resto de scripts.
$final = ($texto.ToString() -replace "`r?`n", "`r`n")
[System.IO.File]::WriteAllText($salida, $final, $utf8)
Write-Host ("Generado {0} con {1} migraciones." -f $salida, $migraciones.Count)
