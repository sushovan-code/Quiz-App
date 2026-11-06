import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ConfirmDialog } from './confirm-dialog';
import { HostStore } from './host.store';
import { JoinQr } from './join-qr';
import { Leaderboard } from './leaderboard';
import { QuestionGrid } from './question-grid';
import { QuestionStage } from './question-stage';
import { RevealDialog } from './reveal-dialog';

@Component({
  selector: 'app-host-shell',
  imports: [
    ConfirmDialog,
    JoinQr,
    Leaderboard,
    QuestionGrid,
    QuestionStage,
    RevealDialog,
    RouterLink,
  ],
  template: `
    <main class="stage">
      <section class="left">
        <app-join-qr />
        <app-question-grid />
        <button class="btn reveal" [disabled]="!store.canReveal()" (click)="store.revealBids()">
          Reveal bids
        </button>
        <div class="links">
          <a class="reset" routerLink="/host/setup">Setup</a>
          <button class="reset" (click)="confirmingReset.set(true)">Reset game</button>
        </div>
      </section>

      <section class="center panel">
        <app-question-stage />
      </section>

      <section class="right panel">
        <app-leaderboard />
      </section>
    </main>

    @if (store.error(); as error) {
      <p class="toast">{{ error }}</p>
    }
    @if (store.dialogOpen()) {
      <app-reveal-dialog />
    }
    @if (confirmingReset()) {
      <app-confirm-dialog
        message="Reset the game? All scores and answers will be cleared, and a new set of questions will be drawn."
        confirmLabel="Reset game"
        (confirmed)="resetGame()"
        (cancelled)="confirmingReset.set(false)"
      />
    }
  `,
  styles: `
    .stage {
      display: grid;
      overflow: hidden;
      width: 100vw;
      height: 100vh;
      grid-template-columns: 20% 55% 25%;
    }

    section {
      min-width: 0;
      min-height: 0;
    }

    .left {
      display: flex;
      flex-direction: column;
      gap: 1.5vh;
      padding: 2vh 1vw;
    }

    app-question-grid {
      min-height: 0;
      flex: 1;
    }

    .reveal {
      padding: 1.6vh 0.5vw;
      font-size: clamp(1.1rem, 1.7vw, 2.2rem);
    }

    .links {
      display: flex;
      justify-content: center;
      gap: 1.5vw;
    }

    .reset {
      border: 0;
      background: none;
      color: var(--champagne);
      cursor: pointer;
      font: inherit;
      font-size: 0.8rem;
      opacity: 0.6;
      text-decoration: underline;

      &:hover {
        opacity: 1;
      }
    }

    .center {
      margin: 2vh 0.5vw;
    }

    .right {
      margin: 2vh 1vw 2vh 0.5vw;
    }

    .toast {
      position: fixed;
      z-index: 30;
      bottom: 2vh;
      left: 50%;
      margin: 0;
      padding: 0.6rem 1.2rem;
      border-radius: 8px;
      background: var(--signal-red);
      transform: translateX(-50%);
    }
  `,
})
export class HostShell {
  protected readonly store = inject(HostStore);
  protected readonly confirmingReset = signal(false);

  protected async resetGame(): Promise<void> {
    this.confirmingReset.set(false);
    await this.store.resetGame();
  }
}
