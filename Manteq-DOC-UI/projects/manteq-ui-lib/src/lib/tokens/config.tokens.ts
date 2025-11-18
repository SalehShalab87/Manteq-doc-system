import { InjectionToken } from '@angular/core';
import { ManteqLibConfig, UserContext } from '../models/config.model';

export const MANTEQ_CONFIG = new InjectionToken<ManteqLibConfig>('MANTEQ_CONFIG', {
  providedIn: 'root',
  factory: () => ({
    companyName: 'Doc & Email Portal',
    companyLogo: 'https://cdn-icons-png.flaticon.com/512/861/861377.png',
    primaryColor: '#2c3e50',
    accentColor: '#3498db',
    fontStyle: 'Inter, system-ui, sans-serif',
    disclaimer: 'With ❤️ By Manteq Team'
  })
});

export const USER_CONTEXT = new InjectionToken<UserContext>('USER_CONTEXT', {
  providedIn: 'root',
  factory: () => ({
    email: 'user@example.com',
    name: 'Guest User'
  })
});
