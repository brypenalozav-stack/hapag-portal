import { Component, DestroyRef, inject, input, model, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AccessService } from '../../../core/services/access.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { GranteeOrganization } from '../../../core/models/access.model';
import { OrganizationType, REGISTRABLE_ORGANIZATION_TYPES } from '../../../core/models/organization.model';
import { ORGANIZATION_TYPE_KEYS } from '../../../core/i18n/labels';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';

/**
 * Destinatario del acceso (M1-24): busca organizaciones aprobadas por razón social o RUT/NIT
 * (GET /access/grantees) y permite elegir una. El campo de búsqueda tiene el id
 * `<idPrefix>-grantee-search`, que usa el resumen de errores del formulario.
 */
@Component({
  selector: 'app-grantee-picker',
  standalone: true,
  imports: [TranslocoPipe, CodeLabelPipe],
  templateUrl: './grantee-picker.html',
})
export class GranteePickerComponent {
  private readonly service = inject(AccessService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  idPrefix = input.required<string>();
  selected = model<GranteeOrganization | null>(null);
  /** Error a mostrar (clave Transloco), p. ej. destinatario obligatorio. */
  errorKey = input<string | null>(null);

  readonly types = REGISTRABLE_ORGANIZATION_TYPES;
  readonly typeKeys = ORGANIZATION_TYPE_KEYS;

  search = signal('');
  organizationType = signal<OrganizationType | ''>('');
  results = signal<GranteeOrganization[] | null>(null);
  searching = signal(false);
  searchError = signal('');

  onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  onTypeChange(event: Event): void {
    this.organizationType.set((event.target as HTMLSelectElement).value as OrganizationType | '');
  }

  /** Enter en el campo busca sin enviar el formulario que contiene al selector. */
  onSearchKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.runSearch();
    }
  }

  runSearch(): void {
    this.searching.set(true);
    this.searchError.set('');
    this.service.searchGrantees(this.search().trim(), this.organizationType()).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (results) => {
        this.results.set(results);
        this.searching.set(false);
        this.announcer.announce(translate('thirdPartyAccess.granteePicker.resultCount', { count: results.length }));
      },
      error: () => {
        this.results.set([]);
        this.searching.set(false);
        this.searchError.set(translate('thirdPartyAccess.granteePicker.error'));
      },
    });
  }

  choose(organization: GranteeOrganization): void {
    this.selected.set(organization);
  }

  clear(): void {
    this.selected.set(null);
  }
}
