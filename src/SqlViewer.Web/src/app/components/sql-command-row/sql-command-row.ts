import { Component, computed, inject, input, signal } from '@angular/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { format } from 'sql-formatter';
import hljs from 'highlight.js/lib/core';
import sql from 'highlight.js/lib/languages/sql';
import { SqlCommandEvent } from '../../models/sql-command-event.model';
import { formatTime } from '../../utils/format-time';
import { SettingsService } from '../../services/settings.service';

// Register once at module level — not per component instance.
hljs.registerLanguage('sql', sql);

@Component({
  selector: 'app-sql-command-row',
  imports: [MatExpansionModule, MatIconModule, MatTooltipModule, MatButtonToggleModule],
  templateUrl: './sql-command-row.html',
  styleUrl: './sql-command-row.scss',
})
export class SqlCommandRow {
  private readonly settings = inject(SettingsService);

  readonly command = input.required<SqlCommandEvent>();

  /** Toggles between formatted+highlighted (false) and raw SQL (true). */
  protected readonly showRaw = signal(false);

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

  // Applied on top of the already-formatted SQL so highlighting respects
  // the indentation and line breaks added by sql-formatter.
  readonly highlightedSql = computed(() =>
    hljs.highlight(this.formattedSql(), { language: 'sql' }).value
  );
}
