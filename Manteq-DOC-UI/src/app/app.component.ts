import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ManteqLibConfig, UserContext, MainLayoutComponent } from '../../projects/manteq-ui-lib/src/public-api';

@Component({
  selector: 'app-root',
  imports: [MainLayoutComponent, RouterOutlet],
  template: `
    <manteq-main-layout 
      [config]="currentConfig()" 
      [userContext]="currentUserContext()">
      <router-outlet></router-outlet>
    </manteq-main-layout>
  `,
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'Manteq-DOC-UI';
  
  // Reactive config and user context
  currentConfig = signal<ManteqLibConfig>({
    companyName: 'Manteq Doc Portal',
    companyLogo: 'https://cdn-icons-png.flaticon.com/512/861/861377.png',
    primaryColor: '#16098dff',
    accentColor: '#112D4E',
    fontStyle: 'OpenSans',
    disclaimer: 'Built By ❤️ With Manteq Team',
  });

  currentUserContext = signal<UserContext>({
    email: 'john.smith@example.com',
    name: 'John Smith',
  });
}
