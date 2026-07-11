import { Routes } from '@angular/router';
import { Inicio } from './pages/inicio/inicio';
import { Reportes } from './pages/reportes/reportes';
import { Matricular } from './pages/matricular/matricular';
import { Docentes } from './pages/docentes/docentes';
import { Colegios } from './pages/colegios/colegios';
import { Login } from './pages/login/login';
import { authGuard, adminGuard, guestGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: '', component: Inicio, canActivate: [authGuard] },
  { path: 'reportes', component: Reportes, canActivate: [authGuard] },
  { path: 'matricular', component: Matricular, canActivate: [authGuard] },
  { path: 'consultar', redirectTo: 'matricular', pathMatch: 'full' },
  { path: 'docentes', component: Docentes, canActivate: [authGuard] },
  { path: 'colegios', component: Colegios, canActivate: [authGuard, adminGuard] },
  { path: '**', redirectTo: '' }
];
