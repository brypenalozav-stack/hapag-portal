import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { CreateManifestRequest, Manifest, Transmission } from '../models/customs.model';

@Injectable({ providedIn: 'root' })
export class CustomsService {
  private readonly api = inject(ApiService);

  getManifests(direction?: string): Observable<Manifest[]> {
    return this.api.get<Manifest[]>('customs/manifests', direction ? { direction } : undefined);
  }

  createManifest(data: CreateManifestRequest): Observable<{ id: string }> {
    return this.api.post<{ id: string }>('customs/manifests', data);
  }

  getTransmissions(manifestId: string): Observable<Transmission[]> {
    return this.api.get<Transmission[]>('customs/transmissions', { manifestId });
  }

  transmitHeader(manifestId: string): Observable<Transmission> {
    return this.api.post<Transmission>(`customs/manifests/${manifestId}/transmit-header`, {});
  }

  transmitBL(manifestId: string, blId: string): Observable<Transmission> {
    return this.api.post<Transmission>(`customs/manifests/${manifestId}/transmit-bl/${blId}`, {});
  }

  retry(transmissionId: string): Observable<Transmission> {
    return this.api.post<Transmission>(`customs/transmissions/${transmissionId}/retry`, {});
  }

  amend(manifestId: string, reason: string, billOfLadingId?: string): Observable<Transmission> {
    return this.api.post<Transmission>(`customs/manifests/${manifestId}/amend`, {
      reason,
      billOfLadingId: billOfLadingId || null,
    });
  }
}
