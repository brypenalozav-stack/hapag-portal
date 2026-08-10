import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { DeadlineItem, DeadlineRule } from '../models/deadline.model';

@Injectable({ providedIn: 'root' })
export class DeadlineService {
  private readonly api = inject(ApiService);

  getDashboard(status?: string, manifestId?: string): Observable<DeadlineItem[]> {
    const params: Record<string, string> = {};
    if (status) params['status'] = status;
    if (manifestId) params['manifestId'] = manifestId;
    return this.api.get<DeadlineItem[]>('deadlines', params);
  }

  getRules(): Observable<DeadlineRule[]> {
    return this.api.get<DeadlineRule[]>('deadlines/rules');
  }

  recalculate(manifestId: string): Observable<{ affected: number }> {
    return this.api.post<{ affected: number }>(`deadlines/manifests/${manifestId}/recalculate`, {});
  }
}
