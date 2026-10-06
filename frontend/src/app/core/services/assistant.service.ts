import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, from, map, switchMap, throwError } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { environment } from '../../../environments/environment';
import {
  AssistantMailbox,
  AssistantMailboxRequest,
  AssistantReply,
  AssistantSession,
  DownloadedFile,
  EndAssistantSessionRequest,
  EndAssistantSessionResult,
  KnowledgeArticle,
  KnowledgeArticleChange,
  KnowledgeArticleRequest,
  KnowledgeArticleSearch,
} from '../models/assistant.model';

const BASE = API_ENDPOINTS.ASSISTANT;

/** Prefijo de las rutas absolutas que informa el servidor (`/api/v1/...`). */
const API_PREFIX = /^\/?api\/v1\//;

/** Nombre del archivo de Content-Disposition (`filename*=UTF-8''…` o `filename="…"`); null si no viene. */
export function contentDispositionFileName(header: string | null): string | null {
  if (!header) return null;
  const encoded = /filename\*\s*=\s*(?:UTF-8'')?([^;]+)/i.exec(header);
  if (encoded) {
    try {
      return decodeURIComponent(encoded[1].trim().replace(/^"|"$/g, ''));
    } catch {
      // Codificación inválida: se usa el nombre simple.
    }
  }
  const plain = /filename\s*=\s*"?([^";]+)"?/i.exec(header);
  return plain ? plain[1].trim() : null;
}

/** Error de una descarga con el cuerpo JSON (ProblemDetails) leído desde el Blob; si no lo es, el error tal cual. */
async function problemFromBlob(err: unknown): Promise<unknown> {
  if (!(err instanceof HttpErrorResponse) || !(err.error instanceof Blob)) return err;
  try {
    const body: unknown = JSON.parse(await err.error.text());
    return new HttpErrorResponse({ error: body, headers: err.headers, status: err.status, statusText: err.statusText, url: err.url ?? undefined });
  } catch {
    return err;
  }
}

/**
 * Asistente del portal: conversación (M10-01 a M10-03), cierre con respaldo por correo (M10-05) y, para el
 * mantenedor interno (permiso maintainers.manage, NF-15), la base de conocimiento por país y las casillas de
 * derivación por país y tema (M10-02). El servidor aplica los permisos del usuario a cada respuesta (M1-11). Fase 2,
 * Ola J: descarga de los documentos que entrega el asistente (M10-04), con los permisos validados de nuevo al descargar.
 */
@Injectable({ providedIn: 'root' })
export class AssistantService {
  private readonly api = inject(ApiService);
  private readonly http = inject(HttpClient);

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

  /**
   * Descarga un documento entregado por el asistente (M10-04) por la ruta de la acción, con el nombre del archivo que
   * informa el servidor. Solo el dueño de la conversación; los permisos se validan de nuevo y, si el usuario perdió el
   * acceso al documento, el servidor responde 404.
   */
  downloadDelivery(path: string): Observable<DownloadedFile> {
    return this.http
      .get(`${environment.apiUrl}/${path.replace(API_PREFIX, '')}`, { observe: 'response', responseType: 'blob' })
      .pipe(
        map((response) => ({
          blob: response.body ?? new Blob([], { type: 'application/pdf' }),
          fileName: contentDispositionFileName(response.headers.get('Content-Disposition')),
        })),
        // Con responseType 'blob' el ProblemDetails llega como Blob: se lee para conocer el código del error.
        catchError((err: unknown) => from(problemFromBlob(err)).pipe(switchMap((problem) => throwError(() => problem)))),
      );
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
