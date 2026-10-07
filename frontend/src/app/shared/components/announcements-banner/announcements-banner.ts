import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AnnouncementService } from '../../../core/services/announcement.service';
import { LocaleService } from '../../../core/services/locale.service';
import { Announcement } from '../../../core/models/announcement.model';
import { HlDatePipe } from '../../pipes/hl-date.pipe';

/** Comunicados que se muestran en el dashboard; el resto queda en /announcements. */
const MAX_SHOWN = 3;

/**
 * Comunicados vigentes en el dashboard (Fase 2, Ola I, M1-26): los más recientes para el país del usuario, con su fecha
 * de publicación; los importantes se destacan. Si no hay comunicados o la consulta falla, no muestra nada: el dashboard
 * no depende de ellos.
 */
@Component({
  selector: 'app-announcements-banner',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, HlDatePipe],
  template: `
    @if (items().length > 0) {
      <section class="hl-card p-3 mb-4" aria-labelledby="dashboard-announcements-title" data-testid="dashboard-announcements">
        <div class="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-2">
          <h2 id="dashboard-announcements-title" class="section-title">{{ 'announcementsView.dashboard.title' | transloco }}</h2>
          <a routerLink="/announcements" class="small">{{ 'announcementsView.dashboard.viewAll' | transloco: { count: items().length } }}</a>
        </div>
        <ul class="list-unstyled mb-0 d-flex flex-column gap-2">
          @for (a of shown(); track a.id) {
            <li [class]="a.severity === 'Important' ? 'alert alert-warning mb-0 py-2' : 'border-start border-3 ps-2'">
              <span class="fw-semibold d-block">
                @if (a.severity === 'Important') {
                  <span class="visually-hidden">{{ 'common.announcementSeverity.important' | transloco }}: </span>
                }
                {{ title(a) }}
              </span>
              <span class="small d-block">{{ 'announcementsView.publishedAt' | transloco: { date: (a.publishedAt | hlDate: 'date') } }}</span>
            </li>
          }
        </ul>
      </section>
    }
  `,
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class AnnouncementsBannerComponent implements OnInit {
  private readonly service = inject(AnnouncementService);
  private readonly locale = inject(LocaleService);
  private readonly destroyRef = inject(DestroyRef);

  items = signal<Announcement[]>([]);
  shown = computed(() => this.items().slice(0, MAX_SHOWN));

  ngOnInit(): void {
    this.service.getCurrent().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items) => this.items.set(items),
      error: () => this.items.set([]),
    });
  }

  title(a: Announcement): string {
    return this.locale.lang() === 'en' ? a.titleEn : a.titleEs;
  }
}
