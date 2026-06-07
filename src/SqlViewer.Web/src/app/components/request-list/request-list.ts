import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { delay, of, switchMap } from 'rxjs';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MonitoringService } from '../../services/monitoring.service';
import { SettingsService } from '../../services/settings.service';
import { RequestRow } from '../request-row/request-row';

@Component({
  selector: 'app-request-list',
  imports: [
    MatExpansionModule, MatFormFieldModule, MatInputModule,
    MatChipsModule, MatButtonModule, MatIconModule, MatTooltipModule,
    RequestRow,
  ],
  templateUrl: './request-list.html',
  styleUrl: './request-list.scss',
})
export class RequestList {
  protected readonly monitoring = inject(MonitoringService);
  private readonly settings = inject(SettingsService);

  // Raw signal — updates on every keystroke, drives the visible input value.
  protected readonly urlInput = signal('');

  // Debounced signal — used by filteredGroups so filtering only runs after
  // the user pauses typing, not on every character.
  // Empty string (clear / initial) emits immediately; non-empty values are
  // debounced 300ms so filtering doesn't run on every keystroke while typing.
  private readonly urlFilter = toSignal(
    toObservable(this.urlInput).pipe(
      switchMap(v => v === '' ? of('') : of(v).pipe(delay(300)))
    ),
    { initialValue: '' }
  );

  protected readonly showLongRunningOnly = signal(false);
  protected readonly showSlowOnly = signal(false);

  // Intentionally uses urlInput (not urlFilter) so the clear button appears
  // and disappears immediately as the user types/clears.
  protected readonly hasActiveFilters = computed(() =>
    !!this.urlInput() || this.showLongRunningOnly() || this.showSlowOnly()
  );

  protected readonly displayLimit = computed(() => this.settings.settings().displayLimit);

  protected readonly filteredGroups = computed(() => {
    const url = (this.urlFilter() ?? '').toLowerCase().trim();
    const longOnly = this.showLongRunningOnly();
    const slowOnly = this.showSlowOnly();
    const { longRunningThresholdMs, slowRequestThresholdMs } = this.settings.settings();

    return this.monitoring.requestGroups().filter(group => {
      if (url && !group.url.toLowerCase().includes(url)) return false;
      if (longOnly && !group.commands.some(cmd => cmd.durationUs / 1000 > longRunningThresholdMs)) return false;
      if (slowOnly && group.totalDurationUs / 1000 <= slowRequestThresholdMs) return false;
      return true;
    });
  });

  protected clearFilters(): void {
    this.urlInput.set('');
    this.showLongRunningOnly.set(false);
    this.showSlowOnly.set(false);
  }
}
