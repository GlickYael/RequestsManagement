export const STATUS_OPTIONS = ['New', 'InProgress', 'Waiting', 'Completed'] as const;
export const PRIORITY_OPTIONS = ['Low', 'Medium', 'High'] as const;

export interface RequestFilters {
  searchTerm: string;
  status: string;
  priority: string;
  organization: string;
  assignedTo: string;
  fromDate: string;
  toDate: string;
  sortBy: string;
  sortDescending: boolean;
}

export const DEFAULT_FILTERS: RequestFilters = {
  searchTerm: '',
  status: '',
  priority: '',
  organization: '',
  assignedTo: '',
  fromDate: '',
  toDate: '',
  sortBy: 'CreatedAt',
  sortDescending: true
};

export interface RequestItem {
  id: number;
  title: string;
  organizationName: string;
  status: string;
  priority: string;
  assignedTo: string;
  createdAt: string;
  updatedAt: string | null;
  version: number;
}

export interface RequestsPageResult {
  items: RequestItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  statusCounts: Record<string, number>;
  priorityCounts: Record<string, number>;
}

export interface UpdateStatusDto {
  status: string;
  version: number;
}

export interface BulkUpdateStatusDto {
  status: string;
  items: BulkUpdateItemDto[];
}

export interface BulkUpdateItemDto {
  id: number;
  version: number;
  oldStatus: string;
}

export interface BulkUpdateFailureDto {
  id: number;
  errorReason: number;
  message: string;
}

export interface RequestStatusAuditDto {
  id: number;
  requestId: number;
  previousStatus: string;
  newStatus: string;
  changedAt: string;
  changedBy?: string;
}
