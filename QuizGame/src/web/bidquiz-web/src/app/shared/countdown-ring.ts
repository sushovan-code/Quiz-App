import { Component, computed, input } from '@angular/core';

const RADIUS = 54;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;
const URGENT_SECONDS = 10;

@Component({
  selector: 'app-countdown-ring',
  template: `
    <svg
      viewBox="0 0 120 120"
      [class.urgent]="urgent()"
      role="timer"
      [attr.aria-label]="seconds() + ' seconds left'"
    >
      <circle class="track" cx="60" cy="60" [attr.r]="radius" />
      <circle
        class="progress"
        cx="60"
        cy="60"
        [attr.r]="radius"
        [attr.stroke-dasharray]="circumference"
        [attr.stroke-dashoffset]="offset()"
      />
      <text x="60" y="60" text-anchor="middle" dominant-baseline="central">{{ seconds() }}</text>
    </svg>
  `,
  styles: `
    :host {
      display: block;
    }

    svg {
      display: block;
      width: 100%;
      height: 100%;
      overflow: visible;
    }

    circle {
      fill: none;
      stroke-width: 8;
    }

    .track {
      stroke: rgb(212 175 55 / 18%);
    }

    .progress {
      stroke: var(--gold);
      stroke-linecap: round;
      transform: rotate(-90deg);
      transform-origin: center;
      transition: stroke 200ms;
      filter: drop-shadow(0 0 3px rgb(212 175 55 / 60%));
    }

    text {
      fill: var(--champagne);
      font-family: var(--heading-font);
      font-size: 42px;
    }

    .urgent .progress {
      stroke: var(--signal-red);
      filter: drop-shadow(0 0 3px var(--signal-red));
    }

    .urgent text {
      fill: var(--signal-red);
    }
  `,
})
export class CountdownRing {
  readonly remainingMs = input.required<number>();
  readonly totalSeconds = input.required<number>();

  protected readonly radius = RADIUS;
  protected readonly circumference = CIRCUMFERENCE;
  protected readonly seconds = computed(() => Math.ceil(this.remainingMs() / 1000));
  protected readonly urgent = computed(() => this.seconds() < URGENT_SECONDS);
  protected readonly offset = computed(() => {
    const fraction = Math.min(1, this.remainingMs() / (this.totalSeconds() * 1000));
    return CIRCUMFERENCE * (1 - fraction);
  });
}
