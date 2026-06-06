import { Component, input } from '@angular/core';

@Component({
  selector: 'app-country-badge',
  standalone: true,
  template: `
    <span class="hl-country-badge">
      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="12" viewBox="0 0 16 12">
        @if (country() === 'CL') {
          <!-- Chile flag simplified -->
          <rect width="16" height="6" fill="#fff"/>
          <rect y="6" width="16" height="6" fill="#D52B1E"/>
          <rect width="5.33" height="6" fill="#0039A6"/>
          <polygon points="2.66,1.2 3.1,2.5 4.4,2.5 3.3,3.3 3.7,4.6 2.66,3.8 1.6,4.6 2,3.3 0.9,2.5 2.2,2.5" fill="#fff"/>
        } @else {
          <!-- Bolivia flag simplified -->
          <rect width="16" height="4" fill="#D52B1E"/>
          <rect y="4" width="16" height="4" fill="#F9E300"/>
          <rect y="8" width="16" height="4" fill="#007934"/>
        }
      </svg>
      {{ country() }}
    </span>
  `,
})
export class CountryBadgeComponent {
  country = input.required<'CL' | 'BO'>();
}
