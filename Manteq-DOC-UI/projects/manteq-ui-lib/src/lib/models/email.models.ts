/**
 * Email body source type enum
 */
export enum EmailBodySourceType {
  PlainText = 0,
  TmsTemplate = 1,
  CustomTemplate = 2
}

/**
 * Attachment source type enum
 */
export enum AttachmentSourceType {
  CmsDocument = 1,
  TmsTemplate = 2,
  CustomFile = 3
}

/**
 * TMS export format enum
 */
export enum TmsExportFormat {
  Original = 0,
  Word = 1,
  Html = 2,
  EmailHtml = 3,
  Pdf = 4
}

/**
 * Email template entity
 */
export interface EmailTemplate {
  id: string;
  name: string;
  subject: string;
  htmlContent: string;
  plainTextContent?: string;
  templateId?: string;
  bodySourceType: EmailBodySourceType;
  tmsTemplateId?: string;
  customTemplateFilePath?: string;
  isActive: boolean;
  category?: string;
  sentCount: number;
  failureCount: number;
  createdBy: string;
  createdDate: string;
  totalAttempts: number;
  successRate: number;
  attachments?: EmailTemplateAttachment[];
}

/**
 * Email template attachment entity
 */
export interface EmailTemplateAttachment {
  id: string;
  emailTemplateId: string;
  sourceType: AttachmentSourceType;
  cmsDocumentId?: string;
  tmsTemplateId?: string;
  tmsExportFormat?: number;
  customFilePath?: string;
  customFileName?: string;
  displayOrder: number;
}

/**
 * Create email template request
 */
export interface CreateEmailTemplateRequest {
  name: string;
  subject: string;
  htmlContent: string;
  plainTextContent?: string;
  templateId?: string;
  category?: string;
  bodySourceType: EmailBodySourceType;
  tmsTemplateId?: string;
  customTemplateFilePath?: string;
}

/**
 * Update email template request
 */
export interface UpdateEmailTemplateRequest {
  name?: string;
  subject?: string;
  htmlContent?: string;
  plainTextContent?: string;
  templateId?: string;
  category?: string;
  bodySourceType?: EmailBodySourceType;
  tmsTemplateId?: string;
  customTemplateFilePath?: string;
}

/**
 * TMS attachment request
 */
export interface TmsAttachmentRequest {
  templateId: string;
  exportFormat: TmsExportFormat;
  propertyValues: Record<string, string>;
  customFilename?: string;
}

/**
 * Send email with flexible attachments request
 */
export interface SendEmailRequest {
  fromAccount?: string;
  toRecipients: string[];
  ccRecipients?: string[];
  bccRecipients?: string[];
  subject: string;
  emailTemplateId: string;
  bodyPropertyValues?: Record<string, string>;
  cmsDocumentIds?: string[];
  tmsAttachments?: TmsAttachmentRequest[];
}

/**
 * Send email with template request
 */
export interface SendEmailWithTemplateRequest {
  fromAccount?: string;
  toRecipients: string[];
  ccRecipients: string[];
  bccRecipients: string[];
  subject: string;
  plainTextBody?: string;
  htmlBody?: string;
  templateId: string;
  TmsBodyPropertyValues: { [key: string]: string };
  exportFormat: number;
}

/**
 * Send email with documents request
 */
export interface SendEmailWithDocumentsRequest {
  fromAccount?: string;
  toRecipients: string[];
  ccRecipients: string[];
  bccRecipients: string[];
  subject: string;
  plainTextBody?: string;
  htmlBody?: string;
  cmsDocumentIds: string[];
}

/**
 * Email send response
 */
export interface EmailSendResponse {
  emailId: string;
  message: string;
  status: EmailStatus;
  sentAt: Date | string;
  errorMessage?: string;
}

/**
 * Email status enum
 */
export enum EmailStatus {
  Pending = 0,
  Sent = 1,
  Failed = 2
}

/**
 * Email account
 */
export interface EmailAccount {
  name: string;
  displayName: string;
  email: string;
}

/**
 * Test email template request
 */
export interface TestEmailTemplateRequest {
  templateId: string;
  toRecipients: string[];
  ccRecipients: string[];
  bccRecipients: string[];
  tmsBodyPropertyValues?: Record<string, string>;
  tmsAttachmentPropertyValues?: Record<number, Record<string, string>>;
}

