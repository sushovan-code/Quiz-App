import { Component, inject, signal } from '@angular/core';
import { HostStore } from './host.store';

@Component({
  selector: 'app-host-login',
  template: `
    <div class="center-screen">
      <h1>Bid Quiz</h1>
      <form class="panel" (submit)="$event.preventDefault(); store.login(password())">
        <label for="admin-password">Admin password</label>
        <!-- The host laptop shows only this field, so focusing it saves a click before the show. -->
        <!-- eslint-disable @angular-eslint/template/no-autofocus -->
        <input
          #adminPasswordField
          id="admin-password"
          class="field"
          type="password"
          autocomplete="current-password"
          autofocus
          [value]="password()"
          (input)="password.set(adminPasswordField.value)"
        />
        <!-- eslint-enable @angular-eslint/template/no-autofocus -->
        <button class="btn" type="submit" [disabled]="!password()">Log in</button>
        @if (store.error(); as error) {
          <p class="error">{{ error }}</p>
        }
      </form>
    </div>
  `,
  styles: `
    h1 {
      font-size: 3.5rem;
    }

    form {
      display: flex;
      width: min(24rem, 90vw);
      flex-direction: column;
      gap: 0.9rem;
      padding: 1.5rem;
      text-align: left;
    }

    p {
      margin: 0;
    }
  `,
})
export class HostLogin {
  protected readonly store = inject(HostStore);
  protected readonly password = signal('');
}
