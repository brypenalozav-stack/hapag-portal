import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  AssistantMailbox,
  AssistantMailboxRequest,
  AssistantReply,
  AssistantSession,
  EndAssistantSessionRequest,
  EndAssistantSessionResult,
  KnowledgeArticle,
  KnowledgeArticleChange,
  KnowledgeArticleRequest,
  KnowledgeArticleSearch,
} from '../models/assistant.model';

const BASE = API_ENDPOINTS.ASSISTANT;

/**
 * Asistente del portal: conversación (M10-01 a M10-03), cierre con respaldo por correo (M10-05) y, para el
 * mantenedor interno (permiso maintainers.manage, NF-15), la base de conocimiento por país y las casillas de
 * derivación por país y tema (M10-02). El servidor aplica los permisos del usuario a cada respuesta (M1-11).
 */
@Injectable({ providedIn: 'root' })
export class AssistantService {
  private readonly api = inject(ApiService);

  // Conversación
  startSession(): Observable<AssistantSession> {
    return this.api.post<AssistantSession>(`${BASE}/sessions`, {});
  }

  /** Historial de la conversación (solo su dueño). */
  getSession(id: string): Observable<AssistantSession> {
    return this.api.get<AssistantSession>(`${BASE}/sessions/${id}`);
  }

  sendMessage(id: string, message: string): Observable<AssistantReply> {
    return this.api.post<AssistantReply>(`${BASE}/sessions/${id}/messages`, { message });
  }

  /** Cierra la conversación y, si se pide, envía el respaldo al correo registrado o al indicado. */
  endSession(id: string, request: EndAssistantSessionRequest): Observable<EndAssistantSessionResult> {
    return this.api.post<EndAssistantSessionResult>(`${BASE}/sessions/${id}/end`, request);
  }

  // Base de conocimiento (M10-02)
  getArticles(filters: KnowledgeArticleSearch): Observable<KnowledgeArticle[]> {
    return this.api.get<KnowledgeArticle[]>(`${BASE}/knowledge`, {
      ...(filters.country ? { country: filters.country } : {}),
      ...(filters.topic ? { topic: filters.topic } : {}),
      ...(filters.includeInactive ? { includeInactive: true } : {}),
    });
  }

  createArticle(body: KnowledgeArticleRequest): Observable<KnowledgeArticle> {
    return this.api.post<KnowledgeArticle>(`${BASE}/knowledge`, body);
  }

  updateArticle(id: string, body: KnowledgeArticleRequest): Observable<KnowledgeArticle> {
    return this.api.put<KnowledgeArticle>(`${BASE}/knowledge/${id}`, body);
  }

  /** Desactiva el artículo; el registro de cambios se conserva. */
  deactivateArticle(id: string): Observable<void> {
    return this.api.delete<void>(`${BASE}/knowledge/${id}`);
  }

  getArticleHistory(id: string): Observable<KnowledgeArticleChange[]> {
    return this.api.get<KnowledgeArticleChange[]>(`${BASE}/knowledge/${id}/history`);
  }

  // Casillas de derivación (M10-02)
  getMailboxes(): Observable<AssistantMailbox[]> {
    return this.api.get<AssistantMailbox[]>(`${BASE}/mailboxes`);
  }

  /** Crea o actualiza la casilla del país y tema. */
  saveMailbox(body: AssistantMailboxRequest): Observable<AssistantMailbox> {
    return this.api.put<AssistantMailbox>(`${BASE}/mailboxes`, body);
  }
}
