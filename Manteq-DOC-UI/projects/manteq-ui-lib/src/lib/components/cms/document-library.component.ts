import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CmsApiService } from '../../services/cms-api.service';
import { NotificationService } from '../../services/notification.service';
import { Document, DocumentFilters } from '../../models/cms.models';
import { FileSizePipe } from '../../pipes/file-size.pipe';
import { DateFormatPipe } from '../../pipes/date-format.pipe';

@Component({
  selector: 'manteq-document-library',
  standalone: true,
  imports: [CommonModule, FormsModule, FileSizePipe, DateFormatPipe],
  templateUrl: './document-library.component.html',
  styleUrls: ['./document-library.component.scss']
})
export class DocumentLibraryComponent implements OnInit {
  private cmsApi = inject(CmsApiService);
  private notificationService = inject(NotificationService);
  
  // Expose Math for template
  Math = Math;

  // Signals for state management
  documents = signal<Document[]>([]);
  filteredDocuments = computed(() => {
    const docs = this.documents();
    const filter = this.currentFilter();
    
    let filtered = docs;

    // Filter by status
    if (filter.status === 'active') {
      filtered = filtered.filter(d => d.isActive);
    } else if (filter.status === 'inactive') {
      filtered = filtered.filter(d => !d.isActive);
    }

    // Filter by type
    if (filter.type) {
      filtered = filtered.filter(d => d.type?.toLowerCase().includes(filter.type!.toLowerCase()));
    }

    // Filter by search term
    if (this.searchTerm()) {
      const term = this.searchTerm().toLowerCase();
      filtered = filtered.filter(d => 
        d.name.toLowerCase().includes(term) ||
        d.type?.toLowerCase().includes(term)
      );
    }

    return filtered;
  });

  loading = signal(false);
  currentFilter = signal<DocumentFilters>({ status: 'all' });
  selectedFile = signal<File | null>(null);
  uploadForm = {
    name: '',
    type: ''
  };
  searchTerm = signal('');
  
  // Pagination
  currentPage = signal(1);
  itemsPerPage = signal(10);
  
  paginatedDocuments = computed(() => {
    const filtered = this.filteredDocuments();
    const page = this.currentPage();
    const perPage = this.itemsPerPage();
    const start = (page - 1) * perPage;
    const end = start + perPage;
    return filtered.slice(start, end);
  });
  
  totalPages = computed(() => {
    const total = this.filteredDocuments().length;
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
    this.loadDocuments();
  }

  /**
   * Load all documents
   */
  loadDocuments(): void {
    this.loading.set(true);
    this.cmsApi.getDocuments().subscribe({
      next: (docs) => {
        this.documents.set(docs);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading documents:', error);
        this.notificationService.error('Failed to load documents');
        this.loading.set(false);
      }
    });
  }

  /**
   * Handle file selection
   */
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedFile.set(input.files[0]);
    }
  }

  /**
   * Upload document
   */
  uploadDocument(): void {
    const file = this.selectedFile();
    if (!file || !this.uploadForm.name) {
      this.notificationService.warning('Please provide document name and select a file');
      return;
    }

    this.notificationService.showLoading('Uploading document...');
    this.cmsApi.uploadDocument(this.uploadForm.name, file, this.uploadForm.type || undefined).subscribe({
      next: (response) => {
        this.notificationService.hideLoading();
        this.notificationService.success('Document uploaded successfully');
        this.resetUploadForm();
        // Reload documents to get complete data
        this.loadDocuments();
      },
      error: (error) => {
        console.error('Error uploading document:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to upload document');
      }
    });
  }

  /**
   * Download document
   */
  downloadDocument(doc: Document): void {
    this.notificationService.showLoading('Downloading...');
    this.cmsApi.downloadDocument(doc.id).subscribe({
      next: (blob) => {
        this.notificationService.hideLoading();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = doc.name;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      },
      error: (error) => {
        console.error('Error downloading document:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to download document');
      }
    });
  }

  /**
   * Delete document (soft delete)
   */
  async deleteDocument(doc: Document): Promise<void> {
    const confirmed = await this.notificationService.confirm(
      'Delete Document',
      `Are you sure you want to delete "${doc.name}"? This will move it to trash.`,
      'Delete',
      'Cancel'
    );

    if (!confirmed) return;

    this.notificationService.showLoading('Deleting document...');

    this.cmsApi.deleteDocument(doc.id).subscribe({
      next: () => {
        this.notificationService.hideLoading();
        this.notificationService.success('Document moved to trash');
        this.loadDocuments();
      },
      error: (error) => {
        console.error('Error deleting document:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to delete document');
      }
    });
  }

  /**
   * Toggle document status
   */
  async toggleDocumentStatus(doc: Document): Promise<void> {
    const action = doc.isActive ? 'deactivate' : 'activate';
    const confirmed = await this.notificationService.confirm(
      `${action.charAt(0).toUpperCase() + action.slice(1)} Document`,
      `Are you sure you want to ${action} "${doc.name}"?`,
      'Yes',
      'Cancel'
    );

    if (!confirmed) return;

    this.notificationService.showLoading(`${action}ing document...`);
    const apiCall = doc.isActive 
      ? this.cmsApi.deactivateDocument(doc.id)
      : this.cmsApi.activateDocument(doc.id);

    apiCall.subscribe({
      next: () => {
        this.notificationService.hideLoading();
        this.notificationService.success(`Document ${action}d successfully`);
        this.documents.update(docs => 
          docs.map(d => d.id === doc.id ? { ...d, isActive: !d.isActive } : d)
        );
      },
      error: (error) => {
        console.error(`Error ${action}ing document:`, error);
        this.notificationService.hideLoading();
        this.notificationService.error(`Failed to ${action} document`);
      }
    });
  }

  /**
   * Filter documents
   */
  filterDocuments(status: 'all' | 'active' | 'inactive'): void {
    this.currentFilter.update(f => ({ ...f, status }));
  }

  /**
   * Search documents
   */
  searchDocuments(): void {
    this.currentFilter.update(f => ({ ...f, searchTerm: this.searchTerm() }));
    this.currentPage.set(1); // Reset to first page on search
  }

  updateSearchTerm(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
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

  /**
   * Reset upload form
   */
  private resetUploadForm(): void {
    this.uploadForm.name = '';
    this.uploadForm.type = '';
    this.selectedFile.set(null);
    // Reset file input
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }
  }
}
