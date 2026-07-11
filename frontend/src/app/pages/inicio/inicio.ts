import { Component, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

interface Modulo {
  ruta: string;
  titulo: string;
  descripcion: string;
  icon: 'reportes' | 'matricular' | 'consultar' | 'docentes';
}

@Component({
  selector: 'app-inicio',
  imports: [RouterLink],
  templateUrl: './inicio.html'
})
export class Inicio {
  readonly modulos = computed<Modulo[]>(() => {
    const admin = this.auth.isAdmin();
    const colegio = this.auth.user()?.colegioNombre;

    return [
      {
        ruta: '/reportes',
        titulo: 'Reportes',
        descripcion: admin
          ? 'Consultas analíticas de estudiantes, docentes y contratos en toda la ciudad.'
          : `Indicadores y estadísticas de ${colegio ?? 'tu colegio'}.`,
        icon: 'reportes'
      },
      {
        ruta: '/matricular',
        titulo: 'Matricular',
        descripcion: 'Registrar nuevos estudiantes y asignarlos a grado, grupo y año académico.',
        icon: 'matricular'
      },
      {
        ruta: '/consultar',
        titulo: 'Consultar',
        descripcion: 'Buscar estudiantes matriculados y revisar su histórico académico.',
        icon: 'consultar'
      },
      {
        ruta: '/docentes',
        titulo: 'Docentes',
        descripcion: admin
          ? 'Asignar docentes a colegios públicos y privados de la ciudad.'
          : 'Gestionar la vinculación de docentes en tu colegio.',
        icon: 'docentes'
      }
    ];
  });

  constructor(protected auth: AuthService) {}
}
