
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  RequestsPageResult,
  UpdateStatusDto,
  BulkUpdateStatusDto,
  BulkUpdateFailureDto,
  RequestStatusAuditDto
} from '../models/request.model';

export interface RequestsQueryParams {
  pageNumber?: number;
  pageSize?: number;
  organization?: string;
  status?: string;
  priority?: string;
  assignedTo?: string;
  fromDate?: string;
  toDate?: string;
  searchTerm?: string;
  sortBy?: string;
  sortDescending?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class RequestsApiService {
  private apiUrl = 'http://localhost:5154/api/requests';

  constructor(private http: HttpClient) {}

  getRequests(params: RequestsQueryParams): Observable<RequestsPageResult> {
    // Explicit mapping to the parameter names expected by RequestsQueryParameters on the backend (PascalCase)
    const backendParams: Record<string, any> = {
      PageNumber: params.pageNumber,
      PageSize: params.pageSize,
      Organization: params.organization,
      Status: params.status,
      Priority: params.priority,
      AssignedTo: params.assignedTo,
      FromDate: params.fromDate,
      ToDate: params.toDate,
      SearchTerm: params.searchTerm,
      SortBy: params.sortBy,
      SortDescending: params.sortDescending
    };

    let httpParams = new HttpParams();
    Object.keys(backendParams).forEach(key => {
      const val = backendParams[key];
      if (val !== null && val !== undefined && val !== '') {
        httpParams = httpParams.set(key, val.toString());
      }
    });

    return this.http.get<RequestsPageResult>(this.apiUrl, { params: httpParams });
  }

  updateStatus(id: number, dto: UpdateStatusDto): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/status`, dto);
  }

  bulkUpdateStatus(dto: BulkUpdateStatusDto): Observable<BulkUpdateFailureDto[]> {
    return this.http.patch<BulkUpdateFailureDto[]>(`${this.apiUrl}/batch-status`, dto);
  }

  getRequestAudit(id: number): Observable<RequestStatusAuditDto[]> {
    return this.http.get<RequestStatusAuditDto[]>(`${this.apiUrl}/${id}/status-audit`);
  }

}
