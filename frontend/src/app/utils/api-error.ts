import { HttpErrorResponse } from '@angular/common/http';

export function extraerMensajeApi(
  err: HttpErrorResponse | { error?: unknown },
  fallback?: string
): string | null {
  const body = err.error;
  if (body && typeof body === 'object') {
    if ('message' in body && body.message) {
      return String(body.message);
    }
    if ('detail' in body && body.detail) {
      return String(body.detail);
    }
    if ('title' in body && body.title) {
      return String(body.title);
    }
  }
  if (typeof body === 'string' && body.trim()) {
    return body;
  }
  return fallback ?? null;
}
