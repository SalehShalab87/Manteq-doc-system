import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TmsApiService } from '../../services/tms-api.service';
import { CmsApiService } from '../../services/cms-api.service';
import { NotificationService } from '../../services/notification.service';
import { Template } from '../../models/tms.models'
import { DateFormatPipe } from '../../pipes/date-format.pipe';

interface TemplateFilters {
  status?: 'all' | 'active' | 'inactive' | 'draft';
  type?: string;
  searchTerm?: string;
}

@Component({
  selector: 'manteq-template-builder',
  standalone: true,
  imports: [CommonModule, FormsModule, DateFormatPipe],
  templateUrl: './template-builder.component.html',
  styleUrls: ['./template-builder.component.scss']
})
export class TemplateBuilderComponent implements OnInit {
  private tmsApi = inject(TmsApiService);
  private cmsApi = inject(CmsApiService);
  private notificationService = inject(NotificationService);
  
  // Expose Math for template
  Math = Math;

  // Signals for state management
  templates = signal<Template[]>([]);
  filteredTemplates = computed(() => {
    const temps = this.templates();
    const filter = this.currentFilter();
    
    let filtered = temps;

    // Filter by status
    if (filter.status === 'active') {
      filtered = filtered.filter(t => t.isActive);
    } else if (filter.status === 'inactive') {
      filtered = filtered.filter(t => !t.isActive);
    }

    // Filter by type
    if (filter.type) {
      filtered = filtered.filter(t => t.category?.toLowerCase().includes(filter.type!.toLowerCase()));
    }

    // Filter by search term
    if (filter.searchTerm) {
      const term = filter.searchTerm.toLowerCase();
      filtered = filtered.filter(t => 
        t.name.toLowerCase().includes(term) ||
        t.description?.toLowerCase().includes(term) ||
        t.category?.toLowerCase().includes(term)
      );
    }

    return filtered;
  });

  loading = signal(false);
  currentFilter = signal<TemplateFilters>({ status: 'all' });
  showAddModal = signal(false);
  selectedFile = signal<File | null>(null);
  testFile = signal<File | null>(null);
  uploadedTemplateId = signal<string | null>(null);
  
  uploadForm = {
    name: '',
    type: '',
    description: '',
    outputFormat: 'PDF'
  };
  
  searchTerm = '';
  
  // Pagination
  currentPage = signal(1);
  itemsPerPage = signal(10);
  
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
    
    // Show max 5 page numbers
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
    this.loadTemplates();
  }

  /**
   * Load all templates
   */
  loadTemplates(): void {
    this.loading.set(true);
    this.tmsApi.getTemplates().subscribe({
      next: (templates) => {
        this.templates.set(templates);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading templates:', error);
        this.notificationService.error('Failed to load templates');
        this.loading.set(false);
      }
    });
  }

  /**
   * Open add template modal
   */
  openAddModal(): void {
    this.showAddModal.set(true);
  }

  /**
   * Close add template modal
   */
  closeAddModal(): void {
    this.showAddModal.set(false);
    this.resetUploadForm();
  }

  /**
   * Handle file selection
   */
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const validExtensions = ['.docx', '.xlsx', '.doc', '.xls'];
      const fileExtension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
      
      if (!validExtensions.includes(fileExtension)) {
        this.notificationService.warning('Only .docx and .xlsx files are supported');
        input.value = '';
        return;
      }
      
      this.selectedFile.set(file);
    }
  }

  /**
   * Upload template
   */
  async uploadTemplate(): Promise<void> {
    const file = this.selectedFile();
    if (!file || !this.uploadForm.name || !this.uploadForm.type) {
      this.notificationService.warning('Please fill in all required fields');
      return;
    }

    const confirmed = await this.notificationService.confirm(
      'Save Template',
      'Are you sure you want to save this template?',
      'Save Template',
      'Cancel'
    );

    if (!confirmed) return;

    this.notificationService.showLoading('Uploading template...');
    this.tmsApi.registerTemplate(
      this.uploadForm.name, 
      file, 
      this.uploadForm.type,
      this.uploadForm.description
    ).subscribe({
      next: (response) => {
        this.notificationService.hideLoading();
        this.notificationService.success('Template uploaded successfully');
        // Store template ID for testing
        if (response.templateId) {
          this.uploadedTemplateId.set(response.templateId);
        }
        this.closeAddModal();
        this.loadTemplates();
      },
      error: (error) => {
        console.error('Error uploading template:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to upload template');
      }
    });
  }

  /**
   * Download placeholder Excel for the current template being uploaded
   */
  downloadPlaceholderForTest(): void {
    const file = this.selectedFile();
    if (!file) {
      this.notificationService.warning('Please select a template file first');
      return;
    }

    this.notificationService.showLoading('Extracting placeholders...');
    this.tmsApi.extractPlaceholdersFromFile(file).subscribe({
      next: (blob) => {
        this.notificationService.hideLoading();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Placeholders_${file.name.replace(/\.[^/.]+$/, '')}.xlsx`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        this.notificationService.success('Placeholders downloaded successfully');
      },
      error: (error) => {
        console.error('Error extracting placeholders:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to extract placeholders from template');
      }
    });
  }

  /**
   * Handle test file selection
   */
  onTestFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const fileExtension = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
      
      if (fileExtension !== '.xlsx') {
        this.notificationService.warning('Only .xlsx files are supported for testing');
        input.value = '';
        return;
      }
      
      this.testFile.set(file);
      this.notificationService.success('Test file selected: ' + file.name);
    }
  }

  /**
   * Test template
   */
  async testTemplate(): Promise<void> {
    const templateFile = this.selectedFile();
    const excelFile = this.testFile();
    
    if (!templateFile) {
      this.notificationService.warning('Please select a template file');
      return;
    }
    
    if (!excelFile) {
      this.notificationService.warning('Please select a filled Excel file for testing');
      return;
    }

    const confirmed = await this.notificationService.confirm(
      'Test Template',
      'Generate and download a test document from your template?',
      'Generate',
      'Cancel'
    );

    if (!confirmed) return;

    this.notificationService.showLoading('Generating test document...');
    
    // Determine export format from uploadForm
    let exportFormat = 0; // Original
    if (this.uploadForm.outputFormat === 'PDF') exportFormat = 4;
    else if (this.uploadForm.outputFormat === 'DOCX') exportFormat = 1;
    
    this.tmsApi.testTemplateWithoutSaving(templateFile, excelFile, exportFormat).subscribe({
      next: (blob) => {
        this.notificationService.hideLoading();
        
        // Determine file extension based on export format
        let extension = '.docx';
        if (this.uploadForm.outputFormat === 'PDF') extension = '.pdf';
        else if (this.uploadForm.outputFormat === 'Excel') extension = '.xlsx';
        
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Test_${templateFile.name.replace(/\.[^/.]+$/, '')}${extension}`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        
        this.notificationService.success('Test document generated successfully!');
      },
      error: (error) => {
        console.error('Error testing template:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to generate test document');
      }
    });
  }

  /**
   * Download template placeholders as Excel
   */
  downloadPlaceholdersExcel(template: Template): void {
    this.notificationService.showLoading('Downloading placeholders...');
    this.tmsApi.downloadPlaceholdersExcel(template.id).subscribe({
      next: (blob) => {
        this.notificationService.hideLoading();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Placeholders_${template.name}.xlsx`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      },
      error: (error) => {
        console.error('Error downloading placeholders:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to download placeholders');
      }
    });
  }

  /**
   * Toggle template active/inactive status
   */
  async toggleTemplateStatus(template: Template): Promise<void> {
    const newStatus = !template.isActive;
    const action = newStatus ? 'activate' : 'deactivate';
    
    const confirmed = await this.notificationService.confirm(
      `${action.charAt(0).toUpperCase() + action.slice(1)} Template`,
      `Are you sure you want to ${action} "${template.name}"?`,
      action.charAt(0).toUpperCase() + action.slice(1),
      'Cancel'
    );

    if (!confirmed) return;

    this.notificationService.showLoading(`${action.charAt(0).toUpperCase() + action.slice(1)}ing template...`);
    
    const observable = newStatus 
      ? this.tmsApi.activateTemplate(template.id)
      : this.tmsApi.deactivateTemplate(template.id);
    
    observable.subscribe({
      next: () => {
        this.notificationService.hideLoading();
        this.notificationService.success(`Template ${action}d successfully`);
        this.loadTemplates();
      },
      error: (error: any) => {
        console.error(`Error ${action}ing template:`, error);
        this.notificationService.hideLoading();
        this.notificationService.error(`Failed to ${action} template`);
      }
    });
  }

  /**
   * Download template file from CMS
   */
  downloadTemplate(template: Template): void {
    if (!template.cmsDocumentId) {
      this.notificationService.error('Template file not found');
      return;
    }

    this.notificationService.showLoading('Downloading template...');
    this.cmsApi.downloadDocument(template.cmsDocumentId).subscribe({
      next: (blob) => {
        this.notificationService.hideLoading();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = template.name;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
        this.notificationService.success('Template downloaded successfully');
      },
      error: (error) => {
        console.error('Error downloading template:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to download template');
      }
    });
  }

  /**
   * Soft delete template (move to trash)
   */
  async deleteTemplate(template: Template): Promise<void> {
    const confirmed = await this.notificationService.confirm(
      'Move to Trash',
      `Are you sure you want to move "${template.name}" to trash?`,
      'Move to Trash',
      'Cancel'
    );

    if (!confirmed) return;

    this.notificationService.showLoading('Moving to trash...');
    this.tmsApi.deleteTemplate(template.id).subscribe({
      next: () => {
        this.notificationService.hideLoading();
        this.notificationService.success('Template moved to trash');
        this.loadTemplates();
      },
        error: (error: any) => {
        console.error('Error moving template to trash:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to move template to trash');
      }
    });
  }

  /**
   * Filter templates by status
   */
  filterTemplates(status: 'all' | 'active' | 'inactive' | 'draft'): void {
    this.currentFilter.update(f => ({ ...f, status }));
  }

  /**
   * Filter templates by type
   */
  filterByType(type: string): void {
    this.currentFilter.update(f => ({ ...f, type }));
  }

  /**
   * Search templates
   */
  searchTemplates(): void {
    this.currentFilter.update(f => ({ ...f, searchTerm: this.searchTerm }));
    this.currentPage.set(1); // Reset to first page on search
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

  /**
   * Get status badge class
   */
  getStatusBadgeClass(template: Template): string {
    if (template.isActive) return 'badge bg-success';
    return 'badge bg-secondary';
  }

  /**
   * Get status text
   */
  getStatusText(template: Template): string {
    if (template.isActive) return 'Active';
    return 'Inactive';
  }

  /**
   * Get type badge class
   */
  getTypeBadgeClass(type: string): string {
    const typeMap: { [key: string]: string } = {
      'Report': 'badge bg-primary',
      'Invoice': 'badge bg-success',
      'Contract': 'badge bg-warning',
      'Letter': 'badge bg-info'
    };
    return typeMap[type] || 'badge bg-secondary';
  }

  /**
   * Reset upload form
   */
  private resetUploadForm(): void {
    this.uploadForm = {
      name: '',
      type: '',
      description: '',
      outputFormat: 'PDF'
    };
    this.selectedFile.set(null);
    this.testFile.set(null);
    this.uploadedTemplateId.set(null);
    const fileInputs = document.querySelectorAll('input[type="file"]') as NodeListOf<HTMLInputElement>;
    fileInputs.forEach(input => input.value = '');
  }
}
