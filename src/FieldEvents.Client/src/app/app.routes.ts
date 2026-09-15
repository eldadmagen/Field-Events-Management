import { Routes } from '@angular/router';
import { roleGuard } from './core/role.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'dispatcher',
    canMatch: [roleGuard('Dispatcher')],
    loadComponent: () => import('./pages/dispatcher/dispatcher.component').then((m) => m.DispatcherComponent)
  },
  {
    path: 'technician',
    canMatch: [roleGuard('Technician')],
    loadComponent: () => import('./pages/technician/technician.component').then((m) => m.TechnicianComponent)
  },
  { path: '**', redirectTo: 'login' }
];
