import { Component, computed, inject } from '@angular/core';
import { HostStore } from './host.store';

@Component({
  selector: 'app-question-grid',
  template: `
    <div class="grid" [style.--columns]="columns()">
      @for (q of store.questions(); track q.id) {
        <button
          class="circle"
          [class.active]="q.id === activeId()"
          [class.asked]="q.asked && q.id !== activeId()"
          [disabled]="q.asked || !store.canStart()"
          [attr.aria-label]="'Question ' + q.order"
          (click)="store.startQuestion(q.id)"
        >
          {{ q.order }}
        </button>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
      overflow: hidden auto;
      margin: -18px;
      padding: 18px;
      scrollbar-width: none;
    }

    .grid {
      display: grid;
      gap: 0.8vw;
      grid-template-columns: repeat(var(--columns), 1fr);
    }

    .circle {
      aspect-ratio: 1;
      padding: 0;
      border: 2px solid var(--gold);
      border-radius: 50%;
      background: radial-gradient(circle at 30% 30%, var(--curtain-red), var(--oxblood));
      color: var(--ivory);
      cursor: pointer;
      font-family: var(--heading-font);
      font-size: clamp(1rem, 1.5vw, 2rem);
      transition:
        box-shadow 150ms,
        transform 150ms;

      &:hover:not(:disabled) {
        box-shadow: var(--gold-glow);
        transform: scale(1.06);
      }

      &:disabled {
        cursor: not-allowed;
      }

      &.active {
        background: radial-gradient(var(--champagne), var(--gold) 60%, #8a6d1e);
        box-shadow: 0 0 20px rgb(243 217 139 / 70%);
        color: var(--theatre-black);
      }

      &.asked {
        border-color: #6b6460;
        background: #3a3533;
        color: #8f8884;
      }
    }
  `,
})
export class QuestionGrid {
  protected readonly store = inject(HostStore);
  protected readonly activeId = computed(() => this.store.question()?.id ?? null);

  /** Keeps up to 25 circles on screen without scrolling. */
  protected readonly columns = computed(() => {
    const count = this.store.questions().length;
    return count <= 12 ? 3 : count <= 20 ? 4 : 5;
  });
}
