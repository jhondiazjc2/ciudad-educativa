-- Migracion: PKs surrogate (Id) + UNIQUE en identificadores de negocio
-- Ejecutar en SSMS sobre CiudadEducativa. Idempotente.

USE CiudadEducativa;
GO

SET DEADLOCK_PRIORITY LOW;
GO

/* ========== COLEGIOS ========== */
IF COL_LENGTH('Colegios', 'Id') IS NULL
    ALTER TABLE Colegios ADD Id INT IDENTITY(1,1) NOT NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_Colegios_CodigoDane' AND object_id = OBJECT_ID('Colegios')
)
    CREATE UNIQUE INDEX UQ_Colegios_CodigoDane ON Colegios(CodigoDane);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints k
    INNER JOIN sys.index_columns ic ON ic.object_id = k.parent_object_id AND ic.index_id = k.unique_index_id
    INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE k.parent_object_id = OBJECT_ID('Colegios') AND k.type = 'PK' AND c.name = 'Id'
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Grupos_Colegios')
        ALTER TABLE Grupos DROP CONSTRAINT FK_Grupos_Colegios;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Matriculas_Colegios')
        ALTER TABLE Matriculas DROP CONSTRAINT FK_Matriculas_Colegios;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocenteColegios_Colegios')
        ALTER TABLE DocenteColegios DROP CONSTRAINT FK_DocenteColegios_Colegios;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Usuarios_Colegios')
        ALTER TABLE Usuarios DROP CONSTRAINT FK_Usuarios_Colegios;

    DECLARE @pkColegio SYSNAME;
    SELECT @pkColegio = name FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID('Colegios') AND type = 'PK';
    IF @pkColegio IS NOT NULL
        EXEC('ALTER TABLE Colegios DROP CONSTRAINT ' + @pkColegio);

    ALTER TABLE Colegios ADD CONSTRAINT PK_Colegios PRIMARY KEY (Id);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Grupos_Colegios')
        ALTER TABLE Grupos ADD CONSTRAINT FK_Grupos_Colegios
            FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Matriculas_Colegios')
        ALTER TABLE Matriculas ADD CONSTRAINT FK_Matriculas_Colegios
            FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocenteColegios_Colegios')
        ALTER TABLE DocenteColegios ADD CONSTRAINT FK_DocenteColegios_Colegios
            FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Usuarios_Colegios')
        ALTER TABLE Usuarios ADD CONSTRAINT FK_Usuarios_Colegios
            FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane);
END
GO

/* ========== DOCENTES ========== */
IF COL_LENGTH('Docentes', 'Id') IS NULL
    ALTER TABLE Docentes ADD Id INT IDENTITY(1,1) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocenteColegios_Docentes')
    ALTER TABLE DocenteColegios DROP CONSTRAINT FK_DocenteColegios_Docentes;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Grupos_Docentes')
    ALTER TABLE Grupos DROP CONSTRAINT FK_Grupos_Docentes;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints k
    INNER JOIN sys.index_columns ic ON ic.object_id = k.parent_object_id AND ic.index_id = k.unique_index_id
    INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE k.parent_object_id = OBJECT_ID('Docentes') AND k.type = 'PK' AND c.name = 'Id'
)
BEGIN
    DECLARE @pkDocente SYSNAME;
    SELECT @pkDocente = name FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID('Docentes') AND type = 'PK';
    IF @pkDocente IS NOT NULL
        EXEC('ALTER TABLE Docentes DROP CONSTRAINT ' + @pkDocente);
    ALTER TABLE Docentes ADD CONSTRAINT PK_Docentes PRIMARY KEY (Id);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_Docentes_Documento' AND object_id = OBJECT_ID('Docentes')
)
    CREATE UNIQUE INDEX UQ_Docentes_Documento ON Docentes(TipoDocumento, NumeroDocumento);
GO

/* ========== DOCENTE COLEGIOS ========== */
IF COL_LENGTH('DocenteColegios', 'DocenteId') IS NULL
    ALTER TABLE DocenteColegios ADD DocenteId INT NULL;
GO

IF COL_LENGTH('DocenteColegios', 'DocenteTipoDocumento') IS NOT NULL
BEGIN
    UPDATE dc SET DocenteId = d.Id
    FROM DocenteColegios dc
    INNER JOIN Docentes d ON d.TipoDocumento = dc.DocenteTipoDocumento
        AND d.NumeroDocumento = dc.DocenteNumeroDocumento
    WHERE dc.DocenteId IS NULL;

    ALTER TABLE DocenteColegios DROP COLUMN DocenteTipoDocumento;
END
GO

IF COL_LENGTH('DocenteColegios', 'DocenteNumeroDocumento') IS NOT NULL
    ALTER TABLE DocenteColegios DROP COLUMN DocenteNumeroDocumento;
GO

IF EXISTS (SELECT 1 FROM DocenteColegios WHERE DocenteId IS NULL)
    THROW 50001, 'Hay asignaciones docente-colegio sin DocenteId.', 1;
GO

ALTER TABLE DocenteColegios ALTER COLUMN DocenteId INT NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_DocenteColegio')
BEGIN
    ALTER TABLE DocenteColegios DROP CONSTRAINT UQ_DocenteColegio;
    ALTER TABLE DocenteColegios ADD CONSTRAINT UQ_DocenteColegio UNIQUE (DocenteId, CodigoDane);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_DocenteColegios_Docentes')
    ALTER TABLE DocenteColegios ADD CONSTRAINT FK_DocenteColegios_Docentes
        FOREIGN KEY (DocenteId) REFERENCES Docentes(Id);
GO

/* ========== GRUPOS ========== */
IF COL_LENGTH('Grupos', 'DocenteDirectorId') IS NULL
    ALTER TABLE Grupos ADD DocenteDirectorId INT NULL;
GO

IF COL_LENGTH('Grupos', 'DocenteDirectorTipoDocumento') IS NOT NULL
BEGIN
    UPDATE g SET DocenteDirectorId = d.Id
    FROM Grupos g
    INNER JOIN Docentes d ON d.TipoDocumento = g.DocenteDirectorTipoDocumento
        AND d.NumeroDocumento = g.DocenteDirectorNumeroDocumento
    WHERE g.DocenteDirectorTipoDocumento IS NOT NULL AND g.DocenteDirectorId IS NULL;

    ALTER TABLE Grupos DROP COLUMN DocenteDirectorTipoDocumento;
END
GO

IF COL_LENGTH('Grupos', 'DocenteDirectorNumeroDocumento') IS NOT NULL
    ALTER TABLE Grupos DROP COLUMN DocenteDirectorNumeroDocumento;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Grupos_Docentes')
    ALTER TABLE Grupos ADD CONSTRAINT FK_Grupos_Docentes
        FOREIGN KEY (DocenteDirectorId) REFERENCES Docentes(Id);
GO

/* ========== ESTUDIANTES ========== */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'UQ_Estudiantes_Documento' AND object_id = OBJECT_ID('Estudiantes')
)
AND NOT EXISTS (
    SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_Estudiantes_Documento'
)
    CREATE UNIQUE INDEX UQ_Estudiantes_Documento ON Estudiantes(TipoDocumento, NumeroDocumento);
GO

PRINT 'Migracion PK surrogate completada.';
GO
