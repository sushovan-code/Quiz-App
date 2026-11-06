import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  template: `
    <div class="overlay">
      <div class="dialog" role="alertdialog" aria-modal="true" [attr.aria-label]="confirmLabel()">
        <p>{{ message() }}</p>
        <div class="actions">
          <button class="btn" (click)="cancelled.emit()">Cancel</button>
          <button class="btn danger" (click)="confirmed.emit()">{{ confirmLabel() }}</button>
        </div>
      </div>
    </div>
  `,
  styles: `
    .dialog {
      display: flex;
      width: min(32rem, 90vw);
      flex-direction: column;
      gap: 1.5rem;
      padding: 2rem;
      text-align: center;
    }

    p {
      margin: 0;
      font-size: 1.25rem;
    }

    .actions {
      display: flex;
      justify-content: center;
      gap: 1rem;
    }

    .danger {
      background: var(--signal-red);
    }
  `,
})
export class ConfirmDialog {
  readonly message = input.required<string>();
  readonly confirmLabel = input('Confirm');
  readonly confirmed = output();
  readonly cancelled = output();
}
