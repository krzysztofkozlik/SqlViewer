import { Component, computed, inject } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MonitoringService } from '../../services/monitoring.service';

@Component({
  selector: 'app-toolbar',
  imports: [MatToolbarModule, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './toolbar.html',
  styleUrl: './toolbar.scss',
})
export class Toolbar {
  protected readonly monitoring = inject(MonitoringService);

  // When the SignalR connection is not healthy, the connection state takes priority
  // over the session state in the badge so the user always knows why things aren't working.
  protected readonly displayState = computed(() => {
    const conn = this.monitoring.connectionState();
    return conn === 'Connected' ? this.monitoring.sessionState() : conn;
  });

  protected readonly isConnected = computed(() =>
    this.monitoring.connectionState() === 'Connected'
  );
}
