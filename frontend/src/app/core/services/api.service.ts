import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  get<T>(path: string, params?: Record<string, string | number | boolean>): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}/${path}`, { params: this.toParams(params) });
  }

  /** POST con cabeceras opcionales (p. ej. `Idempotency-Key` del cierre de pago, NF-01). */
  post<T>(path: string, body: unknown, headers?: Record<string, string>): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${path}`, body, headers ? { headers: new HttpHeaders(headers) } : {});
  }

  /** Descarga de un archivo (PDF, zip) con la sesión del usuario. */
  getBlob(path: string, params?: Record<string, string | number | boolean>): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${path}`, { params: this.toParams(params), responseType: 'blob' });
  }

  postBlob(path: string, body: unknown): Observable<Blob> {
    return this.http.post(`${this.baseUrl}/${path}`, body, { responseType: 'blob' });
  }

  put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}/${path}`, body);
  }

  delete<T>(path: string, params?: Record<string, string | number | boolean>): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}/${path}`, { params: this.toParams(params) });
  }

  private toParams(params?: Record<string, string | number | boolean>): HttpParams {
    let httpParams = new HttpParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          httpParams = httpParams.set(key, String(value));
        }
      });
    }
    return httpParams;
  }
}
