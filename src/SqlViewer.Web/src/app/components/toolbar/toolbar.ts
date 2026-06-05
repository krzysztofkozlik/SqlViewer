import { Component, computed, inject } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MonitoringService } from '../../services/monitoring.service';
import { SettingsService } from '../../services/settings.service';
import { SettingsDialog } from '../settings-dialog/settings-dialog';
import { AppSettings } from '../../models/app-settings.model';

@Component({
  selector: 'app-toolbar',
  imports: [MatToolbarModule, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './toolbar.html',
  styleUrl: './toolbar.scss',
})
export class Toolbar {
  protected readonly monitoring = inject(MonitoringService);
  private readonly settingsService = inject(SettingsService);
  private readonly dialog = inject(MatDialog);

  protected readonly displayState = computed(() => {
    const conn = this.monitoring.connectionState();
    return conn === 'Connected' ? this.monitoring.sessionState() : conn;
  });

  protected readonly isConnected = computed(() =>
    this.monitoring.connectionState() === 'Connected'
  );

  protected openSettings(): void {
    const ref = this.dialog.open(SettingsDialog, {
      data: { ...this.settingsService.settings() },
    });
    ref.afterClosed().subscribe((result?: AppSettings) => {
      if (result) this.settingsService.save(result);
    });
  }
}
