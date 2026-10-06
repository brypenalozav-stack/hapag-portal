import { Component, DestroyRef, ElementRef, Injector, computed, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DangerousGoodsService } from '../../core/services/dangerous-goods.service';
import { LiveAnnouncerService } from '../../core/services/live-announcer.service';
import { LocaleService } from '../../core/services/locale.service';
import {
  DANGEROUS_GOOD_QUERY_MAX,
  DANGEROUS_GOOD_QUERY_MIN,
  DangerousGood,
  DangerousGoodSearchResult,
} from '../../core/models/dangerous-good.model';
import { apiErrorKey } from '../../core/http/api-error';
import { DANGEROUS_GOOD_RESULT_KEYS, PORTAL_ERRORS } from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { focusAfterRender } from '../../shared/focus-after-render';

/**
 * Buscador de clasificación de mercancías peligrosas (M10-06), para todos los perfiles: por nombre, descripción o
 * número ONU (1203, UN1203 o UN 1203), sin BL ni booking. Muestra la clase, el número ONU y el grupo de embalaje,
 * y dice de forma explícita cuando la carga figura como no clasificada o no hay coincidencias. El resultado es de
 * referencia y no constituye la aprobación operacional del embarque.
 */
@Component({
  selector: 'app-dangerous-goods',
  standalone: true,
  imports: [TranslocoPipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './dangerous-goods.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class DangerousGoodsComponent {
  private readonly service = inject(DangerousGoodsService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly locale = inject(LocaleService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly minLength = DANGEROUS_GOOD_QUERY_MIN;
  readonly maxLength = DANGEROUS_GOOD_QUERY_MAX;

  query = signal('');
  submitted = signal(false);
  searching = signal(false);
  /** NF-11: la consulta falló con HTTP 5xx o sin conexión. */
  searchFailed = signal(false);
  error = signal('');
  result = signal<DangerousGoodSearchResult | null>(null);

  private readonly resultHeading = viewChild<ElementRef<HTMLElement>>('resultHeading');
  private readonly queryInput = viewChild<ElementRef<HTMLInputElement>>('queryInput');

  /** Error del campo: vacío o con menos caracteres de los admitidos. */
  queryError = computed(() => {
    if (!this.submitted()) return '';
    const q = this.query().trim();
    if (!q) return 'dangerousGoods.form.errors.required';
    if (q.length < DANGEROUS_GOOD_QUERY_MIN) return 'dangerousGoods.form.errors.tooShort';
    return '';
  });

  onQuery(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }

  search(event?: Event): void {
    event?.preventDefault();
    this.submitted.set(true);
    if (this.queryError()) {
      this.announcer.announce(translate(this.queryError(), { min: DANGEROUS_GOOD_QUERY_MIN }), 'assertive');
      this.queryInput()?.nativeElement.focus();
      return;
    }
    const q = this.query().trim();
    this.searching.set(true);
    this.searchFailed.set(false);
    this.error.set('');
    this.service.search(q).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.searching.set(false);
        this.result.set(result);
        this.announcer.announce(translate('dangerousGoods.result.announce', {
          count: result.items.length,
          result: translate(DANGEROUS_GOOD_RESULT_KEYS[result.resultCode] ?? 'dangerousGoods.result.noMatch'),
        }));
        focusAfterRender(this.injector, () => this.resultHeading()?.nativeElement);
      },
      error: (err) => {
        this.searching.set(false);
        this.result.set(null);
        if (isServiceUnavailable(err)) {
          this.searchFailed.set(true);
        } else {
          const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'dangerousGoods.form.errors.search'));
          this.error.set(message);
          this.announcer.announce(message, 'assertive');
        }
      },
    });
  }

  /** Nombre en el idioma de la interfaz (la base trae español e inglés). */
  name(item: DangerousGood): string {
    return this.locale.lang() === 'en' ? item.properShippingNameEn : item.properShippingNameEs;
  }

  className(item: DangerousGood): string | null {
    return (this.locale.lang() === 'en' ? item.hazardClassNameEn : item.hazardClassNameEs) ?? null;
  }
}
