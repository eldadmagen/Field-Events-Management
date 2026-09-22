import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { EventSummary } from '../models/event.model';
import { AuthService } from './auth.service';

const CLIENTS_HUB_PATH = '/hubs/clients';
const NEW_EVENT_METHOD = 'NewEventReceived';
const TECHNICIAN_PRESENCE_METHOD = 'TechnicianPresenceChanged';

/**
 * Owns the live connection to ClientsHub - the "user is online" real-time path from the
 * architecture doc. Token is passed as a query string param because browsers cannot set
 * custom headers on a WebSocket handshake (standard SignalR JS pattern).
 */
@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection?: signalR.HubConnection;

  readonly events = signal<EventSummary[]>([]);
  readonly connectionState = signal<signalR.HubConnectionState>(signalR.HubConnectionState.Disconnected);
  readonly technicianPresence = signal<Record<number, boolean>>({});

  constructor(private readonly auth: AuthService) {}

  connect(): void {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hubBaseUrl}${CLIENTS_HUB_PATH}`, {
        accessTokenFactory: () => this.auth.token ?? ''
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on(NEW_EVENT_METHOD, (evt: EventSummary) => {
      this.events.update((current) => {
        const index = current.findIndex((e) => e.id === evt.id);
        if (index === -1) return [evt, ...current];
        const next = [...current];
        next[index] = evt;
        return next;
      });
    });

    this.connection.on(TECHNICIAN_PRESENCE_METHOD, (payload: { technicianId: number; isOnline: boolean }) => {
      this.technicianPresence.update((current) => ({ ...current, [payload.technicianId]: payload.isOnline }));
    });

    this.connection.onreconnected(() => this.connectionState.set(signalR.HubConnectionState.Connected));
    this.connection.onreconnecting(() => this.connectionState.set(signalR.HubConnectionState.Reconnecting));
    this.connection.onclose(() => this.connectionState.set(signalR.HubConnectionState.Disconnected));

    this.connection
      .start()
      .then(() => this.connectionState.set(signalR.HubConnectionState.Connected))
      .catch((err) => console.error('SignalR connection failed', err));
  }

  seedEvents(initial: EventSummary[]): void {
    this.events.set(initial);
  }

  seedTechnicianPresence(initial: Record<number, boolean>): void {
    this.technicianPresence.update((current) => ({ ...initial, ...current }));
  }

  disconnect(): void {
    void this.connection?.stop();
    this.connection = undefined;
    this.connectionState.set(signalR.HubConnectionState.Disconnected);
  }
}
