import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URLS, API_ENDPOINTS } from '../constants/api.constants';

export interface TrashItem {
  id: string;
  name: string;
  type: 'Document' | 'Template' | 'EmailTemplate';
  deletedAt: string;
  deletedBy: string;
  size?: number;
  category?: string;
}

export interface TrashResponse {
  documents: TrashItem[];
  templates: TrashItem[];
  emailTemplates: TrashItem[];
  totalCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class CmsTrashApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = API_BASE_URLS.CMS;

  getTrashItems(): Observable<TrashResponse> {
    return this.http.get<TrashResponse>(`${this.baseUrl}${API_ENDPOINTS.CMS.TRASH}`);
  }

  restoreDocument(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}${API_ENDPOINTS.CMS.TRASH_RESTORE_DOCUMENT(id)}`, {});
  }

  restoreTemplate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}${API_ENDPOINTS.CMS.TRASH_RESTORE_TEMPLATE(id)}`, {});
  }

  restoreEmailTemplate(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}${API_ENDPOINTS.CMS.TRASH_RESTORE_EMAIL_TEMPLATE(id)}`, {});
  }

  permanentlyDeleteDocument(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}${API_ENDPOINTS.CMS.TRASH_DELETE_DOCUMENT(id)}`);
  }

  permanentlyDeleteTemplate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}${API_ENDPOINTS.CMS.TRASH_DELETE_TEMPLATE(id)}`);
  }

  permanentlyDeleteEmailTemplate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}${API_ENDPOINTS.CMS.TRASH_DELETE_EMAIL_TEMPLATE(id)}`);
  }

  emptyTrash(): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}${API_ENDPOINTS.CMS.TRASH_EMPTY}`);
  }
}
