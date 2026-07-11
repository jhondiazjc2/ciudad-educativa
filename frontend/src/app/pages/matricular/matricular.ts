import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { CatalogoItem, GrupoItem } from '../../models';

@Component({
  selector: 'app-matricular',
  imports: [FormsModule],
  templateUrl: './matricular.html',
  styleUrl: './matricular.css'
})
export class Matricular implements OnInit {
  colegios: CatalogoItem[] = [];
  grados: CatalogoItem[] = [];
  grupos: GrupoItem[] = [];
  anios: CatalogoItem[] = [];

  nombre = '';
  numeroMatricula = '';
  fechaNacimiento = '';
  colegioId = 0;
  gradoId = 0;
  grupoId = 0;
  anioAcademicoId = 0;

  mensaje = '';
  error = '';
  enviando = false;
  colegioBloqueado = false;

  constructor(private api: ApiService, private auth: AuthService) {}

  ngOnInit(): void {
    this.api.getColegios().subscribe((d) => {
      this.colegios = d;
      if (this.auth.isColegio() && this.auth.getColegioId()) {
        this.colegioId = this.auth.getColegioId()!;
        this.colegioBloqueado = true;
      }
    });
    this.api.getGrados().subscribe((d) => (this.grados = d));
    this.api.getAnios().subscribe((d) => (this.anios = d));
  }

  onGradoChange(): void {
    this.grupoId = 0;
    if (this.gradoId) {
      this.api.getGrupos(this.gradoId).subscribe((d) => (this.grupos = d));
    } else {
      this.grupos = [];
    }
  }

  guardar(): void {
    this.mensaje = '';
    this.error = '';

    if (!this.nombre || !this.numeroMatricula || !this.fechaNacimiento ||
        !this.colegioId || !this.gradoId || !this.grupoId || !this.anioAcademicoId) {
      this.error = 'Completa todos los campos.';
      return;
    }

    this.enviando = true;
    this.api.crearMatricula({
      nombre: this.nombre,
      numeroMatricula: this.numeroMatricula,
      fechaNacimiento: this.fechaNacimiento,
      colegioId: this.colegioId,
      gradoId: this.gradoId,
      grupoId: this.grupoId,
      anioAcademicoId: this.anioAcademicoId
    }).subscribe({
      next: () => {
        this.mensaje = 'Estudiante matriculado correctamente.';
        this.enviando = false;
      },
      error: (err) => {
        this.error = err.status === 403
          ? 'No tiene permiso para matricular en este colegio.'
          : (err.error?.message ?? 'Error al matricular estudiante.');
        this.enviando = false;
      }
    });
  }
}
