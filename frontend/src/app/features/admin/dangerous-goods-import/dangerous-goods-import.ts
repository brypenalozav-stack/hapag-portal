import { Component, DestroyRef, ElementRef, Injector, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DangerousGoodsService } from '../../../core/services/dangerous-goods.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { DangerousGoodImportResult } from '../../../core/models/dangerous-good.model';
import { apiErrorKey } from '../../../core/http/api-error';
import { PORTAL_ERRORS } from '../../../core/i18n/labels';
import { focusAfterRender } from '../../../shared/focus-after-render';

/** Tamaño máximo del CSV que se acepta en el navegador. */
const MAX_FILE_BYTES = 5 * 1024 * 1024;

/**
 * Carga de la base de referencia de mercancías peligrosas desde un CSV (M10-06, permiso maintainers.manage), para
 * incorporar la lista completa cuando Hapag-Lloyd defina la fuente. Una fila existente (mismo número ONU y nombre
 * en inglés) se actualiza; cada alta o cambio queda en el registro de NF-15. El resultado lista las filas omitidas
 * con su motivo.
 */
@Component({
  selector: 'app-dangerous-goods-import',
  standalone: true,
  imports: [TranslocoPipe],
  templateUrl: './dangerous-goods-import.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class DangerousGoodsImportComponent {
  private readonly service = inject(DangerousGoodsService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly header = 'unNumber;nameEs;nameEn;class;subsidiaryRisk;packingGroup;notes;keywords;classified';

  file = signal<File | null>(null);
  fileError = signal('');
  submitting = signal(false);
  submitError = signal('');
  result = signal<DangerousGoodImportResult | null>(null);

  private readonly resultHeading = viewChild<ElementRef<HTMLElement>>('resultHeading');
  private readonly fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');

  onFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.fileError.set('');
    this.result.set(null);
    if (file && file.size > MAX_FILE_BYTES) {
      this.file.set(null);
      this.fileError.set(translate('admin.dangerousGoodsImport.form.errors.tooLarge'));
      return;
    }
    this.file.set(file);
  }

  submit(event: Event): void {
    event.preventDefault();
    this.submitError.set('');
    const file = this.file();
    if (!file) {
      this.fileError.set(translate('admin.dangerousGoodsImport.form.errors.fileRequired'));
      this.announcer.announce(this.fileError(), 'assertive');
      this.fileInput()?.nativeElement.focus();
      return;
    }
    this.submitting.set(true);
    this.announcer.announce(translate('admin.dangerousGoodsImport.form.sending', { name: file.name }));
    this.service.import(file).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.submitting.set(false);
        this.result.set(result);
        this.announcer.announce(translate('admin.dangerousGoodsImport.result.announce', {
          created: result.created,
          updated: result.updated,
          skipped: result.skipped,
        }));
        focusAfterRender(this.injector, () => this.resultHeading()?.nativeElement);
      },
      error: (err) => {
        this.submitting.set(false);
        const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'admin.dangerousGoodsImport.form.errors.submit'));
        this.submitError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }
}
