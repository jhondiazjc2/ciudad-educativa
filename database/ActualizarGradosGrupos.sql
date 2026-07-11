-- Actualizacion: grados hasta 11 y 5 grupos por grado y por colegio
-- Ejecutar despues de ActualizarGruposPorColegio.sql si la BD aun no tiene CodigoDane en Grupos.

USE CiudadEducativa;
GO

INSERT INTO Grados (Nombre, Orden)
SELECT v.Nombre, v.Orden
FROM (VALUES
    (N'6 Grado', 6),
    (N'7 Grado', 7),
    (N'8 Grado', 8),
    (N'9 Grado', 9),
    (N'10 Grado', 10),
    (N'11 Grado', 11)
) v(Nombre, Orden)
WHERE NOT EXISTS (SELECT 1 FROM Grados g WHERE g.Orden = v.Orden);
GO

IF COL_LENGTH('Grupos', 'CodigoDane') IS NOT NULL
BEGIN
    ;WITH Numeros AS (
        SELECT 1 AS n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5
    ),
    GruposBase AS (
        SELECT
            g.Id AS GradoId,
            CASE
                WHEN g.Orden = 0 THEN RIGHT('000' + CAST(n.n AS NVARCHAR(3)), 3)
                ELSE CAST(g.Orden AS NVARCHAR(2)) + RIGHT('0' + CAST(n.n AS NVARCHAR(1)), 2)
            END AS Nombre
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
END
GO

PRINT 'Grados y grupos por colegio verificados.';
GO
