import { Component, OnInit, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { DemurragePanelComponent } from './demurrage-panel/demurrage-panel';

/**
 * Demurrage por BL (Fase 1, Ola C; reemplaza a la consulta anterior): el estado del demurrage
 * decide qué información y qué acción se muestran (M3-18). El número de BL va en la ruta
 * (/demurrage/:blNumber).
 */
@Component({
  selector: 'app-demurrage',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, DemurragePanelComponent],
  templateUrl: './demurrage.html',
  styles: [':host { display: block; }'],
})
export class DemurrageComponent implements OnInit {
  private readonly router = inject(Router);

  /** Número de BL de la ruta (/demurrage/:blNumber). */
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
    this.router.navigate(['/demurrage', bl], { replaceUrl: true });
  }
}
