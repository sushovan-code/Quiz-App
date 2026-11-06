import { Injectable, computed, inject, signal } from '@angular/core';
import { Countdown } from '../core/countdown';
import { GameConnection, errorMessage, isSessionExpired } from '../core/game-connection';
import {
  AdminSession,
  AdminSnapshot,
  BidView,
  GameStatus,
  Question,
  QuestionSummary,
  TeamScore,
  Verdict,
  sortTeams,
} from '../core/models';

const TOKEN_KEY = 'bidquiz.adminToken';

@Injectable({ providedIn: 'root' })
export class HostStore {
  private readonly hub = inject(GameConnection);

  readonly countdown = new Countdown();

  readonly authenticated = signal(false);
  readonly restoring = signal(localStorage.getItem(TOKEN_KEY) !== null);
  readonly status = signal<GameStatus>('Idle');
  readonly question = signal<Question | null>(null);
  readonly questions = signal<QuestionSummary[]>([]);
  readonly answered = signal(0);
  readonly teamCount = signal(0);
  readonly teams = signal<TeamScore[]>([]);
  readonly bids = signal<BidView[] | null>(null);
  readonly correctAnswer = signal<string | null>(null);
  readonly dialogOpen = signal(false);
  readonly answerVisible = signal(false);

  /** Lives here, not on the page, so returning from setup goes straight back to the game. */
  readonly showStarted = signal(false);
  readonly error = signal<string | null>(null);

  readonly leaderboard = computed(() => sortTeams(this.teams()));
  readonly canReveal = computed(() => this.status() !== 'Idle');
  readonly canStart = computed(() => this.status() !== 'Accepting');

  constructor() {
    this.hub.on('QuestionStarted', (question, endsAtUtc, serverNowUtc) => {
      this.status.set('Accepting');
      this.question.set(question);
      this.questions.update((all) =>
        all.map((q) => (q.id === question.id ? { ...q, asked: true } : q)),
      );
      this.closeDialog();
      this.countdown.start(endsAtUtc, serverNowUtc);
    });
    this.hub.on('AnswerCountChanged', (answered, total) => {
      this.answered.set(answered);
      this.teamCount.set(total);
    });
    this.hub.on('TimeUp', () => {
      this.status.set('Closed');
      this.countdown.clear();
    });
    this.hub.on('BidsRevealed', (bids) => {
      this.status.set('Revealed');
      this.bids.set(bids);
      this.dialogOpen.set(true);
    });
    this.hub.on('AnswerRevealed', (text) => this.correctAnswer.set(text));
    this.hub.on('ScoresUpdated', (teams) => this.teams.set(teams));
    this.hub.on('TeamPresenceChanged', (teamId, online) =>
      this.teams.update((teams) => teams.map((t) => (t.id === teamId ? { ...t, online } : t))),
    );
    this.hub.on('QuestionBoardChanged', (questions) => this.questions.set(questions));
    this.hub.on('GameReset', () => {
      this.status.set('Idle');
      this.question.set(null);
      this.answered.set(0);
      this.closeDialog();
      this.countdown.clear();
    });
    this.hub.onReconnected(() => void this.resume());

    void this.resume();
  }

  async login(password: string): Promise<void> {
    this.error.set(null);
    try {
      this.applySession(await this.hub.invoke('AdminLogin', password));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  startQuestion(id: number): Promise<void> {
    return this.run(() => this.hub.invoke('StartQuestion', id));
  }

  /** Also reopens the popup if it was closed: the server simply resends the bids. */
  revealBids(): Promise<void> {
    return this.run(() => this.hub.invoke('RevealBids'));
  }

  /** The server only sends the answer on the first reveal. After that the eye just shows or hides it. */
  async toggleAnswer(): Promise<void> {
    if (this.answerVisible()) {
      this.answerVisible.set(false);
      return;
    }
    if (this.correctAnswer() === null) {
      await this.run(() => this.hub.invoke('RevealAnswer'));
    }
    this.answerVisible.set(this.correctAnswer() !== null);
  }

  judge(bid: BidView, verdict: Verdict): Promise<void> {
    return this.run(async () => {
      const judged = await this.hub.invoke('Judge', bid.id, verdict);
      this.bids.update((bids) => bids?.map((b) => (b.id === judged.id ? judged : b)) ?? null);
    });
  }

  resetGame(): Promise<void> {
    return this.run(() => this.hub.invoke('ResetGame'));
  }

  /** Logs the host out. The game carries on untouched, so logging in again resumes the show. */
  logout(): void {
    const token = localStorage.getItem(TOKEN_KEY);
    localStorage.removeItem(TOKEN_KEY);
    this.authenticated.set(false);
    this.showStarted.set(false);
    this.error.set(null);
    this.closeDialog();
    this.countdown.clear();

    // Best effort: if the connection is down, the token is already gone from this browser,
    // and the server forgets it on restart.
    if (token !== null) {
      void this.hub.invoke('AdminLogout', token).catch(() => undefined);
    }
  }

  private async run(action: () => Promise<unknown>): Promise<void> {
    this.error.set(null);
    try {
      await action();
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  private closeDialog(): void {
    this.bids.set(null);
    this.correctAnswer.set(null);
    this.dialogOpen.set(false);
    this.answerVisible.set(false);
  }

  private async resume(): Promise<void> {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token === null) {
      this.restoring.set(false);
      return;
    }

    try {
      this.applySession(await this.hub.invoke('ResumeAdmin', token));
    } catch (error) {
      if (isSessionExpired(error)) {
        localStorage.removeItem(TOKEN_KEY);
        this.authenticated.set(false);
      }
    } finally {
      this.restoring.set(false);
    }
  }

  private applySession(session: AdminSession): void {
    localStorage.setItem(TOKEN_KEY, session.token);
    this.applySnapshot(session.snapshot);
    this.authenticated.set(true);
  }

  private applySnapshot(snapshot: AdminSnapshot): void {
    if (!this.authenticated()) {
      this.dialogOpen.set(snapshot.bids !== null);
    }
    this.status.set(snapshot.status);
    this.question.set(snapshot.question);
    this.questions.set(snapshot.questions);
    this.answered.set(snapshot.answeredCount);
    this.teamCount.set(snapshot.teamCount);
    this.teams.set(snapshot.teams);
    this.bids.set(snapshot.bids);
    this.correctAnswer.set(snapshot.correctAnswer);

    if (snapshot.endsAtUtc) {
      this.countdown.start(snapshot.endsAtUtc, snapshot.serverNowUtc);
    } else {
      this.countdown.clear();
    }
  }
}
