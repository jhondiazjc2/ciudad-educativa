-- Garantiza a nivel de BD: un estudiante solo puede tener una matricula activa.
-- Ejecutar sobre bases de datos existentes despues de LimpiarMatriculasDuplicadas.sql si aplica.

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_Matriculas_EstudianteActiva'
      AND object_id = OBJECT_ID('Matriculas')
)
BEGIN
    CREATE UNIQUE INDEX UX_Matriculas_EstudianteActiva
        ON Matriculas(EstudianteId)
        WHERE Activa = 1;
    PRINT 'Indice UX_Matriculas_EstudianteActiva creado.';
END
ELSE
    PRINT 'Indice UX_Matriculas_EstudianteActiva ya existe.';
