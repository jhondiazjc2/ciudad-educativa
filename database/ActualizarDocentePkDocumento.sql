-- Migracion: PK de Docentes por documento de identidad (TipoDocumento + NumeroDocumento)
-- Ejecutar en SSMS sobre CiudadEducativa. Idempotente.

USE CiudadEducativa;
GO

IF COL_LENGTH('Docentes', 'TipoDocumento') IS NULL
BEGIN
    ALTER TABLE Docentes ADD TipoDocumento NVARCHAR(5) NULL;
    PRINT 'Columna TipoDocumento agregada a Docentes.';
END
GO

IF COL_LENGTH('Docentes', 'NumeroDocumento') IS NULL
BEGIN
    ALTER TABLE Docentes ADD NumeroDocumento NVARCHAR(20) NULL;
    PRINT 'Columna NumeroDocumento agregada a Docentes.';
END
GO

IF COL_LENGTH('Docentes', 'Id') IS NOT NULL
BEGIN
    UPDATE Docentes
    SET
        TipoDocumento = CASE
            WHEN Nombre LIKE N'Prof. Demo %' THEN N'CC'
            ELSE ISNULL(TipoDocumento, N'CC')
        END,
        NumeroDocumento = CASE
            WHEN Nombre LIKE N'Prof. Demo %' THEN
                N'901' + RIGHT(N'0000000' + CAST(
                    TRY_CAST(LTRIM(RTRIM(REPLACE(Nombre, N'Prof. Demo ', N''))) AS INT) AS NVARCHAR(7)), 7)
            WHEN NumeroDocumento IS NULL THEN
                N'800' + RIGHT(N'0000000' + CAST(Id AS NVARCHAR(10)), 7)
            ELSE NumeroDocumento
        END
    WHERE TipoDocumento IS NULL OR NumeroDocumento IS NULL;

    UPDATE d SET
        TipoDocumento = N'CC',
        NumeroDocumento = N'52890123'
    FROM Docentes d WHERE d.Nombre = N'Maria Gonzalez' AND d.NumeroDocumento LIKE N'800%';

    UPDATE d SET TipoDocumento = N'CC', NumeroDocumento = N'80123456'
    FROM Docentes d WHERE d.Nombre = N'Carlos Ruiz' AND d.NumeroDocumento LIKE N'800%';

    UPDATE d SET TipoDocumento = N'CC', NumeroDocumento = N'52987654'
    FROM Docentes d WHERE d.Nombre = N'Ana Martinez' AND d.NumeroDocumento LIKE N'800%';

    UPDATE d SET TipoDocumento = N'CC', NumeroDocumento = N'79456123'
    FROM Docentes d WHERE d.Nombre = N'Pedro Lopez' AND d.NumeroDocumento LIKE N'800%';

    UPDATE d SET TipoDocumento = N'CC', NumeroDocumento = N'1034567890'
    FROM Docentes d WHERE d.Nombre = N'Laura Torres' AND d.NumeroDocumento LIKE N'800%';

    UPDATE d SET TipoDocumento = N'CC', NumeroDocumento = N'80765432'
    FROM Docentes d WHERE d.Nombre = N'Jorge Herrera' AND d.NumeroDocumento LIKE N'800%';
END
GO

IF COL_LENGTH('DocenteColegios', 'DocenteTipoDocumento') IS NULL
BEGIN
    ALTER TABLE DocenteColegios ADD DocenteTipoDocumento NVARCHAR(5) NULL;
    ALTER TABLE DocenteColegios ADD DocenteNumeroDocumento NVARCHAR(20) NULL;
    PRINT 'Columnas de documento agregadas a DocenteColegios.';
END
GO

IF COL_LENGTH('DocenteColegios', 'DocenteId') IS NOT NULL
BEGIN
    UPDATE dc SET
        DocenteTipoDocumento = d.TipoDocumento,
        DocenteNumeroDocumento = d.NumeroDocumento
    FROM DocenteColegios dc
    INNER JOIN Docentes d ON d.Id = dc.DocenteId
    WHERE dc.DocenteTipoDocumento IS NULL;
END
ELSE IF EXISTS (
    SELECT 1 FROM DocenteColegios
    WHERE DocenteTipoDocumento IS NULL OR DocenteNumeroDocumento IS NULL
)
BEGIN
    UPDATE dc SET
        DocenteTipoDocumento = d.TipoDocumento,
        DocenteNumeroDocumento = d.NumeroDocumento
    FROM DocenteColegios dc
    INNER JOIN Docentes d ON d.TipoDocumento = dc.DocenteTipoDocumento
        AND d.NumeroDocumento = dc.DocenteNumeroDocumento;
END
GO

IF COL_LENGTH('Grupos', 'DocenteDirectorTipoDocumento') IS NULL
BEGIN
    ALTER TABLE Grupos ADD DocenteDirectorTipoDocumento NVARCHAR(5) NULL;
    ALTER TABLE Grupos ADD DocenteDirectorNumeroDocumento NVARCHAR(20) NULL;
    PRINT 'Columnas de documento agregadas a Grupos.';
END
GO

IF COL_LENGTH('Grupos', 'DocenteDirectorId') IS NOT NULL
BEGIN
    UPDATE g SET
        DocenteDirectorTipoDocumento = d.TipoDocumento,
        DocenteDirectorNumeroDocumento = d.NumeroDocumento
    FROM Grupos g
    INNER JOIN Docentes d ON d.Id = g.DocenteDirectorId
    WHERE g.DocenteDirectorId IS NOT NULL;
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocenteColegios_Docentes')
    ALTER TABLE DocenteColegios DROP CONSTRAINT FK_DocenteColegios_Docentes;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Grupos_Docentes')
    ALTER TABLE Grupos DROP CONSTRAINT FK_Grupos_Docentes;
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_DocenteColegio')
    ALTER TABLE DocenteColegios DROP CONSTRAINT UQ_DocenteColegio;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_DocenteColegio' AND object_id = OBJECT_ID('DocenteColegios'))
    DROP INDEX UQ_DocenteColegio ON DocenteColegios;
GO

IF COL_LENGTH('DocenteColegios', 'DocenteId') IS NOT NULL
    ALTER TABLE DocenteColegios DROP COLUMN DocenteId;
GO

IF COL_LENGTH('Grupos', 'DocenteDirectorId') IS NOT NULL
    ALTER TABLE Grupos DROP COLUMN DocenteDirectorId;
GO

ALTER TABLE DocenteColegios ALTER COLUMN DocenteTipoDocumento NVARCHAR(5) NOT NULL;
ALTER TABLE DocenteColegios ALTER COLUMN DocenteNumeroDocumento NVARCHAR(20) NOT NULL;
GO

ALTER TABLE Docentes ALTER COLUMN TipoDocumento NVARCHAR(5) NOT NULL;
ALTER TABLE Docentes ALTER COLUMN NumeroDocumento NVARCHAR(20) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE type = 'PK' AND parent_object_id = OBJECT_ID('Docentes') AND name <> 'PK_Docentes')
BEGIN
    DECLARE @pkDocentes NVARCHAR(200);
    SELECT @pkDocentes = name FROM sys.key_constraints
    WHERE type = 'PK' AND parent_object_id = OBJECT_ID('Docentes');
    EXEC('ALTER TABLE Docentes DROP CONSTRAINT ' + @pkDocentes);
END
GO

IF COL_LENGTH('Docentes', 'Id') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Docentes' AND parent_object_id = OBJECT_ID('Docentes'))
        ALTER TABLE Docentes DROP CONSTRAINT PK_Docentes;

    ALTER TABLE Docentes DROP COLUMN Id;
    PRINT 'Columna Id eliminada de Docentes.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_Docentes' AND parent_object_id = OBJECT_ID('Docentes'))
    ALTER TABLE Docentes ADD CONSTRAINT PK_Docentes PRIMARY KEY (TipoDocumento, NumeroDocumento);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocenteColegios_Docentes')
    ALTER TABLE DocenteColegios ADD CONSTRAINT FK_DocenteColegios_Docentes
        FOREIGN KEY (DocenteTipoDocumento, DocenteNumeroDocumento)
        REFERENCES Docentes(TipoDocumento, NumeroDocumento);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Grupos_Docentes')
    ALTER TABLE Grupos ADD CONSTRAINT FK_Grupos_Docentes
        FOREIGN KEY (DocenteDirectorTipoDocumento, DocenteDirectorNumeroDocumento)
        REFERENCES Docentes(TipoDocumento, NumeroDocumento);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = 'UQ_DocenteColegio' AND parent_object_id = OBJECT_ID('DocenteColegios')
)
    ALTER TABLE DocenteColegios ADD CONSTRAINT UQ_DocenteColegio
        UNIQUE (DocenteTipoDocumento, DocenteNumeroDocumento, CodigoDane);
GO

ALTER TABLE Docentes DROP CONSTRAINT IF EXISTS CK_Docentes_TipoDocumento;
ALTER TABLE Docentes ADD CONSTRAINT CK_Docentes_TipoDocumento
    CHECK (TipoDocumento IN ('RC', 'TI', 'CC', 'CE', 'PA'));
GO

PRINT 'Migracion PK Docentes por documento completada.';
GO

SELECT TOP 5 TipoDocumento, NumeroDocumento, Nombre FROM Docentes ORDER BY Nombre;
GO
