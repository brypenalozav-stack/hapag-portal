import { ChangeDetectionStrategy, Component, ElementRef, Injector, afterNextRender, effect, inject, viewChild } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ModalService } from '../../../core/services/modal.service';

/** Dibuja el modal pedido a ModalService. Va una sola vez en el shell (app.html). */
@Component({
  selector: 'app-modal-host',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslocoPipe],
  templateUrl: './modal-host.html',
})
export class ModalHostComponent {
  readonly modal = inject(ModalService);
  private readonly injector = inject(Injector);
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private returnFocus: HTMLElement | null = null;
  private accepted = false;

  constructor() {
    effect(() => {
      const req = this.modal.request();
      const el = this.dialog().nativeElement;
      if (req && !el.open) {
        this.returnFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;
        this.accepted = false;
        el.showModal();
        // El contenido se dibuja después de abrir: el foco inicial va a la opción segura (Cancelar) o al único botón.
        afterNextRender(() => el.querySelector<HTMLElement>('[data-testid="app-modal-cancel"], [data-testid="app-modal-confirm"]')?.focus(), {
          injector: this.injector,
        });
      } else if (!req && el.open) {
        el.close();
      }
    });
  }

  accept(): void {
    this.accepted = true;
    this.dialog().nativeElement.close();
  }

  cancel(): void {
    this.dialog().nativeElement.close();
  }

  /** Clic en el fondo (fuera de la tarjeta) cancela, igual que Esc. */
  onBackdropClick(event: MouseEvent): void {
    if (event.target === this.dialog().nativeElement) this.cancel();
  }

  onClosed(): void {
    const target = this.returnFocus;
    this.returnFocus = null;
    this.modal.settle(this.accepted);
    if (target?.isConnected) target.focus();
  }
}
