import { Routes } from '@angular/router';
import { MANTEQ_ROUTES } from 'manteq-ui-lib';


export const routes: Routes = [
  { path: '', children: MANTEQ_ROUTES },
  { path: '**', redirectTo: '' }
];
