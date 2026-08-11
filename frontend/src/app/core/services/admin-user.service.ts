import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { AdminUser, CreateUserRequest, PagedResult, RoleOption } from '../models/admin-user.model';

export interface SearchUsersParams {
  isActive?: boolean;
  roleCode?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class AdminUserService {
  private readonly api = inject(ApiService);

  searchUsers(params: SearchUsersParams): Observable<PagedResult<AdminUser>> {
    const query: Record<string, string | number | boolean> = {};
    if (params.isActive !== undefined) query['isActive'] = params.isActive;
    if (params.roleCode) query['roleCode'] = params.roleCode;
    if (params.search) query['search'] = params.search;
    query['page'] = params.page ?? 1;
    query['pageSize'] = params.pageSize ?? 10;
    return this.api.get<PagedResult<AdminUser>>('users', query);
  }

  createUser(data: CreateUserRequest): Observable<AdminUser> {
    return this.api.post<AdminUser>('users', data);
  }

  getRoles(): Observable<RoleOption[]> {
    return this.api.get<RoleOption[]>('roles');
  }
}
