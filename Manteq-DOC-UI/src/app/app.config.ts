import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';


import { routes } from './app.routes';
import { authInterceptor, MANTEQ_CONFIG, USER_CONTEXT } from '../../projects/manteq-ui-lib/src/public-api';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    {
      provide: MANTEQ_CONFIG,
      useValue: {
        companyName: 'Manteq Doc Portal',
        companyLogo: 'https://cdn-icons-png.flaticon.com/512/861/861377.png',
        primaryColor: '#195dc2ff',
        accentColor: '#00050aff',
        fontStyle: 'OpenSans',
        disclaimer: 'Built By ❤️ With Manteq Team',
      },
    },
    {
      provide: USER_CONTEXT,
      useValue: {
        email: 'john.smith@example.com',
        name: 'John Smith',
      },
    },
  ],
};
