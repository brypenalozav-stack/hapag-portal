import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AssistantService } from '../../../core/services/assistant.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { AssistantMailbox, AssistantMailboxRequest, KNOWLEDGE_TOPICS, KnowledgeTopic } from '../../../core/models/assistant.model';
import { apiErrorKey } from '../../../core/http/api-error';
import { KNOWLEDGE_TOPIC_KEYS, PORTAL_ERRORS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { ToastService } from '../../../core/services/toast.service';

interface MailboxForm {
  country: 'CL' | 'BO';
  topic: KnowledgeTopic;
  email: string;
  notes: string;
  isActive: boolean;
}

interface FormError {
  fieldId: string;
  key: string;
}

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function emptyForm(): MailboxForm {
  return { country: 'CL', topic: 'GENERAL', email: '', notes: '', isActive: true };
}

/**
 * Casillas de correo a las que el asistente deriva lo que no puede responder (M10-02, permiso maintainers.manage):
 * una por país y tema; si un tema no tiene casilla se usa la de `GENERAL`. Guardar crea o actualiza la casilla del
 * país y tema elegidos (NF-15).
 */
@Component({
  selector: 'app-assistant-mailboxes',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './assistant-mailboxes.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class AssistantMailboxesComponent implements OnInit {
  private readonly service = inject(AssistantService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly topics = KNOWLEDGE_TOPICS;
  readonly topicKeys = KNOWLEDGE_TOPIC_KEYS;

  mailboxes = signal<AssistantMailbox[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  formOpen = signal(false);
  editing = signal(false);
  form: MailboxForm = emptyForm();
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');

  private readonly formHeading = viewChild<ElementRef<HTMLElement>>('formHeading');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getMailboxes().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (mailboxes) => {
        this.mailboxes.set([...mailboxes].sort((a, b) => a.country.localeCompare(b.country) || a.topic.localeCompare(b.topic)));
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.error.set(translate('admin.assistantMailboxes.list.loadError'));
        this.loading.set(false);
      },
    });
  }

  openCreate(): void {
    this.editing.set(false);
    this.form = emptyForm();
    this.openForm();
  }

  openEdit(mailbox: AssistantMailbox): void {
    this.editing.set(true);
    this.form = { country: mailbox.country, topic: mailbox.topic, email: mailbox.email, notes: mailbox.notes ?? '', isActive: mailbox.isActive };
    this.openForm();
  }

  private openForm(): void {
    this.errors.set([]);
    this.saveError.set('');
    this.formOpen.set(true);
    focusAfterRender(this.injector, () => this.formHeading()?.nativeElement);
  }

  closeForm(): void {
    this.formOpen.set(false);
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  submit(event: Event): void {
    event.preventDefault();
    this.saveError.set('');
    const email = this.form.email.trim();
    const errors: FormError[] = [];
    if (!email) errors.push({ fieldId: 'mailbox-email', key: 'admin.assistantMailboxes.form.errors.emailRequired' });
    else if (!EMAIL.test(email)) errors.push({ fieldId: 'mailbox-email', key: 'admin.assistantMailboxes.form.errors.emailInvalid' });
    this.errors.set(errors);
    if (errors.length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const body: AssistantMailboxRequest = {
      country: this.form.country,
      topic: this.form.topic,
      email,
      notes: this.form.notes.trim() || null,
      isActive: this.form.isActive,
    };
    this.saving.set(true);
    this.service.saveMailbox(body).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        this.toast.success(translate('admin.assistantMailboxes.form.saved', {
          country: body.country,
          topic: translate(KNOWLEDGE_TOPIC_KEYS[body.topic]),
        }));
        this.closeForm();
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'admin.assistantMailboxes.form.errors.submit'));
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
