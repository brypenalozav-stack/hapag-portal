import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { DangerousGoodImportResult, DangerousGoodSearchResult } from '../models/dangerous-good.model';

/**
 * Buscador de clasificación de mercancías peligrosas (M10-06), disponible para todo usuario autenticado, y carga
 * interna de la base de referencia desde un CSV (permiso maintainers.manage, NF-15).
 */
@Injectable({ providedIn: 'root' })
export class DangerousGoodsService {
  private readonly api = inject(ApiService);

  search(query: string): Observable<DangerousGoodSearchResult> {
    return this.api.get<DangerousGoodSearchResult>(`${API_ENDPOINTS.DANGEROUS_GOODS}/search`, { q: query });
  }

  /** CSV UTF-8 con encabezado `unNumber;nameEs;nameEn;class` y columnas opcionales. */
  import(file: File): Observable<DangerousGoodImportResult> {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.api.post<DangerousGoodImportResult>(`${API_ENDPOINTS.DANGEROUS_GOODS}/import`, body);
  }
}
