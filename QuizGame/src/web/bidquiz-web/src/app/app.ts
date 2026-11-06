import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { GameConnection } from './core/game-connection';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `
    @if (hub.state() === 'reconnecting') {
      <div class="conn-banner">Reconnecting…</div>
    }
    <router-outlet />
  `,
})
export class App {
  protected readonly hub = inject(GameConnection);
}
