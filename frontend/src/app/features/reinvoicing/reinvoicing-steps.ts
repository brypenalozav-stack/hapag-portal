import { Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

/** Pasos de la refacturación IAO (M3-11). */
export type ReinvoicingStep = 'billing' | 'submit' | 'payAccept' | 'issued';

const STEPS: { step: ReinvoicingStep; key: string }[] = [
  { step: 'billing', key: 'reinvoicing.steps.billing' },
  { step: 'submit', key: 'reinvoicing.steps.submit' },
  { step: 'payAccept', key: 'reinvoicing.steps.payAccept' },
  { step: 'issued', key: 'reinvoicing.steps.issued' },
];

/**
 * Pasos de la refacturación IAO: datos de la nueva razón social, aprobación y envío, pago y aceptación, y emisión de la
 * factura nueva. El paso actual se marca con `aria-current="step"` y con texto en negrita, no solo con color.
 */
@Component({
  selector: 'app-reinvoicing-steps',
  standalone: true,
  imports: [TranslocoPipe],
  template: `
    <ol class="hl-steps" [attr.aria-label]="'reinvoicing.steps.label' | transloco" data-testid="reinvoicing-steps">
      @for (s of steps; track s.step; let i = $index) {
        <li class="hl-steps__item" [class.hl-steps__item--current]="s.step === current()" [class.hl-steps__item--done]="i < index()"
            [attr.aria-current]="s.step === current() ? 'step' : null">
          {{ 'reinvoicing.steps.item' | transloco: { number: i + 1, name: (s.key | transloco) } }}
        </li>
      }
    </ol>
  `,
  styles: [`
    :host { display: block; }
    .hl-steps {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem 1.5rem;
      list-style: none;
      margin: 0 0 1rem;
      padding: 0 0 0.75rem;
      border-bottom: 1px solid var(--hl-border);
    }
    .hl-steps__item { font-size: 0.9rem; color: var(--hl-text-muted); }
    .hl-steps__item--done { color: var(--bs-body-color); }
    .hl-steps__item--current {
      font-weight: 700;
      color: var(--hl-emphasis);
      text-decoration: underline;
      text-underline-offset: 0.3rem;
    }
  `],
})
export class ReinvoicingStepsComponent {
  current = input.required<ReinvoicingStep>();

  readonly steps = STEPS;
  index = computed(() => STEPS.findIndex((s) => s.step === this.current()));
}
