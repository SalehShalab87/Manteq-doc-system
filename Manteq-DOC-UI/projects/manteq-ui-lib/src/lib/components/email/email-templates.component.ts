import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EmailApiService } from '../../services/email-api.service';
import { EmailTemplate, EmailTemplateAttachment, EmailBodySourceType, AttachmentSourceType, CreateEmailTemplateRequest, UpdateEmailTemplateRequest, TestEmailTemplateRequest } from '../../models/email.models';
import { TmsApiService } from '../../services/tms-api.service';
import { Template } from '../../models/tms.models';
import { NotificationService } from '../../services/notification.service';
import { DateFormatPipe } from '../../pipes/date-format.pipe';
import { CmsApiService } from '../../services/cms-api.service';
import { Document } from '../../models/cms.models';

@Component({
  selector: 'manteq-email-templates',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './email-templates.component.html',
  styleUrls: ['./email-templates.component.scss']
})
export class EmailTemplatesComponent implements OnInit {
  private emailApiService = inject(EmailApiService);
  private tmsApiService = inject(TmsApiService);
  private cmsApiService = inject(CmsApiService);
  private notificationService = inject(NotificationService);

  // Expose enums for template
  EmailBodySourceType = EmailBodySourceType;
  AttachmentSourceType = AttachmentSourceType;
  Math = Math;

  // Signals for state management
  allTemplates = signal<EmailTemplate[]>([]);
  tmsTemplates = signal<Template[]>([]);
  cmsDocuments = signal<Document[]>([]);
  categories = signal<string[]>([]);
  loading = signal(false);
  showModal = signal(false);
  modalMode = signal<'create' | 'edit'>('create');
  searchTerm = '';
  selectedCategory = signal<string>('all');
  selectedStatus = signal<string>('all');

  // Attachment management
  showAttachmentsSection = signal(false);
  activeAttachmentTab = signal<'cms' | 'tms' | 'upload'>('cms');
  selectedAttachments = signal<Array<{
    sourceType: AttachmentSourceType;
    cmsDocumentId?: string;
    tmsTemplateId?: string;
    tmsExportFormat?: 'PDF' | 'DOCX' | 'Excel';
    file?: File;
    displayName: string;
  }>>([]);

  // Test modal
  showTestModal = signal(false);
  testingTemplateId = signal<string | null>(null);
  testForm = signal({
    to: '',
    cc: '',
    bcc: '',
    tmsBodyTestFile: null as File | null,
    tmsAttachmentTestFiles: {} as Record<string, File>
  });

  // Form data
  formData = signal<Partial<CreateEmailTemplateRequest>>({
    name: '',
    subject: '',
    htmlContent: '',
    plainTextContent: '',
    category: '',
    bodySourceType: EmailBodySourceType.PlainText,
    tmsTemplateId: undefined,
    customTemplateFilePath: undefined
  });
  editingId = signal<string | null>(null);
  selectedFile = signal<File | null>(null);

  // Pagination
  currentPage = signal(1);
  itemsPerPage = signal(10);

  // Computed filtered and paginated templates
  filteredTemplates = computed(() => {
    let filtered = this.allTemplates();

    // Filter by search term
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(t =>
        t.name.toLowerCase().includes(term) ||
        t.subject.toLowerCase().includes(term) ||
        t.category?.toLowerCase().includes(term)
      );
    }

    // Filter by category
    if (this.selectedCategory() !== 'all') {
      filtered = filtered.filter(t => t.category === this.selectedCategory());
    }

    // Filter by status
    if (this.selectedStatus() !== 'all') {
      const isActive = this.selectedStatus() === 'active';
      filtered = filtered.filter(t => t.isActive === isActive);
    }

    return filtered;
  });

  paginatedTemplates = computed(() => {
    const filtered = this.filteredTemplates();
    const page = this.currentPage();
    const perPage = this.itemsPerPage();
    const start = (page - 1) * perPage;
    const end = start + perPage;
    return filtered.slice(start, end);
  });

  totalPages = computed(() => {
    const total = this.filteredTemplates().length;
    const perPage = this.itemsPerPage();
    return Math.ceil(total / perPage);
  });

  pages = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: number[] = [];

    let start = Math.max(1, current - 2);
    let end = Math.min(total, start + 4);

    if (end - start < 4) {
      start = Math.max(1, end - 4);
    }

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  });

  ngOnInit(): void {
    this.loadEmailTemplates();
    this.loadTmsTemplates();
    this.loadCmsDocuments();
    this.loadCategories();
  }

  loadEmailTemplates(): void {
    this.loading.set(true);
    this.emailApiService.getEmailTemplates().subscribe({
      next: (templates) => {
        this.allTemplates.set(templates);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading email templates:', error);
        this.notificationService.error('Failed to load email templates');
        this.loading.set(false);
      }
    });
  }

  loadTmsTemplates(): void {
    this.tmsApiService.getTemplates().subscribe({
      next: (templates) => {
        this.tmsTemplates.set(templates);
        console.log('📚 TMS Templates Loaded:', templates.length, 'templates', templates);
      },
      error: (error) => {
        console.error('❌ Error loading TMS templates:', error);
      }
    });
  }

  loadCmsDocuments(): void {
    this.cmsApiService.getDocuments().subscribe({
      next: (documents) => {
        this.cmsDocuments.set(documents);
      },
      error: (error) => {
        console.error('Error loading CMS documents:', error);
      }
    });
  }

  loadCategories(): void {
    this.emailApiService.getEmailTemplateCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
      },
      error: (error) => {
        console.error('Error loading categories:', error);
      }
    });
  }

  openCreateModal(): void {
    this.modalMode.set('create');
    this.formData.set({
      name: '',
      subject: '',
      htmlContent: '',
      plainTextContent: '',
      category: '',
      bodySourceType: EmailBodySourceType.PlainText,
      tmsTemplateId: undefined
    });
    this.editingId.set(null);
    this.selectedFile.set(null);
    this.selectedAttachments.set([]);
    this.showAttachmentsSection.set(false);
    this.showModal.set(true);
    console.log('📝 Create Modal Opened - Initial Attachments:', this.selectedAttachments());
  }

  openEditModal(template: EmailTemplate): void {
    console.log('✏️ Opening Edit Modal for template:', template.name, '- Has attachments?', !!template.attachments, '- Count:', template.attachments?.length || 0);
    this.modalMode.set('edit');
    this.formData.set({
      name: template.name,
      subject: template.subject,
      htmlContent: template.htmlContent,
      plainTextContent: template.plainTextContent,
      category: template.category,
      bodySourceType: template.bodySourceType,
      tmsTemplateId: template.tmsTemplateId,
      customTemplateFilePath: template.customTemplateFilePath
    });
    this.editingId.set(template.id);
    this.selectedFile.set(null);
    
    // Load attachments from API
    this.emailApiService.getEmailTemplateAttachments(template.id).subscribe({
      next: (attachments: EmailTemplateAttachment[]) => {
        if (attachments && attachments.length > 0) {
          const mappedAttachments = attachments.map(att => ({
            sourceType: att.sourceType,
            cmsDocumentId: att.cmsDocumentId,
            tmsTemplateId: att.tmsTemplateId,
            tmsExportFormat: att.tmsExportFormat !== undefined ? this.getTmsExportFormatLabel(att.tmsExportFormat) : undefined,
            displayName: this.getAttachmentDisplayName(att)
          }));
          console.log('📎 Mapped Attachments for Edit:', mappedAttachments);
          this.selectedAttachments.set(mappedAttachments);
          this.showAttachmentsSection.set(true);
        } else {
          console.log('⚠️ No attachments returned from API');
          this.selectedAttachments.set([]);
          this.showAttachmentsSection.set(false);
        }
      },
      error: (error) => {
        console.error('❌ Error loading attachments:', error);
        this.selectedAttachments.set([]);
        this.showAttachmentsSection.set(false);
      }
    });
    
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.formData.set({
      name: '',
      subject: '',
      htmlContent: '',
      plainTextContent: '',
      category: '',
      bodySourceType: EmailBodySourceType.PlainText,
      tmsTemplateId: undefined
    });
    this.editingId.set(null);
    this.selectedFile.set(null);
    this.selectedAttachments.set([]);
    this.showAttachmentsSection.set(false);
  }

  async saveTemplate(): Promise<void> {
    const data = this.formData();

    if (!data.name || !data.subject) {
      this.notificationService.error('Please fill in all required fields');
      return;
    }

    if (data.bodySourceType === EmailBodySourceType.PlainText && !data.plainTextContent) {
      this.notificationService.error('Plain text content is required');
      return;
    }

    if (data.bodySourceType === EmailBodySourceType.TmsTemplate && !data.tmsTemplateId) {
      this.notificationService.error('Please select a TMS template for body generation');
      return;
    }

    if (data.bodySourceType === EmailBodySourceType.CustomTemplate && !this.selectedFile() && !data.customTemplateFilePath) {
      this.notificationService.error('Please upload a custom template file');
      return;
    }

    this.notificationService.showLoading(
      this.modalMode() === 'create' ? 'Creating template...' : 'Updating template...'
    );

    if (this.modalMode() === 'create') {
      // Convert attachments to the proper format
      console.log('🔍 Selected Attachments:', this.selectedAttachments());
      const attachmentsData = this.selectedAttachments().length > 0 ? this.selectedAttachments().map((att, index) => ({
        sourceType: att.sourceType,
        cmsDocumentId: att.cmsDocumentId,
        tmsTemplateId: att.tmsTemplateId,
        tmsExportFormat: att.tmsExportFormat === 'PDF' ? 0 : att.tmsExportFormat === 'DOCX' ? 1 : att.tmsExportFormat === 'Excel' ? 4 : undefined,
        displayOrder: index
      })) : [];

      console.log('📦 Converted Attachments Data:', attachmentsData);

      const createData = {
        ...data,
        attachments: attachmentsData.length > 0 ? attachmentsData : undefined
      };

      console.log('📤 Create Payload:', createData);

      this.emailApiService.createEmailTemplate(createData as CreateEmailTemplateRequest).subscribe({
        next: (createdTemplate) => {
          // If CustomTemplate type and file selected, upload it
          if (data.bodySourceType === EmailBodySourceType.CustomTemplate && this.selectedFile()) {
            this.uploadCustomTemplate(createdTemplate.id);
          } else {
            this.notificationService.hideLoading();
            this.notificationService.success('Email template created successfully');
            this.closeModal();
            this.loadEmailTemplates();
          }
        },
        error: (error) => {
          console.error('Error creating template:', error);
          this.notificationService.hideLoading();
          this.notificationService.error('Failed to create template');
        }
      });
    } else {
      const id = this.editingId();
      if (!id) return;

      // Convert attachments to the proper format
      const attachmentsData = this.selectedAttachments().length > 0 ? this.selectedAttachments().map((att, index) => ({
        sourceType: att.sourceType,
        cmsDocumentId: att.cmsDocumentId,
        tmsTemplateId: att.tmsTemplateId,
        tmsExportFormat: att.tmsExportFormat === 'PDF' ? 0 : att.tmsExportFormat === 'DOCX' ? 1 : att.tmsExportFormat === 'Excel' ? 4 : undefined,
        displayOrder: index
      })) : [];

      const updateData = {
        ...data,
        attachments: attachmentsData.length > 0 ? attachmentsData : undefined
      };

      this.emailApiService.updateEmailTemplate(id, updateData as UpdateEmailTemplateRequest).subscribe({
        next: () => {
          // If CustomTemplate type and file selected, upload it
          if (data.bodySourceType === EmailBodySourceType.CustomTemplate && this.selectedFile()) {
            this.uploadCustomTemplate(id);
          } else {
            this.notificationService.hideLoading();
            this.notificationService.success('Email template updated successfully');
            this.closeModal();
            this.loadEmailTemplates();
          }
        },
        error: (error) => {
          console.error('Error updating template:', error);
          this.notificationService.hideLoading();
          this.notificationService.error('Failed to update template');
        }
      });
    }
  }

  async toggleActive(template: EmailTemplate): Promise<void> {
    const action = template.isActive ? 'deactivate' : 'activate';
    const confirmed = await this.notificationService.confirm(
      `${action.charAt(0).toUpperCase() + action.slice(1)} Template`,
      `Are you sure you want to ${action} "${template.name}"?`,
      action.charAt(0).toUpperCase() + action.slice(1),
      'Cancel'
    );

    if (!confirmed) return;

    this.notificationService.showLoading(`${action}ing template...`);

    const observable = template.isActive
      ? this.emailApiService.deactivateEmailTemplate(template.id)
      : this.emailApiService.activateEmailTemplate(template.id);

    observable.subscribe({
      next: () => {
        this.notificationService.hideLoading();
        this.notificationService.success(`Template ${action}d successfully`);
        this.loadEmailTemplates();
      },
      error: (error) => {
        console.error(`Error ${action}ing template:`, error);
        this.notificationService.hideLoading();
        this.notificationService.error(`Failed to ${action} template`);
      }
    });
  }

  async deleteTemplate(template: EmailTemplate): Promise<void> {
    const confirmed = await this.notificationService.confirm(
      'Delete Template',
      `Are you sure you want to delete "${template.name}"? This will move it to trash.`,
      'Delete',
      'Cancel'
    );

    if (!confirmed) return;

    this.notificationService.showLoading('Deleting template...');

    this.emailApiService.deleteEmailTemplate(template.id).subscribe({
      next: () => {
        this.notificationService.hideLoading();
        this.notificationService.success('Template moved to trash');
        this.loadEmailTemplates();
      },
      error: (error) => {
        console.error('Error deleting template:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to delete template');
      }
    });
  }

  updateFormField<K extends keyof CreateEmailTemplateRequest>(
    field: K,
    value: CreateEmailTemplateRequest[K]
  ): void {
    this.formData.update(data => ({ ...data, [field]: value }));
  }

  onBodySourceTypeChange(type: EmailBodySourceType): void {
    this.updateFormField('bodySourceType', type);
    if (type !== EmailBodySourceType.TmsTemplate) {
      this.updateFormField('tmsTemplateId', undefined);
    }
    // Clear selected file if switching away from CustomTemplate
    if (type !== EmailBodySourceType.CustomTemplate) {
      this.selectedFile.set(null);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      
      // Validate file type
      const allowedExtensions = ['.mhtml', '.mht'];
      const fileName = file.name.toLowerCase();
      const isValidExtension = allowedExtensions.some(ext => fileName.endsWith(ext));
      
      if (!isValidExtension) {
        this.notificationService.error('Please upload an mht or mHTML file');
        input.value = '';
        return;
      }
      
      // Validate file size (5MB max)
      const maxSizeInBytes = 5 * 1024 * 1024;
      if (file.size > maxSizeInBytes) {
        this.notificationService.error('File size must be less than 5MB');
        input.value = '';
        return;
      }
      
      this.selectedFile.set(file);
      this.notificationService.success(`File "${file.name}" selected`);
    }
  }

  clearSelectedFile(): void {
    this.selectedFile.set(null);
  }

  toggleAttachmentsSection(): void {
    this.showAttachmentsSection.update(v => !v);
    console.log('🔄 Attachments Section Toggled:', this.showAttachmentsSection() ? 'SHOWN' : 'HIDDEN', '- Current Attachments:', this.selectedAttachments());
  }

  setActiveAttachmentTab(tab: 'cms' | 'tms' | 'upload'): void {
    this.activeAttachmentTab.set(tab);
    console.log('📑 Active Tab Changed to:', tab);
    if (tab === 'tms') {
      console.log('📚 Available TMS Templates:', this.tmsTemplates().length, this.tmsTemplates());
    } else if (tab === 'cms') {
      console.log('📄 Available CMS Documents:', this.cmsDocuments().length, this.cmsDocuments());
    }
  }

  onTmsTemplateSelected(templateId: string): void {
    console.log('🎯 TMS Template Selected:', templateId, 'isEmpty:', !templateId);
  }

  addCmsAttachment(documentId: string): void {
    const doc = this.cmsDocuments().find(d => d.id === documentId);
    if (!doc) return;

    this.selectedAttachments.update(attachments => [
      ...attachments,
      {
        sourceType: AttachmentSourceType.CmsDocument,
        cmsDocumentId: documentId,
        displayName: doc.name
      }
    ]);
  }

  addTmsAttachment(templateId: string, exportFormat: 'PDF' | 'DOCX' | 'Excel' = 'PDF'): void {
    const tmsTemplate = this.tmsTemplates().find(t => t.id === templateId);
    if (!tmsTemplate) {
      console.warn('⚠️ TMS Template not found:', templateId);
      return;
    }

    console.log('✅ Adding TMS Attachment:', { templateId, exportFormat, templateName: tmsTemplate.name });

    this.selectedAttachments.update(attachments => [
      ...attachments,
      {
        sourceType: AttachmentSourceType.TmsTemplate,
        tmsTemplateId: templateId,
        tmsExportFormat: exportFormat,
        displayName: `${tmsTemplate.name} (${exportFormat})`
      }
    ]);

    console.log('📋 Updated Selected Attachments:', this.selectedAttachments());
  }

  onAttachmentFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      
      // Validate file size (10MB max)
      const maxSizeInBytes = 10 * 1024 * 1024;
      if (file.size > maxSizeInBytes) {
        this.notificationService.error('Attachment size must be less than 10MB');
        input.value = '';
        return;
      }
      
      this.selectedAttachments.update(attachments => [
        ...attachments,
        {
          sourceType: AttachmentSourceType.CustomFile,
          file: file,
          displayName: file.name
        }
      ]);

      this.notificationService.success(`Attachment "${file.name}" added`);
      input.value = '';
    }
  }

  removeAttachment(index: number): void {
    this.selectedAttachments.update(attachments => 
      attachments.filter((_, i) => i !== index)
    );
  }

  uploadCustomTemplate(templateId: string): void {
    const file = this.selectedFile();
    if (!file) {
      this.notificationService.hideLoading();
      this.notificationService.success('Template saved successfully');
      this.closeModal();
      this.loadEmailTemplates();
      return;
    }

    this.emailApiService.uploadCustomTemplate(templateId, file).subscribe({
      next: () => {
        this.notificationService.hideLoading();
        this.notificationService.success('Template and custom file uploaded successfully');
        this.closeModal();
        this.loadEmailTemplates();
      },
      error: (error) => {
        console.error('Error uploading custom template:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to upload custom template file');
      }
    });
  }

  searchTemplates(): void {
    this.currentPage.set(1);
  }

  filterByCategory(category: string): void {
    this.selectedCategory.set(category);
    this.currentPage.set(1);
  }

  filterByStatus(status: string): void {
    this.selectedStatus.set(status);
    this.currentPage.set(1);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  nextPage(): void {
    this.goToPage(this.currentPage() + 1);
  }

  previousPage(): void {
    this.goToPage(this.currentPage() - 1);
  }

  getBodySourceLabel(type: EmailBodySourceType): string {
    switch (type) {
      case EmailBodySourceType.PlainText:
        return 'Plain Text';
      case EmailBodySourceType.TmsTemplate:
        return 'TMS Template';
      case EmailBodySourceType.CustomTemplate:
        return 'Custom Template';
      default:
        return 'Unknown';
    }
  }

  getStatusBadgeClass(isActive: boolean): string {
    return isActive ? 'badge bg-success' : 'badge bg-danger';
  }

  getSuccessRateClass(rate: number): string {
    if (rate >= 90) return 'text-success';
    if (rate >= 70) return 'text-warning';
    return 'text-danger';
  }

  // Test Modal Methods
  openTestModal(template: EmailTemplate): void {
    console.log('🧪 Opening Test Modal for template:', template.name, '- Has attachments?', !!template.attachments, '- Count:', template.attachments?.length || 0);
    this.testingTemplateId.set(template.id);
    this.testForm.set({
      to: '',
      cc: '',
      bcc: '',
      tmsBodyTestFile: null,
      tmsAttachmentTestFiles: {}
    });
    
    // Load attachments from API
    this.emailApiService.getEmailTemplateAttachments(template.id).subscribe({
      next: (attachments: EmailTemplateAttachment[]) => {
        if (attachments && attachments.length > 0) {
          const mappedAttachments = attachments.map((att: EmailTemplateAttachment) => ({
            sourceType: att.sourceType,
            cmsDocumentId: att.cmsDocumentId,
            tmsTemplateId: att.tmsTemplateId,
            tmsExportFormat: att.tmsExportFormat !== undefined ? this.getTmsExportFormatLabel(att.tmsExportFormat) : undefined,
            displayName: this.getAttachmentDisplayName(att)
          }));
          console.log('📎 Mapped Attachments for Test:', mappedAttachments);
          this.selectedAttachments.set(mappedAttachments);
        } else {
          console.log('⚠️ No attachments returned from API');
          this.selectedAttachments.set([]);
        }
      },
      error: (error) => {
        console.error('❌ Error loading attachments:', error);
        this.selectedAttachments.set([]);
      }
    });
    
    this.showTestModal.set(true);
  }

  closeTestModal(): void {
    this.showTestModal.set(false);
    this.testingTemplateId.set(null);
    this.testForm.set({
      to: '',
      cc: '',
      bcc: '',
      tmsBodyTestFile: null,
      tmsAttachmentTestFiles: {}
    });
    this.selectedAttachments.set([]);
  }

  getTestingTemplate(): EmailTemplate | undefined {
    const id = this.testingTemplateId();
    if (!id) return undefined;
    return this.allTemplates().find(t => t.id === id);
  }

  hasTmsAttachments(): boolean {
    return this.selectedAttachments().some(a => a.sourceType === AttachmentSourceType.TmsTemplate);
  }

  downloadTmsPlaceholderForBody(): void {
    const template = this.getTestingTemplate();
    if (!template || !template.tmsTemplateId) return;

    this.notificationService.showLoading('Downloading placeholder Excel...');
    this.tmsApiService.downloadPlaceholdersExcel(template.tmsTemplateId).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${template.name}_body_placeholder.xlsx`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        this.notificationService.hideLoading();
        this.notificationService.success('Placeholder Excel downloaded');
      },
      error: (error: any) => {
        console.error('Error downloading placeholder:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to download placeholder');
      }
    });
  }

  downloadTmsPlaceholderForAttachment(attachmentIndex: number): void {
    const template = this.getTestingTemplate();
    if (!template) return;

    const attachment = this.selectedAttachments()[attachmentIndex];
    if (!attachment || !attachment.tmsTemplateId) return;

    this.notificationService.showLoading('Downloading placeholder Excel...');
    this.tmsApiService.downloadPlaceholdersExcel(attachment.tmsTemplateId).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${attachment.displayName}_placeholder.xlsx`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        this.notificationService.hideLoading();
        this.notificationService.success('Placeholder Excel downloaded');
      },
      error: (error: any) => {
        console.error('Error downloading placeholder:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to download placeholder');
      }
    });
  }

  onTmsBodyTestFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.testForm.update(form => ({ ...form, tmsBodyTestFile: file }));
      this.notificationService.success(`Test file "${file.name}" selected`);
    }
  }

  onTmsAttachmentTestFileSelected(event: Event, tmsTemplateId: string): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.testForm.update(form => ({
        ...form,
        tmsAttachmentTestFiles: { ...form.tmsAttachmentTestFiles, [tmsTemplateId]: file }
      }));
      this.notificationService.success(`Test file "${file.name}" selected`);
    }
  }

  async sendTestEmail(): Promise<void> {
    const form = this.testForm();
    const template = this.getTestingTemplate();

    if (!template) return;

    if (!form.to) {
      this.notificationService.error('Please enter at least one recipient email');
      return;
    }

    // Validate TMS body test file if needed
    if (template.bodySourceType === EmailBodySourceType.TmsTemplate && !form.tmsBodyTestFile) {
      this.notificationService.error('Please upload test data Excel file for TMS body generation');
      return;
    }

    // Validate TMS attachment test files if needed
    const tmsAttachments = this.selectedAttachments().filter(a => a.sourceType === AttachmentSourceType.TmsTemplate);
    for (const attachment of tmsAttachments) {
      if (attachment.tmsTemplateId && !form.tmsAttachmentTestFiles[attachment.tmsTemplateId]) {
        this.notificationService.error(`Please upload test data for attachment: ${attachment.displayName}`);
        return;
      }
    }

    this.notificationService.showLoading('Sending test email...');

    try {
      // Parse Excel files to extract property values
      let tmsBodyPropertyValues: Record<string, string> | undefined;
      let tmsAttachmentPropertyValues: Record<number, Record<string, string>> | undefined;

      if (form.tmsBodyTestFile) {
        tmsBodyPropertyValues = await this.parseExcelFile(form.tmsBodyTestFile);
      }

      if (Object.keys(form.tmsAttachmentTestFiles).length > 0) {
        tmsAttachmentPropertyValues = {};
        let attachmentIndex = 0;
        for (const attachment of this.selectedAttachments()) {
          if (attachment.sourceType === AttachmentSourceType.TmsTemplate && attachment.tmsTemplateId) {
            const file = form.tmsAttachmentTestFiles[attachment.tmsTemplateId];
            if (file) {
              tmsAttachmentPropertyValues[attachmentIndex] = await this.parseExcelFile(file);
            }
          }
          attachmentIndex++;
        }
      }

      // Split recipients by comma and trim
      const toRecipients = form.to.split(',').map(email => email.trim()).filter(email => email.length > 0);
      const ccRecipients = form.cc ? form.cc.split(',').map(email => email.trim()).filter(email => email.length > 0) : [];
      const bccRecipients = form.bcc ? form.bcc.split(',').map(email => email.trim()).filter(email => email.length > 0) : [];

      const testRequest: TestEmailTemplateRequest = {
        templateId: template.id!,
        toRecipients,
        ccRecipients,
        bccRecipients,
        tmsBodyPropertyValues,
        tmsAttachmentPropertyValues
      };

      this.emailApiService.testEmailTemplate(testRequest).subscribe({
        next: (response) => {
          this.notificationService.hideLoading();
          this.notificationService.success('Test email sent successfully!');
          this.closeTestModal();
        },
        error: (error) => {
          this.notificationService.hideLoading();
          this.notificationService.error('Failed to send test email: ' + (error.error?.message || error.message));
        }
      });
    } catch (error: any) {
      this.notificationService.hideLoading();
      this.notificationService.error('Failed to prepare test email: ' + error.message);
    }
  }

  private async parseExcelFile(file: File): Promise<Record<string, string>> {
    return new Promise((resolve, reject) => {
      console.log('📊 Parsing Excel file using TMS API:', file.name);
      
      this.tmsApiService.parseExcelToJson(file).subscribe({
        next: (propertyValues: Record<string, string>) => {
          console.log('✅ Excel parsed successfully:', Object.keys(propertyValues).length, 'properties');
          console.log('📋 Property values:', propertyValues);
          resolve(propertyValues);
        },
        error: (error: any) => {
          console.error('❌ Error parsing Excel file:', error);
          this.notificationService.error('Failed to parse Excel file: ' + (error.error?.error || error.message));
          reject(error);
        }
      });
    });
  }

  private getTmsExportFormatLabel(format: number): 'PDF' | 'DOCX' | 'Excel' {
    // TmsExportFormat enum: Pdf=0, Word=1, Html=2, EmailHtml=3, Excel=4
    switch (format) {
      case 0: return 'PDF';
      case 1: return 'DOCX';
      case 4: return 'Excel';
      default: return 'PDF';
    }
  }

  private getAttachmentDisplayName(att: EmailTemplateAttachment): string {
    if (att.sourceType === AttachmentSourceType.CmsDocument && att.cmsDocumentId) {
      const doc = this.cmsDocuments().find(d => d.id === att.cmsDocumentId);
      return doc ? doc.name : 'CMS Document';
    } else if (att.sourceType === AttachmentSourceType.TmsTemplate && att.tmsTemplateId) {
      const template = this.tmsTemplates().find(t => t.id === att.tmsTemplateId);
      return template ? `${template.name} (${this.getTmsExportFormatLabel(att.tmsExportFormat || 0)})` : 'TMS Template';
    } else if (att.sourceType === AttachmentSourceType.CustomFile) {
      return att.customFileName || 'Custom File';
    }
    return 'Unknown';
  }
}

