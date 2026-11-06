import { Injectable, inject, signal } from '@angular/core';
import { GameConnection, errorMessage } from '../../core/game-connection';
import { QuestionInput, ServerMethods, Setup, TeamInput } from '../../core/models';

@Injectable({ providedIn: 'root' })
export class SetupStore {
  private readonly hub = inject(GameConnection);

  readonly setup = signal<Setup | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    // The main host tab or another setup tab may change what this tab shows.
    const refresh = () => void this.refresh();
    this.hub.on('QuestionStarted', refresh);
    this.hub.on('GameReset', refresh);
    this.hub.on('QuestionBoardChanged', refresh);
    this.hub.on('RosterChanged', refresh);
  }

  load(): Promise<boolean> {
    return this.change(() => this.hub.invoke('GetSetup'));
  }

  addTeam(input: TeamInput): Promise<boolean> {
    return this.change(() => this.hub.invoke('AddTeam', input));
  }

  updateTeam(id: number, input: TeamInput): Promise<boolean> {
    return this.change(() => this.hub.invoke('UpdateTeam', id, input));
  }

  deleteTeam(id: number): Promise<boolean> {
    return this.change(() => this.hub.invoke('DeleteTeam', id));
  }

  addQuestion(input: QuestionInput): Promise<boolean> {
    return this.change(() => this.hub.invoke('AddQuestion', input));
  }

  updateQuestion(id: number, input: QuestionInput): Promise<boolean> {
    return this.change(() => this.hub.invoke('UpdateQuestion', id, input));
  }

  deleteQuestion(id: number): Promise<boolean> {
    return this.change(() => this.hub.invoke('DeleteQuestion', id));
  }

  setQuestionsPerGame(count: number): Promise<boolean> {
    return this.change(() => this.hub.invoke('SetQuestionsPerGame', count));
  }

  setTeamsPerGame(count: number): Promise<boolean> {
    return this.change(() => this.hub.invoke('SetTeamsPerGame', count));
  }

  drawQuestions(): Promise<boolean> {
    return this.change(() => this.hub.invoke('DrawQuestions'));
  }

  private async refresh(): Promise<void> {
    if (this.setup() !== null) {
      this.setup.set(await this.hub.invoke('GetSetup'));
    }
  }

  /** Every call returns the whole setup. Resolves to false if the server refused. */
  private async change(
    call: () => Promise<ReturnType<ServerMethods['GetSetup']>>,
  ): Promise<boolean> {
    this.error.set(null);
    try {
      this.setup.set(await call());
      return true;
    } catch (error) {
      this.error.set(errorMessage(error));
      return false;
    }
  }
}
