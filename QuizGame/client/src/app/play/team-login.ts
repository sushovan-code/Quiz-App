import { Component, inject, signal } from '@angular/core';
import { TeamStore } from './team.store';

@Component({
  selector: 'app-team-login',
  template: `
    <div class="center-screen">
      <h1>Bid Quiz</h1>
      <form class="panel" (submit)="$event.preventDefault(); login()">
        <label for="team">Your team</label>
        <select id="team" class="field" (change)="teamId.set(+$any($event.target).value)">
          <option value="0" [selected]="teamId() === 0" disabled>Choose a team…</option>
          @for (team of store.teamOptions(); track team.id) {
            <option [value]="team.id" [selected]="team.id === teamId()">{{ team.name }}</option>
          }
        </select>

        <label for="password">Team password</label>
        <input
          id="password"
          class="field"
          type="password"
          autocomplete="off"
          [value]="password()"
          (input)="password.set($any($event.target).value)"
        />

        <button class="btn" type="submit" [disabled]="!teamId() || !password()">Join the show</button>
        @if (store.error(); as error) {
          <p class="error">{{ error }}</p>
        }
      </form>
    </div>
  `,
  styles: `
    h1 {
      font-size: 3rem;
    }

    form {
      display: flex;
      width: min(24rem, 100%);
      flex-direction: column;
      gap: 0.7rem;
      padding: 1.25rem;
      text-align: left;
    }

    .btn {
      margin-top: 0.5rem;
    }

    p {
      margin: 0;
    }
  `,
})
export class TeamLogin {
  protected readonly store = inject(TeamStore);
  protected readonly teamId = signal(0);
  protected readonly password = signal('');

  protected login(): void {
    void this.store.login(this.teamId(), this.password());
  }
}
