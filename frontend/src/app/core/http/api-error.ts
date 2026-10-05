import { HttpErrorResponse } from '@angular/common/http';

/**
 * Código de error del backend: los ProblemDetails llevan el código (p. ej. `User.PendingApproval`)
 * en `title`. Devuelve null si la respuesta no es un ProblemDetails con código.
 */
export function apiErrorCode(err: unknown): string | null {
  if (!(err instanceof HttpErrorResponse)) return null;
  const title = (err.error as { title?: unknown } | null)?.title;
  return typeof title === 'string' && title.includes('.') ? title : null;
}

/**
 * Clave Transloco para un error del backend: la de su código si está en `known`, si no `fallback`.
 * Los textos del backend no se muestran tal cual porque vienen en inglés (Fase 2 los traduce).
 */
export function apiErrorKey(err: unknown, known: Record<string, string>, fallback: string): string {
  const code = apiErrorCode(err);
  return (code && known[code]) || fallback;
}
