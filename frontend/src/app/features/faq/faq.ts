import { Component, inject, signal, OnInit, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { FAQService } from '../../core/services/faq.service';
import { AuthService } from '../../core/services/auth.service';
import { FAQ } from '../../core/models/faq.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { FILTER_ALL } from '../../core/constants/app.constants';

@Component({
  selector: 'app-faq',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, LoadingSpinnerComponent],
  templateUrl: './faq.html',
  styleUrl: './faq.scss',
})
export class FAQComponent implements OnInit {
  private readonly faqService = inject(FAQService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  faqs = signal<FAQ[]>([]);
  loading = signal(false);
  error = signal('');
  searchTerm = signal('');
  selectedCategory = signal(FILTER_ALL);
  openFaqId = signal<string | null>(null);

  categories = computed(() => {
    const cats = new Set(this.faqs().map((f) => f.category));
    return [FILTER_ALL, ...Array.from(cats)];
  });

  filteredFaqs = computed(() => {
    let result = this.faqs();
    const cat = this.selectedCategory();
    if (cat !== FILTER_ALL) {
      result = result.filter((f) => f.category === cat);
    }
    const term = this.searchTerm().toLowerCase().trim();
    if (term) {
      result = result.filter(
        (f) => f.question.toLowerCase().includes(term) || f.answer.toLowerCase().includes(term),
      );
    }
    return result;
  });

  ngOnInit(): void {
    this.loading.set(true);
    this.faqService.getAll(this.auth.isAuthenticated() ? this.auth.getCountry() : undefined).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (data) => {
        this.faqs.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(translate('faq.loadError'));
        this.loading.set(false);
      },
    });
  }

  toggleFaq(id: string): void {
    this.openFaqId.update((current) => (current === id ? null : id));
  }

  /** Clave de traducción de la categoría; null si la categoría no tiene texto (se muestra tal cual). */
  categoryKey(cat: string): string | null {
    const keys: Record<string, string> = {
      ALL: 'faq.categories.all',
      GENERAL: 'faq.categories.general',
      PAYMENTS: 'faq.categories.payments',
      SHIPPING: 'faq.categories.shipping',
      DOCUMENTATION: 'faq.categories.documentation',
      DEMURRAGE: 'faq.categories.demurrage',
    };
    return keys[cat] ?? null;
  }
}
