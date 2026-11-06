import { Component, inject } from '@angular/core';
import { AnimatedNumber } from '../shared/animated-number';
import { HostStore } from './host.store';

@Component({
  selector: 'app-leaderboard',
  imports: [AnimatedNumber],
  template: `
    <h2>Leaderboard</h2>
    <div class="head">
      <span>Team</span>
      <span>Bid Balance</span>
      <span>Wallet</span>
    </div>
    <ol>
      @for (team of store.leaderboard(); track team.id) {
        <li [class.out]="!team.canBid" [style.--team-color]="team.color">
          <span class="name">
            <i class="dot" [class.online]="team.online" [attr.aria-label]="team.online ? 'Online' : 'Offline'"></i>
            {{ team.name }}
          </span>
          <app-animated-number class="balance" [value]="team.bidBalance" />
          <app-animated-number class="wallet" [value]="team.wallet" />
        </li>
      }
    </ol>
  `,
  styles: `
    :host {
      display: flex;
      height: 100%;
      flex-direction: column;
      gap: 1.5vh;
      padding: 2vh 1vw;
    }

    h2 {
      font-size: clamp(1.4rem, 2vw, 2.6rem);
      text-align: center;
    }

    .head,
    li {
      display: grid;
      align-items: center;
      column-gap: 0.5vw;
      grid-template-columns: minmax(0, 1fr) 5vw 3.4vw;
    }

    .head {
      color: var(--champagne);
      font-size: 0.7rem;
      letter-spacing: 0.04em;
      text-transform: uppercase;

      span:not(:first-child) {
        text-align: right;
      }
    }

    ol {
      display: flex;
      min-height: 0;
      flex: 1;
      flex-direction: column;
      gap: 0.5vh;
      margin: 0;
      padding: 0;
      list-style: none;
      overflow: hidden;
    }

    li {
      min-height: 0;
      flex: 0 1 5.4vh; // Rows shrink to fit, so 20 teams never run off the screen.
      padding: 0 0.5vw;
      border-radius: 8px;
      background: var(--team-color);

      &.out {
        opacity: 0.45;
      }
    }

    .name {
      overflow: hidden;
      font-size: clamp(0.9rem, 1.15vw, 1.6rem);
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .dot {
      display: inline-block;
      width: 0.6em;
      height: 0.6em;
      margin-right: 0.3em;
      border-radius: 50%;
      background: #2b2624;

      &.online {
        background: #4cd98a;
        box-shadow: 0 0 6px #4cd98a;
      }
    }

    .balance,
    .wallet {
      font-family: var(--heading-font);
      text-align: right;
    }

    .balance {
      font-size: clamp(0.9rem, 1.2vw, 1.7rem);
    }

    .wallet {
      color: var(--champagne);
      font-size: clamp(1.1rem, 1.6vw, 2.2rem);
    }
  `,
})
export class Leaderboard {
  protected readonly store = inject(HostStore);
}
