import { computed, signal } from '@angular/core';

const TICK_MS = 100;

/**
 * Counts down locally to a server deadline. The deadline is converted using the server's own
 * clock reading, so a phone with the wrong time still shows the right number of seconds.
 */
export class Countdown {
  readonly remainingMs = signal(0);
  readonly seconds = computed(() => Math.ceil(this.remainingMs() / 1000));

  private endsAtLocal = 0;
  private timer: ReturnType<typeof setInterval> | null = null;

  start(endsAtUtc: string, serverNowUtc: string): void {
    this.endsAtLocal = Date.now() + (Date.parse(endsAtUtc) - Date.parse(serverNowUtc));
    this.stopTimer();
    this.tick();
    this.timer = setInterval(() => this.tick(), TICK_MS);
  }

  clear(): void {
    this.stopTimer();
    this.remainingMs.set(0);
  }

  private tick(): void {
    const remaining = Math.max(0, this.endsAtLocal - Date.now());
    this.remainingMs.set(remaining);
    if (remaining === 0) {
      this.stopTimer();
    }
  }

  private stopTimer(): void {
    if (this.timer !== null) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }
}
