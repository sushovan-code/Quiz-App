import { Component, computed, inject, input, signal } from '@angular/core';
import { QuestionSetup, Setup } from '../../core/models';
import { ConfirmDialog } from '../confirm-dialog';
import { SetupStore } from './setup.store';

@Component({
  selector: 'app-setup-questions',
  imports: [ConfirmDialog],
  template: `
    <section class="panel">
      <h2>Questions ({{ setup().questions.length }} in the bank)</h2>

      <div class="count">
        <label for="per-game">Questions per game</label>
        <select
          id="per-game"
          class="field"
          [disabled]="setup().locked"
          (change)="store.setQuestionsPerGame(+$any($event.target).value)"
        >
          @for (option of countOptions(); track option) {
            <option [value]="option" [selected]="option === setup().questionsPerGame">{{ option }}</option>
          }
        </select>
        <button class="btn" [disabled]="setup().locked" (click)="store.drawQuestions()">Draw new questions</button>
      </div>
      <p>Questions are picked at random from the bank, with no repeats. Resetting the game draws a new set.</p>

      <form (submit)="$event.preventDefault(); save()">
        <h3>{{ editing() ? 'Edit question' : 'Add a question' }}</h3>
        <div class="fields">
          <input
            class="field category"
            list="categories"
            placeholder="Category"
            aria-label="Category"
            maxlength="40"
            [value]="category()"
            (input)="category.set($any($event.target).value)"
          />
          <datalist id="categories">
            <option value="Hollywood"></option>
            <option value="Bollywood"></option>
          </datalist>
          <input
            class="field text"
            placeholder="Question"
            aria-label="Question"
            maxlength="500"
            [value]="text()"
            (input)="text.set($any($event.target).value)"
          />
          <input
            class="field"
            placeholder="Correct answer"
            aria-label="Correct answer"
            maxlength="500"
            [value]="correctAnswer()"
            (input)="correctAnswer.set($any($event.target).value)"
          />
          <input
            class="field"
            placeholder="Image URL (optional)"
            aria-label="Image URL"
            [value]="imageUrl()"
            (input)="imageUrl.set($any($event.target).value)"
          />
          <button class="btn" type="submit" [disabled]="!canSave()">{{ editing() ? 'Save' : 'Add question' }}</button>
          @if (editing()) {
            <button class="btn" type="button" (click)="clear()">Cancel</button>
          }
        </div>
      </form>

      <label class="reveal">
        <input type="checkbox" [checked]="answersShown()" (change)="answersShown.set(!answersShown())" />
        Show correct answers
      </label>

      <table>
        <thead>
          <tr>
            <th>In game</th>
            <th>Category</th>
            <th>Question</th>
            <th>Answer</th>
            <th><span class="hidden">Actions</span></th>
          </tr>
        </thead>
        <tbody>
          @for (question of sorted(); track question.id) {
            <tr [class.playing]="question.order !== null">
              <td>{{ question.order ?? '' }}</td>
              <td>{{ question.category }}</td>
              <td>{{ question.text }}</td>
              <td>{{ answersShown() ? question.correctAnswer : '•••••' }}</td>
              <td class="actions">
                <button class="link" [disabled]="setup().locked" (click)="edit(question)">Edit</button>
                <button class="link" [disabled]="setup().locked" (click)="deleting.set(question)">Delete</button>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </section>

    @if (deleting(); as question) {
      <app-confirm-dialog
        [message]="'Delete this question from the bank? ' + question.text"
        confirmLabel="Delete question"
        (confirmed)="remove(question)"
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
      flex-wrap: wrap;
      align-items: center;
      gap: 0.75rem;
    }

    .count .field {
      width: 5.5rem;
    }

    .fields {
      display: flex;
      flex-wrap: wrap;
      gap: 0.6rem;
    }

    .fields .field {
      width: auto;
      min-width: 12rem;
      flex: 1;
    }

    .fields .text {
      min-width: 100%;
    }

    .reveal {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    table {
      width: 100%;
      border-collapse: collapse;
    }

    th {
      color: var(--champagne);
      font-weight: 400;
      text-align: left;
    }

    th,
    td {
      padding: 0.4rem 0.5rem;
      border-bottom: 1px solid rgb(212 175 55 / 25%);
      vertical-align: top;
    }

    .playing {
      background: rgb(212 175 55 / 14%);
    }

    .actions {
      white-space: nowrap;
    }

    .hidden {
      position: absolute;
      overflow: hidden;
      width: 1px;
      height: 1px;
      clip-path: inset(50%);
    }

    .link {
      border: 0;
      background: none;
      color: var(--champagne);
      cursor: pointer;
      font: inherit;
      text-decoration: underline;

      &:disabled {
        cursor: not-allowed;
        opacity: 0.5;
      }
    }
  `,
})
export class SetupQuestions {
  protected readonly store = inject(SetupStore);

  readonly setup = input.required<Setup>();

  protected readonly editing = signal<QuestionSetup | null>(null);
  protected readonly deleting = signal<QuestionSetup | null>(null);
  protected readonly category = signal('');
  protected readonly text = signal('');
  protected readonly correctAnswer = signal('');
  protected readonly imageUrl = signal('');

  /** Off by default, in case this page is on the projector. */
  protected readonly answersShown = signal(false);

  protected readonly countOptions = computed(() => {
    const { minQuestionsPerGame: min, maxQuestionsPerGame: max } = this.setup();
    return Array.from({ length: max - min + 1 }, (_, index) => min + index);
  });

  /** The questions in play first, in the order they will appear. */
  protected readonly sorted = computed(() =>
    [...this.setup().questions].sort((a, b) => (a.order ?? Infinity) - (b.order ?? Infinity)),
  );

  protected readonly canSave = computed(
    () =>
      !this.setup().locked &&
      this.category().trim().length > 0 &&
      this.text().trim().length > 0 &&
      this.correctAnswer().trim().length > 0,
  );

  protected edit(question: QuestionSetup): void {
    this.editing.set(question);
    this.category.set(question.category);
    this.text.set(question.text);
    this.correctAnswer.set(question.correctAnswer);
    this.imageUrl.set(question.imageUrl ?? '');
  }

  protected clear(): void {
    this.editing.set(null);
    this.category.set('');
    this.text.set('');
    this.correctAnswer.set('');
    this.imageUrl.set('');
  }

  protected async save(): Promise<void> {
    const question = this.editing();
    const input = {
      category: this.category(),
      text: this.text(),
      correctAnswer: this.correctAnswer(),
      imageUrl: this.imageUrl() || null,
    };
    const saved = await (question ? this.store.updateQuestion(question.id, input) : this.store.addQuestion(input));
    if (saved) {
      this.clear();
    }
  }

  protected async remove(question: QuestionSetup): Promise<void> {
    this.deleting.set(null);
    await this.store.deleteQuestion(question.id);
  }
}
