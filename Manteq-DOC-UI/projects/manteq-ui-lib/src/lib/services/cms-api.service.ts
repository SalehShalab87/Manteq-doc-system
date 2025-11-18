import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URLS, API_ENDPOINTS } from '../constants/api.constants';
import { Document, DocumentFilters } from '../models/cms.models';

@Injectable({
  providedIn: 'root'
})
export class CmsApiService {
  private http = inject(HttpClient);
  private baseUrl = API_BASE_URLS.CMS;

  /**
   * Upload a new document
   */
  uploadDocument(name: string, file: File, type?: string): Observable<Document> {
    const formData = new FormData();
    formData.append('Content', file);
    formData.append('Name', name);
    if (type) {
      formData.append('Type', type);
    }

    return this.http.post<Document>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.DOCUMENTS_REGISTER}`,
      formData
    );
  }

  /**
   * Get all documents with optional filters
   */
  getDocuments(filters?: DocumentFilters): Observable<Document[]> {
    let params = new HttpParams();
    
    if (filters?.status && filters.status !== 'all') {
      params = params.set('status', filters.status);
    }
    if (filters?.type) {
      params = params.set('type', filters.type);
    }
    if (filters?.searchTerm) {
      params = params.set('search', filters.searchTerm);
    }

    return this.http.get<Document[]>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.DOCUMENTS}`,
      { params }
    );
  }

  /**
   * Get document by ID
   */
  getDocument(id: string): Observable<Document> {
    return this.http.get<Document>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.DOCUMENTS}/${id}`
    );
  }

  /**
   * Download document
   */
  downloadDocument(id: string): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}${API_ENDPOINTS.CMS.DOCUMENTS_DOWNLOAD(id)}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Activate document
   */
  activateDocument(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.DOCUMENTS_ACTIVATE(id)}`,
      {}
    );
  }

  /**
   * Deactivate document
   */
  deactivateDocument(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.DOCUMENTS_DEACTIVATE(id)}`,
      {}
    );
  }

  /**
   * Get document types with counts
   */
  getDocumentTypes(): Observable<{ type: string; count: number }[]> {
    return this.http.get<{ type: string; count: number }[]>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.DOCUMENTS_TYPES}`
    );
  }
}
