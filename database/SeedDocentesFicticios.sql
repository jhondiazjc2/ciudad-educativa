-- Ciudad Educativa - 200 docentes ficticios y redistribucion equilibrada de grupos
-- Requiere PK surrogate en Docentes (Id) y UNIQUE en documento.

USE CiudadEducativa;
GO

SET NOCOUNT ON;

DECLARE @DemoActuales INT = (
    SELECT COUNT(*) FROM Docentes WHERE Nombre LIKE N'Prof. Demo %'
);
DECLARE @Faltantes INT = 200 - @DemoActuales;

IF @Faltantes > 0
BEGIN
    ;WITH Numeros AS (
        SELECT TOP (@Faltantes)
            ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) + @DemoActuales AS n
        FROM sys.all_objects a
        CROSS JOIN sys.all_objects b
    )
    INSERT INTO Docentes (TipoDocumento, NumeroDocumento, Nombre, FechaContratacion, PeriodoContrato, VigenciaContrato, Activo)
    SELECT
        N'CC',
        N'901' + RIGHT(N'0000000' + CAST(n AS NVARCHAR(7)), 7),
        N'Prof. Demo ' + RIGHT(N'000' + CAST(n AS NVARCHAR(3)), 3),
        DATEADD(DAY, -(n * 17 + 120), CAST(GETDATE() AS DATE)),
        CASE WHEN n % 5 = 0 THEN N'Anual' ELSE N'Indefinido' END,
        CASE WHEN n % 5 = 0 THEN DATEADD(YEAR, 1, CAST(GETDATE() AS DATE)) ELSE NULL END,
        1
    FROM Numeros;

    PRINT CONCAT('Docentes ficticios insertados: ', @Faltantes);
END
GO

UPDATE DocenteColegios SET Activo = 0;
GO

;WITH ColegiosOrd AS (
    SELECT CodigoDane, ROW_NUMBER() OVER (ORDER BY CodigoDane) AS rn, COUNT(*) OVER () AS total
    FROM Colegios
),
DocentesOrd AS (
    SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS rn FROM Docentes WHERE Activo = 1
),
AsignacionObjetivo AS (
    SELECT d.Id AS DocenteId, c.CodigoDane
    FROM DocentesOrd d
    INNER JOIN ColegiosOrd c ON ((d.rn - 1) % c.total) + 1 = c.rn
)
MERGE DocenteColegios AS target
USING AsignacionObjetivo AS source
    ON target.DocenteId = source.DocenteId AND target.CodigoDane = source.CodigoDane
WHEN MATCHED THEN UPDATE SET Activo = 1, FechaAsignacion = CAST(GETDATE() AS DATE)
WHEN NOT MATCHED BY TARGET THEN
    INSERT (DocenteId, CodigoDane, FechaAsignacion, Activo)
    VALUES (source.DocenteId, source.CodigoDane, CAST(GETDATE() AS DATE), 1);

DELETE FROM DocenteColegios WHERE Activo = 0;
GO

;WITH GruposNumerados AS (
    SELECT g.Id, g.CodigoDane,
        ROW_NUMBER() OVER (PARTITION BY g.CodigoDane ORDER BY g.GradoId, g.Nombre) AS idx
    FROM Grupos g
),
DocentesColegio AS (
    SELECT dc.CodigoDane, dc.DocenteId,
        ROW_NUMBER() OVER (PARTITION BY dc.CodigoDane ORDER BY dc.DocenteId) AS rn,
        COUNT(*) OVER (PARTITION BY dc.CodigoDane) AS cnt
    FROM DocenteColegios dc
    INNER JOIN Docentes d ON d.Id = dc.DocenteId
    WHERE dc.Activo = 1 AND d.Activo = 1
)
UPDATE g SET DocenteDirectorId = dc.DocenteId
FROM Grupos g
INNER JOIN GruposNumerados gn ON g.Id = gn.Id
INNER JOIN DocentesColegio dc ON dc.CodigoDane = g.CodigoDane
    AND dc.cnt > 0 AND dc.rn = ((gn.idx - 1) % dc.cnt) + 1;
GO

PRINT 'Seed docentes ficticios y redistribucion completada.';
GO
