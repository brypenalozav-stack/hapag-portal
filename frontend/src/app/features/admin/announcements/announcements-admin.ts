import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import { AnnouncementService } from '../../../core/services/announcement.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  ANNOUNCEMENT_OPERATIONS,
  ANNOUNCEMENT_SEVERITIES,
  ANNOUNCEMENT_STATUSES,
  AnnouncementAdmin,
  AnnouncementOperation,
  AnnouncementRequest,
  AnnouncementSeverity,
  AnnouncementSnapshot,
} from '../../../core/models/announcement.model';
import { MaintainerChange } from '../../../core/models/payment-config.model';
import {
  ANNOUNCEMENT_OPERATION_KEYS,
  ANNOUNCEMENT_SEVERITY_KEYS,
  ANNOUNCEMENT_STATUS_CLASS,
  ANNOUNCEMENT_STATUS_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { adminErrorMessage } from '../../../shared/administration-errors';
import { fromDateTimeInput, toDateTimeInput } from '../../../shared/date-input';
import { ChangeLogComponent } from '../payment-config/change-log';
import { ModalService } from '../../../core/services/modal.service';

interface AnnouncementForm {
  titleEs: string;
  titleEn: string;
  bodyEs: string;
  bodyEn: string;
  countryCl: boolean;
  countryBo: boolean;
  operation: AnnouncementOperation;
  severity: AnnouncementSeverity;
  validFrom: string;
  validTo: string;
  notifyOnPublish: boolean;
  publish: boolean;
}

interface FormError {
  fieldId: string;
  key: string;
}

const SNAPSHOT_KEYS: Record<string, string> = {
  titleEs: 'admin.announcements.form.titleEs',
  titleEn: 'admin.announcements.form.titleEn',
  countries: 'admin.announcements.form.countries',
  operation: 'admin.announcements.form.operation',
  severity: 'admin.announcements.form.severity',
  validFrom: 'admin.announcements.form.validFrom',
  validTo: 'admin.announcements.form.validTo',
  status: 'admin.announcements.col.status',
  notifyOnPublish: 'admin.announcements.form.notify',
};

function emptyForm(): AnnouncementForm {
  return {
    titleEs: '', titleEn: '', bodyEs: '', bodyEn: '', countryCl: true, countryBo: false,
    operation: 'Both', severity: 'Info', validFrom: '', validTo: '', notifyOnPublish: false, publish: false,
  };
}

/**
 * Mantenedor de comunicados masivos (Fase 2, Ola I, M1-26; permiso `announcements.manage`): textos en español e inglés,
 * segmentación por país y tipo de operación, vigencia, vista previa, publicación y retiro, y su registro de cambios
 * (NF-15). Al publicar con aviso, el servidor notifica una sola vez a los clientes de los países del comunicado.
 */
@Component({
  selector: 'app-announcements-admin',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, ChangeLogComponent],
  templateUrl: './announcements-admin.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; } .hl-preview__body { white-space: pre-line; }'],
})
export class AnnouncementsAdminComponent implements OnInit {
  private readonly service = inject(AnnouncementService);
  private readonly modal = inject(ModalService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly operations = ANNOUNCEMENT_OPERATIONS;
  readonly severities = ANNOUNCEMENT_SEVERITIES;
  readonly statuses = ANNOUNCEMENT_STATUSES;
  readonly operationKeys = ANNOUNCEMENT_OPERATION_KEYS;
  readonly severityKeys = ANNOUNCEMENT_SEVERITY_KEYS;
  readonly statusKeys = ANNOUNCEMENT_STATUS_KEYS;
  readonly statusClass = ANNOUNCEMENT_STATUS_CLASS;
  readonly snapshotKeys = SNAPSHOT_KEYS;

  status = '';
  country = '';
  items = signal<AnnouncementAdmin[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  actionError = signal('');
  busyId = signal<string | null>(null);

  formOpen = signal(false);
  editing = signal<AnnouncementAdmin | null>(null);
  form: AnnouncementForm = emptyForm();
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');
  preview = signal(false);

  historyItem = signal<AnnouncementAdmin | null>(null);
  history = signal<MaintainerChange<AnnouncementSnapshot>[]>([]);
  historyLoading = signal(false);

  private readonly formHeading = viewChild<ElementRef<HTMLElement>>('formHeading');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly previewHeading = viewChild<ElementRef<HTMLElement>>('previewHeading');
  private readonly historyHeading = viewChild<ElementRef<HTMLElement>>('historyHeading');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.search({ status: this.status, country: this.country }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: (err) => {
        this.items.set([]);
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.actionError.set(adminErrorMessage(err, 'admin.announcements.errors.load'));
      },
    });
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = emptyForm();
    this.openForm();
  }

  openEdit(a: AnnouncementAdmin): void {
    this.editing.set(a);
    this.form = {
      titleEs: a.titleEs, titleEn: a.titleEn, bodyEs: a.bodyEs, bodyEn: a.bodyEn,
      countryCl: a.countries.includes('CL'), countryBo: a.countries.includes('BO'),
      operation: a.operation, severity: a.severity,
      validFrom: toDateTimeInput(a.validFrom), validTo: toDateTimeInput(a.validTo),
      notifyOnPublish: a.notifyOnPublish, publish: false,
    };
    this.openForm();
  }

  private openForm(): void {
    this.errors.set([]);
    this.saveError.set('');
    this.preview.set(false);
    this.formOpen.set(true);
    focusAfterRender(this.injector, () => this.formHeading()?.nativeElement);
  }

  closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
    this.preview.set(false);
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  togglePreview(): void {
    this.preview.update((v) => !v);
    if (this.preview()) focusAfterRender(this.injector, () => this.previewHeading()?.nativeElement);
  }

  previewCountries(): string {
    return [this.form.countryCl ? 'CL' : '', this.form.countryBo ? 'BO' : ''].filter(Boolean).join(', ');
  }

  private validate(): FormError[] {
    const f = this.form;
    const list: FormError[] = [];
    if (!f.titleEs.trim()) list.push({ fieldId: 'announcement-title-es', key: 'admin.announcements.form.errors.titleEs' });
    if (!f.titleEn.trim()) list.push({ fieldId: 'announcement-title-en', key: 'admin.announcements.form.errors.titleEn' });
    if (!f.bodyEs.trim()) list.push({ fieldId: 'announcement-body-es', key: 'admin.announcements.form.errors.bodyEs' });
    if (!f.bodyEn.trim()) list.push({ fieldId: 'announcement-body-en', key: 'admin.announcements.form.errors.bodyEn' });
    if (!f.countryCl && !f.countryBo) list.push({ fieldId: 'announcement-country-cl', key: 'admin.announcements.form.errors.countries' });
    if (f.validFrom && f.validTo && new Date(f.validTo) <= new Date(f.validFrom)) {
      list.push({ fieldId: 'announcement-valid-to', key: 'admin.announcements.form.errors.validity' });
    }
    return list;
  }

  submit(event: Event): void {
    event.preventDefault();
    this.saveError.set('');
    const errors = this.validate();
    this.errors.set(errors);
    if (errors.length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const f = this.form;
    const editing = this.editing();
    const body: AnnouncementRequest = {
      titleEs: f.titleEs.trim(),
      titleEn: f.titleEn.trim(),
      bodyEs: f.bodyEs.trim(),
      bodyEn: f.bodyEn.trim(),
      countries: [...(f.countryCl ? ['CL' as const] : []), ...(f.countryBo ? ['BO' as const] : [])],
      operation: f.operation,
      severity: f.severity,
      validFrom: fromDateTimeInput(f.validFrom),
      validTo: fromDateTimeInput(f.validTo),
      notifyOnPublish: f.notifyOnPublish,
      ...(editing ? {} : { publish: f.publish }),
    };
    this.saving.set(true);
    const request$ = editing ? this.service.update(editing.id, body) : this.service.create(body);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (saved) => {
        this.saving.set(false);
        const key = editing ? 'admin.announcements.form.updated' : saved.status === 'Published' ? 'admin.announcements.form.createdPublished' : 'admin.announcements.form.created';
        this.announcer.announce(translate(key, { title: saved.titleEs }));
        this.closeForm();
        this.load();
        if (editing && this.historyItem()?.id === editing.id) this.openHistory(saved);
      },
      error: (err) => {
        this.saving.set(false);
        const message = adminErrorMessage(err, 'admin.announcements.form.errors.submit');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  publish(a: AnnouncementAdmin): void {
    this.act(a, this.service.publish(a.id), 'admin.announcements.published');
  }

  unpublish(a: AnnouncementAdmin): void {
    this.act(a, this.service.unpublish(a.id), 'admin.announcements.unpublished');
  }

  async remove(a: AnnouncementAdmin): Promise<void> {
    const confirmed = await this.modal.confirm({
      title: 'shared.modal.remove.title',
      message: 'shared.modal.remove.message',
      params: { name: a.titleEs },
      confirmLabel: 'shared.modal.remove.action',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.act(a, this.service.remove(a.id), 'admin.announcements.removed');
  }

  private act(a: AnnouncementAdmin, request$: Observable<unknown>, successKey: string): void {
    this.busyId.set(a.id);
    this.actionError.set('');
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.busyId.set(null);
        this.announcer.announce(translate(successKey, { title: a.titleEs }));
        this.load();
        if (this.historyItem()?.id === a.id) this.openHistory(a);
      },
      error: (err) => {
        this.busyId.set(null);
        const message = adminErrorMessage(err, 'admin.announcements.errors.action');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openHistory(a: AnnouncementAdmin): void {
    this.historyItem.set(a);
    this.historyLoading.set(true);
    this.service.getHistory(a.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (history) => {
        this.history.set(history);
        this.historyLoading.set(false);
        focusAfterRender(this.injector, () => this.historyHeading()?.nativeElement);
      },
      error: () => {
        this.history.set([]);
        this.historyLoading.set(false);
      },
    });
  }

  closeHistory(): void {
    this.historyItem.set(null);
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
