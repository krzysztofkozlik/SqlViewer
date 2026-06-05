import { Component, computed, inject, input } from '@angular/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { format } from 'sql-formatter';
import { SqlCommandEvent } from '../../models/sql-command-event.model';
import { formatTime } from '../../utils/format-time';
import { SettingsService } from '../../services/settings.service';

@Component({
  selector: 'app-sql-command-row',
  imports: [MatExpansionModule, MatIconModule, MatTooltipModule],
  templateUrl: './sql-command-row.html',
  styleUrl: './sql-command-row.scss',
})
export class SqlCommandRow {
  private readonly settings = inject(SettingsService);

  readonly command = input.required<SqlCommandEvent>();

  readonly durationMs = computed(() =>
    (this.command().durationUs / 1000).toFixed(1)
  );

  readonly timestamp = computed(() => formatTime(this.command().capturedAt));

  readonly isLongRunning = computed(() =>
    this.command().durationUs / 1000 > this.settings.settings().longRunningThresholdMs
  );

  readonly formattedSql = computed(() =>
    format(this.command().sqlText, { language: 'tsql', tabWidth: 2 })
  );
}
