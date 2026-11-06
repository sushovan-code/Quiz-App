import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HostLogin } from './host-login';
import { HostShell } from './host-shell';
import { HostStore } from './host.store';

@Component({
  selector: 'app-host-page',
  imports: [HostLogin, HostShell, RouterLink],
  template: `
    @if (store.restoring()) {
      <div class="center-screen"><h2>Dimming the lights…</h2></div>
    } @else if (!store.authenticated()) {
      <app-host-login />
    } @else if (!store.showStarted()) {
      <div class="center-screen">
        <h1 class="marquee">Bid Quiz</h1>
        <button class="btn start" (click)="startShow()">Start show</button>
        <a class="setup" routerLink="/host/setup">Teams &amp; questions setup</a>
      </div>
    } @else {
      <app-host-shell />
    }
  `,
  styles: `
    .marquee {
      font-size: 6rem;
      text-shadow: 0 0 24px rgb(212 175 55 / 55%);
    }

    .start {
      font-size: 1.6rem;
    }

    .setup {
      color: var(--champagne);
    }
  `,
})
export class HostPage {
  protected readonly store = inject(HostStore);

  protected async startShow(): Promise<void> {
    // Browsers only allow fullscreen from a click, hence the button even after a refresh.
    await document.documentElement.requestFullscreen().catch(() => undefined);
    this.store.showStarted.set(true);
  }
}
