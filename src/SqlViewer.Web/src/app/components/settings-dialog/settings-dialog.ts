import { Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { AppSettings, DEFAULT_SETTINGS } from '../../models/app-settings.model';

@Component({
  selector: 'app-settings-dialog',
  imports: [MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSlideToggleModule, MatIconModule, MatDividerModule],
  templateUrl: './settings-dialog.html',
  styleUrl: './settings-dialog.scss',
})
export class SettingsDialog {
  private readonly dialogRef = inject(MatDialogRef<SettingsDialog>);
  private readonly data = inject<AppSettings>(MAT_DIALOG_DATA);

  protected readonly defaults = DEFAULT_SETTINGS;

  protected readonly darkMode = signal(this.data.darkMode);
  protected readonly thresholdMs = signal(this.data.longRunningThresholdMs);
  protected readonly slowRequestMs = signal(this.data.slowRequestThresholdMs);
  protected readonly displayLimit = signal(this.data.displayLimit);

  protected onThresholdChange(event: Event): void {
    const value = +(event.target as HTMLInputElement).value;
    if (value > 0) this.thresholdMs.set(value);
  }

  protected onSlowRequestChange(event: Event): void {
    const value = +(event.target as HTMLInputElement).value;
    if (value > 0) this.slowRequestMs.set(value);
  }

  protected onDisplayLimitChange(event: Event): void {
    const value = +(event.target as HTMLInputElement).value;
    if (value > 0) this.displayLimit.set(value);
  }

  protected save(): void {
    this.dialogRef.close({
      ...this.data,
      darkMode: this.darkMode(),
      longRunningThresholdMs: this.thresholdMs(),
      slowRequestThresholdMs: this.slowRequestMs(),
      displayLimit: this.displayLimit(),
    });
  }

  protected cancel(): void {
    this.dialogRef.close();
  }
}
