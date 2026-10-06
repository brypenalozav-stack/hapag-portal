import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { OrganizationNetworkService } from '../../../core/services/organization-network.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { apiErrorCode } from '../../../core/http/api-error';
import { CONTACT_LIST_MAX_EMAILS, ContactList, ContactListChange, ContactListsView } from '../../../core/models/organization-network.model';
import { CONTACT_CHANGE_STATUS_KEYS, CONTACT_REPORT_TYPE_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { adminErrorMessage } from '../../../shared/administration-errors';

const EMAIL = /^[^\s@,;]+@[^\s@,;]+\.[^\s@,;]+$/;

/** Correos ingresados separados por coma, punto y coma, espacio o salto de línea; en minúsculas y sin repetir. */
export function parseEmails(text: string): string[] {
  return [...new Set(text.split(/[\s,;]+/).map((e) => e.trim().toLowerCase()).filter(Boolean))];
}

/**
 * Listas de distribución de contactos por tipo de reporte (Fase 2, Ola I, M1-06): el cliente ve y actualiza los correos
 * que reciben el aviso de llegada, las copias de BL, las facturas, el free time y otros reportes; el cambio se propaga
 * al sistema de origen por su API y queda en el historial, también si falla. Si el origen no responde, las listas no se
 * muestran como vigentes. Solo los perfiles que operan pueden editar.
 */
@Component({
  selector: 'app-contact-lists',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './contact-lists.html',
})
export class ContactListsComponent implements OnInit {
  private readonly service = inject(OrganizationNetworkService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly typeKeys = CONTACT_REPORT_TYPE_KEYS;
  readonly statusKeys = CONTACT_CHANGE_STATUS_KEYS;
  readonly maxEmails = CONTACT_LIST_MAX_EMAILS;

  view = signal<ContactListsView | null>(null);
  loading = signal(true);
  loadFailed = signal(false);
  /** Clave del aviso cuando la organización no puede usar las listas (transportista, sin Match Code). */
  noticeKey = signal<string | null>(null);

  editing = signal<string | null>(null);
  emailsText = '';
  fieldError = signal<string | null>(null);
  saving = signal(false);
  saveError = signal('');

  historyType = '';
  history = signal<ContactListChange[]>([]);
  historyLoading = signal(false);

  private readonly emailsField = viewChild<ElementRef<HTMLTextAreaElement>>('emailsField');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.noticeKey.set(null);
    this.service.getContactLists().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (view) => {
        this.view.set(view);
        this.loading.set(false);
        this.loadHistory();
      },
      error: (err) => {
        this.view.set(null);
        this.loading.set(false);
        const code = apiErrorCode(err);
        if (code === 'ContactList.MatchCodeRequired') this.noticeKey.set('organization.contactLists.matchCodeRequired');
        else if (code === 'ContactList.NotAllowed') this.noticeKey.set('organization.contactLists.notAllowed');
        else this.loadFailed.set(true);
      },
    });
  }

  listFor(type: string): ContactList | undefined {
    return this.view()?.lists.find((l) => l.reportType === type);
  }

  startEdit(type: string): void {
    this.editing.set(type);
    this.emailsText = (this.listFor(type)?.emails ?? []).join('\n');
    this.fieldError.set(null);
    this.saveError.set('');
    focusAfterRender(this.injector, () => this.emailsField()?.nativeElement);
  }

  cancelEdit(): void {
    this.editing.set(null);
  }

  private validate(emails: string[]): string | null {
    if (emails.length === 0) return 'organization.contactLists.errors.required';
    if (emails.length > CONTACT_LIST_MAX_EMAILS) return 'organization.contactLists.errors.tooMany';
    if (emails.some((e) => !EMAIL.test(e))) return 'organization.contactLists.errors.invalid';
    return null;
  }

  invalidEmails(): string {
    return parseEmails(this.emailsText).filter((e) => !EMAIL.test(e)).join(', ');
  }

  save(event: Event): void {
    event.preventDefault();
    const type = this.editing();
    if (!type) return;
    const emails = parseEmails(this.emailsText);
    const error = this.validate(emails);
    this.fieldError.set(error);
    this.saveError.set('');
    if (error) {
      this.announcer.announce(translate(error, { max: CONTACT_LIST_MAX_EMAILS, emails: this.invalidEmails() }), 'assertive');
      focusAfterRender(this.injector, () => this.emailsField()?.nativeElement);
      return;
    }
    this.saving.set(true);
    this.service.updateContactList(type, emails).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (list) => {
        this.saving.set(false);
        this.view.update((v) => (v ? { ...v, lists: [...v.lists.filter((l) => l.reportType !== type), list] } : v));
        this.editing.set(null);
        this.announcer.announce(translate('organization.contactLists.saved', { type: translate(CONTACT_REPORT_TYPE_KEYS[type] ?? type), count: list.emails.length }));
        this.loadHistory();
      },
      error: (err) => {
        this.saving.set(false);
        const message = isServiceUnavailable(err) || apiErrorCode(err)?.startsWith('Integration.')
          ? translate('organization.contactLists.errors.sourceFailed')
          : adminErrorMessage(err, 'organization.contactLists.errors.save');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
        this.loadHistory();
      },
    });
  }

  loadHistory(): void {
    this.historyLoading.set(true);
    this.service.getContactListHistory(this.historyType || undefined).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (history) => {
        this.history.set(history);
        this.historyLoading.set(false);
      },
      error: () => {
        this.history.set([]);
        this.historyLoading.set(false);
      },
    });
  }
}
