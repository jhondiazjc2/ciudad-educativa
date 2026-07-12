# Ciudad Educativa

Sistema web para gestion de matriculas de estudiantes y contratacion de docentes en una ciudad.

**Stack:** Angular 21 · .NET 8 Web API · SQL Server

## Estructura del proyecto

```
Ciudad educativa/
├── database/Script.sql          # Script BD (tablas + datos)
├── backend/CiudadEducativa.Api/ # API REST .NET
├── frontend/                    # App Angular
└── docs/modelo-er.md            # Diagrama entidad-relacion
```

## Requisitos

- .NET SDK 8
- Node.js 18+
- Angular CLI (`npm install -g @angular/cli`)
- SQL Server Express

## 1. Base de datos

1. Instala SQL Server Express
2. Abre **SQL Server Management Studio** o ejecuta:

```powershell
sqlcmd -S localhost\SQLEXPRESS -i "database\Script.sql"
```

3. Ajusta la cadena de conexion en `backend/CiudadEducativa.Api/appsettings.json` si tu instancia es diferente.

## 2. Backend

```powershell
cd backend\CiudadEducativa.Api
dotnet restore
dotnet run
```

API disponible en: http://localhost:5000  
Swagger: http://localhost:5000/swagger

## 3. Frontend

```powershell
cd frontend
npm install
ng serve
```

App disponible en: http://localhost:4200

## Acceso al sistema

| Email | Contraseña | Rol |
|---|---|---|
| admin@ciudad.edu | Ciudad-Adm1n!2026 | Admin |
| colegio@ciudad.edu | SanJose-C0l3gio!2026 | Colegio (solo **Colegio San Jose**) |

### Permisos por rol

| Funcionalidad | Admin | Colegio |
|---|---|---|
| Reportes de toda la ciudad | Si | No |
| Reportes de su colegio | Si | Si |
| Matricular en cualquier colegio | Si | No (solo el suyo) |
| Consultar cualquier colegio | Si | No (solo el suyo) |
| Asignar docentes a cualquier colegio | Si | No (solo el suyo) |
| Docentes por sector (ciudad) | Si | No |

## Seguridad

- Contraseñas almacenadas con **BCrypt** (factor 11)
- Autenticacion **JWT** con expiracion de 1 hora
- Endpoints protegidos con `[Authorize]`
- Token en **sessionStorage** (se borra al cerrar el navegador)
- Mensaje generico en login fallido (no revela si el email existe)

## Funcionalidades

| Pantalla | Descripcion |
|---|---|
| Reportes | Estudiantes por edad, docentes por sector, colegio con mas estudiantes |
| Matricular / Estudiantes | Matricular, renovar, consultar historial e inactivar matrículas (ruta `/matricular`; `/consultar` redirige aquí) |
| Docentes | Asignar docente a multiples colegios |

## Entregables

- Codigo frontend: `frontend/`
- Codigo backend: `backend/CiudadEducativa.Api/`
- Script SQL: `database/Script.sql`
