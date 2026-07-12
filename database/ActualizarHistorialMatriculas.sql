-- Historial e inactivación de matrículas
USE CiudadEducativa;
GO

IF COL_LENGTH('Matriculas', 'FechaAnulacion') IS NULL
BEGIN
    ALTER TABLE Matriculas ADD FechaAnulacion DATE NULL;
    PRINT 'Columna FechaAnulacion agregada.';
END
GO

IF COL_LENGTH('Matriculas', 'MotivoInactivacion') IS NULL
BEGIN
    ALTER TABLE Matriculas ADD MotivoInactivacion NVARCHAR(30) NULL;
    PRINT 'Columna MotivoInactivacion agregada.';
END
GO

-- Marcar matrículas inactivas existentes sin motivo
UPDATE Matriculas
SET MotivoInactivacion = 'FinPeriodo'
WHERE Activa = 0 AND MotivoInactivacion IS NULL;
GO

PRINT 'Migración de historial de matrículas completada.';
