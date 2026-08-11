export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AdminUser {
  id: string;
  displayId: number | null;
  fullName: string;
  email: string;
  phone: string | null;
  isActive: boolean;
  roles: string[];
}

export interface RoleOption {
  id: string;
  code: string;
  name: string;
  isSystem: boolean;
}

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  roleCode: string;
  phone?: string;
  displayId?: number;
  country?: string;
}
