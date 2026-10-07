/** Margen entre el borde inferior de las barras fijas y el título al que se salta. */
const GAP = 12;
/** Tiempo durante el que se corrige la posición si el contenido de arriba cambia de alto (secciones diferidas). */
const PIN_MS = 1500;

/**
 * Alto que tapan las barras fijas: la barra superior del portal y, si la página tiene, el encabezado fijo marcado con
 * `data-hl-sticky` (su `top` resuelto ya incluye la barra superior).
 */
export function stickyOffset(): number {
  const sticky = document.querySelector<HTMLElement>('[data-hl-sticky]');
  if (sticky) return (parseFloat(getComputedStyle(sticky).top) || 0) + sticky.offsetHeight + GAP;
  const navbar = document.querySelector<HTMLElement>('.hl-navbar');
  return (navbar?.offsetHeight ?? 0) + GAP;
}

/**
 * Lleva la página al título indicado, bajo las barras fijas, y le pasa el foco (WCAG 2.4.3, 2.4.11). Las secciones
 * diferidas (`@defer on viewport`) que entran en pantalla al saltar cambian de alto al cargar: durante un momento se
 * corrige la posición para que el título no se desplace, salvo que la persona mueva la página.
 */
export function scrollToHeading(id: string): boolean {
  const heading = document.getElementById(id);
  if (!heading) return false;
  if (!heading.hasAttribute('tabindex')) heading.setAttribute('tabindex', '-1');

  const align = () => {
    const delta = heading.getBoundingClientRect().top - stickyOffset();
    if (Math.abs(delta) > 2) window.scrollBy({ top: delta, behavior: 'instant' });
  };
  align();
  heading.focus({ preventScroll: true });

  const until = performance.now() + PIN_MS;
  let frame = 0;
  const stop = () => {
    cancelAnimationFrame(frame);
    for (const type of ['wheel', 'touchstart', 'keydown', 'mousedown'] as const) window.removeEventListener(type, stop);
  };
  for (const type of ['wheel', 'touchstart', 'keydown', 'mousedown'] as const) window.addEventListener(type, stop, { passive: true });
  const tick = () => {
    if (performance.now() > until) return stop();
    align();
    frame = requestAnimationFrame(tick);
  };
  frame = requestAnimationFrame(tick);
  return true;
}
