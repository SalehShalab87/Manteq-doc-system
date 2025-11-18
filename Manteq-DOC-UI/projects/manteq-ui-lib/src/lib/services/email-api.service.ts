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
  SendEmailWithTemplateRequest,
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
  private readonly cmsBaseUrl = API_BASE_URLS.CMS;
  private readonly emailBaseUrl = API_BASE_URLS.EMAIL;

  // ========== EMAIL TEMPLATE CRUD (via CMS) ==========

  /**
   * Create a new email template
   */
  createEmailTemplate(request: CreateEmailTemplateRequest): Observable<EmailTemplate> {
    return this.http.post<EmailTemplate>(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES}`,
      request
    );
  }

  /**
   * Get all email templates with optional filtering
   */
  getEmailTemplates(isActive?: boolean, category?: string): Observable<EmailTemplate[]> {
    let url = `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES}`;
    const params: string[] = [];
    
    if (isActive !== undefined) params.push(`isActive=${isActive}`);
    if (category) params.push(`category=${encodeURIComponent(category)}`);
    
    if (params.length > 0) url += `?${params.join('&')}`;
    
    return this.http.get<EmailTemplate[]>(url);
  }

  /**
   * Get email template by ID
   */
  getEmailTemplate(id: string): Observable<EmailTemplate> {
    return this.http.get<EmailTemplate>(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_BY_ID(id)}`
    );
  }

  /**
   * Update email template
   */
  updateEmailTemplate(id: string, request: UpdateEmailTemplateRequest): Observable<EmailTemplate> {
    return this.http.put<EmailTemplate>(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_BY_ID(id)}`,
      request
    );
  }

  /**
   * Delete email template (soft delete)
   */
  deleteEmailTemplate(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_BY_ID(id)}`
    );
  }

  /**
   * Activate email template
   */
  activateEmailTemplate(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_ACTIVATE(id)}`,
      {}
    );
  }

  /**
   * Deactivate email template
   */
  deactivateEmailTemplate(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_DEACTIVATE(id)}`,
      {}
    );
  }

  /**
   * Get email template analytics
   */
  getEmailTemplateAnalytics(id: string): Observable<any> {
    return this.http.get(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_ANALYTICS(id)}`
    );
  }

  /**
   * Get custom template content (HTML file)
   */
  getCustomTemplateContent(id: string): Observable<Blob> {
    return this.http.get(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_CUSTOM_TEMPLATE(id)}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Get template attachments metadata
   */
  getTemplateAttachments(id: string): Observable<EmailTemplateAttachment[]> {
    return this.http.get<EmailTemplateAttachment[]>(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_ATTACHMENTS(id)}`
    );
  }

  /**
   * Download custom attachment file
   */
  downloadCustomAttachment(id: string, attachmentIndex: number): Observable<Blob> {
    return this.http.get(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_DOWNLOAD_ATTACHMENT(id, attachmentIndex)}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Increment sent count for email template
   */
  incrementEmailTemplateSent(id: string): Observable<any> {
    return this.http.post(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_INCREMENT_SENT(id)}`,
      {}
    );
  }

  /**
   * Increment failure count for email template
   */
  incrementEmailTemplateFailure(id: string): Observable<any> {
    return this.http.post(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_INCREMENT_FAILURE(id)}`,
      {}
    );
  }

  /**
   * Upload custom template file (mHTML)
   */
  uploadCustomTemplate(id: string, file: File): Observable<void> {
    const formData = new FormData();
    formData.append('file', file);
    
    return this.http.post<void>(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_UPLOAD_CUSTOM(id)}`,
      formData
    );
  }

  /**
   * Get email template categories
   */
  getEmailTemplateCategories(): Observable<string[]> {
    return this.http.get<string[]>(
      `${this.cmsBaseUrl}${API_ENDPOINTS.CMS.EMAIL_TEMPLATES_CATEGORIES}`
    );
  }

  /**
   * Get email template attachments (alias for getTemplateAttachments)
   */
  getEmailTemplateAttachments(id: string): Observable<EmailTemplateAttachment[]> {
    return this.getTemplateAttachments(id);
  }

  // ========== EMAIL SENDING (via EmailService) ==========

  /**
   * Send email with TMS template
   */
  sendEmailWithTemplate(request: any): Observable<EmailSendResponse> {
    return this.http.post<EmailSendResponse>(
      `${this.emailBaseUrl}${API_ENDPOINTS.EMAIL.SEND_WITH_TEMPLATE}`,
      request
    );
  }

  /**
   * Send email with CMS documents
   */
  sendEmailWithDocuments(request: any): Observable<EmailSendResponse> {
    return this.http.post<EmailSendResponse>(
      `${this.emailBaseUrl}${API_ENDPOINTS.EMAIL.SEND_WITH_DOCUMENTS}`,
      request
    );
  }

  /**
   * Send email with TMS HTML body and attachment
   */
  sendEmailWithTmsHtmlAndAttachment(request: any): Observable<EmailSendResponse> {
    return this.http.post<EmailSendResponse>(
      `${this.emailBaseUrl}${API_ENDPOINTS.EMAIL.SEND_TMS_HTML_AND_ATTACHMENT}`,
      request
    );
  }

  /**
   * Send email with flexible attachments
   */
  sendEmail(request: SendEmailRequest): Observable<EmailSendResponse> {
    return this.http.post<EmailSendResponse>(
      `${this.emailBaseUrl}${API_ENDPOINTS.EMAIL.SEND_WITH_FLEXIBLE_ATTACHMENTS}`,
      request
    );
  }

  /**
   * Test email template by sending it
   */
  testEmailTemplate(request: TestEmailTemplateRequest): Observable<EmailSendResponse> {
    // POST the test request directly so TmsBodyPropertyValues stays intact
    return this.http.post<EmailSendResponse>(
      `${this.emailBaseUrl}${API_ENDPOINTS.EMAIL.TEST_TEMPLATE}`,
      request
    );
  }

  /**
   * Get available email accounts
   */
  getEmailAccounts(): Observable<EmailAccount[]> {
    return this.http.get<EmailAccount[]>(
      `${this.emailBaseUrl}${API_ENDPOINTS.EMAIL.ACCOUNTS}`
    );
  }

  /**
   * Check email service health
   */
  checkEmailServiceHealth(): Observable<any> {
    return this.http.get(
      `${this.emailBaseUrl}${API_ENDPOINTS.EMAIL.HEALTH}`
    );
  }
}
