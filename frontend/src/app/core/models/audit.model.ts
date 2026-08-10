export interface AuditLogItem {
  id: string;
  entityName: string;
  entityId: string;
  action: string;
  oldValues?: string;
  newValues?: string;
  userId?: string;
  timestamp: string;
}

export interface AuditPagedResult {
  items: AuditLogItem[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AuditSearchParams {
  entityName?: string;
  userId?: string;
  action?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}
