import { Component, DestroyRef, ElementRef, Injector, OnInit, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, translate } from '@jsverse/transloco';
import { AssistantService } from '../../../core/services/assistant.service';
import { LiveAnnouncerService } from '../../../core/services/live-announcer.service';
import {
  KNOWLEDGE_CONTENT_MAX_LENGTH,
  KNOWLEDGE_TOPICS,
  KnowledgeArticle,
  KnowledgeArticleChange,
  KnowledgeArticleRequest,
  KnowledgeTopic,
} from '../../../core/models/assistant.model';
import { apiErrorKey } from '../../../core/http/api-error';
import { KNOWLEDGE_TOPIC_KEYS, MAINTAINER_ACTION_KEYS, PORTAL_ERRORS } from '../../../core/i18n/labels';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner';
import { StateMessageComponent, isServiceUnavailable } from '../../../shared/components/state-message/state-message';
import { CodeLabelPipe } from '../../../shared/pipes/code-label.pipe';
import { HlDatePipe } from '../../../shared/pipes/hl-date.pipe';
import { focusAfterRender } from '../../../shared/focus-after-render';

interface ArticleForm {
  country: 'CL' | 'BO';
  topic: KnowledgeTopic;
  title: string;
  content: string;
  keywords: string;
  sortOrder: string;
}

interface FormError {
  fieldId: string;
  key: string;
  params?: Record<string, unknown>;
}

function emptyForm(): ArticleForm {
  return { country: 'CL', topic: 'GENERAL', title: '', content: '', keywords: '', sortOrder: '0' };
}

/**
 * Base de conocimiento del asistente por país (M10-02, permiso maintainers.manage): artículos sobre procesos y
 * procedimientos que el asistente entrega tal cual, con tema, palabras clave que ayudan a encontrarlos y orden.
 * Alta, edición, desactivación y registro de cambios (NF-15), sin requerir desarrollo.
 */
@Component({
  selector: 'app-assistant-knowledge',
  standalone: true,
  imports: [FormsModule, TranslocoPipe, CodeLabelPipe, HlDatePipe, LoadingSpinnerComponent, StateMessageComponent],
  templateUrl: './assistant-knowledge.html',
  styles: [':host { display: block; } .section-title { font-size: 1.1rem; font-weight: 700; margin-bottom: 0; } .hl-pre { white-space: pre-line; }'],
})
export class AssistantKnowledgeComponent implements OnInit {
  private readonly service = inject(AssistantService);
  private readonly announcer = inject(LiveAnnouncerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly topics = KNOWLEDGE_TOPICS;
  readonly topicKeys = KNOWLEDGE_TOPIC_KEYS;
  readonly actionKeys = MAINTAINER_ACTION_KEYS;
  readonly contentMax = KNOWLEDGE_CONTENT_MAX_LENGTH;

  // Filtros
  country = '';
  topic = '';
  includeInactive = false;

  articles = signal<KnowledgeArticle[]>([]);
  loading = signal(true);
  loadFailed = signal(false);
  error = signal('');
  actionError = signal('');

  formOpen = signal(false);
  editing = signal<KnowledgeArticle | null>(null);
  form: ArticleForm = emptyForm();
  errors = signal<FormError[]>([]);
  saving = signal(false);
  saveError = signal('');

  historyArticle = signal<KnowledgeArticle | null>(null);
  history = signal<KnowledgeArticleChange[]>([]);
  historyLoading = signal(false);

  private readonly formHeading = viewChild<ElementRef<HTMLElement>>('formHeading');
  private readonly errorSummary = viewChild<ElementRef<HTMLElement>>('errorSummary');
  private readonly historyHeading = viewChild<ElementRef<HTMLElement>>('historyHeading');

  ngOnInit(): void {
    this.search();
  }

  search(): void {
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    this.service.getArticles({ country: this.country, topic: this.topic, includeInactive: this.includeInactive })
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (articles) => {
          this.articles.set(articles);
          this.loading.set(false);
        },
        error: (err) => {
          if (isServiceUnavailable(err)) this.loadFailed.set(true);
          else this.error.set(translate('admin.assistantKnowledge.list.loadError'));
          this.loading.set(false);
        },
      });
  }

  clearFilters(): void {
    this.country = '';
    this.topic = '';
    this.includeInactive = false;
    this.search();
  }

  openCreate(): void {
    this.editing.set(null);
    this.form = emptyForm();
    this.openForm();
  }

  openEdit(article: KnowledgeArticle): void {
    this.editing.set(article);
    this.form = {
      country: article.country,
      topic: article.topic,
      title: article.title,
      content: article.content,
      keywords: article.keywords ?? '',
      sortOrder: String(article.sortOrder),
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

  hasError(fieldId: string): boolean {
    return this.errors().some((e) => e.fieldId === fieldId);
  }

  private validate(): FormError[] {
    const f = this.form;
    const list: FormError[] = [];
    if (!f.title.trim()) list.push({ fieldId: 'kb-title', key: 'admin.assistantKnowledge.form.errors.titleRequired' });
    if (!f.content.trim()) list.push({ fieldId: 'kb-content', key: 'admin.assistantKnowledge.form.errors.contentRequired' });
    else if (f.content.length > KNOWLEDGE_CONTENT_MAX_LENGTH) {
      list.push({ fieldId: 'kb-content', key: 'admin.assistantKnowledge.form.errors.contentTooLong', params: { max: KNOWLEDGE_CONTENT_MAX_LENGTH } });
    }
    if (!/^\d{1,4}$/.test(f.sortOrder.trim())) list.push({ fieldId: 'kb-sort', key: 'admin.assistantKnowledge.form.errors.sortInvalid' });
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
    const body: KnowledgeArticleRequest = {
      country: f.country,
      topic: f.topic,
      title: f.title.trim(),
      content: f.content.trim(),
      keywords: f.keywords.trim() || null,
      sortOrder: Number(f.sortOrder.trim()),
    };
    const editing = this.editing();
    this.saving.set(true);
    const request$ = editing ? this.service.updateArticle(editing.id, body) : this.service.createArticle(body);
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.saving.set(false);
        this.announcer.announce(translate(editing ? 'admin.assistantKnowledge.form.updated' : 'admin.assistantKnowledge.form.created', { title: body.title }));
        this.closeForm();
        this.search();
        if (editing && this.historyArticle()?.id === editing.id) this.openHistory(editing);
      },
      error: (err) => {
        this.saving.set(false);
        const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'admin.assistantKnowledge.form.errors.submit'));
        this.saveError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  deactivate(article: KnowledgeArticle): void {
    this.actionError.set('');
    this.service.deactivateArticle(article.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.announcer.announce(translate('admin.assistantKnowledge.list.deactivated', { title: article.title }));
        this.search();
      },
      error: (err) => {
        const message = translate(apiErrorKey(err, PORTAL_ERRORS, 'admin.assistantKnowledge.list.deactivateError'));
        this.actionError.set(message);
        this.announcer.announce(message, 'assertive');
      },
    });
  }

  openHistory(article: KnowledgeArticle): void {
    this.historyArticle.set(article);
    this.historyLoading.set(true);
    this.service.getArticleHistory(article.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.historyArticle.set(null);
    this.history.set([]);
  }

  focusField(event: Event, fieldId: string): void {
    event.preventDefault();
    document.getElementById(fieldId)?.focus();
  }
}
