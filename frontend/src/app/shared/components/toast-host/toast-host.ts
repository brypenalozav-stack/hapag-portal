import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { TOAST_DURATION_MS, Toast, ToastService } from '../../../core/services/toast.service';

/**
 * Pila de avisos (ToastService) en la esquina superior derecha. Sin región viva propia: el texto ya lo anuncia
 * LiveAnnouncerService. Cada aviso se cierra solo, salvo mientras el puntero o el foco están encima.
 */
@Component({
  selector: 'app-toast-host',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe],
  templateUrl: './toast-host.html',
  styleUrl: './toast-host.scss',
})
export class ToastHostComponent {
  readonly toastService = inject(ToastService);
  private readonly timers = new Map<number, { handle?: ReturnType<typeof setTimeout>; remaining: number; startedAt: number }>();

  constructor() {
    effect(() => {
      const ids = new Set(this.toastService.toasts().map((t) => t.id));
      for (const id of ids) if (!this.timers.has(id)) this.start(id, TOAST_DURATION_MS);
      for (const id of [...this.timers.keys()]) if (!ids.has(id)) this.clear(id);
    });
    inject(DestroyRef).onDestroy(() => [...this.timers.keys()].forEach((id) => this.clear(id)));
  }

  pause(toast: Toast): void {
    const t = this.timers.get(toast.id);
    if (!t?.handle) return;
    clearTimeout(t.handle);
    t.handle = undefined;
    t.remaining = Math.max(1000, t.remaining - (Date.now() - t.startedAt));
  }

  resume(toast: Toast): void {
    const t = this.timers.get(toast.id);
    if (t && !t.handle) this.start(toast.id, t.remaining);
  }

  dismiss(toast: Toast): void {
    this.toastService.dismiss(toast.id);
  }

  private start(id: number, ms: number): void {
    const handle = setTimeout(() => this.toastService.dismiss(id), ms);
    this.timers.set(id, { handle, remaining: ms, startedAt: Date.now() });
  }

  private clear(id: number): void {
    const t = this.timers.get(id);
    if (t?.handle) clearTimeout(t.handle);
    this.timers.delete(id);
  }
}
