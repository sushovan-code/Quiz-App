import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: 'host/setup', loadComponent: () => import('./host/setup/setup-page').then((m) => m.SetupPage) },
  { path: 'host', loadComponent: () => import('./host/host-page').then((m) => m.HostPage) },
  { path: 'play', loadComponent: () => import('./play/play-page').then((m) => m.PlayPage) },
  { path: '**', redirectTo: 'play' },
];
