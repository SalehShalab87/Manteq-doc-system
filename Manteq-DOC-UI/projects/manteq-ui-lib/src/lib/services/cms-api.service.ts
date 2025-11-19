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

  // ========== DOCUMENT OPERATIONS ==========

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
   * Delete document (soft delete)
   */
  deleteDocument(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.DOCUMENTS_DELETE(id)}`
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

  // ========== TEMPLATE OPERATIONS (CMS) ==========

  /**
   * Create a new CMS template
   */
  createTemplate(request: any): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.TEMPLATES_CREATE}`,
      request
    );
  }

  /**
   * Get all CMS templates with optional filtering
   */
  getTemplates(category?: string, isActive?: boolean): Observable<any[]> {
    let params = new HttpParams();
    if (category) params = params.set('category', category);
    if (isActive !== undefined) params = params.set('isActive', isActive.toString());

    return this.http.get<any[]>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.TEMPLATES}`,
      { params }
    );
  }

  /**
   * Get CMS template by ID
   */
  getTemplate(id: string): Observable<any> {
    return this.http.get<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.TEMPLATES_BY_ID(id)}`
    );
  }

  getTemplatePlaceholders(name: string, isActive: boolean = true): Observable<string[]> {
    return this.http.get<string[]>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.TEMPLATE_PLACEHOLDERS(name, isActive)}`
    );
  }

  /**
   * Update CMS template
   */
  updateTemplate(id: string, request: any): Observable<any> {
    return this.http.put<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.TEMPLATES_UPDATE(id)}`,
      request
    );
  }

  /**
   * Activate CMS template
   */
  activateTemplate(id: string): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.TEMPLATES_ACTIVATE(id)}`,
      {}
    );
  }

  /**
   * Deactivate CMS template
   */
  deactivateTemplate(id: string): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.TEMPLATES_DEACTIVATE(id)}`,
      {}
    );
  }

  /**
   * Delete CMS template (soft delete)
   */
  deleteTemplate(id: string): Observable<any> {
    return this.http.delete<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.TEMPLATES_DELETE(id)}`
    );
  }

  /**
   * Increment success count for CMS template
   */
  incrementTemplateSuccess(id: string): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.TEMPLATES_INCREMENT_SUCCESS(id)}`,
      {}
    );
  }

  /**
   * Increment failure count for CMS template
   */
  incrementTemplateFailure(id: string): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.TEMPLATES_INCREMENT_FAILURE(id)}`,
      {}
    );
  }

  // ========== EMAIL TEMPLATE OPERATIONS (CMS) ==========

  /**
   * Create a new email template
   */
  createEmailTemplate(request: any): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES}`,
      request
    );
  }

  /**
   * Get all email templates with optional filtering
   */
  getEmailTemplates(isActive?: boolean, category?: string): Observable<any[]> {
    let params = new HttpParams();
    if (isActive !== undefined) params = params.set('isActive', isActive.toString());
    if (category) params = params.set('category', category);

    return this.http.get<any[]>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES}`,
      { params }
    );
  }

  /**
   * Get email template by ID
   */
  getEmailTemplate(id: string): Observable<any> {
    return this.http.get<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_BY_ID(id)}`
    );
  }

  /**
   * Update email template
   */
  updateEmailTemplate(id: string, request: any): Observable<any> {
    return this.http.put<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_BY_ID(id)}`,
      request
    );
  }

  /**
   * Delete email template (soft delete)
   */
  deleteEmailTemplate(id: string): Observable<any> {
    return this.http.delete<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_BY_ID(id)}`
    );
  }

  /**
   * Activate email template
   */
  activateEmailTemplate(id: string): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_ACTIVATE(id)}`,
      {}
    );
  }

  /**
   * Deactivate email template
   */
  deactivateEmailTemplate(id: string): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_DEACTIVATE(id)}`,
      {}
    );
  }

  /**
   * Get email template analytics
   */
  getEmailTemplateAnalytics(id: string): Observable<any> {
    return this.http.get<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_ANALYTICS(id)}`
    );
  }

  /**
   * Get custom template content (HTML file)
   */
  getCustomTemplateContent(id: string): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_CUSTOM_TEMPLATE(id)}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Get template attachments metadata
   */
  getTemplateAttachments(id: string): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_ATTACHMENTS(id)}`
    );
  }

  /**
   * Download custom attachment file
   */
  downloadCustomAttachment(id: string, attachmentIndex: number): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_DOWNLOAD_ATTACHMENT(id, attachmentIndex)}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Increment sent count for email template
   */
  incrementEmailTemplateSent(id: string): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_INCREMENT_SENT(id)}`,
      {}
    );
  }

  /**
   * Increment failure count for email template
   */
  incrementEmailTemplateFailure(id: string): Observable<any> {
    return this.http.post<any>(
      `${this.baseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_INCREMENT_FAILURE(id)}`,
      {}
    );
  }
}
