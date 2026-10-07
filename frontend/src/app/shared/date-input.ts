/**
 * Conversión entre la fecha y hora ISO-8601 (UTC) de la API y el valor de un `<input type="datetime-local">`, que se
 * edita en la hora local del navegador.
 */
export function toDateTimeInput(iso: string | null | undefined): string {
  if (!iso) return '';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function fromDateTimeInput(value: string): string | null {
  if (!value) return null;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

/** Fecha de calendario de hoy (`yyyy-MM-dd`) en la hora local del navegador. */
export function todayInput(): string {
  return toDateTimeInput(new Date().toISOString()).slice(0, 10);
}
