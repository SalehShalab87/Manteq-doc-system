import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NotificationService } from '../../services/notification.service';
import { DateFormatPipe } from '../../pipes/date-format.pipe';
import { CmsTrashApiService, TrashItem } from '../../services/cms-trash-api.service';

@Component({
  selector: 'manteq-trash',
  standalone: true,
  imports: [CommonModule, FormsModule, DateFormatPipe],
  templateUrl: './trash.component.html',
  styleUrls: ['./trash.component.scss']
})
export class TrashComponent implements OnInit {
  private notificationService = inject(NotificationService);
  private trashService = inject(CmsTrashApiService);
  
  // Expose Math for template
  Math = Math;

  // Signals for state management
  allItems = signal<TrashItem[]>([]);
  filteredItems = computed(() => {
    const items = this.allItems();
    const filter = this.currentFilter();
    
    let filtered = items;

    // Filter by type
    if (filter.type && filter.type !== 'all') {
      filtered = filtered.filter(i => i.type === filter.type);
    }

    // Filter by search term
    if (filter.searchTerm) {
      const term = filter.searchTerm.toLowerCase();
      filtered = filtered.filter(i => 
        i.name.toLowerCase().includes(term) ||
        i.deletedBy?.toLowerCase().includes(term)
      );
    }

    return filtered;
  });

  loading = signal(false);
  currentFilter = signal<{ type?: string; searchTerm?: string }>({ type: 'all' });
  searchTerm = '';
  
  // Pagination
  currentPage = signal(1);
  itemsPerPage = signal(10);
  
  paginatedItems = computed(() => {
    const filtered = this.filteredItems();
    const page = this.currentPage();
    const perPage = this.itemsPerPage();
    const start = (page - 1) * perPage;
    const end = start + perPage;
    return filtered.slice(start, end);
  });
  
  totalPages = computed(() => {
    const total = this.filteredItems().length;
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
    this.loadTrashItems();
  }

  /**
   * Load all trash items from API
   */
  loadTrashItems(): void {
    this.loading.set(true);
    this.trashService.getTrashItems().subscribe({
      next: (response) => {
        const items: TrashItem[] = [
          ...response.documents,
          ...response.templates,
          ...response.emailTemplates
        ];
        this.allItems.set(items);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading trash items:', error);
        this.notificationService.error('Failed to load trash items');
        this.loading.set(false);
      }
    });
  }

  /**
   * Restore item from trash
   */
  async restoreItem(item: TrashItem): Promise<void> {
    const confirmed = await this.notificationService.confirm(
      'Restore Item',
      `Are you sure you want to restore "${item.name}"?`,
      'Restore',
      'Cancel'
    );

    if (!confirmed) return;

    this.notificationService.showLoading('Restoring item...');
    
    const restoreObs = item.type === 'Document' 
      ? this.trashService.restoreDocument(item.id)
      : item.type === 'Template'
      ? this.trashService.restoreTemplate(item.id)
      : this.trashService.restoreEmailTemplate(item.id);

    restoreObs.subscribe({
      next: () => {
        this.notificationService.hideLoading();
        this.notificationService.success(`${item.name} restored successfully`);
        this.loadTrashItems();
      },
      error: (error) => {
        console.error('Error restoring item:', error);
        this.notificationService.hideLoading();
        this.notificationService.error(`Failed to restore ${item.name}`);
      }
    });
  }

  /**
   * Permanently delete item
   */
  async permanentlyDelete(item: TrashItem): Promise<void> {
    const confirmed = await this.notificationService.confirm(
      'Permanently Delete',
      `Are you sure you want to permanently delete "${item.name}"? This action cannot be undone.`,
      'Delete Forever',
      'Cancel'
    );

    if (!confirmed) return;

    this.notificationService.showLoading('Deleting permanently...');
    
    const deleteObs = item.type === 'Document'
      ? this.trashService.permanentlyDeleteDocument(item.id)
      : item.type === 'Template'
      ? this.trashService.permanentlyDeleteTemplate(item.id)
      : this.trashService.permanentlyDeleteEmailTemplate(item.id);

    deleteObs.subscribe({
      next: () => {
        this.notificationService.hideLoading();
        this.notificationService.success(`${item.name} permanently deleted`);
        this.loadTrashItems();
      },
      error: (error) => {
        console.error('Error deleting item:', error);
        this.notificationService.hideLoading();
        this.notificationService.error(`Failed to delete ${item.name}`);
      }
    });
  }

  /**
   * Empty entire trash
   */
  async emptyTrash(): Promise<void> {
    if (this.allItems().length === 0) {
      this.notificationService.warning('Trash is already empty');
      return;
    }

    const confirmed = await this.notificationService.confirm(
      'Empty Trash',
      `Are you sure you want to permanently delete all ${this.allItems().length} items? This action cannot be undone.`,
      'Empty Trash',
      'Cancel'
    );

    if (!confirmed) return;

    this.notificationService.showLoading('Emptying trash...');
    
    this.trashService.emptyTrash().subscribe({
      next: () => {
        this.notificationService.hideLoading();
        this.notificationService.success('Trash emptied successfully');
        this.loadTrashItems();
      },
      error: (error) => {
        console.error('Error emptying trash:', error);
        this.notificationService.hideLoading();
        this.notificationService.error('Failed to empty trash');
      }
    });
  }

  /**
   * Filter items by type
   */
  filterByType(type: string): void {
    this.currentFilter.update(f => ({ ...f, type }));
  }

  /**
   * Search items
   */
  searchItems(): void {
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
   * Get type badge class
   */
  getTypeBadgeClass(type: string): string {
    const typeMap: { [key: string]: string } = {
      'Document': 'badge bg-primary',
      'Template': 'badge bg-success',
      'EmailTemplate': 'badge bg-info'
    };
    return typeMap[type] || 'badge bg-secondary';
  }

  /**
   * Get type icon
   */
  getTypeIcon(type: string): string {
    const iconMap: { [key: string]: string } = {
      'Document': 'bi-file-earmark',
      'Template': 'bi-file-text',
      'EmailTemplate': 'bi-envelope'
    };
    return iconMap[type] || 'bi-file';
  }

  /**
   * Get type label
   */
  getTypeLabel(type: string): string {
    const labelMap: { [key: string]: string } = {
      'Document': 'CMS Document',
      'Template': 'TMS Template',
      'EmailTemplate': 'Email Template'
    };
    return labelMap[type] || type;
  }
}
