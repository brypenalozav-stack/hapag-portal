import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Client } from '../models/client.model';
import { API_ENDPOINTS } from '../constants/app.constants';

export interface UpdateProfileRequest {
  name: string;
  phone: string;
}

@Injectable({ providedIn: 'root' })
export class ClientService {
  private readonly api = inject(ApiService);

  getProfile(): Observable<Client> {
    return this.api.get<Client>(API_ENDPOINTS.CLIENTS_ME);
  }

  updateProfile(data: UpdateProfileRequest): Observable<Client> {
    return this.api.put<Client>(API_ENDPOINTS.CLIENTS_ME, data);
  }
}
