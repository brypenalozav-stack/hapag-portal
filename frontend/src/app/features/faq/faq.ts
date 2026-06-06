import { Component, inject, signal, OnInit, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FAQService } from '../../core/services/faq.service';
import { AuthService } from '../../core/services/auth.service';
import { FAQ } from '../../core/models/faq.model';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { FILTER_ALL } from '../../core/constants/app.constants';

@Component({
  selector: 'app-faq',
  standalone: true,
  imports: [FormsModule, LoadingSpinnerComponent],
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
        this.error.set('Error al cargar las preguntas frecuentes.');
        this.loading.set(false);
      },
    });
  }

  toggleFaq(id: string): void {
    this.openFaqId.update((current) => (current === id ? null : id));
  }

  getCategoryLabel(cat: string): string {
    const labels: Record<string, string> = {
      ALL: 'Todas',
      GENERAL: 'General',
      PAYMENTS: 'Pagos',
      SHIPPING: 'Envíos',
      DOCUMENTATION: 'Documentación',
      DEMURRAGE: 'Demurrage',
    };
    return labels[cat] ?? cat;
  }
}
