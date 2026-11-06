import { InjectionToken, Signal } from '@angular/core';
import { ClientConfig, TeamOption } from './models';

/** The HTTP side of the server: what a device needs before, or besides, the hub. */
export interface GameApi {
  /** Null until the config has loaded. */
  readonly config: Signal<ClientConfig | null>;

  getTeams(): Promise<TeamOption[]>;
}

export const GAME_API = new InjectionToken<GameApi>('GAME_API');
