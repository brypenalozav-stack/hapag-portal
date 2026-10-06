import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { Subscription } from 'rxjs';
import { AssistantService } from '../../../core/services/assistant.service';
import { AuthService } from '../../../core/services/auth.service';
import { DocumentService } from '../../../core/services/document.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  ASSISTANT_DEFAULT_RESPONSE_TARGET_MS,
  ASSISTANT_DELIVERY_INTENT,
  ASSISTANT_DELIVERY_PATH,
  ASSISTANT_MESSAGE_MAX_LENGTH,
  AssistantAction,
  AssistantMessage,
  AssistantSession,
  EndAssistantSessionResult,
  MAILBOX_ANSWER_TYPES,
} from '../../../core/models/assistant.model';
import { apiErrorCode, apiErrorKey } from '../../../core/http/api-error';
import { ASSISTANT_ANSWER_TYPE_KEYS, ASSISTANT_CITATION_KIND_KEYS, PORTAL_ERRORS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../pipes/code-label.pipe';
import { HlDatePipe } from '../../pipes/hl-date.pipe';
import { focusAfterRender } from '../../focus-after-render';
import { saveBlob } from '../../save-blob';
import { isServiceUnavailable } from '../state-message/state-message';

/** Conversación de la pestaña (M10-01): se recupera su historial al recargar mientras dure la sesión. */
const SESSION_KEY = 'hl_assistant_session';

/** Mensaje de la conversación con la casilla de derivación de su respuesta, si la hay. */
interface ConversationMessage extends AssistantMessage {
  mailboxEmail?: string | null;
  pending?: boolean;
}

/** Ruta de la API absoluta (`/api/v1/...`) que informa el servidor. */
const MAILTO = /^mailto:/i;

/**
 * Asistente del portal (M10-01 a M10-03, M10-05): botón flotante y panel de conversación no modal, disponible en
 * todas las pantallas autenticadas sin abandonar la sección. Mantiene el historial mientras dure la sesión (también
 * al navegar o recargar la pestaña), muestra cada mensaje con su autor, las fuentes y las acciones (abrir el BL o los
 * cargos, descargar un documento con la misma descarga del repositorio, ir al carro, escribir a la casilla), y
 * distingue las respuestas "no disponible", derivación a la casilla y rechazo. El estado "escribiendo" y "en proceso"
 * (si la espera supera la meta de NF-18) se anuncia en la región polite. Escape cierra el panel y devuelve el foco al
 * botón. Al terminar la conversación se ofrece el respaldo por correo al registrado o a otro (M10-05). Fase 2, Ola J:
 * entrega de documentos (M10-04): la respuesta ofrece descargar los documentos del repositorio que el usuario puede ver,
 * con el nombre que informa el servidor, o explica que no hay ninguno disponible para él; se anuncia cuántos hay.
 */
@Component({
  selector: 'app-assistant',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe],
  templateUrl: './assistant.html',
  styleUrl: './assistant.scss',
})
export class AssistantComponent {
  private readonly service = inject(AssistantService);
  private readonly auth = inject(AuthService);
  private readonly documents = inject(DocumentService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly maxLength = ASSISTANT_MESSAGE_MAX_LENGTH;
  readonly answerTypeKeys = ASSISTANT_ANSWER_TYPE_KEYS;
  readonly citationKeys = ASSISTANT_CITATION_KIND_KEYS;

  open = signal(false);
  session = signal<AssistantSession | null>(null);
  messages = signal<ConversationMessage[]>([]);
  starting = signal(false);
  startError = signal('');

  draft = signal('');
  sending = signal(false);
  /** NF-18: la espera superó la meta de respuesta. */
  slow = signal(false);
  sendError = signal('');
  /** La conversación terminó o venció: hay que iniciar otra. */
  closedKey = signal('');
  private responseTargetMs = ASSISTANT_DEFAULT_RESPONSE_TARGET_MS;
  private slowTimer: ReturnType<typeof setTimeout> | undefined;
  private sendSub: Subscription | undefined;

  // Cierre con respaldo por correo (M10-05)
  ending = signal(false);
  sendTranscript = signal(false);
  useOtherEmail = signal(false);
  otherEmail = signal('');
  endSubmitted = signal(false);
  endBusy = signal(false);
  endError = signal('');
  endResult = signal<EndAssistantSessionResult | null>(null);

  downloadError = signal('');
  /** Ruta de la entrega que se está descargando (M10-04). */
  deliveryBusy = signal<string | null>(null);

  private readonly launcher = viewChild<ElementRef<HTMLButtonElement>>('launcher');
  private readonly input = viewChild<ElementRef<HTMLTextAreaElement>>('messageInput');
  private readonly list = viewChild<ElementRef<HTMLElement>>('messageList');
  private readonly endHeading = viewChild<ElementRef<HTMLElement>>('endHeading');
  private readonly endResultHeading = viewChild<ElementRef<HTMLElement>>('endResultHeading');
  private readonly otherEmailInput = viewChild<ElementRef<HTMLInputElement>>('otherEmailInput');

  readonly userEmail = computed(() => this.session()?.userEmail ?? this.auth.currentUser()?.email ?? '');
  readonly canSend = computed(() => !!this.session() && !this.closedKey() && !this.endResult());

  readonly otherEmailError = computed(() => {
    if (!this.endSubmitted() || !this.sendTranscript() || !this.useOtherEmail()) return '';
    const email = this.otherEmail().trim();
    if (!email) return 'shared.assistant.end.errors.emailRequired';
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email) ? '' : 'shared.assistant.end.errors.emailInvalid';
  });

  constructor() {
    // Al cerrar la sesión del portal se olvida la conversación.
    effect(() => {
      const authenticated = this.auth.isAuthenticated();
      untracked(() => {
        if (!authenticated) this.reset(true);
      });
    });
    this.destroyRef.onDestroy(() => clearTimeout(this.slowTimer));
  }

  toggle(): void {
    if (this.open()) this.close();
    else this.show();
  }

  show(): void {
    this.open.set(true);
    if (!this.session() && !this.starting()) this.restoreOrStart();
    focusAfterRender(this.injector, () => this.input()?.nativeElement ?? this.endHeading()?.nativeElement);
    this.scrollToEnd();
  }

  /** Cierra el panel sin terminar la conversación; el foco vuelve al botón. */
  close(): void {
    this.open.set(false);
    focusAfterRender(this.injector, () => this.launcher()?.nativeElement);
  }

  onPanelKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.stopPropagation();
      this.close();
    }
  }

  private restoreOrStart(): void {
    const stored = this.storedSessionId();
    if (!stored) {
      this.startSession();
      return;
    }
    this.starting.set(true);
    this.service.getSession(stored).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (session) => {
        this.starting.set(false);
        if (session.status !== 'Active') {
          this.startSession();
          return;
        }
        this.applySession(session);
      },
      // La conversación guardada ya no existe o es de otro usuario: se inicia una nueva.
      error: () => {
        this.starting.set(false);
        this.startSession();
      },
    });
  }

  startSession(): void {
    this.reset(false);
    this.starting.set(true);
    this.startError.set('');
    this.service.startSession().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (session) => {
        this.starting.set(false);
        this.applySession(session);
        this.announcer.announce(translate('shared.assistant.started'));
        focusAfterRender(this.injector, () => this.input()?.nativeElement);
      },
      error: (err) => {
        this.starting.set(false);
        const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'shared.assistant.errors.start'));
        this.startError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  private applySession(session: AssistantSession): void {
    this.session.set(session);
    this.messages.set(session.messages);
    this.saveSessionId(session.id);
    this.scrollToEnd();
  }

  onDraft(event: Event): void {
    this.draft.set((event.target as HTMLTextAreaElement).value);
  }

  /** Enter envía; Mayús+Enter agrega una línea. */
  onDraftKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
      event.preventDefault();
      this.send();
    }
  }

  send(event?: Event): void {
    event?.preventDefault();
    const session = this.session();
    const text = this.draft().trim();
    if (!session || this.sending() || !this.canSend()) return;
    if (!text) {
      this.sendError.set(translate('shared.assistant.errors.empty'));
      this.announcer.announce(this.sendError(), 'assertive');
      return;
    }
    this.sendError.set('');
    this.downloadError.set('');
    this.sending.set(true);
    this.slow.set(false);
    this.draft.set('');
    const pendingId = `pending-${Date.now()}`;
    this.messages.update((list) => [...list, this.pendingMessage(pendingId, text)]);
    this.scrollToEnd();
    this.announcer.announce(translate('shared.assistant.typing'));
    clearTimeout(this.slowTimer);
    this.slowTimer = setTimeout(() => {
      this.slow.set(true);
      this.announcer.announce(translate('shared.assistant.processing'));
    }, this.responseTargetMs);

    this.sendSub = this.service.sendMessage(session.id, text).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (reply) => {
        this.finishSending();
        this.responseTargetMs = reply.responseTargetMs || ASSISTANT_DEFAULT_RESPONSE_TARGET_MS;
        this.messages.update((list) => [
          ...list.filter((m) => m.id !== pendingId),
          reply.userMessage,
          { ...reply.reply, mailboxEmail: reply.mailboxEmail },
        ]);
        this.scrollToEnd();
        this.announcer.announce(this.replyAnnouncement(reply.reply));
      },
      error: (err) => {
        this.finishSending();
        this.messages.update((list) => list.filter((m) => m.id !== pendingId));
        const code = apiErrorCode(err);
        if (code === 'AssistantSession.Expired' || code === 'AssistantSession.Ended' || code === 'AssistantSession.NotFound') {
          this.closedKey.set(PORTAL_ERRORS[code]);
          this.clearSessionId();
          this.announcer.announce(translate(PORTAL_ERRORS[code]), 'assertive');
          return;
        }
        // El mensaje no se pierde: vuelve al campo para reintentar.
        this.draft.set(text);
        const message = err instanceof HttpErrorResponse && err.status === 429
          ? translate('common.portalErrors.rateLimited')
          : translate(apiErrorKey(err, PORTAL_ERRORS, 'shared.assistant.errors.send'));
        this.sendError.set(message);
        this.announcer.announce(message, 'assertive');
        focusAfterRender(this.injector, () => this.input()?.nativeElement);
      },
    });
  }

  /** Anuncio de la respuesta; en una entrega de documentos (M10-04) dice cuántos hay para descargar o que no hay. */
  private replyAnnouncement(reply: AssistantMessage): string {
    const content = translate('shared.assistant.replied', { content: reply.content });
    if (!this.isDelivery(reply)) return content;
    const count = this.deliveryCount(reply);
    const delivery = count > 0
      ? translate('shared.assistant.delivery.ready', { count })
      : translate('shared.assistant.delivery.none');
    return `${content} ${delivery}`;
  }

  private finishSending(): void {
    clearTimeout(this.slowTimer);
    this.sending.set(false);
    this.slow.set(false);
    this.sendSub = undefined;
  }

  private pendingMessage(id: string, content: string): ConversationMessage {
    return {
      id,
      sequence: Number.MAX_SAFE_INTEGER,
      role: 'User',
      content,
      citations: [],
      actions: [],
      engineFallback: false,
      createdAt: new Date().toISOString(),
      pending: true,
    };
  }

  /** La respuesta deriva a la casilla de correo (no disponible, fuente caída, sin respuesta o rechazo). */
  derives(message: ConversationMessage): boolean {
    return !!message.answerType && MAILBOX_ANSWER_TYPES.includes(message.answerType);
  }

  /** Ruta de la pantalla de una acción de navegación (null si la acción no navega). */
  actionRoute(action: AssistantAction): string[] | null {
    switch (action.type) {
      case 'OpenShipment':
        return action.blNumber ? ['/shipments', action.blNumber] : ['/shipments'];
      case 'OpenCharges':
        return action.blNumber ? ['/charges', action.blNumber] : ['/charges'];
      case 'OpenInvoices':
        return ['/invoices'];
      case 'OpenCart':
        return ['/cart'];
      default:
        return null;
    }
  }

  isDownload(action: AssistantAction): boolean {
    return (action.type === 'DownloadDocument' || action.type === 'DownloadInvoice' || action.type === 'DownloadReceipt') && !!action.path;
  }

  isMailto(action: AssistantAction): boolean {
    return action.type === 'ContactMailbox' && !!action.path && MAILTO.test(action.path);
  }

  /** Descarga con la sesión del usuario y las mismas restricciones que el repositorio (M6-09, NF-14). */
  download(action: AssistantAction): void {
    if (!action.path) return;
    if (ASSISTANT_DELIVERY_PATH.test(action.path)) {
      this.downloadDelivery(action, action.path);
      return;
    }
    this.downloadError.set('');
    const segments = action.path.split('/').filter((s) => s !== '');
    const name = `${action.blNumber ?? segments.at(-2) ?? 'documento'}.pdf`;
    this.documents.downloadRelated(action.path).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        saveBlob(blob, name);
        this.announcer.announce(translate('shared.assistant.downloaded', { label: action.label }));
      },
      error: (err) => {
        const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'shared.assistant.errors.download'));
        this.downloadError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /**
   * Documento entregado por el asistente (M10-04): se descarga por la entrega de la conversación, con el nombre que
   * informa el servidor. Los permisos se validan otra vez al descargar: si el usuario perdió el acceso, se explica.
   */
  private downloadDelivery(action: AssistantAction, path: string): void {
    if (this.deliveryBusy()) return;
    this.downloadError.set('');
    this.deliveryBusy.set(path);
    this.service.downloadDelivery(path).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (file) => {
        this.deliveryBusy.set(null);
        // Sin Content-Disposition legible, el número del documento (última palabra de la etiqueta) nombra el archivo.
        const fallback = `${action.label.trim().split(/\s+/).at(-1) || action.blNumber || 'documento'}.pdf`;
        saveBlob(file.blob, file.fileName ?? fallback);
        this.announcer.announce(translate('shared.assistant.downloaded', { label: action.label }));
      },
      error: (err) => {
        this.deliveryBusy.set(null);
        const message = isServiceUnavailable(err)
          ? translate('shared.assistant.delivery.unavailable')
          : translate(apiErrorKey(err, PORTAL_ERRORS, 'shared.assistant.errors.download'));
        this.downloadError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  /** La respuesta entrega documentos del embarque (M10-04). */
  isDelivery(message: ConversationMessage): boolean {
    return message.intent === ASSISTANT_DELIVERY_INTENT;
  }

  /** Cantidad de documentos que ofrece una respuesta de entrega. */
  deliveryCount(message: ConversationMessage): number {
    return message.actions.filter((a) => this.isDownload(a)).length;
  }

  /** Abre la pantalla de la acción; el panel queda abierto con la conversación. */
  navigate(event: Event, action: AssistantAction): void {
    const route = this.actionRoute(action);
    if (!route) return;
    event.preventDefault();
    this.router.navigate(route);
  }

  // Cierre de la conversación (M10-05)
  startEnd(): void {
    this.ending.set(true);
    this.endSubmitted.set(false);
    this.endError.set('');
    focusAfterRender(this.injector, () => this.endHeading()?.nativeElement);
  }

  cancelEnd(): void {
    this.ending.set(false);
    focusAfterRender(this.injector, () => this.input()?.nativeElement);
  }

  onSendTranscript(event: Event): void {
    this.sendTranscript.set((event.target as HTMLInputElement).checked);
  }

  onRecipient(other: boolean): void {
    this.useOtherEmail.set(other);
  }

  onOtherEmail(event: Event): void {
    this.otherEmail.set((event.target as HTMLInputElement).value);
  }

  confirmEnd(event: Event): void {
    event.preventDefault();
    const session = this.session();
    if (!session || this.endBusy()) return;
    this.endSubmitted.set(true);
    this.endError.set('');
    if (this.otherEmailError()) {
      this.announcer.announce(translate(this.otherEmailError()), 'assertive');
      focusAfterRender(this.injector, () => this.otherEmailInput()?.nativeElement);
      return;
    }
    const sendTranscript = this.sendTranscript();
    this.endBusy.set(true);
    this.service.endSession(session.id, {
      sendTranscript,
      email: sendTranscript && this.useOtherEmail() ? this.otherEmail().trim() : null,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.endBusy.set(false);
        this.ending.set(false);
        this.endResult.set(result);
        this.clearSessionId();
        this.announcer.announce(this.endMessage(result));
        focusAfterRender(this.injector, () => this.endResultHeading()?.nativeElement);
      },
      error: (err) => {
        this.endBusy.set(false);
        const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'shared.assistant.end.errors.submit'));
        this.endError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  endMessage(result: EndAssistantSessionResult): string {
    return result.transcriptSent && result.transcriptSentTo
      ? translate('shared.assistant.end.doneWithTranscript', { email: result.transcriptSentTo })
      : translate('shared.assistant.end.done');
  }

  /** Nueva conversación tras terminar o vencer la anterior. */
  newConversation(): void {
    this.clearSessionId();
    this.startSession();
  }

  private reset(forget: boolean): void {
    this.sendSub?.unsubscribe();
    this.finishSending();
    this.session.set(null);
    this.messages.set([]);
    this.draft.set('');
    this.sendError.set('');
    this.closedKey.set('');
    this.ending.set(false);
    this.sendTranscript.set(false);
    this.useOtherEmail.set(false);
    this.otherEmail.set('');
    this.endSubmitted.set(false);
    this.endError.set('');
    this.endResult.set(null);
    this.downloadError.set('');
    this.deliveryBusy.set(null);
    if (forget) {
      this.open.set(false);
      this.clearSessionId();
    }
  }

  /** Lleva la lista al último mensaje después del render. */
  private scrollToEnd(): void {
    afterNextRender({
      write: () => {
        const el = this.list()?.nativeElement;
        if (el) el.scrollTop = el.scrollHeight;
      },
    }, { injector: this.injector });
  }

  private storedSessionId(): string | null {
    try {
      return sessionStorage.getItem(SESSION_KEY);
    } catch {
      return null;
    }
  }

  private saveSessionId(id: string): void {
    try {
      sessionStorage.setItem(SESSION_KEY, id);
    } catch {
      // Sin almacenamiento: el historial se conserva mientras la página siga abierta.
    }
  }

  private clearSessionId(): void {
    try {
      sessionStorage.removeItem(SESSION_KEY);
    } catch {
      // Sin almacenamiento disponible: no hay nada que limpiar.
    }
  }
}
