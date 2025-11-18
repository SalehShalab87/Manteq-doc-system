/**
 * Template model from TMS API
 */
export interface Template {
  id: string;
  name: string;
  description: string;
  category: string;
  cmsDocumentId: string;
  placeholders: string[];
  createdAt: Date | string;
  updatedAt: Date | string;
  isActive: boolean;
  createdBy: string;
  updatedBy: string;
  templateDownloadUrl: string;
  successCount: number;
  failureCount: number;
}

/**
 * Template property
 */
export interface TemplateProperty {
  name: string;
  type: string;
  isRequired: boolean;
  description: string;
  currentValue: string;
}

/**
 * Template registration request
 */
export interface RegisterTemplateRequest {
  name: string;
  description: string;
  category: string;
  templateFile: File;
  createdBy: string;
}

/**
 * Document generation request
 */
export interface DocumentGenerationRequest {
  templateId: string;
  propertyValues: { [key: string]: string };
  exportFormat: ExportFormat;
  generatedBy: string;
}

/**
 * Document generation response
 */
export interface DocumentGenerationResponse {
  generationId: string;
  message: string;
  fileName: string;
  fileSizeBytes: number;
  downloadUrl: string;
  expiresAt: Date | string;
  exportFormat: ExportFormat;
  processedPlaceholders: number;
}

/**
 * Export format enum
 */
export enum ExportFormat {
  Original = 0,
  Word = 1,
  Html = 2,
  EmailHtml = 3,
  Pdf = 4
}

/**
 * Template type enum
 */
export enum TemplateType {
  Document = 0,
  TOB = 1,
  Quotation = 2
}
