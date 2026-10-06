import { Component, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { OrganizationNetworkService } from '../../../core/services/organization-network.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';

/** Código del registro cuando el correo o el RUT corresponden a un transportista pre-creado (M1-09). */
export const PRE_CREATED_ACCOUNT_EXISTS = 'Registration.PreCreatedAccountExists';

/**
 * Aviso del registro y de la solicitud de vinculación (Fase 2, Ola I, M1-09) cuando ya existe una cuenta pre-creada por
 * un cliente para ese correo o RUT: no se crea un duplicado; se dirige al ingreso con la invitación y se puede pedir una
 * nueva. La respuesta del reenvío no revela si la cuenta existe.
 */
@Component({
  selector: 'app-pre-created-notice',
  standalone: true,
  imports: [RouterLink, TranslocoPipe],
  template: `
    <div class="alert alert-warning" role="alert" data-testid="pre-created-notice">
      <h2 class="h6 fw-bold mb-2">{{ 'auth.preCreated.title' | transloco }}</h2>
      <p class="mb-2">{{ 'auth.preCreated.body' | transloco }}</p>
      <div class="d-flex flex-wrap align-items-center gap-2">
        <a routerLink="/login" class="btn btn-sm btn-hl-orange">{{ 'auth.preCreated.goToLogin' | transloco }}</a>
        @if (email()) {
          <button type="button" class="btn btn-sm btn-outline-secondary" [disabled]="sending() || sent()" (click)="resend()" data-testid="pre-created-resend">
            {{ 'auth.preCreated.resend' | transloco }}
          </button>
        }
      </div>
      @if (sent()) {
        <p class="small mb-0 mt-2" role="status">{{ 'auth.preCreated.resent' | transloco }}</p>
      }
    </div>
  `,
})
export class PreCreatedNoticeComponent {
  private readonly service = inject(OrganizationNetworkService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  /** Correo ingresado en el formulario, para reenviar la invitación. */
  email = input('');

  sending = signal(false);
  sent = signal(false);

  resend(): void {
    const email = this.email().trim();
    if (!email) return;
    this.sending.set(true);
    this.service.resendPreCreatedInvitation(email).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.done(),
      // El reenvío responde siempre igual: un error no cambia lo que se le dice al usuario.
      error: () => this.done(),
    });
  }

  private done(): void {
    this.sending.set(false);
    this.sent.set(true);
    this.announcer.announce(translate('auth.preCreated.resent'));
  }
}
