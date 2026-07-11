-- Elimina matriculas duplicadas (mismo estudiante, colegio, grado y ano).
-- Deja activa la mas reciente por estudiante/periodo; el resto queda inactivo.
-- Ejecutar una sola vez en CiudadEducativa.

USE CiudadEducativa;
GO

;WITH Duplicadas AS (
    SELECT
        Id,
        ROW_NUMBER() OVER (
            PARTITION BY EstudianteId, CodigoDane, GradoId, AnioAcademicoId
            ORDER BY Activa DESC, FechaMatricula DESC, Id DESC
        ) AS Fila
    FROM Matriculas
)
UPDATE m
SET Activa = 0
FROM Matriculas m
JOIN Duplicadas d ON d.Id = m.Id
WHERE d.Fila > 1 AND m.Activa = 1;
GO

PRINT 'Matriculas duplicadas desactivadas.';
GO
