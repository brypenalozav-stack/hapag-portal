import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { AnnouncementService } from '../../core/services/announcement.service';
import { AuthService } from '../../core/services/auth.service';
import { LocaleService } from '../../core/services/locale.service';
import { Announcement } from '../../core/models/announcement.model';
import { ANNOUNCEMENT_OPERATION_KEYS } from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../shared/pipes/hl-date.pipe';

/**
 * Comunicados vigentes para el cliente (Fase 2, Ola I, M1-26): los publicados dentro de su vigencia para el país de
 * operación (por defecto el del usuario, M1-04) y el tipo de operación (M2-07), cada uno con su fecha de publicación.
 */
@Component({
  selector: 'app-announcements',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './announcements.html',
  styles: [':host { display: block; } .hl-announcement__body { white-space: pre-line; }'],
})
export class AnnouncementsComponent implements OnInit {
  private readonly service = inject(AnnouncementService);
  private readonly auth = inject(AuthService);
  readonly locale = inject(LocaleService);
  private readonly destroyRef = inject(DestroyRef);

  readonly operationKeys = ANNOUNCEMENT_OPERATION_KEYS;

  country: 'CL' | 'BO' | '' = this.auth.getCountry();
  operation: '' | 'Import' | 'Export' = '';
  items = signal<Announcement[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal(false);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set(false);
    this.service.getCurrent({ country: this.country, operation: this.operation }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: (err) => {
        this.items.set([]);
        this.loadFailed.set(isServiceUnavailable(err));
        this.error.set(!isServiceUnavailable(err));
        this.loading.set(false);
      },
    });
  }

  title(a: Announcement): string {
    return this.locale.lang() === 'en' ? a.titleEn : a.titleEs;
  }

  body(a: Announcement): string {
    return this.locale.lang() === 'en' ? a.bodyEn : a.bodyEs;
  }
}
