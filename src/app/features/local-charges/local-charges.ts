import { Component, inject, signal, OnInit, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BillOfLadingService } from '../../core/services/bl.service';
import { AuthService } from '../../core/services/auth.service';
import { LocalCharge } from '../../core/models/bl.model';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { TAX_RATES } from '../../core/constants/app.constants';

@Component({
  selector: 'app-local-charges',
  standalone: true,
  imports: [RouterLink, DecimalPipe, FormsModule, StatusBadgeComponent, LoadingSpinnerComponent],
  templateUrl: './local-charges.html',
  styleUrl: './local-charges.scss',
})
export class LocalChargesComponent implements OnInit {
  private readonly blService = inject(BillOfLadingService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);

  searchBlNumber = '';
  activeBlNumber = signal('');
  charges = signal<LocalCharge[]>([]);
  selectedIds = signal<Set<string>>(new Set());
  loading = signal(false);
  error = signal('');
  searched = signal(false);

  taxRate = computed(() => TAX_RATES[this.auth.getCountry()]);

  pendingCharges = computed(() => this.charges().filter((c) => c.status === 'Pending'));

  selectedCharges = computed(() => {
    const ids = this.selectedIds();
    return this.pendingCharges().filter((c) => ids.has(c.id));
  });

  subtotal = computed(() => this.selectedCharges().reduce((sum, c) => sum + c.amount, 0));
  taxAmount = computed(() => this.subtotal() * this.taxRate());
  total = computed(() => this.subtotal() + this.taxAmount());

  ngOnInit(): void {
    const blNumber = this.route.snapshot.paramMap.get('blNumber');
    if (blNumber) {
      this.searchBlNumber = blNumber;
      this.search();
    }
  }

  search(): void {
    const bl = this.searchBlNumber.trim();
    if (!bl) return;

    this.activeBlNumber.set(bl);
    this.loading.set(true);
    this.error.set('');
    this.searched.set(true);
    this.selectedIds.set(new Set());

    this.blService.getCharges(bl).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (response) => {
        this.charges.set(response.localCharges ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Error al cargar los cargos locales. Verifique el número de BL.');
        this.loading.set(false);
      },
    });
  }

  toggleCharge(id: string): void {
    this.selectedIds.update((ids) => {
      const next = new Set(ids);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  toggleAll(): void {
    const pending = this.pendingCharges();
    if (this.selectedIds().size === pending.length) {
      this.selectedIds.set(new Set());
    } else {
      this.selectedIds.set(new Set(pending.map((c) => c.id)));
    }
  }

  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  get allSelected(): boolean {
    return this.pendingCharges().length > 0 && this.selectedIds().size === this.pendingCharges().length;
  }
}
