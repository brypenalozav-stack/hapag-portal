import { ChangeDetectionStrategy, Component, DestroyRef, Injector, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { ReleaseStatusService } from '../../core/services/release-status.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { ToastService } from '../../core/services/toast.service';
import { ReleaseStatus, ReleaseStep, ReleaseStepItem, ReleaseStepStatus } from '../../core/models/release-status.model';
import { NO_DEBT_BLOCKER_KEYS, TATC_PENDING_REASON_KEYS, TATC_STATUS_CLASS, TATC_STATUS_KEYS } from '../../core/i18n/labels';
import { CountryBadgeComponent } from '../../shared/components/country-badge/country-badge';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { ShipGraphicComponent } from '../../shared/components/ship-graphic/ship-graphic';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../shared/focus-after-render';
import { saveBlob } from '../../shared/save-blob';
import { RELEASE_ACTION_KEYS, RELEASE_STEP_TITLE, releaseActionRoute } from './release-steps';

const RECENT_KEY = 'hl_recent_bl_status';
const RECENT_MAX = 5;

/** Título de cada requisito y texto del botón de cada acción (compartidos con el encabezado del detalle del BL). */
const STEP_TITLE = RELEASE_STEP_TITLE;
const ACTION_KEYS = RELEASE_ACTION_KEYS;

/** Avisos del TATC. */
const NOTICE_KEYS: Record<string, string> = {
  TATC_WINDOW_72H: 'releaseStatus.notices.TATC_WINDOW_72H',
  TATC_WINDOW_48H: 'releaseStatus.notices.TATC_WINDOW_48H',
  SOW_NO_TATC: 'releaseStatus.notices.SOW_NO_TATC',
  TATC_REQUESTED: 'releaseStatus.notices.TATC_REQUESTED',
};

/** Clave de texto y clase visual de cada estado de requisito. */
const STEP_STATUS: Record<ReleaseStepStatus, { key: string; css: string }> = {
  Done: { key: 'releaseStatus.status.done', css: 'is-done' },
  Pending: { key: 'releaseStatus.status.pending', css: 'is-pending' },
  InProgress: { key: 'releaseStatus.status.inProgress', css: 'is-progress' },
  NotRequired: { key: 'releaseStatus.status.notRequired', css: 'is-skipped' },
  Unavailable: { key: 'releaseStatus.status.unavailable', css: 'is-unavailable' },
};

/**
 * Consulta de BL y TATC (M2-09, CL-IMP-13, BO-IMP-13): el cliente busca un BL y ve, paso a paso, qué requisitos de
 * liberación están cumplidos y cuáles faltan (flete, Gate In / EDS y recargos, carta de responsabilidad y demurrage;
 * en Bolivia además demoras anticipadas, certificado de libre deuda y carta de liberación), con la acción que
 * resuelve cada uno. Con todo cumplido se habilita el TATC: si aún no está emitido, el portal lo solicita una vez
 * automáticamente; si está emitido, se descarga su comprobante.
 */
@Component({
  selector: 'app-release-status',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe, CountryBadgeComponent,
    StateMessageComponent, ShipGraphicComponent,
  ],
  templateUrl: './release-status.html',
  styleUrl: './release-status.scss',
})
export class ReleaseStatusComponent {
  private readonly service = inject(ReleaseStatusService);
  private readonly router = inject(Router);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** BL de la ruta (`/bl-status/:blNumber`). */
  readonly blNumber = input<string | undefined>();

  readonly query = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(50)] });
  readonly submitted = signal(false);
  readonly recent = signal<string[]>(this.loadRecent());

  readonly loading = signal(false);
  readonly loadFailed = signal(false);
  readonly notFound = signal(false);
  readonly status = signal<ReleaseStatus | null>(null);
  readonly requesting = signal(false);
  readonly downloading = signal<string | null>(null);

  readonly tatcKeys = TATC_STATUS_KEYS;
  readonly tatcClass = TATC_STATUS_CLASS;
  readonly reasonKeys = TATC_PENDING_REASON_KEYS;

  /** Solicitudes automáticas ya hechas en esta sesión (una por BL). */
  private readonly autoRequested = new Set<string>();

  readonly progress = computed(() => {
    const s = this.status();
    if (!s || s.totalSteps === 0) return 0;
    return Math.round((s.completedSteps / s.totalSteps) * 100);
  });

  /** Primer requisito pendiente: lo que el cliente debe resolver ahora. */
  readonly nextStep = computed(() => this.status()?.steps.find((s) => s.status === 'Pending') ?? null);

  readonly issuedContainers = computed(() => (this.status()?.containers ?? []).filter((c) => c.tatcStatus === 'Issued' && c.tatcNumber));

  constructor() {
    effect(() => {
      const bl = this.blNumber()?.trim().toUpperCase();
      untracked(() => {
        if (!bl) {
          this.status.set(null);
          return;
        }
        this.query.setValue(bl);
        this.load(bl);
      });
    });
  }

  search(event: Event): void {
    event.preventDefault();
    this.submitted.set(true);
    const bl = this.query.value.trim().toUpperCase();
    if (!bl) {
      focusAfterRender(this.injector, () => document.getElementById('release-status-bl'));
      return;
    }
    if (bl === this.blNumber()?.toUpperCase()) this.load(bl);
    else void this.router.navigate(['/bl-status', bl]);
  }

  open(bl: string): void {
    void this.router.navigate(['/bl-status', bl]);
  }

  load(bl: string): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.notFound.set(false);
    this.service.get(bl).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (status) => {
        this.loading.set(false);
        this.status.set(status);
        this.remember(status.blNumber);
        this.announcer.announce(
          status.applicable
            ? translate(status.released ? 'releaseStatus.announce.released' : 'releaseStatus.announce.pending', {
                bl: status.blNumber, done: status.completedSteps, total: status.totalSteps,
              })
            : translate('releaseStatus.notApplicable.title'),
        );
        focusAfterRender(this.injector, () => document.getElementById('release-status-result'));
        this.maybeRequestTatc(status);
      },
      error: (err: unknown) => {
        this.loading.set(false);
        this.status.set(null);
        if (err instanceof HttpErrorResponse && (err.status === 404 || err.status === 403)) {
          this.notFound.set(true);
          this.announcer.announce(translate('releaseStatus.notFound.title'), 'assertive');
        } else {
          this.loadFailed.set(true);
          if (!isServiceUnavailable(err)) this.announcer.announce(translate('releaseStatus.loadError'), 'assertive');
        }
      },
    });
  }

  retry(): void {
    const bl = this.blNumber();
    if (bl) this.load(bl.toUpperCase());
  }

  /** Con los requisitos cumplidos y el TATC sin emitir, se solicita una sola vez por sesión y BL. */
  private maybeRequestTatc(status: ReleaseStatus): void {
    if (!status.tatc.canRequest || status.tatc.lastRequestStatus === 'Accepted' || this.autoRequested.has(status.blNumber)) return;
    this.autoRequested.add(status.blNumber);
    this.requestTatc(status.blNumber, true);
  }

  requestTatc(bl: string, automatic = false): void {
    this.requesting.set(true);
    this.service.requestTatc(bl).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (batch) => {
        this.requesting.set(false);
        const accepted = batch.acceptedItems > 0;
        if (accepted) this.toast.success(translate(automatic ? 'releaseStatus.tatc.autoRequested' : 'releaseStatus.tatc.requested', { bl }));
        else this.announcer.announce(translate('releaseStatus.tatc.requestRejected'), 'assertive');
        this.load(bl);
      },
      error: () => {
        this.requesting.set(false);
        this.announcer.announce(translate('releaseStatus.tatc.requestFailed'), 'assertive');
      },
    });
  }

  downloadVoucher(bl: string, container?: string): void {
    this.downloading.set(container ?? 'all');
    this.service.downloadVoucher(bl, container).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        this.downloading.set(null);
        saveBlob(blob, `comprobante-tatc-${container ? `${bl}-${container}` : bl}.pdf`);
        this.toast.success(translate('releaseStatus.tatc.voucherDownloaded'));
      },
      error: () => {
        this.downloading.set(null);
        this.announcer.announce(translate('releaseStatus.tatc.voucherFailed'), 'assertive');
      },
    });
  }

  stepTitle(code: string): string {
    return STEP_TITLE[code] ?? STEP_TITLE['FREIGHT'];
  }

  actionKey(action: string): string {
    return ACTION_KEYS[action] ?? 'releaseStatus.action.notAllowed';
  }

  noticeKey(notice: string): string {
    return NOTICE_KEYS[notice] ?? 'releaseStatus.notices.help';
  }

  statusKey(status: ReleaseStepStatus): string {
    return STEP_STATUS[status]?.key ?? STEP_STATUS.Unavailable.key;
  }

  statusCss(status: ReleaseStepStatus): string {
    return STEP_STATUS[status]?.css ?? STEP_STATUS.Unavailable.css;
  }

  /** Texto que explica el estado: el motivo del backend o, sin motivo, el texto del estado del paso. */
  descriptionKey(step: ReleaseStep): string {
    const reason = step.reason ? `releaseStatus.reason.${step.reason}` : null;
    return reason ?? `releaseStatus.step.${step.code}.${step.status === 'Done' || step.status === 'NotRequired' ? 'done' : 'pending'}`;
  }

  /** Ruta de la pantalla que resuelve el paso. */
  actionRoute(step: ReleaseStep, bl: string): string[] | null {
    return releaseActionRoute(step, bl);
  }

  /** Estado de un elemento en palabras; un código sin traducción se muestra tal cual. */
  itemStatus(status: string): string {
    const key = `releaseStatus.itemStatus.${status}`;
    const text = translate(key);
    return text === key ? status : text;
  }

  /** Nombre de un elemento: contenedores, documentos y solicitudes con su número; bloqueos del CLD traducidos. */
  itemLabel(item: ReleaseStepItem): string {
    switch (item.code) {
      case 'CONTAINER':
        return translate('releaseStatus.item.container', { number: item.label });
      case 'DOCUMENT':
        return translate('releaseStatus.item.document', { number: item.label });
      case 'REQUEST':
        return translate('releaseStatus.item.request', { number: item.label });
      case 'FREIGHT':
        return translate('releaseStatus.item.freight');
      case 'ADVANCE_DEMURRAGE_BO':
        return translate('releaseStatus.step.ADVANCE_DEMURRAGE.title');
      default:
        return NO_DEBT_BLOCKER_KEYS[item.code] ? translate(NO_DEBT_BLOCKER_KEYS[item.code]) : item.label;
    }
  }

  private remember(bl: string): void {
    const next = [bl, ...this.recent().filter((b) => b !== bl)].slice(0, RECENT_MAX);
    this.recent.set(next);
    try {
      localStorage.setItem(RECENT_KEY, JSON.stringify(next));
    } catch {
      // Sin almacenamiento: los BL recientes valen solo para esta visita.
    }
  }

  private loadRecent(): string[] {
    try {
      const stored = JSON.parse(localStorage.getItem(RECENT_KEY) ?? '[]');
      return Array.isArray(stored) ? stored.filter((b): b is string => typeof b === 'string').slice(0, RECENT_MAX) : [];
    } catch {
      return [];
    }
  }
}
