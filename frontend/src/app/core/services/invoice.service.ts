import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { InvoiceList, InvoiceOrganization, InvoiceRefreshResult, InvoiceSearch } from '../models/invoice.model';

const BASE = API_ENDPOINTS.INVOICES;

/** Facturas del cliente por organización (M7-01): consulta, PDF, descarga múltiple y actualización. */
@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private readonly api = inject(ApiService);

  getOrganizations(): Observable<InvoiceOrganization[]> {
    return this.api.get<InvoiceOrganization[]>(`${BASE}/organizations`);
  }

  search(filters: InvoiceSearch): Observable<InvoiceList> {
    const params: Record<string, string | number> = {};
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && value !== null && value !== '') params[key] = value as string | number;
    }
    return this.api.get<InvoiceList>(BASE, params);
  }

  downloadPdf(id: string): Observable<Blob> {
    return this.api.getBlob(`${BASE}/${id}/pdf`);
  }

  /** Descarga múltiple (zip) de facturas con folio emitido. */
  downloadZip(ids: string[]): Observable<Blob> {
    return this.api.postBlob(`${BASE}/download`, { ids });
  }

  refresh(organizationId?: string): Observable<InvoiceRefreshResult> {
    return this.api.post<InvoiceRefreshResult>(`${BASE}/refresh`, organizationId ? { organizationId } : {});
  }
}
