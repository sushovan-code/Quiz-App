import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { ConnectionState, GameConnection } from './game-connection';
import { ServerEvents, ServerMethods } from './models';

const RETRY_MS = 2000;
const MAX_BACKOFF_MS = 5000;

@Injectable()
export class GameHubService extends GameConnection {
  readonly state = signal<ConnectionState>('connecting');

  private readonly connection: HubConnection = new HubConnectionBuilder()
    .withUrl('/hubs/game')
    .withAutomaticReconnect({
      // Never give up: a phone can be out of Wi-Fi range for a whole round.
      nextRetryDelayInMilliseconds: (ctx) =>
        Math.min(1000 * 2 ** ctx.previousRetryCount, MAX_BACKOFF_MS),
    })
    .configureLogging(LogLevel.Warning)
    .build();

  private readonly reconnectHandlers: (() => void)[] = [];
  private starting: Promise<void> | null = null;

  constructor() {
    super();
    this.connection.onreconnecting(() => this.state.set('reconnecting'));
    this.connection.onreconnected(() => this.notifyConnected());
    this.connection.onclose(() => {
      this.state.set('reconnecting');
      this.starting = null;
      void this.ready().then(() => this.notifyConnected());
    });
  }

  ready(): Promise<void> {
    return (this.starting ??= this.startWithRetry());
  }

  on<K extends keyof ServerEvents>(event: K, handler: ServerEvents[K]): void {
    this.connection.on(event, handler);
  }

  onReconnected(handler: () => void): void {
    this.reconnectHandlers.push(handler);
  }

  async invoke<K extends keyof ServerMethods>(
    method: K,
    ...args: Parameters<ServerMethods[K]>
  ): Promise<ReturnType<ServerMethods[K]>> {
    await this.ready();
    return this.connection.invoke<ReturnType<ServerMethods[K]>>(method, ...args);
  }

  private async startWithRetry(): Promise<void> {
    for (;;) {
      try {
        await this.connection.start();
        this.state.set('connected');
        return;
      } catch {
        await new Promise((resolve) => setTimeout(resolve, RETRY_MS));
      }
    }
  }

  private notifyConnected(): void {
    this.state.set('connected');
    this.reconnectHandlers.forEach((handler) => handler());
  }
}
