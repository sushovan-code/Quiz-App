import { Signal } from '@angular/core';
import { ServerEvents, ServerMethods } from './models';

export type ConnectionState = 'connecting' | 'connected' | 'reconnecting';

/** The live link to the server. Stores depend on this, never on SignalR itself. */
export abstract class GameConnection {
  abstract readonly state: Signal<ConnectionState>;

  /** Resolves once connected, retrying for as long as it takes. */
  abstract ready(): Promise<void>;

  abstract on<K extends keyof ServerEvents>(event: K, handler: ServerEvents[K]): void;

  abstract onReconnected(handler: () => void): void;

  abstract invoke<K extends keyof ServerMethods>(
    method: K,
    ...args: Parameters<ServerMethods[K]>
  ): Promise<ReturnType<ServerMethods[K]>>;
}

/** The server's message, without the wrapper SignalR adds to a HubException. */
export function errorMessage(error: unknown): string {
  const message = error instanceof Error ? error.message : String(error);
  return message.replace(/^.*\bon the server\. \w*Exception: /s, '');
}

export function isSessionExpired(error: unknown): boolean {
  return errorMessage(error).startsWith('Session expired');
}
