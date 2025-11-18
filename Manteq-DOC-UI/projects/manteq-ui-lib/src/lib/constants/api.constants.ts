/**
 * API Base URLs - Developers can modify these URLs to match their environment
 */
export const API_BASE_URLS = {
  CMS: 'http://localhost:5000',
  TMS: 'http://localhost:5267',
  EMAIL: 'http://localhost:5030'
} as const;

/**
 * API Endpoints
 */
export const API_ENDPOINTS = {
  CMS: {
    DOCUMENTS: '/api/documents',
    DOCUMENTS_REGISTER: '/api/documents/register',
    DOCUMENTS_BY_ID: (id: string) => `/api/documents/${id}`,
    DOCUMENTS_DOWNLOAD: (id: string) => `/api/documents/${id}/download`,
    DOCUMENTS_ACTIVATE: (id: string) => `/api/documents/${id}/activate`,
    DOCUMENTS_DEACTIVATE: (id: string) => `/api/documents/${id}/deactivate`,
    DOCUMENTS_DELETE: (id: string) => `/api/documents/${id}`,
    DOCUMENTS_TYPES: '/api/documents/types',
    TEMPLATES: '/api/templates',
    TEMPLATES_BY_ID: (id: string) => `/api/templates/${id}`,
    TEMPLATES_CREATE: '/api/templates',
    TEMPLATES_UPDATE: (id: string) => `/api/templates/${id}`,
    TEMPLATES_DELETE: (id: string) => `/api/templates/${id}`,
    TEMPLATES_ACTIVATE: (id: string) => `/api/templates/${id}/activate`,
    TEMPLATES_DEACTIVATE: (id: string) => `/api/templates/${id}/deactivate`,
    TEMPLATES_INCREMENT_SUCCESS: (id: string) => `/api/templates/${id}/increment-success`,
    TEMPLATES_INCREMENT_FAILURE: (id: string) => `/api/templates/${id}/increment-failure`,
    EMAIL_TEMPLATES: '/api/email-templates',
    EMAIL_TEMPLATES_BY_ID: (id: string) => `/api/email-templates/${id}`,
    EMAIL_TEMPLATES_ACTIVATE: (id: string) => `/api/email-templates/${id}/activate`,
    EMAIL_TEMPLATES_DEACTIVATE: (id: string) => `/api/email-templates/${id}/deactivate`,
    EMAIL_TEMPLATES_ANALYTICS: (id: string) => `/api/email-templates/${id}/analytics`,
    EMAIL_TEMPLATES_CUSTOM_TEMPLATE: (id: string) => `/api/email-templates/${id}/custom-template`,
    EMAIL_TEMPLATES_UPLOAD_CUSTOM: (id: string) => `/api/email-templates/${id}/upload-custom`,
    EMAIL_TEMPLATES_ATTACHMENTS: (id: string) => `/api/email-templates/${id}/attachments`,
    EMAIL_TEMPLATES_DOWNLOAD_ATTACHMENT: (id: string, index: number) => `/api/email-templates/${id}/attachments/${index}/download`,
    EMAIL_TEMPLATES_INCREMENT_SENT: (id: string) => `/api/email-templates/${id}/increment-sent`,
    EMAIL_TEMPLATES_INCREMENT_FAILURE: (id: string) => `/api/email-templates/${id}/increment-failure`,
    EMAIL_TEMPLATES_CATEGORIES: '/api/email-templates/categories',
    TRASH: '/api/trash',
    TRASH_RESTORE_DOCUMENT: (id: string) => `/api/trash/documents/${id}/restore`,
    TRASH_RESTORE_TEMPLATE: (id: string) => `/api/trash/templates/${id}/restore`,
    TRASH_RESTORE_EMAIL_TEMPLATE: (id: string) => `/api/trash/email-templates/${id}/restore`,
    TRASH_DELETE_DOCUMENT: (id: string) => `/api/trash/documents/${id}/permanent`,
    TRASH_DELETE_TEMPLATE: (id: string) => `/api/trash/templates/${id}/permanent`,
    TRASH_DELETE_EMAIL_TEMPLATE: (id: string) => `/api/trash/email-templates/${id}/permanent`,
    TRASH_EMPTY: '/api/trash/empty'
  },
  TMS: {
    TEMPLATES: '/api/templates',
    TEMPLATES_REGISTER: '/api/templates/register',
    TEMPLATES_BY_ID: (id: string) => `/api/templates/${id}`,
    TEMPLATES_DELETE: (id: string) => `/api/templates/${id}`,
    TEMPLATES_PROPERTIES: (id: string) => `/api/templates/${id}/properties`,
    TEMPLATES_ANALYTICS: (id: string) => `/api/templates/${id}/analytics`,
    TEMPLATES_INCREMENT_SUCCESS: (id: string) => `/api/templates/${id}/increment-success`,
    TEMPLATES_INCREMENT_FAILURE: (id: string) => `/api/templates/${id}/increment-failure`,
    TEMPLATES_GENERATE: '/api/templates/generate',
    TEMPLATES_GENERATE_WITH_EMBEDDINGS: '/api/templates/generate-with-embeddings',
    TEMPLATES_DOWNLOAD: (generationId: string) => `/api/templates/download/${generationId}`,
    TEMPLATES_DOWNLOAD_EXCEL: (id: string) => `/api/templates/${id}/download-placeholders-excel`,
    TEMPLATES_EXTRACT_PLACEHOLDERS: '/api/templates/extract-placeholders',
    TEMPLATES_TEST_TEMPLATE: '/api/templates/test-template',
    TEMPLATES_TEST_GENERATE: (id: string) => `/api/templates/${id}/test-generate`,
    TEMPLATES_PARSE_EXCEL: '/api/templates/parse-excel',
    TEMPLATE_TYPES: '/api/templates/template-types',
    EXPORT_FORMATS: '/api/templates/export-formats'
  },
  EMAIL: {
    TEST_TEMPLATE: '/api/email/test-template',
    SEND_WITH_TEMPLATE: '/api/email/send-with-template',
    SEND_WITH_DOCUMENTS: '/api/email/send-with-documents',
    SEND_TMS_HTML_AND_ATTACHMENT: '/api/email/send-tms-html-and-attachment',
    SEND_WITH_FLEXIBLE_ATTACHMENTS: '/api/email/send-with-flexible-attachments',
    ACCOUNTS: '/api/email/accounts',
    HEALTH: '/api/email/health'
  }
} as const;
