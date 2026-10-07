import { Component, DestroyRef, ElementRef, Injector, OnInit, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { DemurrageService } from '../../../core/services/demurrage.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { CalculationInputs, DemurrageCalculation, DemurrageStatus } from '../../../core/models/demurrage.model';
import { apiErrorKey } from '../../../core/http/api-error';
import { CHARGE_ERRORS, DATA_SOURCE_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlCurrencyPipe } from '../../../shared/pipes/hl-currency.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { ToastService } from '../../../core/services/toast.service';

interface CalculatorError {
  fieldId: string;
  key: string;
}

/**
 * Calculadora de demurrage (M3-18, demurrage aún no calculado): fecha hasta la que se calcula y
 * contenedores; "Vista previa" no guarda (save=false) y "Calcular y guardar" deja el BL en
 * "calculado y no pagado". Los tramos de la tarifa salen del mantenedor (M8-01) y los días se
 * cuentan en UTC con el calendario del país (NF-22). El servidor rechaza el cálculo si ya existe
 * una factura (Demurrage.InvoiceExists).
 */
@Component({
  selector: 'app-demurrage-calculator',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe, HlCurrencyPipe, HlDatePipe],
  templateUrl: './demurrage-calculator.html',
  styles: [':host { display: block; }'],
})
export class DemurrageCalculatorComponent implements OnInit {
  private readonly service = inject(DemurrageService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  blNumber = input.required<string>();
  inputs = input.required<CalculationInputs>();
  timeZone = input.required<string>();
  /** Cálculo guardado: nuevo estado del BL. */
  saved = output<DemurrageStatus>();

  readonly sourceKeys = DATA_SOURCE_KEYS;

  untilDate = signal('');
  selected = signal<Set<string>>(new Set());
  submitted = signal(false);
  busy = signal(false);
  submitError = signal('');
  preview = signal<DemurrageCalculation | null>(null);

  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('calcErrorSummary');

  errors = computed<CalculatorError[]>(() => {
    if (!this.submitted()) return [];
    const list: CalculatorError[] = [];
    if (!this.untilDate()) list.push({ fieldId: 'demurrage-calc-until', key: 'demurrage.calculator.errors.untilRequired' });
    if (this.selected().size === 0) {
      const first = this.inputs().containers[0]?.containerNumber;
      list.push({ fieldId: first ? `demurrage-calc-container-${first}` : 'demurrage-calc-until', key: 'demurrage.calculator.errors.containersRequired' });
    }
    return list;
  });

  ngOnInit(): void {
    this.untilDate.set(this.inputs().today);
    this.selected.set(new Set(this.inputs().containers.map((c) => c.containerNumber)));
  }

  onUntilDate(event: Event): void {
    this.untilDate.set((event.target as HTMLInputElement).value);
    this.preview.set(null);
  }

  isSelected(containerNumber: string): boolean {
    return this.selected().has(containerNumber);
  }

  toggleContainer(containerNumber: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selected.update((current) => {
      const next = new Set(current);
      if (checked) next.add(containerNumber);
      else next.delete(containerNumber);
      return next;
    });
    this.preview.set(null);
  }

  /** `save=false`: vista previa; `save=true`: guarda y actualiza el estado del BL. */
  calculate(save: boolean): void {
    this.submitted.set(true);
    this.submitError.set('');
    if (this.errors().length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    this.busy.set(true);
    this.service.calculate(this.blNumber(), {
      untilDate: this.untilDate(),
      containerNumbers: [...this.selected()],
      save,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.busy.set(false);
        this.preview.set(result);
        if (save && result.status) {
          this.toast.success(translate('demurrage.calculator.savedAnnouncement'));
          this.saved.emit(result.status);
        } else {
          this.announcer.announce(translate('demurrage.calculator.previewAnnouncement', { count: result.lines.length }));
        }
      },
      error: (err) => {
        this.busy.set(false);
        const message = translate(apiErrorKey(err, CHARGE_ERRORS, 'demurrage.calculator.errors.submit'));
        this.submitError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
