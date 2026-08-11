import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { AuditPagedResult, AuditSearchParams } from '../models/audit.model';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly api = inject(ApiService);

  search(params: AuditSearchParams): Observable<AuditPagedResult> {
    const query: Record<string, string | number> = {};
    if (params.entityName) query['entityName'] = params.entityName;
    if (params.userId) query['userId'] = params.userId;
    if (params.action) query['action'] = params.action;
    if (params.from) query['from'] = params.from;
    if (params.to) query['to'] = params.to;
    query['page'] = params.page ?? 1;
    query['pageSize'] = params.pageSize ?? 20;
    return this.api.get<AuditPagedResult>('audit', query);
  }
}
