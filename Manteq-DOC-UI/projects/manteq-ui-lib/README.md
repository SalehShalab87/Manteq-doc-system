# Manteq UI Library

Embeddable Angular 19 UI library for Document Management, Template Building, and Email Services with dynamic theming and user context.

## Prerequisites

The parent Angular application must include Bootstrap 5.3+ and Bootstrap Icons.

### Install Dependencies

```bash
npm install bootstrap bootstrap-icons sweetalert2
```

### Add Bootstrap CSS

In your parent app's `angular.json`:

```json
{
  "styles": [
    "node_modules/bootstrap/dist/css/bootstrap.min.css",
    "node_modules/bootstrap-icons/font/bootstrap-icons.css",
    "src/styles.scss"
  ]
}
```

Or in your `styles.scss`:

```scss
@import 'bootstrap/scss/bootstrap';
@import 'bootstrap-icons/font/bootstrap-icons.css';
```

## Configuration

### API Base URLs

Developers can modify API base URLs in the constants file:

**File:** `projects/manteq-ui-lib/src/lib/constants/api.constants.ts`

```typescript
export const API_BASE_URLS = {
  CMS: 'http://localhost:5000',      // Change to your CMS API URL
  TMS: 'http://localhost:5267',      // Change to your TMS API URL
  EMAIL: 'http://localhost:5030'     // Change to your Email API URL
} as const;
```

### Parent App Configuration

Configure the library in your parent app with theme and user context:

```typescript
// app.config.ts
import { ApplicationConfig, provideHttpClient, withInterceptors } from '@angular/core';
import { MANTEQ_CONFIG, USER_CONTEXT, authInterceptor } from '@manteq/ui-lib';

export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor])),
    {
      provide: MANTEQ_CONFIG,
      useValue: {
        companyName: "SME Portal",
        companyLogo: null,
        primaryColor: "#2c3e50",
        accentColor: "#2c3e50",
        fontStyle: "Inter",
        disclaimer: "By Manteq"
      }
    },
    {
      provide: USER_CONTEXT,
      useValue: {
        email: 'john.smith@example.com',
        name: 'John Smith'
      }
    }
  ]
};
```

## Usage

### Document Library

```typescript
import { Component } from '@angular/core';
import { DocumentLibraryComponent } from '@manteq/ui-lib';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [DocumentLibraryComponent],
  template: '<manteq-document-library></manteq-document-library>'
})
export class DocumentsPage {}
```

## Features

- **Dynamic Theming**: Automatically applies primary/accent colors and font styles from configuration
- **User Context**: All API requests include user email in `X-SME-UserId` header
- **SweetAlert2 Integration**: Beautiful toasts and confirmation dialogs
- **Signal-based State**: Modern Angular signals for reactive state management
- **Standalone Components**: Tree-shakeable, can be imported individually
- **Bootstrap 5**: Responsive, mobile-friendly UI components

## Build

Run `ng build manteq-ui-lib` to build the project. The build artifacts will be stored in the `dist/` directory.

## Current Implementation Status

✅ **Phase 1: Core Infrastructure** - Complete
- Configuration and theming with signals
- API services with constants
- SweetAlert2 integration
- HTTP interceptor for user context
- Shared utilities and pipes

✅ **Phase 2: CMS Module - Document Library** - Complete
- Document upload with file selection
- Document list with filtering (status, type, search)
- Download functionality
- Activate/deactivate documents
- Responsive Bootstrap UI matching mockups

⏳ **Phase 3: TMS Module** - Next
- Template builder
- Template upload and management
- Document generation with placeholders

⏳ **Phase 4: Email Module** - Planned
- Email templates management
- Email composer with rich text
- Template and document attachments
