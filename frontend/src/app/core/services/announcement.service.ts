import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import {
  Announcement,
  AnnouncementAdmin,
  AnnouncementRequest,
  AnnouncementSnapshot,
} from '../models/announcement.model';
import { MaintainerChange } from '../models/payment-config.model';

const ADMIN = API_ENDPOINTS.ADMIN_ANNOUNCEMENTS;

/**
 * Comunicados masivos (Fase 2, Ola I, M1-26): los vigentes para el país y la operación del cliente, y el mantenedor
 * interno con segmentación, vigencia, publicación y registro de cambios (NF-15).
 */
@Injectable({ providedIn: 'root' })
export class AnnouncementService {
  private readonly api = inject(ApiService);

  /** Vigentes; sin país, el del usuario (claim `country`). La operación incluye los de ambas. */
  getCurrent(filters: { country?: string; operation?: string } = {}): Observable<Announcement[]> {
    return this.api.get<Announcement[]>(API_ENDPOINTS.ANNOUNCEMENTS, filters);
  }

  search(filters: { status?: string; country?: string } = {}): Observable<AnnouncementAdmin[]> {
    return this.api.get<AnnouncementAdmin[]>(ADMIN, filters);
  }

  create(body: AnnouncementRequest): Observable<AnnouncementAdmin> {
    return this.api.post<AnnouncementAdmin>(ADMIN, body);
  }

  update(id: string, body: AnnouncementRequest): Observable<AnnouncementAdmin> {
    return this.api.put<AnnouncementAdmin>(`${ADMIN}/${id}`, body);
  }

  publish(id: string): Observable<AnnouncementAdmin> {
    return this.api.post<AnnouncementAdmin>(`${ADMIN}/${id}/publish`, {});
  }

  unpublish(id: string): Observable<AnnouncementAdmin> {
    return this.api.post<AnnouncementAdmin>(`${ADMIN}/${id}/unpublish`, {});
  }

  remove(id: string): Observable<void> {
    return this.api.delete<void>(`${ADMIN}/${id}`);
  }

  getHistory(id: string): Observable<MaintainerChange<AnnouncementSnapshot>[]> {
    return this.api.get<MaintainerChange<AnnouncementSnapshot>[]>(`${ADMIN}/${id}/history`);
  }
}
