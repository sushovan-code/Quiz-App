import { Component, inject } from '@angular/core';
import { Confirmation } from './confirmation';
import { QuestionPopup } from './question-popup';
import { TeamLogin } from './team-login';
import { TeamStore } from './team.store';
import { Waiting } from './waiting';
import { WalletBar } from './wallet-bar';

@Component({
  selector: 'app-play-page',
  imports: [Confirmation, QuestionPopup, TeamLogin, Waiting, WalletBar],
  template: `
    <div class="surface" [class.themed]="store.me()" [style.--team-color]="store.me()?.color">
      @switch (store.view()) {
        @case ('restoring') {
          <div class="center-screen"><h2>Taking your seat…</h2></div>
        }
        @case ('login') {
          <app-team-login />
        }
        @default {
          <app-wallet-bar />
          @switch (store.view()) {
            @case ('waiting') {
              <app-waiting />
            }
            @case ('question') {
              <app-question-popup />
            }
            @case ('confirmed') {
              <app-confirmation />
            }
          }
        }
      }
    </div>
  `,
  styles: `
    .surface {
      min-height: 100dvh;
    }

    // Tells a team at a glance that they joined as the team they meant to.
    .themed {
      background: linear-gradient(var(--team-color) 55%, var(--theatre-black) 130%);
    }
  `,
})
export class PlayPage {
  protected readonly store = inject(TeamStore);
}
