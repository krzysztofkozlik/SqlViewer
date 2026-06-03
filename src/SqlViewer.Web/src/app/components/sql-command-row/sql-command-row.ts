import { Component, computed, input } from '@angular/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { format } from 'sql-formatter';
import { SqlCommandEvent } from '../../models/sql-command-event.model';
import { formatTime } from '../../utils/format-time';

@Component({
  selector: 'app-sql-command-row',
  imports: [MatExpansionModule],
  templateUrl: './sql-command-row.html',
  styleUrl: './sql-command-row.scss',
})
export class SqlCommandRow {
  readonly command = input.required<SqlCommandEvent>();

  readonly durationMs = computed(() =>
    (this.command().durationUs / 1000).toFixed(1)
  );

  readonly timestamp = computed(() => formatTime(this.command().capturedAt));

  readonly formattedSql = computed(() =>
    format(this.command().sqlText, { language: 'tsql', tabWidth: 2 })
  );
}
