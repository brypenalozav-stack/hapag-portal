import { ChangeDetectionStrategy, Component, DestroyRef, NgZone, afterNextRender, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { Subscription } from 'rxjs';
import { ReleaseStatusService } from '../../../core/services/release-status.service';
import { ReleaseStatus, ReleaseStep } from '../../../core/models/release-status.model';
import { ShipmentDetail } from '../../../core/models/shipment.model';
import { SHIPMENT_OPERATION_KEYS } from '../../../core/i18n/labels';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { SectionNavComponent, SectionNavItem } from '../../../shared/components/section-nav/section-nav';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { scrollToHeading } from '../../../shared/scroll-to-heading';
import { RELEASE_ACTION_KEYS, RELEASE_STEP_PENDING, releaseActionRoute } from '../../release-status/release-steps';
import { DETAIL_GROUPS } from '../shipment-detail/detail-groups';

/** Acciones que se resuelven en un grupo del mismo detalle (si el grupo está en la página). */
const IN_PAGE_ACTION: Record<string, string> = {
  PayFreight: DETAIL_GROUPS.charges.id,
  PayCharges: DETAIL_GROUPS.charges.id,
  IssueResponsibilityLetter: DETAIL_GROUPS.documents.id,
  RequestNoDebtCertificate: DETAIL_GROUPS.documents.id,
};

/** Desplazamiento desde el que el encabezado se compacta, y bajo el que vuelve a abrirse (histéresis: sin parpadeo). */
const COLLAPSE_AT = 96;
const EXPAND_AT = 8;

type ReleaseState =
  | { kind: 'none' }
  | { kind: 'loading' }
  | { kind: 'unavailable' }
  | { kind: 'released'; status: ReleaseStatus }
  | { kind: 'pending'; status: ReleaseStatus; next: ReleaseStep | null };

/**
 * Encabezado fijo del detalle del BL (cierre de Fase 1, UX): BL, estado, nave y viaje, ETA y ruta, más la "Próxima
 * acción" con el resumen de los requisitos de liberación de `GET shipments/{bl}/release-status` (solo importación) y el
 * índice "Ir a" por grupos. Al desplazar la página se compacta en una barra delgada; si la consulta de liberación no
 * aplica, falla o el perfil no la ve (403), el encabezado se muestra igual sin ese resumen.
 */
@Component({
  selector: 'app-shipment-summary',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, StatusBadgeComponent, SectionNavComponent],
  templateUrl: './shipment-summary.html',
})
export class ShipmentSummaryComponent {
  private readonly releaseService = inject(ReleaseStatusService);
  private readonly destroyRef = inject(DestroyRef);

  readonly shipment = input.required<ShipmentDetail>();
  /** Grupos presentes en la página, en orden, para el índice. */
  readonly sections = input<SectionNavItem[]>([]);

  readonly operationKeys = SHIPMENT_OPERATION_KEYS;
  readonly collapsed = signal(false);
  readonly release = signal<ReleaseState>({ kind: 'none' });

  readonly progress = computed(() => {
    const r = this.release();
    if (r.kind !== 'pending' && r.kind !== 'released') return 0;
    return r.status.totalSteps ? Math.round((r.status.completedSteps / r.status.totalSteps) * 100) : 0;
  });

  private request?: Subscription;

  constructor() {
    effect(() => {
      const s = this.shipment();
      untracked(() => this.loadRelease(s));
    });

    const zone = inject(NgZone);
    afterNextRender(() => {
      const onScroll = () => {
        const y = window.scrollY;
        const next = this.collapsed() ? y > EXPAND_AT : y > COLLAPSE_AT;
        if (next !== this.collapsed()) zone.run(() => this.collapsed.set(next));
      };
      zone.runOutsideAngular(() => window.addEventListener('scroll', onScroll, { passive: true }));
      onScroll();

      // El alto del encabezado fijo se suma al margen de desplazamiento del documento: el foco con Tab no queda
      // tapado (WCAG 2.4.11).
      const header = document.querySelector<HTMLElement>('[data-hl-sticky]');
      const root = document.documentElement;
      const resize = header ? new ResizeObserver(() => root.style.setProperty('--hl-sticky-extra', `${header.offsetHeight}px`)) : null;
      if (header) resize?.observe(header);
      this.destroyRef.onDestroy(() => {
        window.removeEventListener('scroll', onScroll);
        resize?.disconnect();
        root.style.removeProperty('--hl-sticky-extra');
      });
    });
  }

  private loadRelease(s: ShipmentDetail): void {
    this.request?.unsubscribe();
    if (s.operation !== 'IMPORT') {
      this.release.set({ kind: 'none' });
      return;
    }
    this.release.set({ kind: 'loading' });
    this.request = this.releaseService.get(s.blNumber).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (status) => {
        // Sin requisitos (no aplica, o respuesta sin pasos) no hay próxima acción que mostrar.
        if (!status?.applicable || !Array.isArray(status.steps) || status.totalSteps === 0) {
          this.release.set({ kind: 'none' });
        } else if (status.released) {
          this.release.set({ kind: 'released', status });
        } else {
          this.release.set({ kind: 'pending', status, next: status.steps.find((st) => st.status === 'Pending') ?? null });
        }
      },
      // 403, 404, 5xx o sin conexión: el encabezado sigue; el resumen invita a la consulta completa.
      error: () => this.release.set({ kind: 'unavailable' }),
    });
  }

  pendingKey(step: ReleaseStep): string {
    return RELEASE_STEP_PENDING[step.code] ?? RELEASE_STEP_PENDING['FREIGHT'];
  }

  actionKey(step: ReleaseStep): string {
    return RELEASE_ACTION_KEYS[step.action] ?? RELEASE_ACTION_KEYS['PayFreight'];
  }

  /**
   * Dónde se resuelve el siguiente paso: un grupo de esta misma página (flete y recargos, documentos) o la pantalla
   * enfocada que lo resuelve. Sin permiso para la acción (`actionAllowed`) no hay botón.
   */
  readonly nextAction = computed<{ anchor: string } | { route: string[] } | null>(() => {
    const r = this.release();
    const step = r.kind === 'pending' ? r.next : null;
    if (!step || !step.actionAllowed || step.action === 'None') return null;
    const id = IN_PAGE_ACTION[step.action];
    if (id && this.sections().some((g) => g.id === id)) return { anchor: id };
    const route = releaseActionRoute(step, this.shipment().blNumber);
    // El flete se paga en este mismo detalle: sin su grupo no hay a dónde llevar.
    return route && step.action !== 'PayFreight' ? { route } : null;
  });

  anchorOf(action: { anchor: string } | { route: string[] }): string | null {
    return 'anchor' in action ? action.anchor : null;
  }

  routeOf(action: { anchor: string } | { route: string[] }): string[] | null {
    return 'route' in action ? action.route : null;
  }

  goTo(id: string): void {
    scrollToHeading(id);
  }
}
