import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { translate } from '@jsverse/transloco';
import { Observable, catchError, finalize, of, tap } from 'rxjs';
import { AuthService } from './auth.service';
import { AdministrationService } from './administration.service';
import { LiveAnnouncerService } from './live-announcer.service';
import { NotificationService } from './notification.service';
import { ImpersonationSession, ImpersonationStarted } from '../models/administration.model';

/** Ruta del área de administración donde se inician y revisan las sesiones (M8-08). */
const IMPERSONATION_ROUTE = '/admin/impersonation';

/**
 * Vista como cliente (Fase 2, Ola I, M8-08) en el navegador: cambia a las credenciales del cliente, muestra el banner
 * permanente, informa las escrituras que el servidor bloquea (403 `Impersonation.ReadOnly`) y, al terminar o cuando la
 * sesión vence (401 `Impersonation.Ended`), restaura la sesión del administrador.
 */
@Injectable({ providedIn: 'root' })
export class ImpersonationService {
  private readonly auth = inject(AuthService);
  private readonly api = inject(AdministrationService);
  private readonly router = inject(Router);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly notifications = inject(NotificationService);

  /** Escrituras bloqueadas en esta sesión del navegador; el banner muestra el aviso mientras sea mayor que 0. */
  readonly blockedWrites = signal(0);
  /** Aviso para el administrador después de volver a su sesión (clave Transloco). */
  readonly endedNoticeKey = signal<string | null>(null);
  readonly ending = signal(false);

  /** Inicia la sesión con la respuesta de POST /admin/impersonation/sessions y abre el portal del cliente. */
  begin(started: ImpersonationStarted): void {
    this.blockedWrites.set(0);
    this.endedNoticeKey.set(null);
    this.auth.beginImpersonation(started);
    // La campana muestra la bandeja del cliente mientras dura la sesión.
    this.notifications.refreshUnreadCount();
    const name = started.session.subject.fullName || started.session.subject.email;
    this.announcer.announce(translate('shared.impersonation.started', { name, organization: started.session.organization.name }));
    this.router.navigate(['/dashboard']);
  }

  /** Vuelve a leer la sesión en curso (tiempo restante y bloqueos registrados en el servidor). */
  refresh(): Observable<ImpersonationSession | null> {
    return this.api.getCurrentImpersonation().pipe(
      tap((session) => this.auth.setImpersonation(session)),
      catchError(() => of(null)),
    );
  }

  /** Escritura bloqueada por el servidor: la vista como cliente es de solo lectura. */
  notifyBlocked(): void {
    this.blockedWrites.update((n) => n + 1);
    this.announcer.announce(translate('shared.impersonation.blocked'), 'assertive');
  }

  /** Termina la sesión en el servidor (si aún existe) y restaura la del administrador. */
  end(): void {
    if (this.ending()) return;
    this.ending.set(true);
    this.api.endCurrentImpersonation().pipe(
      catchError(() => of(null)),
      finalize(() => {
        this.ending.set(false);
        this.restore('shared.impersonation.endedManual');
      }),
    ).subscribe();
  }

  /** La sesión venció o la terminó otro administrador (401 `Impersonation.Ended`) o se agotó su tiempo. */
  expired(): void {
    if (!this.auth.isImpersonating()) return;
    this.restore('shared.impersonation.endedExpired');
  }

  dismissNotice(): void {
    this.endedNoticeKey.set(null);
  }

  private restore(noticeKey: string): void {
    this.auth.restoreAdminSession();
    this.notifications.refreshUnreadCount();
    this.blockedWrites.set(0);
    this.endedNoticeKey.set(noticeKey);
    this.announcer.announce(translate(noticeKey));
    this.router.navigate([IMPERSONATION_ROUTE]);
  }
}
