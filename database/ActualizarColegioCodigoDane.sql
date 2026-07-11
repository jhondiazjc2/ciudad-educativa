-- Migracion: Colegios.Id INT -> CodigoDane NVARCHAR(12) como PK
-- Ejecutar en SSMS sobre CiudadEducativa con datos existentes.
-- IMPORTANTE: Ejecutar todo el script (F5). Usa GO entre pasos.
-- Los pasos que referencian columnas antiguas usan SQL dinamico (Msg 207 si no).

USE CiudadEducativa;
GO

IF COL_LENGTH('Colegios', 'CodigoDane') IS NOT NULL
   AND COL_LENGTH('Colegios', 'Id') IS NULL
   AND COL_LENGTH('Matriculas', 'ColegioId') IS NULL
   AND COL_LENGTH('DocenteColegios', 'ColegioId') IS NULL
   AND COL_LENGTH('Usuarios', 'ColegioId') IS NULL
BEGIN
    PRINT 'La base de datos ya usa CodigoDane como PK. No se requiere migracion.';
END
ELSE IF COL_LENGTH('Colegios', 'Id') IS NULL
   AND COL_LENGTH('Colegios', 'CodigoDane') IS NULL
BEGIN
    RAISERROR('La tabla Colegios no tiene Id ni CodigoDane. Revise el esquema manualmente.', 16, 1);
END
ELSE IF COL_LENGTH('Colegios', 'Id') IS NULL
BEGIN
    PRINT 'Completando migracion pendiente...';
END
ELSE
BEGIN
    PRINT 'Iniciando migracion CodigoDane...';
END
GO

IF COL_LENGTH('Colegios', 'Id') IS NOT NULL
   AND COL_LENGTH('Colegios', 'CodigoDane') IS NULL
BEGIN
    IF OBJECT_ID('FK_DocenteColegios_Colegios', 'F') IS NOT NULL
        ALTER TABLE DocenteColegios DROP CONSTRAINT FK_DocenteColegios_Colegios;
    IF OBJECT_ID('FK_Matriculas_Colegios', 'F') IS NOT NULL
        ALTER TABLE Matriculas DROP CONSTRAINT FK_Matriculas_Colegios;
    IF OBJECT_ID('FK_Usuarios_Colegios', 'F') IS NOT NULL
        ALTER TABLE Usuarios DROP CONSTRAINT FK_Usuarios_Colegios;

    ALTER TABLE Colegios ADD CodigoDane NVARCHAR(12) NULL;
    PRINT 'Columna CodigoDane agregada a Colegios.';
END
GO

IF COL_LENGTH('Colegios', 'Id') IS NOT NULL
   AND COL_LENGTH('Colegios', 'CodigoDane') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE Colegios SET CodigoDane = CASE Id
            WHEN 1 THEN N''105001000001''
            WHEN 2 THEN N''105001000002''
            WHEN 3 THEN N''105001000003''
            WHEN 4 THEN N''105001000004''
            WHEN 5 THEN N''105001000005''
            WHEN 6 THEN N''105001000006''
            ELSE RIGHT(N''000000000000'' + CAST(Id AS NVARCHAR(12)), 12)
        END
        WHERE CodigoDane IS NULL;
        ALTER TABLE Colegios ALTER COLUMN CodigoDane NVARCHAR(12) NOT NULL;
    ');
    PRINT 'CodigoDane poblado en Colegios.';
END
GO

IF COL_LENGTH('Matriculas', 'ColegioId') IS NOT NULL
   AND COL_LENGTH('Matriculas', 'CodigoDane') IS NULL
BEGIN
    ALTER TABLE Matriculas ADD CodigoDane NVARCHAR(12) NULL;
END
GO

IF COL_LENGTH('Matriculas', 'ColegioId') IS NOT NULL
   AND COL_LENGTH('Matriculas', 'CodigoDane') IS NOT NULL
   AND COL_LENGTH('Colegios', 'Id') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE m SET CodigoDane = c.CodigoDane
        FROM Matriculas m
        INNER JOIN Colegios c ON m.ColegioId = c.Id
        WHERE m.CodigoDane IS NULL;
        ALTER TABLE Matriculas ALTER COLUMN CodigoDane NVARCHAR(12) NOT NULL;
    ');
    PRINT 'CodigoDane poblado en Matriculas.';
END
GO

IF COL_LENGTH('DocenteColegios', 'ColegioId') IS NOT NULL
   AND COL_LENGTH('DocenteColegios', 'CodigoDane') IS NULL
BEGIN
    ALTER TABLE DocenteColegios ADD CodigoDane NVARCHAR(12) NULL;
END
GO

IF COL_LENGTH('DocenteColegios', 'ColegioId') IS NOT NULL
   AND COL_LENGTH('DocenteColegios', 'CodigoDane') IS NOT NULL
   AND COL_LENGTH('Colegios', 'Id') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE dc SET CodigoDane = c.CodigoDane
        FROM DocenteColegios dc
        INNER JOIN Colegios c ON dc.ColegioId = c.Id
        WHERE dc.CodigoDane IS NULL;
        ALTER TABLE DocenteColegios ALTER COLUMN CodigoDane NVARCHAR(12) NOT NULL;
    ');
    PRINT 'CodigoDane poblado en DocenteColegios.';
END
GO

IF COL_LENGTH('Usuarios', 'ColegioId') IS NOT NULL
   AND COL_LENGTH('Usuarios', 'CodigoDane') IS NULL
BEGIN
    ALTER TABLE Usuarios ADD CodigoDane NVARCHAR(12) NULL;
END
GO

IF COL_LENGTH('Usuarios', 'ColegioId') IS NOT NULL
   AND COL_LENGTH('Usuarios', 'CodigoDane') IS NOT NULL
   AND COL_LENGTH('Colegios', 'Id') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE u SET CodigoDane = c.CodigoDane
        FROM Usuarios u
        INNER JOIN Colegios c ON u.ColegioId = c.Id
        WHERE u.ColegioId IS NOT NULL AND u.CodigoDane IS NULL;
    ');
    PRINT 'CodigoDane poblado en Usuarios.';
END
GO

IF COL_LENGTH('Matriculas', 'ColegioId') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Matriculas_Consulta' AND object_id = OBJECT_ID('Matriculas'))
        DROP INDEX IX_Matriculas_Consulta ON Matriculas;

    ALTER TABLE Matriculas DROP COLUMN ColegioId;
    PRINT 'Columna ColegioId eliminada de Matriculas.';
END
GO

IF COL_LENGTH('DocenteColegios', 'ColegioId') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.key_constraints
        WHERE name = 'UQ_DocenteColegio' AND parent_object_id = OBJECT_ID('DocenteColegios')
    )
        ALTER TABLE DocenteColegios DROP CONSTRAINT UQ_DocenteColegio;

    ALTER TABLE DocenteColegios DROP COLUMN ColegioId;
    PRINT 'Columna ColegioId eliminada de DocenteColegios.';
END
GO

IF COL_LENGTH('Usuarios', 'ColegioId') IS NOT NULL
BEGIN
    IF OBJECT_ID('CK_Usuarios_RolColegio', 'C') IS NOT NULL
        ALTER TABLE Usuarios DROP CONSTRAINT CK_Usuarios_RolColegio;

    ALTER TABLE Usuarios DROP COLUMN ColegioId;
    PRINT 'Columna ColegioId eliminada de Usuarios.';
END
GO

IF COL_LENGTH('DocenteColegios', 'CodigoDane') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.key_constraints
        WHERE name = 'UQ_DocenteColegio' AND parent_object_id = OBJECT_ID('DocenteColegios')
   )
BEGIN
    ALTER TABLE DocenteColegios ADD CONSTRAINT UQ_DocenteColegio UNIQUE (DocenteId, CodigoDane);
    PRINT 'Restriccion UQ_DocenteColegio recreada con CodigoDane.';
END
GO

IF COL_LENGTH('Usuarios', 'CodigoDane') IS NOT NULL
   AND OBJECT_ID('CK_Usuarios_RolColegio', 'C') IS NULL
BEGIN
    ALTER TABLE Usuarios ADD CONSTRAINT CK_Usuarios_RolColegio CHECK (
        (Rol = 'Admin' AND CodigoDane IS NULL) OR
        (Rol = 'Colegio' AND CodigoDane IS NOT NULL)
    );
    PRINT 'Restriccion CK_Usuarios_RolColegio recreada con CodigoDane.';
END
GO

IF COL_LENGTH('Colegios', 'Id') IS NOT NULL
   AND COL_LENGTH('Colegios', 'CodigoDane') IS NOT NULL
BEGIN
    DECLARE @pk NVARCHAR(200);
    SELECT @pk = name FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID('Colegios') AND type = 'PK';
    IF @pk IS NOT NULL EXEC('ALTER TABLE Colegios DROP CONSTRAINT ' + @pk);

    EXEC(N'ALTER TABLE Colegios DROP COLUMN Id;');

    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('Colegios') AND type = 'PK')
        ALTER TABLE Colegios ADD CONSTRAINT PK_Colegios PRIMARY KEY (CodigoDane);

    PRINT 'Colegios ahora usa CodigoDane como PK.';
END
GO

IF OBJECT_ID('FK_Matriculas_Colegios', 'F') IS NULL
   AND COL_LENGTH('Matriculas', 'CodigoDane') IS NOT NULL
BEGIN
    ALTER TABLE Matriculas ADD CONSTRAINT FK_Matriculas_Colegios
        FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane);
END
GO

IF OBJECT_ID('FK_DocenteColegios_Colegios', 'F') IS NULL
   AND COL_LENGTH('DocenteColegios', 'CodigoDane') IS NOT NULL
BEGIN
    ALTER TABLE DocenteColegios ADD CONSTRAINT FK_DocenteColegios_Colegios
        FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane);
END
GO

IF OBJECT_ID('FK_Usuarios_Colegios', 'F') IS NULL
   AND COL_LENGTH('Usuarios', 'CodigoDane') IS NOT NULL
BEGIN
    ALTER TABLE Usuarios ADD CONSTRAINT FK_Usuarios_Colegios
        FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Matriculas_Consulta' AND object_id = OBJECT_ID('Matriculas'))
   AND COL_LENGTH('Matriculas', 'CodigoDane') IS NOT NULL
BEGIN
    CREATE INDEX IX_Matriculas_Consulta ON Matriculas(CodigoDane, GradoId, AnioAcademicoId);
END
GO

PRINT 'Migracion CodigoDane completada.';
GO

SELECT TOP 3 CodigoDane, Nombre FROM Colegios ORDER BY Nombre;
GO
