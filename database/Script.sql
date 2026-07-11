-- Ciudad Educativa - Script de base de datos
-- SQL Server
-- Ejecutar en SSMS conectado a localhost\SQLEXPRESS

USE master;
GO

IF DB_ID('CiudadEducativa') IS NULL
    CREATE DATABASE CiudadEducativa;
GO

USE CiudadEducativa;
GO

-- Limpiar tablas si se re-ejecuta el script
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
    Nombre NVARCHAR(150) NOT NULL,
    Sector NVARCHAR(20) NOT NULL CHECK (Sector IN ('Publico', 'Privado'))
);

CREATE TABLE Usuarios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Rol NVARCHAR(20) NOT NULL CHECK (Rol IN ('Admin', 'Colegio')),
    ColegioId INT NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FechaCreacion DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Usuarios_Colegios FOREIGN KEY (ColegioId) REFERENCES Colegios(Id),
    CONSTRAINT CK_Usuarios_RolColegio CHECK (
        (Rol = 'Admin' AND ColegioId IS NULL) OR
        (Rol = 'Colegio' AND ColegioId IS NOT NULL)
    )
);

CREATE TABLE Grados (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    Orden INT NOT NULL
);

CREATE TABLE Docentes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(150) NOT NULL,
    FechaContratacion DATE NOT NULL,
    PeriodoContrato NVARCHAR(50) NOT NULL,
    VigenciaContrato DATE NULL,
    Activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT CK_Docentes_Vigencia CHECK (VigenciaContrato IS NULL OR VigenciaContrato >= FechaContratacion)
);

CREATE TABLE Grupos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    GradoId INT NOT NULL,
    Nombre NVARCHAR(10) NOT NULL,
    DocenteDirectorId INT NULL,
    CONSTRAINT FK_Grupos_Grados FOREIGN KEY (GradoId) REFERENCES Grados(Id),
    CONSTRAINT FK_Grupos_Docentes FOREIGN KEY (DocenteDirectorId) REFERENCES Docentes(Id)
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
    ColegioId INT NOT NULL,
    GradoId INT NOT NULL,
    GrupoId INT NOT NULL,
    AnioAcademicoId INT NOT NULL,
    Activa BIT NOT NULL DEFAULT 1,
    FechaMatricula DATE NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Matriculas_Estudiantes FOREIGN KEY (EstudianteId) REFERENCES Estudiantes(Id),
    CONSTRAINT FK_Matriculas_Colegios FOREIGN KEY (ColegioId) REFERENCES Colegios(Id),
    CONSTRAINT FK_Matriculas_Grados FOREIGN KEY (GradoId) REFERENCES Grados(Id),
    CONSTRAINT FK_Matriculas_Grupos FOREIGN KEY (GrupoId) REFERENCES Grupos(Id),
    CONSTRAINT FK_Matriculas_Anios FOREIGN KEY (AnioAcademicoId) REFERENCES AniosAcademicos(Id)
);

CREATE TABLE DocenteColegios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DocenteId INT NOT NULL,
    ColegioId INT NOT NULL,
    FechaAsignacion DATE NOT NULL DEFAULT GETDATE(),
    Activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_DocenteColegios_Docentes FOREIGN KEY (DocenteId) REFERENCES Docentes(Id),
    CONSTRAINT FK_DocenteColegios_Colegios FOREIGN KEY (ColegioId) REFERENCES Colegios(Id),
    CONSTRAINT UQ_DocenteColegio UNIQUE (DocenteId, ColegioId)
);

CREATE INDEX IX_Matriculas_Consulta ON Matriculas(ColegioId, GradoId, AnioAcademicoId);
CREATE INDEX IX_Matriculas_EstudianteActiva ON Matriculas(EstudianteId, Activa);

INSERT INTO Colegios (Nombre, Sector) VALUES
(N'Colegio San José', 'Privado'),
(N'Instituto Nacional', 'Publico'),
(N'Colegio Los Andes', 'Privado'),
(N'Escuela República de Colombia', 'Publico'),
(N'Colegio Santa María', 'Privado'),
(N'Liceo Municipal', 'Publico');

INSERT INTO Grados (Nombre, Orden) VALUES
(N'Preescolar', 0),
(N'1 Grado', 1),
(N'2 Grado', 2),
(N'3 Grado', 3),
(N'4 Grado', 4),
(N'5 Grado', 5);

INSERT INTO AniosAcademicos (Anio) VALUES (2024), (2025), (2026);

INSERT INTO Docentes (Nombre, FechaContratacion, PeriodoContrato, VigenciaContrato, Activo) VALUES
(N'María González', '2020-02-01', 'Indefinido', NULL, 1),
(N'Carlos Ruiz', '2019-08-15', 'Indefinido', NULL, 1),
(N'Ana Martínez', '2021-01-10', 'Anual', '2026-07-25', 1),
(N'Pedro Lopez', '2018-03-20', 'Indefinido', NULL, 1),
(N'Laura Torres', '2022-07-01', 'Anual', '2026-12-31', 1),
(N'Jorge Herrera', '2017-01-05', 'Indefinido', NULL, 1);

INSERT INTO Grupos (GradoId, Nombre, DocenteDirectorId) VALUES
(1, 'A', 1), (1, 'B', 2),
(2, 'A', 3), (2, 'B', 4),
(3, 'A', 5), (3, 'B', 6),
(4, 'A', 1), (4, 'B', 2),
(5, 'A', 3), (5, 'B', 4);

INSERT INTO DocenteColegios (DocenteId, ColegioId, FechaAsignacion, Activo) VALUES
(1, 1, '2020-02-01', 1),
(1, 3, '2021-01-01', 1),
(2, 2, '2019-08-15', 1),
(2, 4, '2020-01-01', 1),
(3, 1, '2021-01-10', 1),
(4, 5, '2018-03-20', 1),
(4, 6, '2019-01-01', 1),
(5, 3, '2022-07-01', 1),
(6, 2, '2017-01-05', 1),
(6, 4, '2018-01-01', 1);

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

INSERT INTO Matriculas (EstudianteId, ColegioId, GradoId, GrupoId, AnioAcademicoId, Activa, FechaMatricula) VALUES
(1, 1, 1, 1, 3, 1, '2026-01-15'),
(2, 1, 2, 3, 3, 1, '2026-01-15'),
(3, 2, 4, 7, 3, 1, '2026-01-20'),
(4, 2, 5, 9, 3, 1, '2026-01-20'),
(5, 3, 1, 2, 3, 1, '2026-01-18'),
(6, 4, 3, 5, 3, 1, '2026-01-22'),
(7, 5, 4, 8, 3, 1, '2026-01-10'),
(8, 6, 5, 10, 3, 1, '2026-01-12'),
(9, 1, 2, 4, 2, 0, '2025-01-15'),
(9, 1, 3, 5, 3, 1, '2026-01-15'),
(10, 2, 3, 6, 2, 0, '2025-01-20'),
(10, 2, 4, 7, 3, 1, '2026-01-20');

-- Usuarios: contraseñas hasheadas con BCrypt (factor 11)
-- admin@ciudad.edu  -> Admin123!
-- colegio@ciudad.edu -> Colegio123!
INSERT INTO Usuarios (Email, PasswordHash, Nombre, Rol, ColegioId, Activo) VALUES
(N'admin@ciudad.edu', N'$2b$11$nNZkAZIKhHTodb5Ak3u28ewtMv8m7xuyXzSt/6nWrJeq2FGiMVj3O', N'Administrador Ciudad', N'Admin', NULL, 1),
(N'colegio@ciudad.edu', N'$2b$11$hA1laYOjMi5UDcNuTV6HtuFIwD.i3BkwqCOWFDNU0Q4X.95khouIm', N'Administrador Colegio', N'Colegio', 1, 1);

GO

PRINT 'Base de datos CiudadEducativa creada correctamente.';
