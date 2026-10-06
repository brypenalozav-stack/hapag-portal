import { Component, DestroyRef, OnInit, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { AdminOrganizationService } from '../../../core/services/admin-organization.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { PERMISSIONS } from '../../../core/constants/app.constants';
import { AdminOrganizationDetail, OrganizationDocument } from '../../../core/models/organization.model';
import {
  MEMBERSHIP_STATUS_KEYS,
  ORGANIZATION_DOCUMENT_TYPE_KEYS,
  ORGANIZATION_PROFILE_KEYS,
  ORGANIZATION_STATUS_KEYS,
  ORGANIZATION_TYPE_KEYS,
} from '../../../core/i18n/labels';
import { apiErrorKey } from '../../../core/http/api-error';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { HlNumberPipe } from '../../../shared/pipes/hl-number.pipe';

const REVIEW_ERRORS: Record<string, string> = {
  'Organization.MatchCodeExists': 'admin.organizations.review.errors.matchCodeExists',
  'Organization.InvalidStatus': 'admin.organizations.review.errors.invalidStatus',
};

/** Formato del Match Code que acepta el backend. */
const MATCH_CODE_PATTERN = /^[A-Za-z0-9-]+$/;

/**
 * Revisión interna de una organización (M8-04): validación del cliente
 * (PendingValidation → PendingArCheck), punto de control con AR y asignación del Match Code
 * (PendingArCheck → Approved) o rechazo, con descarga de la documentación de respaldo.
 */
@Component({
  selector: 'app-organization-review',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, TranslocoPipe, CodeLabelPipe, HlDatePipe, HlNumberPipe,
    LoadingSpinnerComponent, StateMessageComponent,
  ],
  templateUrl: './organization-review.html',
  styles: [':host { display: block; } .detail-label { font-size: 0.75rem; font-weight: 600; color: var(--hl-text-muted); text-transform: uppercase; letter-spacing: 0.04em; margin-bottom: 0.25rem; } .detail-value { font-weight: 600; color: var(--hl-dark); margin-bottom: 0; }'],
})
export class OrganizationReviewComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly service = inject(AdminOrganizationService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);

  id = input.required<string>();

  readonly typeKeys = ORGANIZATION_TYPE_KEYS;
  readonly statusKeys = ORGANIZATION_STATUS_KEYS;
  readonly profileKeys = ORGANIZATION_PROFILE_KEYS;
  readonly membershipKeys = MEMBERSHIP_STATUS_KEYS;
  readonly documentTypeKeys = ORGANIZATION_DOCUMENT_TYPE_KEYS;

  organization = signal<AdminOrganizationDetail | null>(null);
  loading = signal(true);
  error = signal('');
  loadFailed = signal(false);
  submitting = signal(false);
  actionError = signal('');
  /** Formulario de rechazo abierto. */
  rejecting = signal(false);

  canReview = computed(() => this.auth.hasPermission(PERMISSIONS.REVIEW_ORGANIZATIONS));
  canCheckAr = computed(() => this.auth.hasPermission(PERMISSIONS.CHECK_ORGANIZATIONS_AR));

  validateForm = this.fb.nonNullable.group({
    notes: ['', Validators.maxLength(1000)],
  });

  arForm = this.fb.nonNullable.group({
    matchCode: ['', [Validators.required, Validators.maxLength(20), Validators.pattern(MATCH_CODE_PATTERN)]],
    arReference: ['', Validators.maxLength(100)],
    operatesCl: [false],
    operatesBo: [false],
    notes: ['', Validators.maxLength(1000)],
  });

  rejectForm = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.maxLength(1000)]],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.loadFailed.set(false);
    this.service.getById(this.id()).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (organization) => {
        this.organization.set(organization);
        const countries = organization.operatingCountries.length > 0 ? organization.operatingCountries : [organization.country];
        this.arForm.reset({
          matchCode: organization.matchCode ?? '',
          arReference: organization.arReference ?? '',
          operatesCl: countries.includes('CL'),
          operatesBo: countries.includes('BO'),
          notes: '',
        });
        this.loading.set(false);
      },
      error: (err) => {
        if (isServiceUnavailable(err)) {
          this.loadFailed.set(true);
        } else if (err instanceof HttpErrorResponse && err.status === 404) {
          this.error.set(translate('admin.organizations.review.notFound'));
        } else {
          this.error.set(translate('admin.organizations.review.loadError'));
        }
        this.loading.set(false);
      },
    });
  }

  /** Paso 1 (M8-04): validación del cliente. */
  validate(): void {
    if (this.validateForm.invalid) return;
    const notes = this.validateForm.getRawValue().notes.trim();
    this.run(this.service.validate(this.id(), notes || undefined), 'admin.organizations.review.validated');
  }

  /** Paso 2 (M8-04): control con AR, Match Code y países de operación; la organización queda aprobada. */
  completeArCheck(): void {
    if (this.arForm.invalid) {
      this.arForm.markAllAsTouched();
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      return;
    }
    const raw = this.arForm.getRawValue();
    const operatingCountries: ('CL' | 'BO')[] = [];
    if (raw.operatesCl) operatingCountries.push('CL');
    if (raw.operatesBo) operatingCountries.push('BO');
    this.run(
      this.service.completeArCheck(this.id(), {
        matchCode: raw.matchCode.trim(),
        arReference: raw.arReference.trim() || undefined,
        operatingCountries: operatingCountries.length > 0 ? operatingCountries : undefined,
        notes: raw.notes.trim() || undefined,
      }),
      'admin.organizations.review.approved',
    );
  }

  startReject(): void {
    this.rejectForm.reset();
    this.rejecting.set(true);
  }

  cancelReject(): void {
    this.rejecting.set(false);
  }

  reject(): void {
    if (this.rejectForm.invalid) {
      this.rejectForm.markAllAsTouched();
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      return;
    }
    this.run(
      this.service.reject(this.id(), this.rejectForm.getRawValue().reason.trim()),
      'admin.organizations.review.rejected',
    );
  }

  download(document: OrganizationDocument): void {
    this.actionError.set('');
    this.service.downloadDocument(this.id(), document.id).pipe(
      takeUntilDestroyed(this.destroyRef),
    ).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = window.document.createElement('a');
        link.href = url;
        link.download = document.fileName;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.actionError.set(translate('admin.organizations.review.errors.download')),
    });
  }

  private run(request: Observable<void>, successKey: string): void {
    this.submitting.set(true);
    this.actionError.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.rejecting.set(false);
        this.announcer.announce(translate(successKey));
        this.load();
      },
      error: (err) => {
        this.submitting.set(false);
        this.actionError.set(translate(apiErrorKey(err, REVIEW_ERRORS, 'admin.organizations.review.errors.action')));
      },
    });
  }
}
