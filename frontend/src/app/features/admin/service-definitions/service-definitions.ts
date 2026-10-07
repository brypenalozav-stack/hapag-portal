import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { ServiceDefinitionService } from '../../../core/services/service-definition.service';
import { LocaleService } from '../../../core/services/locale.service';
import { ServiceDefinition } from '../../../core/models/service-request.model';
import {
  CHARGE_CONCEPT_KEYS,
  SERVICE_PRICING_MODE_KEYS,
  SERVICE_TEAM_KEYS,
  SHIPMENT_OPERATION_KEYS,
} from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { localized, serviceErrorMessage } from '../../service-requests/shared/service-text';
import { ClientTable, codeText } from '../../../shared/utils/client-table';
import { TableFilterComponent } from '../../../shared/components/table-filter/table-filter';
import { SortHeaderComponent } from '../../../shared/components/sort-header/sort-header';
import { PaginatorComponent } from '../../../shared/components/paginator/paginator';

/**
 * Mantenedor de servicios on demand (M2-03, M2-04; permiso `maintainers.manage`): definiciones por país y operación,
 * con su cobro, equipos y estado. Crear y editar se hace en /admin/service-definitions/new y /:id, junto con el
 * registro de cambios (NF-15).
 */
@Component({
  selector: 'app-service-definitions',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, LoadingSpinnerComponent, StateMessageComponent, TableFilterComponent, SortHeaderComponent, PaginatorComponent],
  templateUrl: './service-definitions.html',
  styles: [':host { display: block; }'],
})
export class ServiceDefinitionsComponent implements OnInit {
  private readonly service = inject(ServiceDefinitionService);
  private readonly locale = inject(LocaleService);
  private readonly destroyRef = inject(DestroyRef);

  readonly conceptKeys = CHARGE_CONCEPT_KEYS;
  readonly pricingKeys = SERVICE_PRICING_MODE_KEYS;
  readonly teamKeys = SERVICE_TEAM_KEYS;
  readonly operationKeys = SHIPMENT_OPERATION_KEYS;

  country = '';
  operation = '';
  includeInactive = false;

  definitions = signal<ServiceDefinition[]>([]);
  /** Filtro rápido, orden y paginación en el navegador sobre el resultado de la búsqueda. */
  readonly table = new ClientTable(this.definitions, {
    searchText: (d) =>
      [
        this.name(d),
        d.code,
        d.operations.map((op) => codeText(op, this.operationKeys)).join(' '),
        d.countries.join(' '),
        codeText(d.pricingMode, this.pricingKeys),
        codeText(d.chargeConceptCode, this.conceptKeys),
        codeText(d.approvalTeam, this.teamKeys),
        codeText(d.fulfillmentTeam, this.teamKeys),
      ].join(' '),
    sortValues: {
      order: (d) => d.displayOrder,
      service: (d) => this.name(d),
      pricing: (d) => codeText(d.pricingMode, this.pricingKeys),
      status: (d) => d.isActive,
    },
  });
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.list({ includeInactive: this.includeInactive, country: this.country, operation: this.operation })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (definitions) => {
          this.definitions.set([...definitions].sort((a, b) => a.displayOrder - b.displayOrder));
          this.loading.set(false);
        },
        error: (err) => {
          if (isServiceUnavailable(err)) this.loadFailed.set(true);
          else this.error.set(serviceErrorMessage(err, 'admin.serviceDefinitions.list.loadError'));
          this.loading.set(false);
        },
      });
  }

  clearFilters(): void {
    this.country = '';
    this.operation = '';
    this.includeInactive = false;
    this.search();
  }

  name(d: ServiceDefinition): string {
    return localized(this.locale.lang(), d.nameEs, d.nameEn);
  }
}
