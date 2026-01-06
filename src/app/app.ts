import { Component } from '@angular/core';
import { MainLayoutComponent } from './components/main-layout/main-layout.component';
import { NotificationToastComponent } from './components/notification-toast/notification-toast.component';

@Component({
  selector: 'app-root',
  imports: [MainLayoutComponent, NotificationToastComponent],
  template: `
    <app-main-layout />
    <app-notification-toast />
  `,
  styles: [],
})
export class App { }
