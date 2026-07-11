-- Actualiza nombres con ortografía correcta en una BD ya creada.
-- Ejecutar en SSMS sobre CiudadEducativa si instalaste antes de esta corrección.

USE CiudadEducativa;
GO

UPDATE Colegios SET Nombre = N'Colegio San José' WHERE Nombre = N'Colegio San Jose';
UPDATE Colegios SET Nombre = N'Escuela República de Colombia' WHERE Nombre = N'Escuela Republica de Colombia';
UPDATE Colegios SET Nombre = N'Colegio Santa María' WHERE Nombre = N'Colegio Santa Maria';

UPDATE Docentes SET Nombre = N'María González' WHERE Nombre = N'Maria Gonzalez';
UPDATE Docentes SET Nombre = N'Ana Martínez' WHERE Nombre = N'Ana Martinez';

UPDATE Estudiantes SET Nombre = N'Tomás Méndez' WHERE Nombre = N'Tomas Mendez';
