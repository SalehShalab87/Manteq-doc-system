import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URLS, API_ENDPOINTS } from '../constants/api.constants';
import { 
  Template, 
  RegisterTemplateRequest, 
  DocumentGenerationRequest, 
  DocumentGenerationResponse,
  TemplateProperty,
  ExportFormat
} from '../models/tms.models';

@Injectable({
  providedIn: 'root'
})
export class TmsApiService {
  private http = inject(HttpClient);
  private baseUrl = API_BASE_URLS.TMS;

  // ========== TEMPLATE REGISTRATION & MANAGEMENT ==========

  /**
   * Register a new template
   */
  registerTemplate(name: string, file: File, category: string, description?: string): Observable<any> {
    const formData = new FormData();
    formData.append('TemplateFile', file);
    formData.append('Name', name);
    formData.append('Category', category);
    if (description) {
      formData.append('Description', description);
    }
    formData.append('CreatedBy', 'UI-User');

    return this.http.post(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_REGISTER}`,
      formData
    );
  }

  /**
   * Get all templates
   */
  getTemplates(): Observable<Template[]> {
    return this.http.get<Template[]>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES}`
    );
  }

  /**
   * Get template by ID
   */
  getTemplate(id: string): Observable<Template> {
    return this.http.get<Template>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_BY_ID(id)}`
    );
  }

  /**
   * Get template properties (placeholders)
   */
  getTemplateProperties(id: string): Observable<{ templateId: string; templateName: string; properties: TemplateProperty[] }> {
    return this.http.get<{ templateId: string; templateName: string; properties: TemplateProperty[] }>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_PROPERTIES(id)}`
    );
  }

  /**
   * Activate template
   */
  activateTemplate(id: string): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_INCREMENT_SUCCESS(id)}`,
      {}
    );
  }

  /**
   * Deactivate template
   */
  deactivateTemplate(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_DELETE(id)}`
    );
  }

  /**
   * Delete template (soft delete)
   */
  deleteTemplate(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_DELETE(id)}`
    );
  }

  /**
   * Get template analytics
   */
  getTemplateAnalytics(id: string): Observable<any> {
    return this.http.get(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_ANALYTICS(id)}`
    );
  }

  // ========== DOCUMENT GENERATION ==========

  /**
   * Generate document from template
   */
  generateDocument(request: DocumentGenerationRequest): Observable<DocumentGenerationResponse> {
    return this.http.post<DocumentGenerationResponse>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_GENERATE}`,
      request
    );
  }

  /**
   * Download generated document
   */
  downloadGeneratedDocument(generationId: string): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_DOWNLOAD(generationId)}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Generate document with embeddings
   */
  generateDocumentWithEmbeddings(request: any): Observable<DocumentGenerationResponse> {
    return this.http.post<DocumentGenerationResponse>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_GENERATE_WITH_EMBEDDINGS}`,
      request
    );
  }

  // ========== PLACEHOLDER & FILE OPERATIONS ==========

  /**
   * Download template placeholders as Excel
   */
  downloadPlaceholdersExcel(id: string): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_DOWNLOAD_EXCEL(id)}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Extract placeholders from a template file without saving
   */
  extractPlaceholdersFromFile(file: File): Observable<Blob> {
    const formData = new FormData();
    formData.append('TemplateFile', file);

    return this.http.post(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_EXTRACT_PLACEHOLDERS}`,
      formData,
      { responseType: 'blob' }
    );
  }

  /**
   * Parse Excel file and return property values as JSON
   */
  parseExcelToJson(excelFile: File): Observable<Record<string, string>> {
    const formData = new FormData();
    formData.append('ExcelFile', excelFile);

    return this.http.post<Record<string, string>>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_PARSE_EXCEL}`,
      formData
    );
  }

  // ========== TESTING ENDPOINTS ==========

  /**
   * Test template without saving - uploads template and filled Excel
   */
  testTemplateWithoutSaving(templateFile: File, excelFile: File, exportFormat: ExportFormat): Observable<Blob> {
    const formData = new FormData();
    formData.append('TemplateFile', templateFile);
    formData.append('ExcelFile', excelFile);
    formData.append('ExportFormat', exportFormat.toString());

    return this.http.post(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_TEST_TEMPLATE}`,
      formData,
      { responseType: 'blob' }
    );
  }

  /**
   * Test generate document with Excel file
   */
  testGenerateWithExcel(id: string, excelFile: File, exportFormat: ExportFormat): Observable<DocumentGenerationResponse> {
    const formData = new FormData();
    formData.append('ExcelFile', excelFile);
    formData.append('ExportFormat', exportFormat.toString());

    return this.http.post<DocumentGenerationResponse>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATES_TEST_GENERATE(id)}`,
      formData
    );
  }

  // ========== UTILITY ENDPOINTS ==========

  /**
   * Get template types
   */
  getTemplateTypes(): Observable<{ value: number; name: string }[]> {
    return this.http.get<{ value: number; name: string }[]>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.TEMPLATE_TYPES}`
    );
  }

  /**
   * Get export formats
   */
  getExportFormats(): Observable<{ value: number; name: string }[]> {
    return this.http.get<{ value: number; name: string }[]>(
      `${this.baseUrl}${API_ENDPOINTS.TMS.EXPORT_FORMATS}`
    );
  }
}
