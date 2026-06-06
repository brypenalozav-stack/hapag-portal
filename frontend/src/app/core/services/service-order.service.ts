import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { API_ENDPOINTS } from '../constants/app.constants';
import { environment } from '../../../environments/environment';

export interface ServiceOrder {
  id: string;
  orderNumber: string;
  blNumber: string;
  blId: string;
  type: string;
  status: string;
  description: string;
  requestedDate: string;
  completedDate?: string;
  clientId: string;
  clientName: string;
  country: 'CL' | 'BO';
  createdAt: string;
}

export interface CreateServiceOrderRequest {
  blNumber: string;
  type: string;
  description: string;
}

@Injectable({ providedIn: 'root' })
export class ServiceOrderService {
  private readonly api = inject(ApiService);

  create(data: CreateServiceOrderRequest): Observable<ServiceOrder> {
    return this.api.post<ServiceOrder>(API_ENDPOINTS.SERVICE_ORDERS, data);
  }

  getMyOrders(): Observable<ServiceOrder[]> {
    return this.api.get<ServiceOrder[]>(`${API_ENDPOINTS.SERVICE_ORDERS}/my`);
  }

  getPdfUrl(id: string): string {
    return `${environment.apiUrl}/${API_ENDPOINTS.SERVICE_ORDERS}/${id}/pdf`;
  }
}
