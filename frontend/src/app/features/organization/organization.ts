import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AuthService } from '../../core/services/auth.service';
import { OrganizationService } from '../../core/services/organization.service';
import { OrganizationSummary } from '../../core/models/organization.model';
import { PERMISSIONS } from '../../core/constants/app.constants';
import {
  ORGANIZATION_PROFILE_KEYS,
  ORGANIZATION_STATUS_KEYS,
  ORGANIZATION_TYPE_KEYS,
} from '../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../shared/pipes/code-label.pipe';
import { OrganizationDocumentsComponent } from './organization-documents/organization-documents';
import { OrganizationUsersComponent } from './organization-users/organization-users';
import { JoinRequestsComponent } from './join-requests/join-requests';
import { AccessManagementComponent } from '../access/access-management/access-management';

/**
 * Mi organización: datos y estado del registro (M1-07), documentación de respaldo (M1-07),
 * solicitudes de vinculación (M1-08), usuarios de la organización (M1-02) y la vista única de
 * accesos y permisos (M1-24). Las secciones de gestión se muestran según los permisos del JWT;
 * el servidor vuelve a exigirlos.
 */
@Component({
  selector: 'app-organization',
  standalone: true,
  imports: [
    TranslocoPipe, CodeLabelPipe, LoadingSpinnerComponent, StateMessageComponent,
    OrganizationDocumentsComponent, OrganizationUsersComponent, JoinRequestsComponent, AccessManagementComponent,
  ],
  templateUrl: './organization.html',
  styleUrl: './organization.scss',
})
export class OrganizationComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly service = inject(OrganizationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly typeKeys = ORGANIZATION_TYPE_KEYS;
  readonly statusKeys = ORGANIZATION_STATUS_KEYS;
  readonly profileKeys = ORGANIZATION_PROFILE_KEYS;

  organization = signal<OrganizationSummary | null>(null);
  loading = signal(true);
  error = signal('');
  /** NF-11: la consulta falló con HTTP 5xx o sin conexión. */
  loadFailed = signal(false);

  canManageUsers = computed(() => this.auth.hasPermission(PERMISSIONS.MANAGE_ORGANIZATION_USERS));
  canApproveRequests = computed(() => this.auth.hasPermission(PERMISSIONS.APPROVE_JOIN_REQUESTS));
  canManageAccess = computed(() => this.auth.hasPermission(PERMISSIONS.MANAGE_THIRD_PARTY_ACCESS));

  /** Accesos y permisos (M1-24): organizaciones aprobadas que no son Hapag-Lloyd (el servidor responde 403 al interno). */
  showAccess = computed(() => {
    const org = this.organization();
    return !!org && org.status === 'Approved' && org.organizationType !== 'Internal';
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.loadFailed.set(false);
    this.service.getMine().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (organization) => {
        this.organization.set(organization);
        // Mantiene al día canOperate y los países de operación de la sesión.
        this.auth.setOrganization(organization);
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else {
          this.error.set(translate('organization.loadError'));
        }
        this.loading.set(false);
      },
    });
  }
}
