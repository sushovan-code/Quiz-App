import { Component, computed, inject, linkedSignal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ApiService } from '../core/api.service';
import { CountdownRing } from '../shared/countdown-ring';
import { TeamStore } from './team.store';

@Component({
  selector: 'app-question-popup',
  imports: [CountdownRing, DecimalPipe],
  template: `
    <div class="overlay">
      @if (store.question(); as question) {
        <form class="dialog" (submit)="$event.preventDefault(); submit()">
          <app-countdown-ring
            class="ring"
            [remainingMs]="store.countdown.remainingMs()"
            [totalSeconds]="totalSeconds()"
          />
          <p class="category">{{ question.category }}: name the movie</p>
          <h2>{{ question.text }}</h2>
          @if (question.imageUrl) {
            <img [src]="question.imageUrl" alt="" />
          }

          <label for="answer">Your answer</label>
          <input
            #answerField
            id="answer"
            class="field"
            maxlength="500"
            autocomplete="off"
            [value]="answer()"
            (input)="answer.set(answerField.value)"
          />

          @if (store.bidOptions().length) {
            <label for="bid"
              >Your bid (Bid Balance {{ store.me()?.bidBalance | number: '1.0-0' }})</label
            >
            <select #bidField id="bid" class="field" (change)="bid.set(+bidField.value)">
              @for (option of store.bidOptions(); track option) {
                <option [value]="option" [selected]="option === bid()">
                  {{ option | number: '1.0-0' }}
                </option>
              }
            </select>
            <button class="btn" type="submit" [disabled]="!canSubmit()">Submit</button>
          }

          @if (store.error(); as error) {
            <p class="error">{{ error }}</p>
          }
        </form>
      }
    </div>
  `,
  styles: `
    // In the page flow, so the balance bar stays readable while bidding.
    .overlay {
      position: static;
      align-items: flex-start;
      background: none;
    }

    .dialog {
      display: flex;
      background: rgb(10 5 5 / 55%);
      width: min(30rem, 100%);
      flex-direction: column;
      gap: 0.6rem;
      padding: 1.1rem;
    }

    .ring {
      width: 6.5rem;
      height: 6.5rem;
      align-self: center;
    }

    h2 {
      margin-bottom: 0.3rem;
      color: var(--ivory);
      font-size: 1.35rem;
      line-height: 1.25;
      text-align: center;
    }

    img {
      max-width: 100%;
      max-height: 30dvh;
      align-self: center;
      border-radius: 6px;
    }

    .btn {
      margin-top: 0.4rem;
    }

    p {
      margin: 0;
      text-align: center;
    }

    .category {
      color: var(--champagne);
      font-size: 0.9rem;
    }
  `,
})
export class QuestionPopup {
  protected readonly store = inject(TeamStore);
  private readonly api = inject(ApiService);

  /** Prefilled from the existing bid when a team changes their answer. */
  protected readonly answer = linkedSignal(() => this.store.myBid()?.answer ?? '');
  protected readonly bid = linkedSignal(
    () => this.store.myBid()?.amount ?? this.store.bidOptions()[0] ?? 0,
  );

  protected readonly totalSeconds = computed(() => this.api.config()?.questionSeconds ?? 30);
  protected readonly canSubmit = computed(
    () =>
      this.answer().trim().length > 0 &&
      this.store.bidOptions().includes(this.bid()) &&
      this.store.countdown.remainingMs() > 0 &&
      !this.store.submitting(),
  );

  protected submit(): void {
    if (this.canSubmit()) {
      void this.store.submitBid(this.answer(), this.bid());
    }
  }
}
