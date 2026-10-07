import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { AccessMatrixAction, UpdateAccessMatrixLevelRequest } from '../models/access-matrix.model';

/** Matriz base de accesos por rol (M1-11), administrada desde el portal interno. */
@Injectable({ providedIn: 'root' })
export class AccessMatrixService {
  private readonly api = inject(ApiService);

  getAll(): Observable<AccessMatrixAction[]> {
    return this.api.get<AccessMatrixAction[]>(API_ENDPOINTS.ACCESS_MATRIX);
  }

  updateLevel(actionCode: string, data: UpdateAccessMatrixLevelRequest): Observable<AccessMatrixAction> {
    return this.api.put<AccessMatrixAction>(
      `${API_ENDPOINTS.ACCESS_MATRIX}/${encodeURIComponent(actionCode)}`,
      data,
    );
  }
}
