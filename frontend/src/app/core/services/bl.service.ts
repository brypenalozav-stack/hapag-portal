import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from './api.service';
import { BillOfLading, LocalCharge, DemurrageCharge, BLChargesResponse } from '../models/bl.model';

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

  getCharges(blNumber: string): Observable<BLChargesResponse> {
    return this.api.get<BLChargesResponse>(`bills-of-lading/${blNumber}/charges`);
  }

  getDemurrage(blNumber: string): Observable<DemurrageCharge[]> {
    return this.api.get<DemurrageCharge[]>(`bills-of-lading/${blNumber}/demurrage`);
  }
}
