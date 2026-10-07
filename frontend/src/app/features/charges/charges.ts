import { Component, OnInit, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ChargesPanelComponent } from './charges-panel/charges-panel';

/**
 * Cargos locales por BL con las reglas de Nexus (Fase 1, Ola C; reemplaza a la consulta de cargos
 * locales anterior). El número de BL va en la ruta (/charges/:blNumber) para poder compartirla y
 * volver a ella desde el detalle del embarque.
 */
@Component({
  selector: 'app-charges',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, ChargesPanelComponent],
  template: `
    <div class="hl-page-header">
      <h1>{{ 'charges.title' | transloco }}</h1>
      <p class="text-muted mb-0">{{ 'charges.subtitle' | transloco }}</p>
    </div>

    <div class="hl-card p-4 mb-4">
      <form class="row g-3 hl-form-row" role="search" [attr.aria-label]="'charges.search.label' | transloco" (ngSubmit)="search()">
        <div class="col-md-8 col-lg-6">
          <label for="charges-bl-number" class="form-label fw-semibold">{{ 'charges.search.blNumber' | transloco }}</label>
          <input id="charges-bl-number" type="text" class="form-control" name="blNumber" autocomplete="off"
                 aria-describedby="charges-bl-number-help" [(ngModel)]="searchBlNumber" />
          <div id="charges-bl-number-help" class="form-text">{{ 'charges.search.help' | transloco }}</div>
        </div>
        <div class="col-auto hl-form-action hl-form-action--lg-label">
          <button type="submit" class="btn btn-hl-orange px-4" [disabled]="!searchBlNumber.trim()">{{ 'charges.search.submit' | transloco }}</button>
        </div>
      </form>
    </div>

    @if (activeBl(); as bl) {
      <!-- Vista enfocada: el detalle del BL reúne los cargos con el resto del embarque -->
      <p class="mb-3"><a [routerLink]="['/shipments', bl]" data-testid="view-full-detail">{{ 'shipments.detail.viewFull' | transloco: { bl } }}</a></p>
      <app-charges-panel [blNumber]="bl" />
    } @else {
      <div class="hl-card p-5 text-center" role="status">
        <p class="text-muted mb-0">{{ 'charges.initial' | transloco }}</p>
      </div>
    }
  `,
  styles: [':host { display: block; }'],
})
export class ChargesComponent implements OnInit {
  private readonly router = inject(Router);

  /** Número de BL de la ruta (/charges/:blNumber). */
  blNumber = input<string>();

  searchBlNumber = '';
  activeBl = signal('');

  ngOnInit(): void {
    const bl = this.blNumber()?.trim() ?? '';
    this.searchBlNumber = bl;
    this.activeBl.set(bl);
  }

  search(): void {
    const bl = this.searchBlNumber.trim();
    if (!bl) return;
    this.activeBl.set(bl);
    this.router.navigate(['/charges', bl], { replaceUrl: true });
  }
}
