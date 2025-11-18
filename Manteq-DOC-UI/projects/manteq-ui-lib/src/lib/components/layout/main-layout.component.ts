import { Component, signal, Input, OnChanges, SimpleChanges, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { HeaderComponent } from './header.component';
import { SidebarComponent, NavItem } from './sidebar.component';
import { ThemeService } from '../../services/theme.service';
import { ManteqLibConfig, UserContext } from '../../models/config.model';

@Component({
  selector: 'manteq-main-layout',
  standalone: true,
  imports: [CommonModule, HeaderComponent, SidebarComponent, RouterOutlet],
  template: `
    <div class="main-layout-container">
      <!-- Mobile Menu Toggle -->
      <button 
        class="mobile-menu-toggle d-lg-none"
        (click)="toggleSidebar()"
        [attr.aria-expanded]="sidebarOpen()">
        <i class="bi" [class.bi-list]="!sidebarOpen()" [class.bi-x-lg]="sidebarOpen()"></i>
      </button>

      <!-- Sidebar -->
      <div class="sidebar-wrapper" [class.open]="sidebarOpen()">
        <manteq-sidebar 
          [items]="navItems()"
          [activeRoute]="getCurrentRoute()"
          (navigate)="handleNavigate($event)">
        </manteq-sidebar>
      </div>

      <!-- Overlay for mobile -->
      <div 
        class="sidebar-overlay d-lg-none" 
        [class.show]="sidebarOpen()"
        (click)="closeSidebar()">
      </div>
      
      <!-- Main Content Area -->
      <div class="content-wrapper">
        <manteq-header></manteq-header>
        <div class="main-content">
          <router-outlet></router-outlet>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .main-layout-container {
      min-height: 100vh;
      background-color: #f8f9fa;
      font-family: var(--manteq-font-family, 'Inter', sans-serif);
      display: flex;
      position: relative;
    }

    /* Mobile Menu Toggle */
    .mobile-menu-toggle {
      position: absolute;
      top: 1.25rem;
      left: 1rem;
      z-index: 10;
      width: 40px;
      height: 40px;
      border-radius: 8px;
      border: none;
      background-color: var(--manteq-primary-color, #2c3e50);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1.5rem;
      cursor: pointer;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
      transition: all 0.3s ease;
    }

    .mobile-menu-toggle:hover {
      background-color: var(--manteq-primary-hover, #1a252f);
      transform: scale(1.05);
    }

    .mobile-menu-toggle:active {
      transform: scale(0.95);
    }

    /* Sidebar Wrapper */
    .sidebar-wrapper {
      position: fixed;
      top: 0;
      left: 0;
      height: 100vh;
      width: 280px;
      background-color: white;
      border-right: 1px solid #dee2e6;
      z-index: 1040;
      transition: transform 0.3s ease;
      overflow-y: auto;
    }

    /* Mobile: Hidden by default */
    @media (max-width: 991.98px) {
      .sidebar-wrapper {
        transform: translateX(-100%);
      }

      .sidebar-wrapper.open {
        transform: translateX(0);
      }
    }

    /* Desktop: Always visible */
    @media (min-width: 992px) {
      .sidebar-wrapper {
        position: fixed;
        transform: none;
      }

      .mobile-menu-toggle {
        display: none;
      }
    }

    /* Sidebar Overlay */
    .sidebar-overlay {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background-color: rgba(0, 0, 0, 0.5);
      z-index: 1030;
      opacity: 0;
      visibility: hidden;
      transition: opacity 0.3s ease, visibility 0.3s ease;
    }

    .sidebar-overlay.show {
      opacity: 1;
      visibility: visible;
    }

    /* Content Wrapper */
    .content-wrapper {
      flex: 1;
      margin-left: 0;
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    @media (min-width: 992px) {
      .content-wrapper {
        margin-left: 280px;
      }
    }

    /* Main Content */
    .main-content {
      flex: 1;
      padding: 2rem;
    }

    @media (max-width: 767.98px) {
      .main-content {
        padding: 1rem;
      }
    }
  `]
})
export class MainLayoutComponent implements OnChanges {
  sidebarOpen = signal(false);
  
  // Inputs for dynamic configuration
  @Input() config!: ManteqLibConfig;
  @Input() userContext!: UserContext;
  
  navItems = signal<NavItem[]>([
    { label: 'Document Library', icon: 'bi-folder-fill', route: 'documents' },
    { label: 'Template Builder', icon: 'bi-file-earmark-text', route: 'templates' },
    { label: 'Email Templates', icon: 'bi-envelope', route: 'emails' },
    { label: 'Trash', icon: 'bi-trash', route: 'trash' }
  ]);
  
  private themeService = inject(ThemeService);
  
  constructor(private router: Router) {
    // Watch for config changes and apply theme
    effect(() => {
      if (this.config) {
        this.themeService.setConfig(this.config);
      }
    });
  }
  
  ngOnChanges(changes: SimpleChanges): void {
    // Handle initial load and subsequent changes
    if (changes['config'] && this.config) {
      this.themeService.setConfig(this.config);
    }
  }
  
  toggleSidebar(): void {
    this.sidebarOpen.update(v => !v);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }
  
  handleNavigate(route: string): void {
    this.router.navigate([route]);
    // Close sidebar on mobile after navigation
    this.closeSidebar();
  }

  getCurrentRoute(): string {
    const url = this.router.url;
    const route = url.split('/').pop() || 'documents';
    return route;
  }
}
