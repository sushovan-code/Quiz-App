import { provideHttpClient, withFetch } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { GAME_API } from './core/game-api';
import { GameConnection } from './core/game-connection';
import { GameHubService } from './core/game-hub.service';
import { HttpGameApi } from './core/http-game-api';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    { provide: GAME_API, useClass: HttpGameApi },
    { provide: GameConnection, useClass: GameHubService },
  ],
};
