import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html'
})
export class Login {
  protected readonly showDemoCredentials = environment.showDemoCredentials;
  email = '';
  password = '';
  error = '';
  loading = false;

  constructor(private auth: AuthService, private router: Router) {}

  ingresar(): void {
    this.error = '';
    if (!this.email || !this.password) {
      this.error = 'Ingresa email y contraseña.';
      return;
    }

    this.loading = true;
    this.auth.login(this.email, this.password).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/']);
      },
      error: () => {
        this.loading = false;
        this.error = 'Credenciales inválidas. Verifica tu email y contraseña.';
      }
    });
  }
}
