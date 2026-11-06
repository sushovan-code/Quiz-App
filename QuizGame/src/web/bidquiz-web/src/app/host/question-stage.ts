import { Component, computed, inject } from '@angular/core';
import { GAME_API } from '../core/game-api';
import { CountdownRing } from '../shared/countdown-ring';
import { HostStore } from './host.store';

@Component({
  selector: 'app-question-stage',
  imports: [CountdownRing],
  template: `
    @if (store.question(); as question) {
      <h3 class="label">Question {{ question.order }} · {{ question.category }}: name the movie</h3>
      <h1 class="text">{{ question.text }}</h1>
      @if (question.imageUrl) {
        <img [src]="question.imageUrl" alt="" />
      }
      @if (store.status() === 'Accepting') {
        <app-countdown-ring
          class="ring"
          [remainingMs]="store.countdown.remainingMs()"
          [totalSeconds]="totalSeconds()"
        />
      } @else {
        <h2 class="closed">{{ store.status() === 'Revealed' ? 'Bids revealed' : 'Time’s up!' }}</h2>
      }
      <p class="count">{{ store.answered() }} of {{ store.teamCount() }} teams answered</p>
    } @else {
      <h1 class="idle">Pick a question</h1>
    }
  `,
  styles: `
    :host {
      display: flex;
      height: 100%;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 2.5vh;
      padding: 3vh 3vw;
      text-align: center;
    }

    .label {
      font-size: clamp(1rem, 1.6vw, 2rem);
      letter-spacing: 0.1em;
    }

    .text {
      color: var(--ivory);
      font-size: clamp(1.8rem, 3.2vw, 4.5rem);
      line-height: 1.2;
    }

    img {
      min-height: 0;
      max-width: 100%;
      flex: 0 1 28vh;
      border: 2px solid var(--gold);
      border-radius: 8px;
      object-fit: contain;
    }

    .ring {
      width: 34vh;
      height: 34vh;
      flex: none;
    }

    .closed {
      font-size: clamp(1.6rem, 3vw, 3.5rem);
    }

    .count {
      margin: 0;
      font-size: clamp(1.1rem, 1.7vw, 2.2rem);
    }

    .idle {
      font-size: clamp(2.5rem, 5vw, 6rem);
      text-shadow: 0 0 24px rgb(212 175 55 / 55%);
    }
  `,
})
export class QuestionStage {
  protected readonly store = inject(HostStore);
  private readonly api = inject(GAME_API);

  protected readonly totalSeconds = computed(() => this.api.config()?.questionSeconds ?? 30);
}
