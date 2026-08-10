import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Client } from '../models/client.model';
import { API_ENDPOINTS } from '../constants/app.constants';

export interface UpdateProfileRequest {
  // El backend (UpdateMyClientCommand) solo acepta Phone/Address/City; el nombre
  // no es editable por el cliente, así que no se envía (BUG-13).
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
