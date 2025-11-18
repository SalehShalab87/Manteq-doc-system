import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'manteq-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="library-header">
      <div class="d-flex justify-content-between align-items-center">
        <div>
          <h1 class="page-title mb-1">{{ themeService.companyName() }}</h1>
          <p class="text-muted mb-0">Manage and organize your documents efficiently</p>
        </div>
        <div class="text-end">
          <small class="text-muted">{{ themeService.disclaimer() }}</small>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .library-header {
      background-color: white;
      padding: 1.5rem 2rem;
      border-bottom: 3px solid var(--manteq-accent-color, #3498db);
      margin-bottom: 0;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
      position: relative;
    }

    @media (max-width: 991.98px) {
      .library-header {
        padding: 1rem 1.5rem 1rem 4.5rem;
      }
    }

    @media (max-width: 767.98px) {
      .library-header h1 {
        font-size: 1.25rem;
      }

      .library-header p {
        font-size: 0.875rem;
      }
    }

    .page-title {
      color: var(--manteq-primary-color, #2c3e50);
      font-size: 1.75rem;
      font-weight: 700;
      letter-spacing: -0.5px;
    }
  `]
})
export class HeaderComponent {
  themeService = inject(ThemeService);
}
