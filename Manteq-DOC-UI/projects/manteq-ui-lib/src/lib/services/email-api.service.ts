import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URLS, API_ENDPOINTS } from '../constants/api.constants';
import {
  EmailTemplate,
  EmailTemplateAttachment,
  CreateEmailTemplateRequest,
  UpdateEmailTemplateRequest,
  SendEmailRequest,
  EmailSendResponse,
  EmailAccount,
  TmsAttachmentRequest,
  TestEmailTemplateRequest
} from '../models/email.models';

@Injectable({
  providedIn: 'root'
})
export class EmailApiService {
  private readonly http = inject(HttpClient);
  private readonly emailBaseUrl = API_BASE_URLS.EMAIL;

  // Email Template Management (via Email Service)
  getEmailTemplates(isActive?: boolean, category?: string): Observable<EmailTemplate[]> {
    let url = `${this.emailBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES}`;
    const params: string[] = [];
    
    if (isActive !== undefined) params.push(`isActive=${isActive}`);
    if (category) params.push(`category=${encodeURIComponent(category)}`);
    
    if (params.length > 0) url += `?${params.join('&')}`;
    
    return this.http.get<EmailTemplate[]>(url);
  }

  getEmailTemplateById(id: string): Observable<EmailTemplate> {
    return this.http.get<EmailTemplate>(`${this.emailBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_BY_ID(id)}`);
  }

  createEmailTemplate(request: CreateEmailTemplateRequest): Observable<EmailTemplate> {
    return this.http.post<EmailTemplate>(`${this.emailBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES}`, request);
  }

  updateEmailTemplate(id: string, request: UpdateEmailTemplateRequest): Observable<EmailTemplate> {
    return this.http.put<EmailTemplate>(`${this.emailBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_BY_ID(id)}`, request);
  }

  deleteEmailTemplate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.emailBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_BY_ID(id)}`);
  }

  activateEmailTemplate(id: string): Observable<void> {
    return this.http.post<void>(`${this.emailBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_ACTIVATE(id)}`, {});
  }

  deactivateEmailTemplate(id: string): Observable<void> {
    return this.http.post<void>(`${this.emailBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_DEACTIVATE(id)}`, {});
  }

  getEmailTemplateCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.emailBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_CATEGORIES}`);
  }

  getEmailTemplateAnalytics(id: string): Observable<any> {
    return this.http.get(`${this.emailBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_ANALYTICS(id)}`);
  }

  getEmailTemplateAttachments(id: string): Observable<EmailTemplateAttachment[]> {
    return this.http.get<EmailTemplateAttachment[]>(`${this.emailBaseUrl}/api/email-templates/${id}/attachments`);
  }

  // File Upload Operations
  uploadCustomTemplate(id: string, file: File): Observable<EmailTemplate> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<EmailTemplate>(`${this.emailBaseUrl}/api/emailtemplates/${id}/upload-template`, formData);
  }

  getCustomTemplate(id: string): Observable<Blob> {
    return this.http.get(`${this.emailBaseUrl}/api/emailtemplates/${id}/custom-template`, { responseType: 'blob' });
  }

  // Email Sending (Email Service)
  sendEmail(request: SendEmailRequest): Observable<EmailSendResponse> {
    return this.http.post<EmailSendResponse>(`${this.emailBaseUrl}${API_ENDPOINTS.EMAIL.SEND_WITH_FLEXIBLE_ATTACHMENTS}`, request);
  }

  testEmailTemplate(request: TestEmailTemplateRequest): Observable<EmailSendResponse> {
    return this.http.post<EmailSendResponse>(`${this.emailBaseUrl}/api/email/test-template`, request);
  }

  getEmailAccounts(): Observable<EmailAccount[]> {
    return this.http.get<EmailAccount[]>(`${this.emailBaseUrl}${API_ENDPOINTS.EMAIL.ACCOUNTS}`);
  }

  checkEmailServiceHealth(): Observable<any> {
    return this.http.get(`${this.emailBaseUrl}${API_ENDPOINTS.EMAIL.HEALTH}`);
  }
}
