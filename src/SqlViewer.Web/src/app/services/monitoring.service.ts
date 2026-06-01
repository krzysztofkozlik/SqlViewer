import { Injectable, OnDestroy, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { SqlCommandEvent } from '../models/sql-command-event.model';
import { RequestGroup } from '../models/request-group.model';

@Injectable({ providedIn: 'root' })
export class MonitoringService implements OnDestroy {
  private readonly hub: HubConnection;
  private readonly groupMap = new Map<string, RequestGroup>();
  private readonly traceIdOrder: string[] = [];
  private totalRequestsSeen = 0;

  readonly sessionState = signal<string>('Stopped');
  readonly requestGroups = signal<RequestGroup[]>([]);
  readonly displayedCount = signal<number>(0);
  readonly totalCount = signal<number>(0);

  constructor(private http: HttpClient) {
    this.hub = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hub/sql`)
      .withAutomaticReconnect()
      .build();

    this.hub.on('ReceiveCommand', (event: SqlCommandEvent) => this.addCommand(event));
    this.hub.on('SessionStateChanged', (state: string) => this.sessionState.set(state));
    this.hub.onreconnected(() => this.fetchState());

    this.hub.start()
      .then(() => this.fetchState())
      .catch(err => console.error('SignalR connection failed:', err));
  }

  play(): void {
    this.http.post(`${environment.apiUrl}/api/session/play`, {}).subscribe();
  }

  pause(): void {
    this.http.post(`${environment.apiUrl}/api/session/pause`, {}).subscribe();
  }

  stop(): void {
    this.http.post(`${environment.apiUrl}/api/session/stop`, {}).subscribe();
  }

  clear(): void {
    this.groupMap.clear();
    this.traceIdOrder.length = 0;
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
    let group = this.groupMap.get(event.traceId);

    if (!group) {
      this.totalRequestsSeen++;

      if (this.traceIdOrder.length >= environment.displayLimit) {
        const oldestId = this.traceIdOrder.shift()!;
        this.groupMap.delete(oldestId);
      }

      group = {
        traceId: event.traceId,
        url: event.url,
        methodName: event.methodName,
        commands: [],
        totalDurationUs: 0,
        capturedAt: event.capturedAt,
      };

      this.groupMap.set(event.traceId, group);
      this.traceIdOrder.push(event.traceId);
    }

    group.commands.push(event);
    group.totalDurationUs += event.durationUs;

    // Newest first
    this.requestGroups.set(
      [...this.traceIdOrder].reverse().map(id => this.groupMap.get(id)!)
    );
    this.displayedCount.set(this.traceIdOrder.length);
    this.totalCount.set(this.totalRequestsSeen);
  }

  ngOnDestroy(): void {
    this.hub.stop();
  }
}
