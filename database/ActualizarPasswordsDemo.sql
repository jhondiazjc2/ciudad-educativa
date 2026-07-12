-- Actualiza contraseñas de usuarios demo (BCrypt).
-- admin@ciudad.edu     -> Ciudad-Adm1n!2026
-- colegio@ciudad.edu   -> SanJose-C0l3gio!2026

USE CiudadEducativa;
GO

UPDATE Usuarios
SET PasswordHash = N'$2a$11$ajFpbfQzDkOoKxFtoa8Oiu1jmcb5IeH8.68DVHXdVAExiX59sAXki',
    Nombre = N'Admin'
WHERE Email = N'admin@ciudad.edu';

UPDATE Usuarios
SET PasswordHash = N'$2a$11$861XtV9VNZ/L1eP2gw1LS.j79j1aAjQUeanYBh6aUyGRLe26rJe2m',
    Nombre = N'Admin'
WHERE Email = N'colegio@ciudad.edu';

PRINT 'Contraseñas demo actualizadas.';
