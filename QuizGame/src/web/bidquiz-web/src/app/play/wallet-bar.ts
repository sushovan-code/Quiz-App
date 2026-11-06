import { Component, inject } from '@angular/core';
import { AnimatedNumber } from '../shared/animated-number';
import { TeamStore } from './team.store';

@Component({
  selector: 'app-wallet-bar',
  imports: [AnimatedNumber],
  template: `
    @if (store.me(); as me) {
      <header>
        <div class="top">
          <strong>{{ me.name }}</strong>
          <button class="logout" (click)="store.logout()">Log out</button>
        </div>
        <div class="stats">
          <span>Bid Balance <app-animated-number [value]="me.bidBalance" /></span>
          <span>Wallet <app-animated-number [value]="me.wallet" /></span>
        </div>
      </header>
    }
  `,
  styles: `
    header {
      position: sticky;
      z-index: 5;
      top: 0;
      padding: 0.7rem 1rem;
      border-bottom: 2px solid var(--gold);
      background: rgb(10 5 5 / 45%);
    }

    .top {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
    }

    .logout {
      flex: none;
      padding: 0.3rem 0.7rem;
      border: 1px solid var(--champagne);
      border-radius: 8px;
      background: rgb(10 5 5 / 45%);
      color: var(--ivory);
      cursor: pointer;
      font: inherit;
      font-size: 0.85rem;
    }

    strong {
      display: block;
      min-width: 0;
      overflow: hidden;
      color: var(--champagne);
      font-family: var(--heading-font);
      font-size: 1.2rem;
      font-weight: 400;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .stats {
      display: flex;
      justify-content: space-between;
      margin-top: 0.2rem;
      font-size: 0.95rem;
    }

    app-animated-number {
      margin-left: 0.25rem;
      font-family: var(--heading-font);
      font-size: 1.15rem;
    }
  `,
})
export class WalletBar {
  protected readonly store = inject(TeamStore);
}
