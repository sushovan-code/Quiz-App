import { HttpClient, httpResource } from '@angular/common/http';
import { Injectable, computed, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { GameApi } from './game-api';
import { ClientConfig, TeamOption } from './models';

const API_BASE = '/api/v1';

@Injectable()
export class HttpGameApi implements GameApi {
  private readonly http = inject(HttpClient);
  private readonly configResource = httpResource<ClientConfig>(() => `${API_BASE}/config`);

  readonly config = computed(() =>
    this.configResource.hasValue() ? this.configResource.value() : null,
  );

  getTeams(): Promise<TeamOption[]> {
    return firstValueFrom(this.http.get<TeamOption[]>(`${API_BASE}/teams`));
  }
}
