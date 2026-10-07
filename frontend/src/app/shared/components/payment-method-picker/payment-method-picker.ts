import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { PaymentMethod } from '../../../core/models/cart.model';
import { PaymentLogoComponent } from '../payment-logo/payment-logo';

/** Grupo de medios por tipo, en el orden en que se muestran. */
interface MethodGroup {
  kind: 'online' | 'deposit';
  headingKey: string;
  pillKey: string;
  kindKey: string;
  methods: PaymentMethod[];
}

/**
 * Selector de medio de pago (carro, pago desde la cuenta y cierre del estado de cuenta). Cada medio es una
 * tarjeta seleccionable con su logo, su nombre, una pastilla con el tipo (en línea o depósito) y su descripción,
 * agrupadas por tipo en una grilla (1 columna en móvil, 2 desde md). Por dentro es un grupo de radios nativo
 * dentro de un fieldset: las flechas recorren las opciones, toda la tarjeta es la etiqueta y el fieldset
 * deshabilitado las deshabilita. El nombre accesible de cada opción es "<nombre>, <tipo>" (WCAG 2.5.3: empieza
 * con el nombre visible) y la pastilla y la descripción quedan como descripción.
 */
@Component({
  selector: 'app-payment-method-picker',
  standalone: true,
  imports: [TranslocoPipe, PaymentLogoComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <fieldset class="hl-pmp" [disabled]="disabled()">
      <legend class="form-label small fw-semibold mb-2">{{ legendKey() | transloco }}</legend>
      @for (group of groups(); track group.kind) {
        <div class="hl-pmp-group" role="group" [attr.aria-labelledby]="idPrefix() + '-group-' + group.kind"
             [attr.data-testid]="'payment-method-group-' + group.kind">
          <p class="hl-pmp-heading" [id]="idPrefix() + '-group-' + group.kind">{{ group.headingKey | transloco }}</p>
          <div class="hl-pmp-grid">
            @for (method of group.methods; track method.code) {
              @let id = idPrefix() + '-' + method.code;
              <label class="hl-pmp-card" [for]="id" [attr.data-testid]="'payment-method-card-' + method.code">
                <input class="form-check-input hl-pmp-radio" type="radio" [name]="name()" [id]="id" [value]="method.code"
                       [checked]="selected() === method.code" (change)="selectedChange.emit(method.code)"
                       [attr.aria-label]="'shared.paymentMethodPicker.optionLabel' | transloco: { name: method.name, kind: (group.kindKey | transloco) }"
                       [attr.aria-describedby]="id + '-kind' + (method.description ? ' ' + id + '-desc' : '')" />
                <span class="hl-pmp-body">
                  <span class="hl-pmp-top">
                    <app-payment-logo [method]="method" />
                    <span class="hl-pmp-name">{{ method.name }}</span>
                  </span>
                  <span class="hl-pmp-kind" [id]="id + '-kind'">{{ group.pillKey | transloco }}</span>
                  @if (method.description) {
                    <span class="hl-pmp-desc" [id]="id + '-desc'">{{ method.description }}</span>
                  }
                </span>
                <svg class="hl-pmp-check" viewBox="0 0 16 16" width="20" height="20" aria-hidden="true" focusable="false">
                  <circle cx="8" cy="8" r="8" />
                  <path d="M4.5 8.2l2.3 2.3 4.7-4.9" />
                </svg>
              </label>
            }
          </div>
        </div>
      }
    </fieldset>
  `,
  styles: `
    :host {
      display: block;
    }

    .hl-pmp {
      min-width: 0;
    }

    .hl-pmp-group + .hl-pmp-group {
      margin-top: 1rem;
    }

    .hl-pmp-heading {
      margin: 0 0 0.5rem;
      font-size: 0.8125rem;
      font-weight: 700;
      letter-spacing: 0.02em;
      text-transform: uppercase;
      color: var(--hl-text-muted);
    }

    .hl-pmp-grid {
      display: grid;
      grid-template-columns: minmax(0, 1fr);
      gap: 0.75rem;
    }

    @media (min-width: 768px) {
      .hl-pmp-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }
    }

    .hl-pmp-card {
      position: relative;
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      height: 100%;
      margin: 0;
      padding: 0.875rem 2.75rem 0.875rem 0.875rem;
      border: 1px solid var(--hl-border);
      border-radius: 0.5rem;
      background-color: var(--hl-surface);
      color: var(--hl-body-color);
      cursor: pointer;
      transition: border-color 0.15s, background-color 0.15s, box-shadow 0.15s;
    }

    .hl-pmp-card:hover {
      border-color: var(--hl-link);
    }

    /* Seleccionada: borde grueso del color primario, fondo suave y marca de verificación. */
    .hl-pmp-card:has(.hl-pmp-radio:checked) {
      border-color: var(--hl-link);
      box-shadow: inset 0 0 0 1px var(--hl-link);
      background-color: color-mix(in srgb, var(--hl-link) 7%, var(--hl-surface));
    }

    /* El anillo de foco va en la tarjeta entera (WCAG 2.4.7, 2.4.13). */
    .hl-pmp-card:has(.hl-pmp-radio:focus-visible) {
      outline: 3px solid var(--hl-focus);
      outline-offset: 2px;
    }

    .hl-pmp-radio {
      flex: none;
      margin: 0.35rem 0 0;
      cursor: inherit;
    }

    .hl-pmp-radio:focus-visible {
      outline: none;
    }

    .hl-pmp-card:has(.hl-pmp-radio:disabled) {
      cursor: not-allowed;
      opacity: 0.65;
    }

    .hl-pmp-card:has(.hl-pmp-radio:disabled):hover {
      border-color: var(--hl-border);
    }

    .hl-pmp-body {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 0.35rem;
      min-width: 0;
    }

    .hl-pmp-top {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.5rem 0.625rem;
    }

    .hl-pmp-name {
      font-weight: 700;
      line-height: 1.3;
      color: var(--hl-emphasis);
    }

    .hl-pmp-kind {
      display: inline-block;
      padding: 0.1rem 0.5rem;
      border: 1px solid var(--hl-border);
      border-radius: 999px;
      background-color: var(--hl-surface-muted);
      color: var(--hl-text-muted);
      font-size: 0.75rem;
      font-weight: 600;
      line-height: 1.5;
    }

    .hl-pmp-desc {
      font-size: 0.875rem;
      line-height: 1.4;
      color: var(--hl-text-muted);
    }

    .hl-pmp-check {
      position: absolute;
      top: 0.875rem;
      right: 0.875rem;
      visibility: hidden;
    }

    .hl-pmp-check circle {
      fill: var(--hl-link);
    }

    .hl-pmp-check path {
      fill: none;
      stroke: var(--hl-surface);
      stroke-width: 1.8;
      stroke-linecap: round;
      stroke-linejoin: round;
    }

    .hl-pmp-card:has(.hl-pmp-radio:checked) .hl-pmp-check {
      visibility: visible;
    }

    @media (prefers-reduced-motion: reduce) {
      .hl-pmp-card {
        transition: none;
      }
    }
  `,
})
export class PaymentMethodPickerComponent {
  /** Medios habilitados, en el orden de la configuración. */
  readonly methods = input.required<PaymentMethod[]>();
  /** Código del medio elegido ('' si ninguno). */
  readonly selected = input<string>('');
  /** Atributo name del grupo de radios. */
  readonly name = input.required<string>();
  /** Prefijo de los id: cada radio es "<prefijo>-<código>". */
  readonly idPrefix = input.required<string>();
  readonly disabled = input(false);
  /** Clave de traducción de la leyenda del fieldset. */
  readonly legendKey = input.required<string>();

  readonly selectedChange = output<string>();

  /** En línea primero y luego depósito; los grupos vacíos no se muestran. */
  readonly groups = computed<MethodGroup[]>(() => {
    const all = this.methods();
    const groups: MethodGroup[] = [
      {
        kind: 'online',
        headingKey: 'shared.paymentMethodPicker.group.online',
        pillKey: 'shared.paymentMethodPicker.pill.online',
        kindKey: 'shared.paymentMethodPicker.kindShort.online',
        methods: all.filter((m) => m.kind !== 'Deposit'),
      },
      {
        kind: 'deposit',
        headingKey: 'shared.paymentMethodPicker.group.deposit',
        pillKey: 'shared.paymentMethodPicker.pill.deposit',
        kindKey: 'shared.paymentMethodPicker.kindShort.deposit',
        methods: all.filter((m) => m.kind === 'Deposit'),
      },
    ];
    return groups.filter((g) => g.methods.length > 0);
  });
}
