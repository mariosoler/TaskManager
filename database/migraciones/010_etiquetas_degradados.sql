-- Migración 010: más colores de etiqueta y degradados.
-- Etiquetas.Color pasa a admitir "grad:<clave>" (degradados de la lista blanca Models/ColorEtiqueta.vb) además de
-- "#rrggbb": se amplía la columna a NVARCHAR(30) y se cambia la restricción CK_Etiquetas_Color.
-- Idempotente: se puede ejecutar varias veces sobre la base TaskManager.

IF COL_LENGTH('dbo.Etiquetas', 'Color') < 60   -- en bytes: NVARCHAR(30) = 60
BEGIN
    -- La restricción y el valor por defecto dependen de la columna: se quitan para ampliarla y se vuelven a crear.
    IF OBJECT_ID('dbo.CK_Etiquetas_Color', 'C') IS NOT NULL
        ALTER TABLE dbo.Etiquetas DROP CONSTRAINT CK_Etiquetas_Color;

    DECLARE @Predeterminado SYSNAME = (
        SELECT dc.name FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.Etiquetas') AND c.name = 'Color');
    IF @Predeterminado IS NOT NULL
    BEGIN
        -- El nombre sale del catálogo (no de datos del usuario) y va entre corchetes con QUOTENAME.
        DECLARE @Sql NVARCHAR(400) = N'ALTER TABLE dbo.Etiquetas DROP CONSTRAINT ' + QUOTENAME(@Predeterminado);
        EXEC sp_executesql @Sql;
    END

    ALTER TABLE dbo.Etiquetas ALTER COLUMN Color NVARCHAR(30) NOT NULL;
    ALTER TABLE dbo.Etiquetas ADD CONSTRAINT DF_Etiquetas_Color DEFAULT '#1f6fc5' FOR Color;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = 'CK_Etiquetas_Color' AND definition LIKE '%grad:%')
BEGIN
    IF OBJECT_ID('dbo.CK_Etiquetas_Color', 'C') IS NOT NULL
        ALTER TABLE dbo.Etiquetas DROP CONSTRAINT CK_Etiquetas_Color;
    ALTER TABLE dbo.Etiquetas ADD CONSTRAINT CK_Etiquetas_Color CHECK (
        Color LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'
        OR (Color LIKE 'grad:_%' AND Color NOT LIKE 'grad:%[^a-z0-9-]%'));
END
GO
