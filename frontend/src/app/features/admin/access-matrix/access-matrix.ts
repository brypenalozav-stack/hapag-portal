import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { concatMap, from, toArray } from 'rxjs';
import { AccessMatrixService } from '../../../core/services/access-matrix.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  ACCESS_LEVELS,
  AccessLevel,
  AccessMatrixAction,
  AccessMatrixLevel,
} from '../../../core/models/access-matrix.model';
import { OrganizationType } from '../../../core/models/organization.model';
import { SHIPMENT_ROLES, ShipmentRole } from '../../../core/models/shipment.model';
import {
  ACCESS_ACTION_KEYS,
  ACCESS_LEVEL_HELP_KEYS,
  ACCESS_LEVEL_KEYS,
  ORGANIZATION_TYPE_KEYS,
  SHIPMENT_ROLE_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';

/** Cambio pendiente de guardar en una celda de la matriz. */
interface PendingChange {
  actionCode: string;
  role: ShipmentRole;
  organizationType: OrganizationType | null;
  level: AccessLevel;
}

/** Excepción por tipo de organización (p. ej. consignee de un Freight Forwarder). */
interface MatrixException {
  action: AccessMatrixAction;
  level: AccessMatrixLevel;
}

function cellKey(actionCode: string, role: string, organizationType: string | null): string {
  return `${actionCode}|${role}|${organizationType ?? ''}`;
}

/**
 * Editor de la matriz base de accesos por rol (M1-11): acciones × roles con O, X o X (o).
 * Los cambios se acumulan y se guardan con PUT /access-matrix/{acción}; aplican de inmediato en
 * el servidor, que es quien controla los permisos (NF-05). Requiere access-matrix.manage.
 */
@Component({
  selector: 'app-access-matrix',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './access-matrix.html',
  styleUrl: './access-matrix.scss',
})
export class AccessMatrixComponent implements OnInit {
  private readonly service = inject(AccessMatrixService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  readonly roles = SHIPMENT_ROLES;
  readonly levels = ACCESS_LEVELS;
  readonly roleKeys = SHIPMENT_ROLE_KEYS;
  readonly levelKeys = ACCESS_LEVEL_KEYS;
  readonly levelHelpKeys = ACCESS_LEVEL_HELP_KEYS;
  readonly actionKeys = ACCESS_ACTION_KEYS;
  readonly typeKeys = ORGANIZATION_TYPE_KEYS;

  actions = signal<AccessMatrixAction[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  saving = signal(false);
  pending = signal<Map<string, PendingChange>>(new Map());

  information = computed(() => this.actions().filter((a) => a.category === 'Information'));
  administration = computed(() => this.actions().filter((a) => a.category === 'Administration'));
  exceptions = computed<MatrixException[]>(() =>
    this.actions().flatMap((action) =>
      action.levels.filter((level) => level.organizationType !== null).map((level) => ({ action, level })),
    ),
  );
  pendingCount = computed(() => this.pending().size);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getAll().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (actions) => {
        this.actions.set([...actions].sort((a, b) => a.displayOrder - b.displayOrder));
        this.pending.set(new Map());
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else {
          this.error.set(translate('admin.accessMatrix.errors.load'));
        }
        this.loading.set(false);
      },
    });
  }

  /** Nivel mostrado: el cambio pendiente o el vigente; sin regla, X. */
  levelOf(action: AccessMatrixAction, role: ShipmentRole, organizationType: OrganizationType | null = null): AccessLevel {
    const change = this.pending().get(cellKey(action.code, role, organizationType));
    if (change) return change.level;
    return (
      action.levels.find((l) => l.role === role && (l.organizationType ?? null) === organizationType)?.level ?? 'Denied'
    );
  }

  isChanged(action: AccessMatrixAction, role: ShipmentRole, organizationType: OrganizationType | null = null): boolean {
    return this.pending().has(cellKey(action.code, role, organizationType));
  }

  onLevelChange(
    action: AccessMatrixAction,
    role: ShipmentRole,
    organizationType: OrganizationType | null,
    event: Event,
  ): void {
    const level = (event.target as HTMLSelectElement).value as AccessLevel;
    const key = cellKey(action.code, role, organizationType);
    const current =
      action.levels.find((l) => l.role === role && (l.organizationType ?? null) === organizationType)?.level ?? 'Denied';
    this.pending.update((map) => {
      const next = new Map(map);
      if (level === current) {
        next.delete(key);
      } else {
        next.set(key, { actionCode: action.code, role, organizationType, level });
      }
      return next;
    });
  }

  discard(): void {
    this.pending.set(new Map());
  }

  /** Guarda los cambios uno a uno (PUT por acción y rol) y refleja la fila que devuelve el servidor. */
  save(): void {
    const changes = [...this.pending().values()];
    if (changes.length === 0) return;
    this.saving.set(true);
    this.error.set('');
    from(changes).pipe(
      concatMap((change) =>
        this.service.updateLevel(change.actionCode, {
          role: change.role,
          organizationType: change.organizationType,
          level: change.level,
        }),
      ),
      toArray(),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (updated) => {
        const byCode = new Map(updated.map((a) => [a.code, a]));
        this.actions.update((list) => list.map((a) => byCode.get(a.code) ?? a));
        this.pending.set(new Map());
        this.saving.set(false);
        this.announcer.announce(translate('admin.accessMatrix.saved', { count: changes.length }));
      },
      error: () => {
        this.saving.set(false);
        this.error.set(translate('admin.accessMatrix.errors.save'));
        // Recarga para mostrar lo que sí quedó guardado.
        this.load();
      },
    });
  }
}
