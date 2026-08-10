import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ReportResult, ReportType } from '../models/report.model';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  get(type: ReportType, params: Record<string, string | boolean> = {}): Observable<ReportResult> {
    return this.http.get<ReportResult>(`${this.baseUrl}/reports/${type}`, { params: this.toParams(params) });
  }

  export(type: ReportType, params: Record<string, string | boolean> = {}): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/reports/${type}/export`, {
      params: this.toParams(params),
      responseType: 'blob',
    });
  }

  private toParams(params: Record<string, string | boolean>): HttpParams {
    let httpParams = new HttpParams();
    for (const [k, v] of Object.entries(params)) {
      if (v !== undefined && v !== null && v !== '') httpParams = httpParams.set(k, String(v));
    }
    return httpParams;
  }
}
