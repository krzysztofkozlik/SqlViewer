import { Component, computed, inject, input } from '@angular/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RequestGroup } from '../../models/request-group.model';
import { SqlCommandRow } from '../sql-command-row/sql-command-row';
import { formatTime } from '../../utils/format-time';
import { SettingsService } from '../../services/settings.service';

@Component({
  selector: 'app-request-row',
  imports: [MatExpansionModule, MatIconModule, MatTooltipModule, SqlCommandRow],
  templateUrl: './request-row.html',
  styleUrl: './request-row.scss',
})
export class RequestRow {
  private readonly settings = inject(SettingsService);

  readonly group = input.required<RequestGroup>();

  readonly totalDurationMs = computed(() =>
    (this.group().totalDurationUs / 1000).toFixed(1)
  );

  readonly timestamp = computed(() => formatTime(this.group().capturedAt));

  readonly hasLongRunning = computed(() => {
    const threshold = this.settings.settings().longRunningThresholdMs;
    return this.group().commands.some(cmd => cmd.durationUs / 1000 > threshold);
  });

  readonly isSlowRequest = computed(() =>
    this.group().totalDurationUs / 1000 > this.settings.settings().slowRequestThresholdMs
  );

  readonly hasEmptyQueries = computed(() =>
    this.group().commands.some(cmd => !cmd.firstTable && cmd.rowCount === 0)
  );
}
