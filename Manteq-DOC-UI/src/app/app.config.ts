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
        companyLogo: null,
        primaryColor: '#222831',
        accentColor: '#112D4E',
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
