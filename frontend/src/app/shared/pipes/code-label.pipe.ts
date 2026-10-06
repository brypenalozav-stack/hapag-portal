import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { LocaleService } from '../../core/services/locale.service';

/**
 * Texto traducido de un código del backend (tipo, estado, perfil, rol…) según un mapa
 * código → clave Transloco (core/i18n/labels.ts). Un código sin clave se muestra tal cual.
 * Impuro para reaccionar al cambio de idioma en caliente.
 */
@Pipe({ name: 'codeLabel', standalone: true, pure: false })
export class CodeLabelPipe implements PipeTransform {
  private readonly transloco = inject(TranslocoService);
  private readonly localeService = inject(LocaleService);

  transform(code: string | null | undefined, keys: Record<string, string>): string {
    if (code === null || code === undefined || code === '') return '';
    const key = keys[code];
    // Lee el idioma activo para recalcular el texto cuando cambia.
    this.localeService.lang();
    return key ? this.transloco.translate(key) : code;
  }
}
