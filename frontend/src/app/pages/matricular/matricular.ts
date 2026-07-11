import { Component, ElementRef, OnInit, ViewChild, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize, timeout } from 'rxjs';
import { SelectField, SelectOption, toSelectOptions } from '../../components/select-field/select-field';
import { ApiService } from '../../services/api.service';
import { AuthService } from '../../services/auth.service';
import { CatalogoDocumentoItem, CatalogoItem, GrupoItem } from '../../models';

@Component({
  selector: 'app-matricular',
  imports: [FormsModule, SelectField],
  templateUrl: './matricular.html'
})
export class Matricular implements OnInit {
  @ViewChild('errorAlert') errorAlert?: ElementRef<HTMLElement>;

  colegios: CatalogoItem[] = [];
  grados: CatalogoItem[] = [];
  grupos: GrupoItem[] = [];
  anios: CatalogoItem[] = [];
  tiposDocumento: CatalogoDocumentoItem[] = [];

  nombre = '';
  tipoDocumento = 'RC';
  numeroDocumento = '';
  fechaNacimiento = '';
  colegioId = 0;
  gradoId = 0;
  grupoId = 0;
  anioAcademicoId = 0;

  readonly mensaje = signal('');
  readonly error = signal('');
  readonly enviando = signal(false);
  colegioBloqueado = false;
  // Usuario Colegio: colegio fijado en UI (colegioBloqueado). El backend es la autoridad real (403).

  constructor(private api: ApiService, private auth: AuthService) {}

  get colegioOptions() {
    return toSelectOptions(this.colegios);
  }

  get gradoOptions() {
    return toSelectOptions(this.grados);
  }

  get anioOptions() {
    return toSelectOptions(this.anios);
  }

  get grupoOptions(): SelectOption[] {
    return this.grupos.map((g) => ({
      id: g.id,
      label: g.docenteDirector ? `${g.nombre} (Dir: ${g.docenteDirector})` : g.nombre
    }));
  }

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
    this.api.getTiposDocumento().subscribe((d) => {
      this.tiposDocumento = d;
      if (d.length && !d.some((t) => t.codigo === this.tipoDocumento)) {
        this.tipoDocumento = d[0].codigo;
      }
    });
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
    this.mensaje.set('');
    this.error.set('');

    if (!this.nombre || !this.tipoDocumento || !this.numeroDocumento || !this.fechaNacimiento ||
        !this.colegioId || !this.gradoId || !this.grupoId || !this.anioAcademicoId) {
      this.error.set('Completa todos los campos.');
      return;
    }

    this.enviando.set(true);
    this.api.crearMatricula({
      nombre: this.nombre,
      tipoDocumento: this.tipoDocumento,
      numeroDocumento: this.numeroDocumento.replace(/\D/g, ''),
      fechaNacimiento: this.fechaNacimiento,
      colegioId: this.colegioId,
      gradoId: this.gradoId,
      grupoId: this.grupoId,
      anioAcademicoId: this.anioAcademicoId
    }).pipe(
      timeout(15000),
      finalize(() => { this.enviando.set(false); })
    ).subscribe({
      next: () => {
        this.mensaje.set('Estudiante matriculado correctamente.');
      },
      error: (err: HttpErrorResponse | { name?: string; status?: number; error?: unknown }) => {
        if (err.name === 'TimeoutError') {
          this.mostrarError('El servidor no respondió a tiempo. Verifica que el backend esté corriendo en localhost:5000.');
          return;
        }
        if (err.status === 0) {
          this.mostrarError('No se pudo conectar con el servidor. Reinicia el backend e intenta de nuevo.');
          return;
        }
        if (err.status === 403) {
          this.mostrarError('No tiene permiso para matricular en este colegio.');
          return;
        }
        this.mostrarError(this.extraerMensajeApi(err, 'Error al matricular estudiante.'));
      }
    });
  }

  private extraerMensajeApi(err: { error?: unknown }, fallback: string): string {
    const body = err.error;
    if (body && typeof body === 'object' && 'message' in body && body.message) {
      return String(body.message);
    }
    if (typeof body === 'string' && body.trim()) {
      return body;
    }
    return fallback;
  }

  private mostrarError(mensaje: string): void {
    this.error.set(mensaje);
    setTimeout(() => {
      this.errorAlert?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    });
  }
}
