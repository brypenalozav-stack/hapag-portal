import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { FAQ } from '../models/faq.model';

@Injectable({ providedIn: 'root' })
export class FAQService {
  private readonly api = inject(ApiService);

  getAll(country?: string, category?: string): Observable<FAQ[]> {
    const params: Record<string, string> = {};
    if (country) params['country'] = country;
    if (category) params['category'] = category;
    return this.api.get<FAQ[]>('faqs', params);
  }
}
