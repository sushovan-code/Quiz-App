import { Component, DestroyRef, effect, inject, input, signal, untracked } from '@angular/core';
import { DecimalPipe } from '@angular/common';

const DURATION_MS = 700;
const FLASH_MS = 900;

/** Counts up or down to its value, with a gold flash on each change. */
@Component({
  selector: 'app-animated-number',
  imports: [DecimalPipe],
  template: `<span [class.flash]="flashing()">{{ shown() | number: '1.0-0' }}</span>`,
  styles: `
    span {
      display: inline-block;
      border-radius: 4px;
    }

    .flash {
      animation: gold-flash ${FLASH_MS}ms ease-out;
    }

    @keyframes gold-flash {
      0% {
        background: var(--gold);
        color: var(--theatre-black);
        box-shadow: 0 0 14px var(--gold);
      }

      100% {
        background: transparent;
      }
    }
  `,
})
export class AnimatedNumber {
  readonly value = input.required<number>();

  protected readonly shown = signal(0);
  protected readonly flashing = signal(false);

  private initialized = false;
  private frame = 0;
  private flashTimer: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    effect(() => {
      const target = this.value();
      untracked(() => this.animateTo(target));
    });

    inject(DestroyRef).onDestroy(() => {
      cancelAnimationFrame(this.frame);
      clearTimeout(this.flashTimer);
    });
  }

  private animateTo(target: number): void {
    if (!this.initialized) {
      this.initialized = true;
      this.shown.set(target);
      return;
    }

    const from = this.shown();
    const startedAt = performance.now();
    cancelAnimationFrame(this.frame);

    const step = (now: number) => {
      const progress = Math.min(1, (now - startedAt) / DURATION_MS);
      this.shown.set(Math.round(from + (target - from) * progress));
      if (progress < 1) {
        this.frame = requestAnimationFrame(step);
      }
    };
    this.frame = requestAnimationFrame(step);

    this.flashing.set(true);
    clearTimeout(this.flashTimer);
    this.flashTimer = setTimeout(() => this.flashing.set(false), FLASH_MS);
  }
}
