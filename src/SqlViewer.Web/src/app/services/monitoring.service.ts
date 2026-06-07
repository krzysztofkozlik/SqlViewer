import { Injectable, OnDestroy, effect, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { ErrorService } from './error.service';
import { SettingsService } from './settings.service';
import { environment } from '../../environments/environment';
import { SqlCommandEvent } from '../models/sql-command-event.model';
import { RequestGroup } from '../models/request-group.model';

export type ConnectionState = 'Connected' | 'Reconnecting' | 'Disconnected';

@Injectable({ providedIn: 'root' })
export class MonitoringService implements OnDestroy {
  private readonly errors = inject(ErrorService);
  private readonly settingsService = inject(SettingsService);
  private readonly hub: HubConnection;
  private readonly groupMap = new Map<string, RequestGroup>();
  private readonly spanIdOrder: string[] = [];
  private totalRequestsSeen = 0;

  readonly connectionState = signal<ConnectionState>('Disconnected');
  readonly sessionState = signal<string>('Stopped');
  readonly requestGroups = signal<RequestGroup[]>([]);
  readonly displayedCount = signal<number>(0);
  readonly totalCount = signal<number>(0);

  constructor(private http: HttpClient) {
    effect(() => {
      const limit = this.settingsService.settings().displayLimit;
      const excess = this.spanIdOrder.length - limit;
      if (excess <= 0) return;

      const removed = this.spanIdOrder.splice(0, excess);
      removed.forEach(id => this.groupMap.delete(id));

      this.requestGroups.set(
        [...this.spanIdOrder].reverse().map(id => this.groupMap.get(id)!)
      );
      this.displayedCount.set(this.spanIdOrder.length);
    });

    this.hub = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hub/sql`)
      .withAutomaticReconnect()
      .build();

    this.hub.on('ReceiveCommand', (event: SqlCommandEvent) => this.addCommand(event));
    this.hub.on('SessionStateChanged', (state: string) => this.sessionState.set(state));

    this.hub.onreconnecting(() => this.connectionState.set('Reconnecting'));

    this.hub.onreconnected(() => {
      this.connectionState.set('Connected');
      this.fetchState();
    });

    this.hub.onclose(() => {
      this.connectionState.set('Disconnected');
      this.errors.show('Connection to backend lost. Use the Reconnect button to retry.');
    });

    this.hub.start()
      .then(() => {
        this.connectionState.set('Connected');
        this.fetchState();
      })
      .catch(() => this.connectionState.set('Disconnected'));
  }

  reconnect(): void {
    this.connectionState.set('Reconnecting');
    this.hub.start()
      .then(() => {
        this.connectionState.set('Connected');
        this.fetchState();
      })
      .catch(() => {
        this.connectionState.set('Disconnected');
        this.errors.show('Reconnect failed. Is the SQL Viewer API running?');
      });
  }

  start(): void {
    this.http.post(`${environment.apiUrl}/api/session/start`, {})
      .subscribe({ error: (e: HttpErrorResponse) => this.errors.show(this.toMessage('Start', e)) });
  }

  pause(): void {
    this.http.post(`${environment.apiUrl}/api/session/pause`, {})
      .subscribe({ error: (e: HttpErrorResponse) => this.errors.show(this.toMessage('Pause', e)) });
  }

  stop(): void {
    this.http.post(`${environment.apiUrl}/api/session/stop`, {})
      .subscribe({ error: (e: HttpErrorResponse) => this.errors.show(this.toMessage('Stop', e)) });
  }

  private toMessage(action: string, err: HttpErrorResponse): string {
    if (err.status === 0)
      return `${action} failed: cannot reach the backend. Is the SQL Viewer API running?`;
    const detail = err.error?.message ?? err.message;
    return `${action} failed (${err.status}): ${detail}`;
  }

  clear(): void {
    this.groupMap.clear();
    this.spanIdOrder.length = 0;
    this.totalRequestsSeen = 0;
    this.requestGroups.set([]);
    this.displayedCount.set(0);
    this.totalCount.set(0);
  }

  private fetchState(): void {
    this.http
      .get<{ state: string }>(`${environment.apiUrl}/api/session`)
      .subscribe({ next: res => this.sessionState.set(res.state) });
  }

  private addCommand(event: SqlCommandEvent): void {
    let group = this.groupMap.get(event.spanId);

    if (!group) {
      this.totalRequestsSeen++;

      if (this.spanIdOrder.length >= this.settingsService.settings().displayLimit) {
        const oldestId = this.spanIdOrder.shift()!;
        this.groupMap.delete(oldestId);
      }

      group = {
        spanId: event.spanId,
        traceId: event.traceId,
        url: event.url,
        commands: [],
        totalDurationUs: 0,
        capturedAt: event.capturedAt,
      };

      this.groupMap.set(event.spanId, group);
      this.spanIdOrder.push(event.spanId);
    }

    // Replace with a new object so Angular's signal input detects the change
    // and recomputes derived values (totalDurationMs, hasLongRunning, etc.).
    const updated = {
      ...group,
      commands: [...group.commands, event],
      totalDurationUs: group.totalDurationUs + event.durationUs,
    };
    this.groupMap.set(event.spanId, updated);

    // Newest first
    this.requestGroups.set(
      [...this.spanIdOrder].reverse().map(id => this.groupMap.get(id)!)
    );
    this.displayedCount.set(this.spanIdOrder.length);
    this.totalCount.set(this.totalRequestsSeen);
  }

  ngOnDestroy(): void {
    this.hub.stop();
  }
}
