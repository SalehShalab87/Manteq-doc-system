import { Routes } from '@angular/router';
import { DocumentLibraryComponent } from './components/cms/document-library.component';
import { TemplateBuilderComponent } from './components/tms/template-builder.component';
import { TrashComponent } from './components/trash/trash.component';
import { EmailTemplatesComponent } from './components/email/email-templates.component';

export const MANTEQ_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'documents',
    pathMatch: 'full'
  },
  {
    path: 'documents',
    component: DocumentLibraryComponent
  },
  {
    path: 'templates',
    component: TemplateBuilderComponent
  },
  {
    path: 'emails',
    component: EmailTemplatesComponent,
    data: { title: 'Email Templates' }
  },
  {
    path: 'trash',
    component: TrashComponent
  }
];
