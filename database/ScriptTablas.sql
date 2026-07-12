-- Ciudad Educativa - Tablas y datos (SIN crear la base de datos)
-- Usar si tu usuario no tiene permiso CREATE DATABASE
-- PASO PREVIO: crear la BD manualmente en SSMS (clic derecho Databases -> New Database -> CiudadEducativa)

USE CiudadEducativa;
GO

IF OBJECT_ID('DocenteColegios', 'U') IS NOT NULL DROP TABLE DocenteColegios;
IF OBJECT_ID('Matriculas', 'U') IS NOT NULL DROP TABLE Matriculas;
IF OBJECT_ID('Estudiantes', 'U') IS NOT NULL DROP TABLE Estudiantes;
IF OBJECT_ID('Grupos', 'U') IS NOT NULL DROP TABLE Grupos;
IF OBJECT_ID('Docentes', 'U') IS NOT NULL DROP TABLE Docentes;
IF OBJECT_ID('Usuarios', 'U') IS NOT NULL DROP TABLE Usuarios;
IF OBJECT_ID('AniosAcademicos', 'U') IS NOT NULL DROP TABLE AniosAcademicos;
IF OBJECT_ID('Grados', 'U') IS NOT NULL DROP TABLE Grados;
IF OBJECT_ID('Colegios', 'U') IS NOT NULL DROP TABLE Colegios;
GO

CREATE TABLE Colegios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CodigoDane NVARCHAR(12) NOT NULL UNIQUE,
    Nombre NVARCHAR(150) NOT NULL,
    Sector NVARCHAR(20) NOT NULL CHECK (Sector IN ('Publico', 'Privado'))
);

CREATE TABLE Usuarios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Rol NVARCHAR(20) NOT NULL CHECK (Rol IN ('Admin', 'Colegio')),
    CodigoDane NVARCHAR(12) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Usuarios_Colegios FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane),
    CONSTRAINT CK_Usuarios_RolColegio CHECK (
        (Rol = 'Admin' AND CodigoDane IS NULL) OR
        (Rol = 'Colegio' AND CodigoDane IS NOT NULL)
    )
);

CREATE TABLE Grados (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    Orden INT NOT NULL
);

CREATE TABLE Docentes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TipoDocumento NVARCHAR(5) NOT NULL,
    NumeroDocumento NVARCHAR(20) NOT NULL,
    Nombre NVARCHAR(150) NOT NULL,
    FechaContratacion DATE NOT NULL,
    PeriodoContrato NVARCHAR(50) NOT NULL,
    VigenciaContrato DATE NULL,
    Activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Docentes_Documento UNIQUE (TipoDocumento, NumeroDocumento),
    CONSTRAINT CK_Docentes_TipoDocumento CHECK (TipoDocumento IN ('RC', 'TI', 'CC', 'CE', 'PA')),
    CONSTRAINT CK_Docentes_Vigencia CHECK (VigenciaContrato IS NULL OR VigenciaContrato >= FechaContratacion)
);

CREATE TABLE Grupos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CodigoDane NVARCHAR(12) NOT NULL,
    GradoId INT NOT NULL,
    Nombre NVARCHAR(10) NOT NULL,
    DocenteDirectorId INT NULL,
    CONSTRAINT FK_Grupos_Colegios FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane),
    CONSTRAINT FK_Grupos_Grados FOREIGN KEY (GradoId) REFERENCES Grados(Id),
    CONSTRAINT FK_Grupos_Docentes FOREIGN KEY (DocenteDirectorId) REFERENCES Docentes(Id),
    CONSTRAINT UQ_GrupoColegio UNIQUE (CodigoDane, GradoId, Nombre)
);

CREATE TABLE AniosAcademicos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Anio INT NOT NULL UNIQUE
);

CREATE TABLE Estudiantes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(150) NOT NULL,
    TipoDocumento NVARCHAR(5) NOT NULL CHECK (TipoDocumento IN ('RC', 'TI', 'CC', 'CE', 'PA')),
    NumeroDocumento NVARCHAR(20) NOT NULL,
    FechaNacimiento DATE NOT NULL,
    CONSTRAINT UQ_Estudiantes_Documento UNIQUE (TipoDocumento, NumeroDocumento)
);

CREATE TABLE Matriculas (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EstudianteId INT NOT NULL,
    CodigoDane NVARCHAR(12) NOT NULL,
    GradoId INT NOT NULL,
    GrupoId INT NOT NULL,
    AnioAcademicoId INT NOT NULL,
    Activa BIT NOT NULL DEFAULT 1,
    FechaMatricula DATE NOT NULL DEFAULT GETDATE(),
    FechaAnulacion DATE NULL,
    MotivoInactivacion NVARCHAR(30) NULL,
    CONSTRAINT FK_Matriculas_Estudiantes FOREIGN KEY (EstudianteId) REFERENCES Estudiantes(Id),
    CONSTRAINT FK_Matriculas_Colegios FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane),
    CONSTRAINT FK_Matriculas_Grados FOREIGN KEY (GradoId) REFERENCES Grados(Id),
    CONSTRAINT FK_Matriculas_Grupos FOREIGN KEY (GrupoId) REFERENCES Grupos(Id),
    CONSTRAINT FK_Matriculas_Anios FOREIGN KEY (AnioAcademicoId) REFERENCES AniosAcademicos(Id)
);

CREATE TABLE DocenteColegios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DocenteId INT NOT NULL,
    CodigoDane NVARCHAR(12) NOT NULL,
    FechaAsignacion DATE NOT NULL DEFAULT GETDATE(),
    Activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_DocenteColegios_Docentes FOREIGN KEY (DocenteId) REFERENCES Docentes(Id),
    CONSTRAINT FK_DocenteColegios_Colegios FOREIGN KEY (CodigoDane) REFERENCES Colegios(CodigoDane),
    CONSTRAINT UQ_DocenteColegio UNIQUE (DocenteId, CodigoDane)
);

CREATE INDEX IX_Matriculas_Consulta ON Matriculas(CodigoDane, GradoId, AnioAcademicoId);
CREATE INDEX IX_Matriculas_EstudianteActiva ON Matriculas(EstudianteId, Activa);
CREATE UNIQUE INDEX UX_Matriculas_EstudianteActiva ON Matriculas(EstudianteId) WHERE Activa = 1;

INSERT INTO Colegios (CodigoDane, Nombre, Sector) VALUES
(N'105001000001', N'Colegio San José', 'Privado'),
(N'105001000002', N'Instituto Nacional', 'Publico'),
(N'105001000003', N'Colegio Los Andes', 'Privado'),
(N'105001000004', N'Escuela República de Colombia', 'Publico'),
(N'105001000005', N'Colegio Santa María', 'Privado'),
(N'105001000006', N'Liceo Municipal', 'Publico');

INSERT INTO Grados (Nombre, Orden) VALUES
(N'Preescolar', 0),
(N'1 Grado', 1),
(N'2 Grado', 2),
(N'3 Grado', 3),
(N'4 Grado', 4),
(N'5 Grado', 5),
(N'6 Grado', 6),
(N'7 Grado', 7),
(N'8 Grado', 8),
(N'9 Grado', 9),
(N'10 Grado', 10),
(N'11 Grado', 11);

INSERT INTO AniosAcademicos (Anio) VALUES (2024), (2025), (2026);

INSERT INTO Docentes (TipoDocumento, NumeroDocumento, Nombre, FechaContratacion, PeriodoContrato, VigenciaContrato, Activo) VALUES
(N'CC', N'52890123', N'María González', '2020-02-01', 'Indefinido', NULL, 1),
(N'CC', N'80123456', N'Carlos Ruiz', '2019-08-15', 'Indefinido', NULL, 1),
(N'CC', N'52987654', N'Ana Martínez', '2021-01-10', 'Anual', '2026-07-25', 1),
(N'CC', N'79456123', N'Pedro Lopez', '2018-03-20', 'Indefinido', NULL, 1),
(N'CC', N'1034567890', N'Laura Torres', '2022-07-01', 'Anual', '2026-12-31', 1),
(N'CC', N'80765432', N'Jorge Herrera', '2017-01-05', 'Indefinido', NULL, 1);

INSERT INTO DocenteColegios (DocenteId, CodigoDane, FechaAsignacion, Activo)
SELECT d.Id, v.CodigoDane, v.FechaAsignacion, v.Activo
FROM (VALUES
    (N'CC', N'52890123', N'105001000001', '2020-02-01', 1),
    (N'CC', N'52890123', N'105001000003', '2021-01-01', 1),
    (N'CC', N'80123456', N'105001000002', '2019-08-15', 1),
    (N'CC', N'80123456', N'105001000004', '2020-01-01', 1),
    (N'CC', N'52987654', N'105001000001', '2021-01-10', 1),
    (N'CC', N'79456123', N'105001000005', '2018-03-20', 1),
    (N'CC', N'79456123', N'105001000006', '2019-01-01', 1),
    (N'CC', N'1034567890', N'105001000003', '2022-07-01', 1),
    (N'CC', N'80765432', N'105001000002', '2017-01-05', 1),
    (N'CC', N'80765432', N'105001000004', '2018-01-01', 1)
) v(TipoDocumento, NumeroDocumento, CodigoDane, FechaAsignacion, Activo)
INNER JOIN Docentes d ON d.TipoDocumento = v.TipoDocumento AND d.NumeroDocumento = v.NumeroDocumento;

-- 5 grupos por grado y por colegio; director rotado entre docentes del colegio
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
),
DocentesColegio AS (
    SELECT
        dc.CodigoDane,
        dc.DocenteId,
        ROW_NUMBER() OVER (PARTITION BY dc.CodigoDane ORDER BY dc.DocenteId) AS rn,
        COUNT(*) OVER (PARTITION BY dc.CodigoDane) AS Cnt
    FROM DocenteColegios dc
    WHERE dc.Activo = 1
)
INSERT INTO Grupos (CodigoDane, GradoId, Nombre, DocenteDirectorId)
SELECT
    c.CodigoDane,
    gb.GradoId,
    gb.Nombre,
    dc.DocenteId
FROM Colegios c
CROSS JOIN GruposBase gb
OUTER APPLY (
    SELECT d.DocenteId
    FROM DocentesColegio d
    WHERE d.CodigoDane = c.CodigoDane
      AND d.Cnt > 0
      AND d.rn = ((gb.GrupoIdx - 1) % d.Cnt) + 1
) dc;

INSERT INTO Estudiantes (Nombre, TipoDocumento, NumeroDocumento, FechaNacimiento) VALUES
(N'Sofia Ramirez', 'RC', '1098765432', '2019-03-15'),
(N'Mateo Castro', 'RC', '1098765433', '2018-07-22'),
(N'Valentina Morales', 'TI', '1002345678', '2015-11-08'),
(N'Santiago Vargas', 'TI', '1002345679', '2012-01-30'),
(N'Isabella Rios', 'RC', '1098765434', '2019-09-12'),
(N'Tomás Méndez', 'TI', '1002345680', '2017-04-05'),
(N'Camila Ortiz', 'TI', '1002345681', '2013-06-18'),
(N'Daniel Paez', 'CC', '1020304050', '2010-12-25'),
(N'Lucia Herrera', 'RC', '1098765435', '2018-02-14'),
(N'Nicolas Silva', 'TI', '1002345682', '2016-08-09');

INSERT INTO Matriculas (EstudianteId, CodigoDane, GradoId, GrupoId, AnioAcademicoId, Activa, FechaMatricula) VALUES
(1, N'105001000001', 1, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000001' AND GradoId = 1 AND Nombre = '001'), 3, 1, '2026-01-15'),
(2, N'105001000001', 2, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000001' AND GradoId = 2 AND Nombre = '101'), 3, 1, '2026-01-15'),
(3, N'105001000002', 4, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000002' AND GradoId = 4 AND Nombre = '301'), 3, 1, '2026-01-20'),
(4, N'105001000002', 5, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000002' AND GradoId = 5 AND Nombre = '401'), 3, 1, '2026-01-20'),
(5, N'105001000003', 1, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000003' AND GradoId = 1 AND Nombre = '002'), 3, 1, '2026-01-18'),
(6, N'105001000004', 3, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000004' AND GradoId = 3 AND Nombre = '201'), 3, 1, '2026-01-22'),
(7, N'105001000005', 4, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000005' AND GradoId = 4 AND Nombre = '302'), 3, 1, '2026-01-10'),
(8, N'105001000006', 5, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000006' AND GradoId = 5 AND Nombre = '403'), 3, 1, '2026-01-12'),
(9, N'105001000001', 2, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000001' AND GradoId = 2 AND Nombre = '102'), 2, 0, '2025-01-15'),
(9, N'105001000001', 3, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000001' AND GradoId = 3 AND Nombre = '201'), 3, 1, '2026-01-15'),
(10, N'105001000002', 3, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000002' AND GradoId = 3 AND Nombre = '202'), 2, 0, '2025-01-20'),
(10, N'105001000002', 4, (SELECT Id FROM Grupos WHERE CodigoDane = N'105001000002' AND GradoId = 4 AND Nombre = '301'), 3, 1, '2026-01-20');

INSERT INTO Usuarios (Email, PasswordHash, Nombre, Rol, CodigoDane, Activo) VALUES
(N'admin@ciudad.edu', N'$2b$11$nNZkAZIKhHTodb5Ak3u28ewtMv8m7xuyXzSt/6nWrJeq2FGiMVj3O', N'Administrador Ciudad', N'Admin', NULL, 1),
(N'colegio@ciudad.edu', N'$2b$11$hA1laYOjMi5UDcNuTV6HtuFIwD.i3BkwqCOWFDNU0Q4X.95khouIm', N'Administrador Colegio', N'Colegio', N'105001000001', 1);

GO

PRINT 'Tablas y datos creados correctamente en CiudadEducativa.';
