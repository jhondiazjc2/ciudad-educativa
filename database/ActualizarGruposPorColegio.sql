-- Migracion: asociar grupos a colegio (CodigoDane) con directores por colegio
-- Ejecutar en SSMS sobre CiudadEducativa. Idempotente.

USE CiudadEducativa;
GO

IF COL_LENGTH('Grupos', 'CodigoDane') IS NULL
BEGIN
    ALTER TABLE Grupos ADD CodigoDane NVARCHAR(12) NULL;
    PRINT 'Columna CodigoDane agregada a Grupos.';
END
GO

IF EXISTS (SELECT 1 FROM Grupos WHERE CodigoDane IS NULL)
BEGIN
    DECLARE @PrimerColegio NVARCHAR(12);
    SELECT TOP 1 @PrimerColegio = CodigoDane FROM Colegios ORDER BY CodigoDane;

    UPDATE Grupos SET CodigoDane = @PrimerColegio WHERE CodigoDane IS NULL;

    INSERT INTO Grupos (CodigoDane, GradoId, Nombre, DocenteDirectorId)
    SELECT c.CodigoDane, g.GradoId, g.Nombre, NULL
    FROM Colegios c
    CROSS JOIN (
        SELECT DISTINCT GradoId, Nombre
        FROM Grupos
        WHERE CodigoDane = @PrimerColegio
    ) g
    WHERE c.CodigoDane <> @PrimerColegio
      AND NOT EXISTS (
          SELECT 1 FROM Grupos gx
          WHERE gx.CodigoDane = c.CodigoDane
            AND gx.GradoId = g.GradoId
            AND gx.Nombre = g.Nombre
      );

    UPDATE m SET GrupoId = gN.Id
    FROM Matriculas m
    INNER JOIN Grupos gV ON m.GrupoId = gV.Id
    INNER JOIN Grupos gN ON gN.CodigoDane = m.CodigoDane
        AND gN.GradoId = gV.GradoId
        AND gN.Nombre = gV.Nombre
    WHERE m.GrupoId <> gN.Id;

    PRINT 'Grupos duplicados por colegio y matriculas actualizadas.';
END
GO

-- Completar grupos faltantes por colegio/grado
;WITH Numeros AS (
    SELECT 1 AS n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5
),
GruposBase AS (
    SELECT
        g.Id AS GradoId,
        g.Orden,
        CASE
            WHEN g.Orden = 0 THEN RIGHT('000' + CAST(n.n AS NVARCHAR(3)), 3)
            ELSE CAST(g.Orden AS NVARCHAR(2)) + RIGHT('0' + CAST(n.n AS NVARCHAR(1)), 2)
        END AS Nombre,
        ROW_NUMBER() OVER (ORDER BY g.Orden, n.n) AS GrupoIdx
    FROM Grados g
    CROSS JOIN Numeros n
)
INSERT INTO Grupos (CodigoDane, GradoId, Nombre, DocenteDirectorId)
SELECT c.CodigoDane, gb.GradoId, gb.Nombre, NULL
FROM Colegios c
CROSS JOIN GruposBase gb
WHERE NOT EXISTS (
    SELECT 1 FROM Grupos gr
    WHERE gr.CodigoDane = c.CodigoDane
      AND gr.GradoId = gb.GradoId
      AND gr.Nombre = gb.Nombre
);
GO

-- Asignar directores rotando docentes activos de cada colegio
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
    WHERE dc.Activo = 1
)
UPDATE g SET DocenteDirectorId = dc.DocenteId
FROM Grupos g
INNER JOIN GruposNumerados gn ON g.Id = gn.Id
INNER JOIN DocentesColegio dc ON dc.CodigoDane = g.CodigoDane
    AND dc.cnt > 0
    AND dc.rn = ((gn.idx - 1) % dc.cnt) + 1;
GO

IF COL_LENGTH('Grupos', 'CodigoDane') IS NOT NULL
BEGIN
    UPDATE Grupos SET CodigoDane = (SELECT TOP 1 CodigoDane FROM Colegios ORDER BY CodigoDane)
    WHERE CodigoDane IS NULL;

    ALTER TABLE Grupos ALTER COLUMN CodigoDane NVARCHAR(12) NOT NULL;
END
GO

IF OBJECT_ID('FK_Grupos_Colegios', 'F') IS NULL
    ALTER TABLE Grupos ADD CONSTRAINT FK_Grupos_Colegios
        FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints
    WHERE name = 'UQ_GrupoColegio' AND parent_object_id = OBJECT_ID('Grupos')
)
    ALTER TABLE Grupos ADD CONSTRAINT UQ_GrupoColegio UNIQUE (CodigoDane, GradoId, Nombre);
GO

PRINT 'Migracion grupos por colegio completada.';
GO

SELECT TOP 10 c.Nombre AS Colegio, gr.Nombre AS Grupo, d.Nombre AS Director
FROM Grupos gr
INNER JOIN Colegios c ON c.CodigoDane = gr.CodigoDane
LEFT JOIN Docentes d ON d.Id = gr.DocenteDirectorId
WHERE gr.GradoId = 2
ORDER BY c.Nombre, gr.Nombre;
GO
