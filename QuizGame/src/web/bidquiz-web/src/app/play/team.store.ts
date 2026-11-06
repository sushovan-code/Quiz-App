import { Injectable, computed, inject, signal } from '@angular/core';
import { GAME_API } from '../core/game-api';
import { Countdown } from '../core/countdown';
import { GameConnection, errorMessage, isSessionExpired } from '../core/game-connection';
import { MyBid, Question, TeamOption, TeamScore, TeamSession, TeamSnapshot } from '../core/models';

const TOKEN_KEY = 'bidquiz.teamToken';

export type PlayView = 'restoring' | 'login' | 'waiting' | 'question' | 'confirmed';

@Injectable({ providedIn: 'root' })
export class TeamStore {
  private readonly hub = inject(GameConnection);
  private readonly api = inject(GAME_API);

  readonly countdown = new Countdown();

  readonly restoring = signal(localStorage.getItem(TOKEN_KEY) !== null);
  readonly teamId = signal<number | null>(null);
  readonly teamOptions = signal<TeamOption[]>([]);
  readonly teams = signal<TeamScore[]>([]);
  readonly question = signal<Question | null>(null);
  readonly myBid = signal<MyBid | null>(null);
  readonly editing = signal(false);
  readonly notice = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly submitting = signal(false);

  readonly me = computed(() => this.teams().find((t) => t.id === this.teamId()) ?? null);
  readonly outOfGame = computed(() => this.me()?.canBid === false);

  /** Bids this team can afford. The server enforces the same rule. */
  readonly bidOptions = computed(() => {
    const config = this.api.config();
    const balance = this.me()?.bidBalance ?? 0;
    if (!config) {
      return [];
    }

    const options: number[] = [];
    for (let bid = config.bidStep; bid <= balance; bid += config.bidStep) {
      options.push(bid);
    }
    return options;
  });

  readonly view = computed<PlayView>(() => {
    if (this.restoring()) {
      return 'restoring';
    }
    if (this.teamId() === null) {
      return 'login';
    }
    if (this.question() === null || this.outOfGame()) {
      return 'waiting';
    }
    return this.myBid() !== null && !this.editing() ? 'confirmed' : 'question';
  });

  constructor() {
    this.hub.on('QuestionStarted', (question, endsAtUtc, serverNowUtc) => {
      this.notice.set(null);
      this.error.set(null);
      this.myBid.set(null);
      this.editing.set(false);
      this.question.set(question);
      this.countdown.start(endsAtUtc, serverNowUtc);
    });
    this.hub.on('TimeUp', () => {
      if (this.question() !== null) {
        this.notice.set(this.myBid() ? 'Bids are closed. Good luck!' : 'Time’s up!');
      }
      this.closeQuestion();
    });
    this.hub.on('ScoresUpdated', (teams) => {
      this.teams.set(teams);
      if (this.teamId() !== null && !teams.some((t) => t.id === this.teamId())) {
        this.logout();
      }
    });
    this.hub.on('RosterChanged', () => void this.loadTeamOptions());
    this.hub.on('TeamPresenceChanged', (teamId, online) =>
      this.teams.update((teams) => teams.map((t) => (t.id === teamId ? { ...t, online } : t))),
    );
    this.hub.on('GameReset', () => {
      this.notice.set(null);
      this.closeQuestion();
    });
    this.hub.onReconnected(() => void this.resume());

    void this.resume();
    void this.loadTeamOptions();
  }

  async login(teamId: number, password: string): Promise<void> {
    this.error.set(null);
    try {
      this.applySession(await this.hub.invoke('JoinTeam', teamId, password));
    } catch (error) {
      this.error.set(errorMessage(error));
    }
  }

  async submitBid(answer: string, amount: number): Promise<void> {
    this.error.set(null);
    this.submitting.set(true);
    try {
      this.myBid.set(await this.hub.invoke('SubmitBid', answer, amount));
      this.editing.set(false);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.submitting.set(false);
    }
  }

  /** Logs this device out, by choice or because the host removed the team from the game. */
  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.notice.set(null);
    this.error.set(null);
    this.closeQuestion();
    this.teamId.set(null);
    this.teams.set([]);

    // If the connection is down, the server has already marked this device offline.
    void this.hub.invoke('LeaveTeam').catch(() => undefined);
  }

  private closeQuestion(): void {
    this.question.set(null);
    this.myBid.set(null);
    this.editing.set(false);
    this.countdown.clear();
  }

  private async loadTeamOptions(): Promise<void> {
    await this.hub.ready();
    this.teamOptions.set(await this.api.getTeams());
  }

  private async resume(): Promise<void> {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token === null) {
      this.restoring.set(false);
      return;
    }

    try {
      this.applySession(await this.hub.invoke('ResumeTeam', token));
    } catch (error) {
      if (isSessionExpired(error)) {
        this.logout();
      }
    } finally {
      this.restoring.set(false);
    }
  }

  private applySession(session: TeamSession): void {
    localStorage.setItem(TOKEN_KEY, session.token);
    this.applySnapshot(session.snapshot);
  }

  private applySnapshot(snapshot: TeamSnapshot): void {
    this.teamId.set(snapshot.teamId);
    this.teams.set(snapshot.teams);
    this.question.set(snapshot.question);
    this.myBid.set(snapshot.myBid);

    if (snapshot.endsAtUtc) {
      this.countdown.start(snapshot.endsAtUtc, snapshot.serverNowUtc);
    } else {
      this.countdown.clear();
    }
  }
}
