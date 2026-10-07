import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { GuideService } from '../../../core/services/guide.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import { GUIDE_AUDIENCES, GuideAudience, GuideDefinition, GuideRequest, GuideSnapshot, GuideStep } from '../../../core/models/guide.model';
import { MaintainerChange } from '../../../core/models/payment-config.model';
import { GUIDE_AUDIENCE_KEYS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';
import { adminErrorMessage } from '../../../shared/administration-errors';
import { ChangeLogComponent } from '../payment-config/change-log';
import { ModalService } from '../../../core/services/modal.service';
import { ToastService } from '../../../core/services/toast.service';

interface GuideForm {
  code: string;
  nameEs: string;
  nameEn: string;
  descriptionEs: string;
  descriptionEn: string;
  route: string;
  audience: GuideAudience;
  isActive: boolean;
  displayOrder: number;
  steps: GuideStep[];
}

interface FormError {
  fieldId: string;
  key: string;
  params?: Record<string, unknown>;
}

const CODE = /^[a-z0-9][a-z0-9-]{1,49}$/;

const SNAPSHOT_KEYS: Record<string, string> = {
  code: 'admin.guides.form.code',
  nameEs: 'admin.guides.form.nameEs',
  nameEn: 'admin.guides.form.nameEn',
  route: 'admin.guides.form.route',
  audience: 'admin.guides.form.audience',
  isActive: 'admin.guides.form.active',
  displayOrder: 'admin.guides.form.displayOrder',
  version: 'admin.guides.col.version',
};

function emptyStep(route: string, order: number): GuideStep {
  return { order, route, elementKey: '', titleEs: '', titleEn: '', textEs: '', textEn: '' };
}

function emptyForm(): GuideForm {
  return {
    code: '', nameEs: '', nameEn: '', descriptionEs: '', descriptionEn: '', route: '/', audience: 'Client', isActive: true,
    displayOrder: 1, steps: [emptyStep('/', 1)],
  };
}

/**
 * Mantenedor de guías del portal (Fase 2, Ola I, M1-27; permiso `maintainers.manage`): pantalla, público y orden de
 * cada guía y sus pasos (ruta, elemento señalado con `data-guide-key` y textos en español e inglés). Cambiar los pasos
 * sube la versión y la guía se vuelve a ofrecer a los usuarios. Registro de cambios según NF-15.
 */
@Component({
  selector: 'app-guides-admin',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent, ChangeLogComponent],
  templateUrl: './guides-admin.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; }'],
})
export class GuidesAdminComponent implements OnInit {
  private readonly service = inject(GuideService);
  private readonly modal = inject(ModalService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly audiences = GUIDE_AUDIENCES;
  readonly audienceKeys = GUIDE_AUDIENCE_KEYS;
  readonly snapshotKeys = SNAPSHOT_KEYS;

  guides = signal<GuideDefinition[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  actionError = signal('');

  formOpen = signal(false);
  editing = signal<GuideDefinition | null>(null);
  form: GuideForm = emptyForm();
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');

  historyGuide = signal<GuideDefinition | null>(null);
  history = signal<MaintainerChange<GuideSnapshot>[]>([]);
  historyLoading = signal(false);

  private readonly formHeading = viewChild<ElementRef<HTMLElement>>('formHeading');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly historyHeading = viewChild<ElementRef<HTMLElement>>('historyHeading');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.service.search().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (guides) => {
        this.guides.set([...guides].sort((a, b) => a.displayOrder - b.displayOrder));
        this.loading.set(false);
      },
      error: (err) => {
        this.guides.set([]);
        this.loading.set(false);
        if (isServiceUnavailable(err)) this.loadFailed.set(true);
        else this.actionError.set(adminErrorMessage(err, 'admin.guides.errors.load'));
      },
    });
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = emptyForm();
    this.openForm();
  }

  openEdit(g: GuideDefinition): void {
    this.editing.set(g);
    this.form = {
      code: g.code, nameEs: g.nameEs, nameEn: g.nameEn, descriptionEs: g.descriptionEs ?? '', descriptionEn: g.descriptionEn ?? '',
      route: g.route, audience: g.audience, isActive: g.isActive, displayOrder: g.displayOrder,
      steps: [...g.steps].sort((a, b) => a.order - b.order).map((s) => ({ ...s })),
    };
    this.openForm();
  }

  private openForm(): void {
    this.errors.set([]);
    this.saveError.set('');
    this.formOpen.set(true);
    focusAfterRender(this.injector, () => this.formHeading()?.nativeElement);
  }

  closeForm(): void {
    this.formOpen.set(false);
    this.editing.set(null);
  }

  addStep(): void {
    this.form.steps = [...this.form.steps, emptyStep(this.form.route, this.form.steps.length + 1)];
    const index = this.form.steps.length - 1;
    this.announcer.announce(translate('admin.guides.form.stepAdded', { number: index + 1 }));
    focusAfterRender(this.injector, () => document.getElementById(`guide-step-key-${index}`));
  }

  removeStep(index: number): void {
    this.form.steps = this.form.steps.filter((_, i) => i !== index);
    this.announcer.announce(translate('admin.guides.form.stepRemoved', { number: index + 1 }));
  }

  /** Cambia el orden de un paso (subir o bajar) sin arrastrar: accesible con teclado. */
  moveStep(index: number, delta: number): void {
    const target = index + delta;
    if (target < 0 || target >= this.form.steps.length) return;
    const steps = [...this.form.steps];
    [steps[index], steps[target]] = [steps[target], steps[index]];
    this.form.steps = steps;
    this.announcer.announce(translate('admin.guides.form.stepMoved', { from: index + 1, to: target + 1 }));
    focusAfterRender(this.injector, () => document.getElementById(`guide-step-${delta < 0 ? 'up' : 'down'}-${target}`) ?? document.getElementById(`guide-step-key-${target}`));
  }

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  private validate(): FormError[] {
    const f = this.form;
    const list: FormError[] = [];
    if (!this.editing() && !CODE.test(f.code.trim())) list.push({ fieldId: 'guide-code', key: 'admin.guides.form.errors.code' });
    if (!f.nameEs.trim()) list.push({ fieldId: 'guide-name-es', key: 'admin.guides.form.errors.nameEs' });
    if (!f.nameEn.trim()) list.push({ fieldId: 'guide-name-en', key: 'admin.guides.form.errors.nameEn' });
    if (!f.route.trim().startsWith('/')) list.push({ fieldId: 'guide-route', key: 'admin.guides.form.errors.route' });
    if (f.steps.length === 0) list.push({ fieldId: 'guide-add-step', key: 'admin.guides.form.errors.steps' });
    f.steps.forEach((s, i) => {
      const n = { number: i + 1 };
      if (!s.route.trim().startsWith('/')) list.push({ fieldId: `guide-step-route-${i}`, key: 'admin.guides.form.errors.stepRoute', params: n });
      if (!s.elementKey.trim()) list.push({ fieldId: `guide-step-key-${i}`, key: 'admin.guides.form.errors.stepKey', params: n });
      if (!s.titleEs.trim() || !s.titleEn.trim()) list.push({ fieldId: `guide-step-title-es-${i}`, key: 'admin.guides.form.errors.stepTitle', params: n });
      if (!s.textEs.trim() || !s.textEn.trim()) list.push({ fieldId: `guide-step-text-es-${i}`, key: 'admin.guides.form.errors.stepText', params: n });
    });
    return list;
  }

  submit(event: Event): void {
    event.preventDefault();
    this.saveError.set('');
    const errors = this.validate();
    this.errors.set(errors);
    if (errors.length > 0) {
      this.announcer.announce(translate('common.form.invalid'), 'assertive');
      focusAfterRender(this.injector, () => this.errorSummary()?.nativeElement);
      return;
    }
    const f = this.form;
    const editing = this.editing();
    const body: GuideRequest = {
      ...(editing ? {} : { code: f.code.trim() }),
      nameEs: f.nameEs.trim(),
      nameEn: f.nameEn.trim(),
      descriptionEs: f.descriptionEs.trim() || null,
      descriptionEn: f.descriptionEn.trim() || null,
      route: f.route.trim(),
      audience: f.audience,
      isActive: f.isActive,
      displayOrder: Number(f.displayOrder) || 1,
      steps: f.steps.map((s, i) => ({
        order: i + 1, route: s.route.trim(), elementKey: s.elementKey.trim(),
        titleEs: s.titleEs.trim(), titleEn: s.titleEn.trim(), textEs: s.textEs.trim(), textEn: s.textEn.trim(),
      })),
    };
    this.saving.set(true);
    const request$ = editing ? this.service.update(editing.id, body) : this.service.create(body);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.announcer.announce(translate(editing ? 'admin.guides.form.updated' : 'admin.guides.form.created', { name: saved.nameEs, version: saved.version }));
        this.closeForm();
        this.load();
        if (editing && this.historyGuide()?.id === editing.id) this.openHistory(saved);
      },
      error: (err) => {
        this.saving.set(false);
        const message = adminErrorMessage(err, 'admin.guides.form.errors.submit');
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  async remove(g: GuideDefinition): Promise<void> {
    const confirmed = await this.modal.confirm({
      title: 'shared.modal.remove.title',
      message: 'shared.modal.remove.message',
      params: { name: g.nameEs },
      confirmLabel: 'shared.modal.remove.action',
      tone: 'danger',
    });
    if (!confirmed) return;
    this.actionError.set('');
    this.service.remove(g.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.toast.success(translate('admin.guides.removed', { name: g.nameEs }));
        this.load();
      },
      error: (err) => {
        const message = adminErrorMessage(err, 'admin.guides.errors.remove');
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openHistory(g: GuideDefinition): void {
    this.historyGuide.set(g);
    this.historyLoading.set(true);
    this.service.getHistory(g.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (history) => {
        this.history.set(history);
        this.historyLoading.set(false);
        focusAfterRender(this.injector, () => this.historyHeading()?.nativeElement);
      },
      error: () => {
        this.history.set([]);
        this.historyLoading.set(false);
      },
    });
  }

  closeHistory(): void {
    this.historyGuide.set(null);
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
