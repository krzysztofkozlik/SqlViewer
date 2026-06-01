import { Component, computed, input } from '@angular/core';
import { MatExpansionModule } from '@angular/material/expansion';
import { RequestGroup } from '../../models/request-group.model';
import { SqlCommandRow } from '../sql-command-row/sql-command-row';

@Component({
  selector: 'app-request-row',
  imports: [MatExpansionModule, SqlCommandRow],
  templateUrl: './request-row.html',
  styleUrl: './request-row.scss',
})
export class RequestRow {
  readonly group = input.required<RequestGroup>();

  readonly totalDurationMs = computed(() =>
    (this.group().totalDurationUs / 1000).toFixed(1)
  );
}
