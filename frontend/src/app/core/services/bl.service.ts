import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { BillOfLading } from '../models/bl.model';

@Injectable({ providedIn: 'root' })
export class BillOfLadingService {
  private readonly api = inject(ApiService);

  getByNumber(blNumber: string): Observable<BillOfLading> {
    return this.api.get<BillOfLading>(`bills-of-lading/${blNumber}`);
  }

  getAll(country?: string, clientId?: string): Observable<BillOfLading[]> {
    const params: Record<string, string> = {};
    if (country) params['country'] = country;
    if (clientId) params['clientId'] = clientId;
    return this.api.get<BillOfLading[]>('bills-of-lading', params);
  }

  getMyBLs(): Observable<BillOfLading[]> {
    return this.api.get<BillOfLading[]>('bills-of-lading/my');
  }
}
