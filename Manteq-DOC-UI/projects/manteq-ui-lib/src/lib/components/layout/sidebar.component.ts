import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface NavItem {
  label: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'manteq-sidebar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="sidebar">
      <div class="sidebar-content">
        <div class="nav flex-column nav-pills" role="tablist">
          @for (item of items(); track item.route) {
            <button 
              class="nav-link" 
              [class.active]="item.route === activeRoute()"
              type="button"
              (click)="navigate.emit(item.route)">
              <i class="bi {{ item.icon }} me-2"></i>
              {{ item.label }}
            </button>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .sidebar {
      background-color: white;
      height: 100%;
      display: flex;
      flex-direction: column;
    }

    .sidebar-content {
      padding: 1.5rem 1rem;
      flex: 1;
      overflow-y: auto;
    }

    .nav-pills .nav-link {
      border-radius: 0.5rem;
      padding: 0.75rem 1rem;
      margin-bottom: 0.5rem;
      color: #6c757d;
      transition: all 0.2s;
      text-align: left;
      border: none;
      background: transparent;
      width: 100%;
    }

    .nav-pills .nav-link:hover {
      background-color: rgba(52, 152, 219, 0.08);
      color: var(--manteq-primary-color, #2c3e50);
      border-left: 3px solid var(--manteq-accent-color, #3498db);
      padding-left: calc(1rem - 3px);
    }

    .nav-pills .nav-link.active {
      background-color: var(--manteq-accent-color, #3498db);
      color: white;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      border-left: 3px solid var(--manteq-primary-color, #2c3e50);
      padding-left: calc(1rem - 3px);
    }
  `]
})
export class SidebarComponent {
  items = input.required<NavItem[]>();
  activeRoute = input<string>('');
  navigate = output<string>();
}
