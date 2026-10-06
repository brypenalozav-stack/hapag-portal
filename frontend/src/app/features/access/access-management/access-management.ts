import { Component, ElementRef, computed, inject, input, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { OrganizationType } from '../../../core/models/organization.model';
import { AccessGrantsComponent } from '../access-grants/access-grants';
import { AccessDefaultsComponent } from '../access-defaults/access-defaults';
import { OpenAccessComponent } from '../open-access/open-access';
import { EarlyBookingComponent } from '../early-booking/early-booking';
import { AccessAuditComponent } from '../access-audit/access-audit';

export type AccessTab = 'grants' | 'defaults' | 'openAccess' | 'earlyBooking' | 'audit';

interface TabDefinition {
  id: AccessTab;
  labelKey: string;
}

/** Tipos que no configuran terceros por defecto, acceso abierto ni acceso por booking (actúan como receptores). */
const RECEIVER_ONLY_TYPES: readonly OrganizationType[] = ['CustomsAgency', 'Carrier'];

/**
 * Vista única de accesos y permisos (M1-24) dentro de Mi organización: reúne en pestañas los
 * accesos otorgados y recibidos (M1-12, M1-14, M1-15, M1-22, M1-03), los terceros por defecto
 * (M1-13), el acceso abierto (M1-17), el acceso anticipado por booking (M1-20) y la auditoría
 * (M1-23). Sin `org.access.manage` la vista es de solo lectura; el servidor vuelve a validar.
 * Pestañas con el patrón ARIA (flechas, Inicio y Fin).
 */
@Component({
  selector: 'app-access-management',
  standalone: true,
  imports: [
    TranslocoPipe, AccessGrantsComponent, AccessDefaultsComponent, OpenAccessComponent, EarlyBookingComponent, AccessAuditComponent,
  ],
  templateUrl: './access-management.html',
})
export class AccessManagementComponent {
  private readonly host = inject(ElementRef<HTMLElement>);

  canManage = input(false);
  organizationType = input<OrganizationType | null>(null);

  active = signal<AccessTab>('grants');

  tabs = computed<TabDefinition[]>(() => {
    const receiverOnly = RECEIVER_ONLY_TYPES.includes(this.organizationType() as OrganizationType);
    const tabs: TabDefinition[] = [{ id: 'grants', labelKey: 'thirdPartyAccess.management.tabs.grants' }];
    if (!receiverOnly) {
      tabs.push({ id: 'defaults', labelKey: 'thirdPartyAccess.management.tabs.defaults' });
      tabs.push({ id: 'openAccess', labelKey: 'thirdPartyAccess.management.tabs.openAccess' });
      if (this.canManage()) tabs.push({ id: 'earlyBooking', labelKey: 'thirdPartyAccess.management.tabs.earlyBooking' });
    }
    tabs.push({ id: 'audit', labelKey: 'thirdPartyAccess.management.tabs.audit' });
    return tabs;
  });

  select(tab: AccessTab): void {
    this.active.set(tab);
  }

  /** Flechas izquierda/derecha, Inicio y Fin mueven la selección y el foco entre pestañas. */
  onTabKeydown(event: KeyboardEvent, index: number): void {
    const tabs = this.tabs();
    let next: number;
    switch (event.key) {
      case 'ArrowRight':
        next = (index + 1) % tabs.length;
        break;
      case 'ArrowLeft':
        next = (index - 1 + tabs.length) % tabs.length;
        break;
      case 'Home':
        next = 0;
        break;
      case 'End':
        next = tabs.length - 1;
        break;
      default:
        return;
    }
    event.preventDefault();
    this.active.set(tabs[next].id);
    setTimeout(() =>
      (this.host.nativeElement as HTMLElement).querySelector<HTMLElement>(`#access-tab-${tabs[next].id}`)?.focus(),
    );
  }
}
