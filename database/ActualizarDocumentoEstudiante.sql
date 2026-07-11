-- Migra Estudiantes de NumeroMatricula a TipoDocumento + NumeroDocumento.
-- Ejecutar en SSMS sobre CiudadEducativa si la BD fue creada con el esquema anterior.
-- Seguro de re-ejecutar: omite pasos ya aplicados.

USE CiudadEducativa;
GO

IF COL_LENGTH('Estudiantes', 'NumeroMatricula') IS NULL
BEGIN
    PRINT 'La tabla Estudiantes ya usa documento de identidad.';
    RETURN;
END
GO

IF COL_LENGTH('Estudiantes', 'TipoDocumento') IS NULL
    ALTER TABLE Estudiantes ADD TipoDocumento NVARCHAR(5) NULL;

IF COL_LENGTH('Estudiantes', 'NumeroDocumento') IS NULL
    ALTER TABLE Estudiantes ADD NumeroDocumento NVARCHAR(20) NULL;
GO

UPDATE Estudiantes
SET TipoDocumento = CASE
        WHEN TipoDocumento IS NOT NULL THEN TipoDocumento
        WHEN YEAR(FechaNacimiento) >= YEAR(GETDATE()) - 7 THEN 'RC'
        WHEN YEAR(FechaNacimiento) >= YEAR(GETDATE()) - 17 THEN 'TI'
        ELSE 'CC'
    END,
    NumeroDocumento = CASE
        WHEN NumeroDocumento IS NOT NULL THEN NumeroDocumento
        ELSE REPLACE(NumeroMatricula, 'MAT-', '')
    END
WHERE TipoDocumento IS NULL OR NumeroDocumento IS NULL;
GO

ALTER TABLE Estudiantes ALTER COLUMN TipoDocumento NVARCHAR(5) NOT NULL;
ALTER TABLE Estudiantes ALTER COLUMN NumeroDocumento NVARCHAR(20) NOT NULL;
GO

-- En SQL Server la unicidad de NumeroMatricula es una CONSTRAINT, no un indice suelto.
DECLARE @constraint SYSNAME;

SELECT @constraint = kc.name
FROM sys.key_constraints kc
JOIN sys.index_columns ic
    ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
JOIN sys.columns c
    ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE kc.parent_object_id = OBJECT_ID('Estudiantes')
  AND kc.type = 'UQ'
  AND c.name = 'NumeroMatricula';

IF @constraint IS NOT NULL
BEGIN
    DECLARE @sql NVARCHAR(400) = N'ALTER TABLE Estudiantes DROP CONSTRAINT [' + @constraint + N']';
    EXEC sp_executesql @sql;
    PRINT 'Restriccion eliminada: ' + @constraint;
END
GO

IF COL_LENGTH('Estudiantes', 'NumeroMatricula') IS NOT NULL
    ALTER TABLE Estudiantes DROP COLUMN NumeroMatricula;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Estudiantes_TipoDocumento' AND parent_object_id = OBJECT_ID('Estudiantes')
)
    ALTER TABLE Estudiantes ADD CONSTRAINT CK_Estudiantes_TipoDocumento
        CHECK (TipoDocumento IN ('RC', 'TI', 'CC', 'CE', 'PA'));
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = 'UQ_Estudiantes_Documento' AND parent_object_id = OBJECT_ID('Estudiantes')
)
    ALTER TABLE Estudiantes ADD CONSTRAINT UQ_Estudiantes_Documento
        UNIQUE (TipoDocumento, NumeroDocumento);
GO

PRINT 'Migracion de documento completada.';
GO
