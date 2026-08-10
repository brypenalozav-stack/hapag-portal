import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { ClientOption, ImportBillRow, ImportResult } from '../models/bl-import.model';

@Injectable({ providedIn: 'root' })
export class BlImportService {
  private readonly api = inject(ApiService);

  getClients(): Observable<ClientOption[]> {
    return this.api.get<ClientOption[]>('clients');
  }

  import(rows: ImportBillRow[]): Observable<ImportResult> {
    return this.api.post<ImportResult>('bills-of-lading/import', { rows });
  }
}
