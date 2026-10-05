import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { OrganizationService } from '../../../core/services/organization.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { JoinRequest, ORGANIZATION_PROFILES, OrganizationProfile } from '../../../core/models/organization.model';
import { ORGANIZATION_PROFILE_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';

/**
 * Bandeja de solicitudes de vinculación a la organización (M1-08): el administrador aprueba
 * (eligiendo el perfil, M1-02) o rechaza; el backend notifica el resultado al solicitante.
 */
@Component({
  selector: 'app-join-requests',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './join-requests.html',
})
export class JoinRequestsComponent implements OnInit {
  private readonly service = inject(OrganizationService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  readonly profiles = ORGANIZATION_PROFILES;
  readonly profileKeys = ORGANIZATION_PROFILE_KEYS;

  requests = signal<JoinRequest[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  actionError = signal('');
  busy = signal<string | null>(null);

  /** Perfil elegido por solicitud (por defecto, el de solo consulta). */
  profileFor: Record<string, OrganizationProfile> = {};

  /** Solicitud que se está rechazando y su motivo opcional. */
  rejecting = signal<JoinRequest | null>(null);
  rejectReason = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.getJoinRequests().pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (requests) => {
        this.requests.set(requests);
        for (const r of requests) this.profileFor[r.userId] ??= 'OrgViewer';
        this.loading.set(false);
      },
      error: (err) => {
        this.requests.set([]);
        this.loadFailed.set(isServiceUnavailable(err));
        if (!isServiceUnavailable(err)) this.actionError.set(translate('organization.joinRequests.errors.load'));
        this.loading.set(false);
      },
    });
  }

  approve(request: JoinRequest): void {
    const profile = this.profileFor[request.userId] ?? 'OrgViewer';
    this.busy.set(request.userId);
    this.actionError.set('');
    this.service.approveJoinRequest(request.userId, profile).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.busy.set(null);
        this.remove(request);
        this.announcer.announce(translate('organization.joinRequests.approved', { name: request.fullName }));
      },
      error: () => {
        this.busy.set(null);
        this.actionError.set(translate('organization.joinRequests.errors.approve'));
      },
    });
  }

  startReject(request: JoinRequest): void {
    this.rejectReason = '';
    this.rejecting.set(request);
  }

  cancelReject(): void {
    this.rejecting.set(null);
  }

  confirmReject(): void {
    const request = this.rejecting();
    if (!request) return;
    this.busy.set(request.userId);
    this.actionError.set('');
    this.service.rejectJoinRequest(request.userId, this.rejectReason.trim() || undefined).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: () => {
        this.busy.set(null);
        this.rejecting.set(null);
        this.remove(request);
        this.announcer.announce(translate('organization.joinRequests.rejected', { name: request.fullName }));
      },
      error: () => {
        this.busy.set(null);
        this.actionError.set(translate('organization.joinRequests.errors.reject'));
      },
    });
  }

  private remove(request: JoinRequest): void {
    this.requests.update((list) => list.filter((r) => r.userId !== request.userId));
  }
}
