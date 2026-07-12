import { Component, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

interface Modulo {
  ruta: string;
  titulo: string;
  descripcion: string;
  icon: 'reportes' | 'matricular' | 'docentes' | 'colegios';
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

    const modulos: Modulo[] = [
      {
        ruta: '/reportes',
        titulo: 'Reportes',
        descripcion: admin
          ? 'Consultas analíticas de estudiantes y docentes en toda la ciudad.'
          : `Indicadores y estadísticas de ${colegio ?? 'tu colegio'}.`,
        icon: 'reportes'
      },
      {
        ruta: '/matricular',
        titulo: 'Estudiantes',
        descripcion: 'Matricular y consultar estudiantes, asignarlos a grado, grupo y año académico.',
        icon: 'matricular'
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

    if (admin) {
      modulos.push({
        ruta: '/colegios',
        titulo: 'Colegios',
        descripcion: 'Crear, editar y eliminar colegios de la ciudad educativa.',
        icon: 'colegios'
      });
    }

    return modulos;
  });

  constructor(protected auth: AuthService) {}
}
