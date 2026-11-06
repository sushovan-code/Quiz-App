import { Component, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { TeamStore } from './team.store';

@Component({
  selector: 'app-confirmation',
  imports: [DecimalPipe],
  template: `
    <div class="center-screen">
      <h2>Bid locked in!</h2>
      @if (store.myBid(); as bid) {
        <section class="panel">
          <p>{{ bid.answer }}</p>
          <strong>{{ bid.amount | number: '1.0-0' }}</strong>
        </section>
      }
      <p>{{ store.countdown.seconds() }}s left</p>
      <button class="btn" (click)="store.editing.set(true)">Change my bid</button>
    </div>
  `,
  styles: `
    .center-screen {
      min-height: calc(100dvh - 5rem);
    }

    h2 {
      font-size: 2rem;
    }

    section {
      width: min(24rem, 100%);
      padding: 1rem;
    }

    p {
      margin: 0;
      overflow-wrap: anywhere;
    }

    strong {
      display: block;
      margin-top: 0.4rem;
      color: var(--champagne);
      font-family: var(--heading-font);
      font-size: 2rem;
      font-weight: 400;
    }
  `,
})
export class Confirmation {
  protected readonly store = inject(TeamStore);
}
