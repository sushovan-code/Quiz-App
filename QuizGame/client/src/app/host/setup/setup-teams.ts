import { Component, computed, inject, input, signal } from '@angular/core';
import { Setup, TeamSetup } from '../../core/models';
import { ConfirmDialog } from '../confirm-dialog';
import { SetupStore } from './setup.store';

@Component({
  selector: 'app-setup-teams',
  imports: [ConfirmDialog],
  template: `
    <section class="panel">
      <h2>Teams ({{ setup().teams.length }} of {{ setup().maxTeams }} on the roster)</h2>

      <div class="count">
        <label for="teams-per-game">Teams playing</label>
        <select
          id="teams-per-game"
          class="field"
          [disabled]="setup().locked"
          (change)="store.setTeamsPerGame(+$any($event.target).value)"
        >
          @for (option of countOptions(); track option) {
            <option [value]="option" [selected]="option === setup().teamsPerGame">{{ option }}</option>
          }
        </select>
      </div>
      <p>The first {{ setup().teamsPerGame }} teams below play. The rest sit out and can't log in.</p>

      <ul>
        @for (team of setup().teams; track team.id) {
          <li [class.benched]="!team.playing" [style.--team-color]="team.color">
            <span class="name">{{ team.name }}</span>
            <button class="link" [disabled]="setup().locked" (click)="edit(team)">Edit</button>
            <button class="link" [disabled]="setup().locked" (click)="deleting.set(team)">Delete</button>
          </li>
        }
      </ul>

      <form (submit)="$event.preventDefault(); save()">
        <h3>{{ editing() ? 'Edit ' + editing()?.name : 'Add a team' }}</h3>
        <div class="row">
          <input
            class="field"
            placeholder="Team name"
            aria-label="Team name"
            maxlength="40"
            [value]="name()"
            (input)="name.set($any($event.target).value)"
          />
          <input
            class="field"
            type="text"
            autocomplete="off"
            aria-label="Team password"
            [placeholder]="editing() ? 'New password (optional)' : 'Team password'"
            [value]="password()"
            (input)="password.set($any($event.target).value)"
          />
          <button class="btn" type="submit" [disabled]="!canSave()">{{ editing() ? 'Save' : 'Add team' }}</button>
          @if (editing()) {
            <button class="btn" type="button" (click)="clear()">Cancel</button>
          }
        </div>
      </form>
    </section>

    @if (deleting(); as team) {
      <app-confirm-dialog
        [message]="'Delete ' + team.name + '? Their devices will return to the login screen.'"
        confirmLabel="Delete team"
        (confirmed)="remove(team)"
        (cancelled)="deleting.set(null)"
      />
    }
  `,
  styles: `
    section {
      display: flex;
      flex-direction: column;
      gap: 0.9rem;
      padding: 1.25rem;
    }

    h2 {
      font-size: 1.6rem;
    }

    h3 {
      margin-bottom: 0.4rem;
      font-size: 1.1rem;
    }

    p {
      margin: 0;
    }

    .count {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .count .field {
      width: 5.5rem;
    }

    .benched {
      opacity: 0.45;
    }

    ul {
      display: grid;
      gap: 0.5rem;
      margin: 0;
      padding: 0;
      grid-template-columns: repeat(auto-fill, minmax(19rem, 1fr));
      list-style: none;
    }

    li {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.5rem 0.75rem;
      border-radius: 8px;
      background: var(--team-color);
    }

    .name {
      min-width: 0;
      flex: 1;
      overflow: hidden;
      font-weight: 700;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .link {
      border: 0;
      background: none;
      color: inherit;
      cursor: pointer;
      font: inherit;
      text-decoration: underline;

      &:disabled {
        cursor: not-allowed;
        opacity: 0.5;
      }
    }

    .row {
      display: flex;
      flex-wrap: wrap;
      gap: 0.6rem;
    }

    .row .field {
      width: auto;
      min-width: 12rem;
      flex: 1;
    }
  `,
})
export class SetupTeams {
  protected readonly store = inject(SetupStore);

  readonly setup = input.required<Setup>();

  protected readonly editing = signal<TeamSetup | null>(null);
  protected readonly deleting = signal<TeamSetup | null>(null);
  protected readonly name = signal('');
  protected readonly password = signal('');

  protected readonly countOptions = computed(() =>
    Array.from({ length: this.setup().teams.length }, (_, index) => index + 1),
  );

  protected readonly canSave = computed(() => {
    if (this.setup().locked || !this.name().trim()) {
      return false;
    }
    if (this.editing()) {
      return true;
    }
    return this.password().trim().length > 0 && this.setup().teams.length < this.setup().maxTeams;
  });

  protected edit(team: TeamSetup): void {
    this.editing.set(team);
    this.name.set(team.name);
    this.password.set('');
  }

  protected clear(): void {
    this.editing.set(null);
    this.name.set('');
    this.password.set('');
  }

  protected async save(): Promise<void> {
    const team = this.editing();
    const input = { name: this.name(), password: this.password() || null };
    const saved = await (team ? this.store.updateTeam(team.id, input) : this.store.addTeam(input));
    if (saved) {
      this.clear();
    }
  }

  protected async remove(team: TeamSetup): Promise<void> {
    this.deleting.set(null);
    await this.store.deleteTeam(team.id);
  }
}
