/**
 * Asistente conversacional del portal (Fase 1, Ola F): ventana de conversación (M10-01), consultas sobre
 * procesos con la base de conocimiento por país (M10-02), consultas sobre datos del cliente con sus permisos
 * (M10-03) y respaldo de la conversación por correo (M10-05). Las respuestas vienen en español (la traducción
 * es de Fase 2). Las propiedades en null no llegan en el JSON (el backend las omite).
 */

export type AssistantRole = 'User' | 'Assistant';

/** Tipo de respuesta: decide cómo se presenta (dato, rechazo, derivación a la casilla, etc.). */
export type AssistantAnswerType =
  | 'Greeting'
  | 'Knowledge'
  | 'Data'
  | 'NeedsReference'
  | 'NotAvailable'
  | 'SourceUnavailable'
  | 'NoAnswer'
  | 'Refused';

/** Respuestas que derivan a la casilla de correo del país y tema (M10-02, NF-11). */
export const MAILBOX_ANSWER_TYPES: readonly AssistantAnswerType[] = ['NotAvailable', 'SourceUnavailable', 'NoAnswer', 'Refused'];

export type AssistantCitationKind = 'KnowledgeArticle' | 'Shipment' | 'Documents' | 'Charges' | 'PendingPayments' | 'Invoice' | 'Tatc';

export type AssistantActionType =
  | 'DownloadDocument'
  | 'DownloadInvoice'
  | 'DownloadReceipt'
  | 'OpenShipment'
  | 'OpenCharges'
  | 'OpenInvoices'
  | 'OpenCart'
  | 'ContactMailbox';

/** Fuente de una respuesta; `path` es la ruta de la API. */
export interface AssistantCitation {
  kind: AssistantCitationKind;
  reference: string;
  title?: string | null;
  path?: string | null;
}

/** Acción ofrecida con la respuesta (abrir una pantalla, descargar, escribir a la casilla). */
export interface AssistantAction {
  type: AssistantActionType;
  label: string;
  /** Ruta de la API (descargas) o `mailto:` (casilla). */
  path?: string | null;
  blNumber?: string | null;
  id?: string | null;
}

export interface AssistantMessage {
  id: string;
  sequence: number;
  role: AssistantRole;
  /** Texto con saltos de línea `\n` y listas `- etiqueta: valor`. */
  content: string;
  intent?: string | null;
  answerType?: AssistantAnswerType | null;
  citations: AssistantCitation[];
  actions: AssistantAction[];
  engine?: string | null;
  engineFallback: boolean;
  elapsedMs?: number | null;
  createdAt: string;
}

export interface AssistantSession {
  id: string;
  country: 'CL' | 'BO';
  language: string;
  status: string;
  engineMode: string;
  startedAt: string;
  lastActivityAt: string;
  endedAt?: string | null;
  idleTimeoutMinutes: number;
  userEmail: string;
  transcriptSentTo?: string | null;
  transcriptSentAt?: string | null;
  /** Alcance del asistente. */
  disclaimer: string;
  messages: AssistantMessage[];
}

/** Respuesta a un mensaje. `responseTargetMs`: meta de NF-18 para mostrar "en proceso". */
export interface AssistantReply {
  sessionId: string;
  userMessage: AssistantMessage;
  reply: AssistantMessage;
  /** Casilla de derivación (solo NoAnswer, NotAvailable, SourceUnavailable y Refused). */
  mailboxEmail?: string | null;
  responseTargetMs: number;
  withinResponseTarget: boolean;
}

export interface EndAssistantSessionRequest {
  sendTranscript: boolean;
  /** null = correo registrado del usuario. */
  email: string | null;
}

export interface EndAssistantSessionResult {
  sessionId: string;
  endedAt: string;
  transcriptSent: boolean;
  transcriptSentTo?: string | null;
}

/** Máximo de caracteres de un mensaje al asistente. */
export const ASSISTANT_MESSAGE_MAX_LENGTH = 1000;

/** Meta de respuesta por defecto (NF-18) hasta que el servidor informe la suya. */
export const ASSISTANT_DEFAULT_RESPONSE_TARGET_MS = 3000;

// ---------------------------------------------------------------------------
// Base de conocimiento y casillas (mantenedores internos, NF-15).
// ---------------------------------------------------------------------------

export type KnowledgeTopic = 'GENERAL' | 'SHIPPING' | 'PAYMENTS' | 'DOCUMENTATION' | 'DEMURRAGE' | 'COMMERCIAL';

export const KNOWLEDGE_TOPICS: readonly KnowledgeTopic[] = ['GENERAL', 'SHIPPING', 'PAYMENTS', 'DOCUMENTATION', 'DEMURRAGE', 'COMMERCIAL'];

/** Máximo de caracteres del contenido de un artículo. */
export const KNOWLEDGE_CONTENT_MAX_LENGTH = 4000;

export interface KnowledgeArticle {
  id: string;
  country: 'CL' | 'BO';
  topic: KnowledgeTopic;
  title: string;
  content: string;
  /** Palabras clave separadas por coma (ayudan a encontrar el artículo). */
  keywords?: string | null;
  sortOrder: number;
  isActive: boolean;
  sourceFaqId?: string | null;
  createdAt: string;
  createdBy: string;
  modifiedAt?: string | null;
  modifiedBy?: string | null;
}

export interface KnowledgeArticleRequest {
  country: 'CL' | 'BO';
  topic: KnowledgeTopic;
  title: string;
  content: string;
  keywords: string | null;
  sortOrder: number;
}

export interface KnowledgeArticleSnapshot {
  country: 'CL' | 'BO';
  topic: KnowledgeTopic;
  title: string;
  content: string;
  keywords?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface KnowledgeArticleChange {
  id: string;
  articleId: string;
  action: string;
  changedAt: string;
  changedBy: string;
  changedByUserId?: string | null;
  previous?: KnowledgeArticleSnapshot | null;
  current?: KnowledgeArticleSnapshot | null;
}

export interface KnowledgeArticleSearch {
  country?: string;
  topic?: string;
  includeInactive?: boolean;
}

/** Casilla de derivación por país y tema (M10-02). */
export interface AssistantMailbox {
  id: string;
  country: 'CL' | 'BO';
  topic: KnowledgeTopic;
  email: string;
  notes?: string | null;
  isActive: boolean;
  modifiedAt?: string | null;
  modifiedBy?: string | null;
}

export interface AssistantMailboxRequest {
  country: 'CL' | 'BO';
  topic: KnowledgeTopic;
  email: string;
  notes: string | null;
  isActive: boolean;
}
