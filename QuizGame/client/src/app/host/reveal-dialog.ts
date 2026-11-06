import { Component, computed, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { HostStore } from './host.store';

@Component({
  selector: 'app-reveal-dialog',
  imports: [DecimalPipe],
  template: `
    <div class="overlay">
      <div class="dialog" role="dialog" aria-modal="true" aria-label="Revealed bids">
        <header>
          <button
            class="eye"
            [attr.aria-label]="
              store.answerVisible() ? 'Hide correct answer' : 'Show correct answer'
            "
            [attr.aria-pressed]="store.answerVisible()"
            (click)="store.toggleAnswer()"
          >
            <svg viewBox="0 0 24 24" aria-hidden="true">
              <path d="M1.5 12S5.5 4.5 12 4.5 22.5 12 22.5 12 18.5 19.5 12 19.5 1.5 12 1.5 12Z" />
              <circle cx="12" cy="12" r="3.5" />
              @if (!store.answerVisible()) {
                <path d="M3.5 3.5l17 17" />
              }
            </svg>
          </button>
          <h2>{{ store.answerVisible() ? store.correctAnswer() : 'The bids are in' }}</h2>
          <button class="close" aria-label="Close" (click)="store.dialogOpen.set(false)">✕</button>
        </header>

        @if (store.bids()?.length) {
          <ul>
            @for (bid of store.bids(); track bid.id) {
              <li [class.top]="bid.amount === topBid()">
                <span class="team" [style.--team-color]="bid.teamColor">{{ bid.teamName }}</span>
                <span class="answer">{{ bid.answer }}</span>
                <span class="amount">{{ bid.amount | number: '1.0-0' }}</span>
                <button
                  class="thumb up"
                  [class.chosen]="bid.verdict === 'Up'"
                  aria-label="Thumbs up"
                  (click)="store.judge(bid, 'Up')"
                >
                  <svg viewBox="0 0 24 24" aria-hidden="true">
                    <path
                      d="M7 21H4a1 1 0 0 1-1-1v-8a1 1 0 0 1 1-1h3m0 10V11l4.5-8a2.5 2.5 0 0 1 2.5 2.5V9h5a2 2 0 0 1 2 2.3l-1.2 7.6a2.5 2.5 0 0 1-2.5 2.1H7Z"
                    />
                  </svg>
                </button>
                <button
                  class="thumb down"
                  [class.chosen]="bid.verdict === 'Down'"
                  aria-label="Thumbs down"
                  (click)="store.judge(bid, 'Down')"
                >
                  <svg viewBox="0 0 24 24" aria-hidden="true">
                    <path
                      d="M7 3H4a1 1 0 0 0-1 1v8a1 1 0 0 0 1 1h3m0-10v10l4.5 8a2.5 2.5 0 0 0 2.5-2.5V15h5a2 2 0 0 0 2-2.3l-1.2-7.6A2.5 2.5 0 0 0 16.8 3H7Z"
                    />
                  </svg>
                </button>
              </li>
            }
          </ul>
        }

        <p class="summary">
          {{ store.bids()?.length ?? 0 }} out of {{ store.teamCount() }} teams participated
        </p>
      </div>
    </div>
  `,
  styles: `
    .dialog {
      display: flex;
      width: min(70vw, 1200px);
      max-height: 86vh;
      flex-direction: column;
      gap: 2vh;
      padding: 2.5vh 2vw;
    }

    header {
      display: grid;
      align-items: center;
      gap: 1vw;
      grid-template-columns: auto minmax(0, 1fr) auto;
    }

    h2 {
      min-width: 0;
      font-size: clamp(1.5rem, 2.6vw, 3.2rem);
      overflow-wrap: anywhere;
      text-align: left;
    }

    .eye,
    .close {
      display: grid;
      width: 3.2rem;
      height: 3.2rem;
      place-items: center;
      padding: 0.5rem;
      border: 2px solid var(--gold);
      border-radius: 50%;
      background: var(--oxblood);
      color: var(--champagne);
      cursor: pointer;
      font-size: 1.1rem;

      &:hover {
        box-shadow: var(--gold-glow);
      }
    }

    svg {
      width: 100%;
      height: 100%;
      fill: none;
      stroke: currentcolor;
      stroke-linecap: round;
      stroke-linejoin: round;
      stroke-width: 1.8;
    }

    ul {
      display: flex;
      flex-direction: column;
      gap: 1vh;
      margin: 0;
      padding: 4px;
      list-style: none;
      overflow-y: auto;
    }

    li {
      display: grid;
      align-items: center;
      gap: 1vw;
      grid-template-columns: 22% minmax(0, 1fr) auto auto auto;
      padding: 1.2vh 1vw;
      border: 1px solid rgb(212 175 55 / 30%);
      border-radius: 10px;
      background: rgb(10 5 5 / 35%);
      font-size: clamp(1rem, 1.4vw, 1.9rem);

      &.top {
        border: 2px solid var(--gold);
        background: rgb(212 175 55 / 18%);
        box-shadow: var(--gold-glow);
      }
    }

    .team {
      padding: 0.2em 0.5em;
      border-radius: 6px;
      background: var(--team-color);
      font-weight: 700;
    }

    .summary {
      margin: 0;
      color: var(--champagne);
      font-size: clamp(1rem, 1.4vw, 1.9rem);
      text-align: center;
    }

    .answer {
      overflow-wrap: anywhere;
    }

    .amount {
      font-family: var(--heading-font);
      font-size: 1.25em;
    }

    .thumb {
      display: grid;
      width: 3.2rem;
      height: 3.2rem;
      place-items: center;
      padding: 0.6rem;
      border: 2px solid currentcolor;
      border-radius: 50%;
      background: transparent;
      cursor: pointer;

      &.up {
        color: #4cc586;
      }

      &.down {
        color: #ef6a60;
      }

      &.up.chosen {
        border-color: var(--emerald);
        background: var(--emerald);
        color: var(--ivory);
      }

      &.down.chosen {
        border-color: var(--signal-red);
        background: var(--signal-red);
        color: var(--ivory);
      }
    }
  `,
})
export class RevealDialog {
  protected readonly store = inject(HostStore);
  protected readonly topBid = computed(() => this.store.bids()?.[0]?.amount);
}
