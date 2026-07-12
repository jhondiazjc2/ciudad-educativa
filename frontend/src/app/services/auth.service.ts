import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface LoginResponse {
  token: string;
  nombre: string;
  email: string;
  rol: string;
  codigoDane: string | null;
  colegioNombre: string | null;
  expiraEn: string;
}

export interface AuthUser {
  nombre: string;
  email: string;
  rol: string;
  codigoDane: string | null;
  colegioNombre: string | null;
  expiraEn: string;
}

const API = `${environment.apiUrl}/auth`;
const TOKEN_KEY = 'ce_token';
const USER_KEY = 'ce_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly user = signal<AuthUser | null>(this.loadUser());

  constructor(private http: HttpClient, private router: Router) {}

  login(email: string, password: string) {
    return this.http.post<LoginResponse>(`${API}/login`, { email, password }).pipe(
      tap((res) => {
        sessionStorage.setItem(TOKEN_KEY, res.token);
        const user: AuthUser = {
          nombre: res.nombre,
          email: res.email,
          rol: res.rol,
          codigoDane: res.codigoDane,
          colegioNombre: res.colegioNombre,
          expiraEn: res.expiraEn
        };
        sessionStorage.setItem(USER_KEY, JSON.stringify(user));
        this.user.set(user);
      })
    );
  }

  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
    this.user.set(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return sessionStorage.getItem(TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    const user = this.loadUser();
    if (!token || !user) return false;
    return new Date(user.expiraEn) > new Date();
  }

  isAdmin(): boolean {
    return this.user()?.rol === 'Admin';
  }

  isColegio(): boolean {
    return this.user()?.rol === 'Colegio';
  }

  getCodigoDane(): string | null {
    return this.user()?.codigoDane ?? null;
  }

  private loadUser(): AuthUser | null {
    const raw = sessionStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      const user = JSON.parse(raw) as AuthUser;
      if (new Date(user.expiraEn) <= new Date()) {
        sessionStorage.removeItem(TOKEN_KEY);
        sessionStorage.removeItem(USER_KEY);
        return null;
      }
      return user;
    } catch {
      return null;
    }
  }
}
