import { Component, inject } from '@angular/core';
import { TeamStore } from './team.store';

@Component({
  selector: 'app-waiting',
  template: `
    <div class="center-screen">
      @if (store.outOfGame()) {
        <h2>You’re out of the game</h2>
        <p>Your Bid Balance is 0, so you can’t bid any more.</p>
      } @else {
        @if (store.notice(); as notice) {
          <h2>{{ notice }}</h2>
        }
        <p>Waiting for next question…</p>
      }
    </div>
  `,
  styles: `
    .center-screen {
      min-height: calc(100dvh - 5rem);
    }

    p {
      margin: 0;
      animation: pulse 2.4s ease-in-out infinite;
      font-size: 1.25rem;
    }

    @keyframes pulse {
      50% {
        opacity: 0.45;
      }
    }
  `,
})
export class Waiting {
  protected readonly store = inject(TeamStore);
}
