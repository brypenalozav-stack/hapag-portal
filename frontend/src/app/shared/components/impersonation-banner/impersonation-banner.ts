import { Component, DestroyRef, OnInit, computed, effect, inject, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../../core/services/auth.service';
import { ImpersonationService } from '../../../core/services/impersonation.service';

/** Cada cuánto se recalcula el tiempo restante de la sesión. */
const TICK_MS = 15_000;

/**
 * Banner permanente de la vista como cliente (Fase 2, Ola I, M8-08): identifica al cliente y la organización que se están
 * viendo, que es solo lectura, el tiempo restante y el botón "Terminar", que restaura la sesión del administrador. Las
 * escrituras que el servidor bloquea (403 `Impersonation.ReadOnly`) se informan aquí con `role="alert"`. Fuera de la
 * sesión muestra, una vez, el aviso de que terminó.
 */
@Component({
  selector: 'app-impersonation-banner',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './impersonation-banner.html',
  styleUrl: './impersonation-banner.scss',
})
export class ImpersonationBannerComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly impersonation = inject(ImpersonationService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly now = signal(Date.now());
  readonly session = this.auth.impersonation;

  /** Minutos restantes (redondeados hacia arriba); 0 cuando la sesión ya venció. */
  minutesLeft = computed(() => {
    const s = this.session();
    if (!s) return 0;
    const ms = new Date(s.expiresAt).getTime() - this.now();
    return ms > 0 ? Math.ceil(ms / 60_000) : 0;
  });

  subjectName = computed(() => {
    const s = this.session();
    return s ? s.subject.fullName || s.subject.email : '';
  });

  constructor() {
    // Al iniciar o cambiar la sesión, el tiempo restante se calcula desde ahora (no desde el último tic).
    effect(() => {
      this.session();
      this.now.set(Date.now());
    });
    // Al agotarse el tiempo, la sesión del administrador se restaura sin esperar a la próxima consulta.
    effect(() => {
      if (this.session() && this.minutesLeft() === 0) this.impersonation.expired();
    });
  }

  ngOnInit(): void {
    const timer = setInterval(() => this.now.set(Date.now()), TICK_MS);
    this.destroyRef.onDestroy(() => clearInterval(timer));
    // Confirma la sesión con el servidor (vencida o terminada por otro administrador: 401, se restaura la sesión).
    if (this.auth.isImpersonating()) this.impersonation.refresh().subscribe();
  }

  end(): void {
    this.impersonation.end();
  }
}
