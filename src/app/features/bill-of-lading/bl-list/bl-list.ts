import { Component, inject, signal, OnInit, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { BillOfLadingService } from '../../../core/services/bl.service';
import { AuthService } from '../../../core/services/auth.service';
import { BillOfLading } from '../../../core/models/bl.model';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { CountryBadgeComponent } from '../../../shared/components/country-badge/country-badge';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';

@Component({
  selector: 'app-bl-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, StatusBadgeComponent, CountryBadgeComponent, LoadingSpinnerComponent],
  templateUrl: './bl-list.html',
  styleUrl: './bl-list.scss',
})
export class BLListComponent implements OnInit {
  private readonly blService = inject(BillOfLadingService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  bls = signal<BillOfLading[]>([]);
  loading = signal(false);
  searchTerm = signal('');
  error = signal('');

  ngOnInit(): void {
    this.loadBLs();
  }

  loadBLs(): void {
    this.loading.set(true);
    this.error.set('');
    this.blService.getMyBLs().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (data) => {
        this.bls.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar los BLs. Intente nuevamente.');
        this.loading.set(false);
      },
    });
  }

  search(): void {
    const term = this.searchTerm().trim();
    if (!term) {
      this.loadBLs();
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.blService.getByNumber(term).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (bl) => {
        this.bls.set([bl]);
        this.loading.set(false);
      },
      error: () => {
        this.bls.set([]);
        this.error.set('No se encontró el BL especificado.');
        this.loading.set(false);
      },
    });
  }
}
