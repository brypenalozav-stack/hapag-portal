import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { NotificationService } from '../../core/services/notification.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { NOTIFICATION_MODULES, NotificationPreference, NotificationPreferenceUpdate } from '../../core/models/notification.model';
import { NOTIFICATION_MODULE_KEYS, NOTIFICATION_TYPE_KEYS } from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { adminErrorMessage } from '../../shared/administration-errors';

interface PreferenceGroup {
  module: string;
  items: NotificationPreference[];
}

/**
 * Preferencias de correo por tipo de notificación (Fase 2, Ola I, M1-25): la bandeja siempre recibe la notificación; el
 * usuario decide cuáles recibe además por correo. Los tipos con correo obligatorio (resultado del registro y de la
 * vinculación) y los solo de bandeja (plazos, aduana, revisiones internas) no se pueden cambiar y se explica por qué.
 */
@Component({
  selector: 'app-notification-preferences',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './notification-preferences.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class NotificationPreferencesComponent implements OnInit {
  private readonly service = inject(NotificationService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  readonly typeKeys = NOTIFICATION_TYPE_KEYS;
  readonly moduleKeys = NOTIFICATION_MODULE_KEYS;

  preferences = signal<NotificationPreference[]>([]);
  /** Cambios sin guardar por tipo (`null` = volver al valor por defecto). */
  pending = signal<Record<string, boolean | null>>({});
  loading = signal(true);
  loadFailed = signal(false);
  saving = signal(false);
  saveError = signal('');

  groups = computed<PreferenceGroup[]>(() => {
    const prefs = this.preferences();
    const order = [...NOTIFICATION_MODULES] as string[];
    const modules = [...new Set(prefs.map((p) => p.module))].sort((a, b) => order.indexOf(a) - order.indexOf(b));
    return modules.map((module) => ({ module, items: prefs.filter((p) => p.module === module) }));
  });

  changeCount = computed(() => Object.keys(this.pending()).length);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.getPreferences().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (prefs) => {
        this.preferences.set(prefs);
        this.pending.set({});
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.loadFailed.set(true);
        if (!isServiceUnavailable(err)) this.saveError.set(adminErrorMessage(err, 'notifications.preferences.errors.load'));
      },
    });
  }

  locked(p: NotificationPreference): boolean {
    return !p.emailAvailable || p.emailMandatory;
  }

  /** Valor mostrado: el cambio sin guardar, si lo hay. */
  checked(p: NotificationPreference): boolean {
    const change = this.pending()[p.type];
    if (change === undefined) return p.emailEnabled;
    return change ?? p.emailDefault;
  }

  toggle(p: NotificationPreference, event: Event): void {
    const value = (event.target as HTMLInputElement).checked;
    this.pending.update((current) => {
      const next = { ...current };
      if (value === p.emailEnabled && !p.isCustomized) delete next[p.type];
      else next[p.type] = value;
      return next;
    });
  }

  /** Vuelve al valor por defecto del tipo. */
  reset(p: NotificationPreference): void {
    this.pending.update((current) => ({ ...current, [p.type]: null }));
  }

  typeName(p: NotificationPreference): string {
    const key = NOTIFICATION_TYPE_KEYS[p.type];
    return key ? translate(key) : p.type;
  }

  save(event: Event): void {
    event.preventDefault();
    const items: NotificationPreferenceUpdate[] = Object.entries(this.pending()).map(([type, emailEnabled]) => ({ type, emailEnabled }));
    if (items.length === 0) {
      this.announcer.announce(translate('notifications.preferences.noChanges'));
      return;
    }
    this.saving.set(true);
    this.saveError.set('');
    this.service.updatePreferences(items).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (prefs) => {
        this.saving.set(false);
        this.preferences.set(prefs);
        this.pending.set({});
        this.announcer.announce(translate('notifications.preferences.saved', { count: items.length }));
      },
      error: (err) => {
        this.saving.set(false);
        const message = adminErrorMessage(err, 'notifications.preferences.errors.save');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
