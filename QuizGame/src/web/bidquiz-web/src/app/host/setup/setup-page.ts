import { Component, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HostLogin } from '../host-login';
import { HostStore } from '../host.store';
import { SetupQuestions } from './setup-questions';
import { SetupStore } from './setup.store';
import { SetupTeams } from './setup-teams';

/** A separate page, so the host screen never has to show the question bank. */
@Component({
  selector: 'app-setup-page',
  imports: [HostLogin, RouterLink, SetupQuestions, SetupTeams],
  template: `
    @if (host.restoring()) {
      <div class="center-screen"><h2>Dimming the lights…</h2></div>
    } @else if (!host.authenticated()) {
      <app-host-login />
    } @else if (store.setup(); as setup) {
      <main>
        <header>
          <a class="btn back" routerLink="/host">← Back to game</a>
          <h1>Show setup</h1>
        </header>
        @if (setup.locked) {
          <p class="locked">
            A game is in progress. Use <strong>Reset game</strong> on the host screen to change the
            setup.
          </p>
        }
        @if (store.error(); as error) {
          <p class="error">{{ error }}</p>
        }
        <app-setup-teams [setup]="setup" />
        <app-setup-questions [setup]="setup" />
      </main>
    }
  `,
  styles: `
    main {
      display: flex;
      width: min(68rem, 100%);
      flex-direction: column;
      gap: 1.25rem;
      margin: 0 auto;
      padding: 1.5rem 1rem 3rem;
    }

    header {
      display: grid;
      align-items: center;
      gap: 1rem;
      grid-template-columns: 1fr auto 1fr;
    }

    .back {
      justify-self: start;
      font-size: 1rem;
      text-decoration: none;
    }

    h1 {
      font-size: 2.6rem;
      text-align: center;
    }

    p {
      margin: 0;
      text-align: center;
    }

    .locked {
      padding: 0.6rem;
      border-radius: 8px;
      background: var(--signal-red);
    }
  `,
})
export class SetupPage {
  protected readonly host = inject(HostStore);
  protected readonly store = inject(SetupStore);

  constructor() {
    effect(() => {
      if (this.host.authenticated()) {
        void this.store.load();
      }
    });
  }
}
