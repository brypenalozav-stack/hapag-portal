import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Ola repetida: 8 crestas de 30 unidades; el desplazamiento de una cresta hace el bucle sin saltos. */
function wavePath(y: number, amplitude: number): string {
  let d = `M0 ${y}`;
  for (let i = 0; i < 8; i++) d += ` q7.5 ${-amplitude} 15 0 t15 0`;
  return `${d} V80 H0 Z`;
}

/** Bahías de contenedores sobre cubierta: [x, y, tono]. Los tonos salen de los tokens del portal. */
const CONTAINERS: readonly (readonly [number, number, string])[] = [
  [32, 36, 'orange'], [42, 36, 'blue'], [52, 36, 'green'], [62, 36, 'orange'], [72, 36, 'blue'], [82, 36, 'green'], [92, 36, 'orange'],
  [32, 28, 'blue'], [42, 28, 'orange'], [52, 28, 'blue'], [62, 28, 'green'], [72, 28, 'orange'], [82, 28, 'blue'],
  [42, 20, 'green'], [52, 20, 'orange'], [62, 20, 'blue'], [72, 20, 'green'],
];

/**
 * Portacontenedores animado (cabecea sobre olas que avanzan), usado por el spinner de sección y por la
 * pantalla de carga global. Es decorativo: quien lo usa anuncia el estado con texto (role="status").
 * Con movimiento reducido queda quieto.
 */
@Component({
  selector: 'app-ship-graphic',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg class="hl-ship" [style.width]="width()" viewBox="0 0 120 72" aria-hidden="true" focusable="false">
      <g class="hl-ship__waves hl-ship__waves--back"><path [attr.d]="backWave" /></g>
      <g class="hl-ship__vessel">
        <rect class="hl-ship__funnel" x="18" y="14" width="5" height="7" rx="1" />
        <rect class="hl-ship__bridge" x="14" y="21" width="13" height="23" rx="1" />
        <rect class="hl-ship__window" x="16" y="24" width="9" height="2.5" />
        <rect class="hl-ship__window" x="16" y="29" width="9" height="2.5" />
        @for (c of containers; track $index) {
          <rect [attr.class]="'hl-ship__box hl-ship__box--' + c[2]" [attr.x]="c[0]" [attr.y]="c[1]" width="9.2" height="7.6" rx="0.6" />
        }
        <path class="hl-ship__hull" d="M6 44 H114 L106 58 H18 Z" />
        <path class="hl-ship__stripe" d="M10 50 H110 L108.6 52.4 H11.6 Z" />
      </g>
      <g class="hl-ship__waves hl-ship__waves--front"><path [attr.d]="frontWave" /></g>
    </svg>
  `,
  styleUrl: './ship-graphic.scss',
})
export class ShipGraphicComponent {
  /** Ancho CSS del gráfico; el alto sigue la proporción 5:3. */
  readonly width = input('7.5rem');

  readonly containers = CONTAINERS;
  readonly backWave = wavePath(55, 3);
  readonly frontWave = wavePath(60, 4);
}
