/**
 * Document model from CMS API
 */
export interface Document {
  id: string;
  name: string;
  type?: string;
  size: number;
  extension: string;
  mimeType: string;
  creationDate: Date | string;
  filePath: string;
  isActive: boolean;
  createdBy: string;
  downloadUrl: string;
}

/**
 * Document upload DTO
 */
export interface DocumentUploadRequest {
  name: string;
  type?: string;
  file: File;
  createdBy: string;
}

/**
 * Document filter options
 */
export interface DocumentFilters {
  status?: 'active' | 'inactive' | 'all';
  type?: string;
  searchTerm?: string;
}
