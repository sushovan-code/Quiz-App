import { Injectable, signal } from '@angular/core';
import { ClientConfig, TeamOption } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  readonly config = signal<ClientConfig | null>(null);

  constructor() {
    void this.loadConfig();
  }

  async getTeams(): Promise<TeamOption[]> {
    const response = await fetch('/api/teams');
    return response.json();
  }

  private async loadConfig(): Promise<void> {
    const response = await fetch('/api/config');
    this.config.set(await response.json());
  }
}
