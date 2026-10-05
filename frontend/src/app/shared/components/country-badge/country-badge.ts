import { Component, input } from '@angular/core';

@Component({
  selector: 'app-country-badge',
  standalone: true,
  template: `
    <span class="hl-country-badge">
      <!-- Banderas simplificadas: colores nacionales, no tokens de marca (public/flags/) -->
      @if (country() === 'CL') {
        <img src="flags/cl-16x12.svg" alt="" width="16" height="12" />
      } @else {
        <img src="flags/bo-16x12.svg" alt="" width="16" height="12" />
      }
      {{ country() }}
    </span>
  `,
})
export class CountryBadgeComponent {
  country = input.required<'CL' | 'BO'>();
}
