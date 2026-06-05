import { Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AppSettings } from '../../models/app-settings.model';

@Component({
  selector: 'app-settings-dialog',
  imports: [MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './settings-dialog.html',
  styleUrl: './settings-dialog.scss',
})
export class SettingsDialog {
  private readonly dialogRef = inject(MatDialogRef<SettingsDialog>);
  private readonly data = inject<AppSettings>(MAT_DIALOG_DATA);

  protected readonly thresholdMs = signal(this.data.longRunningThresholdMs);

  protected onThresholdChange(event: Event): void {
    const value = +(event.target as HTMLInputElement).value;
    if (value > 0) this.thresholdMs.set(value);
  }

  protected save(): void {
    this.dialogRef.close({ ...this.data, longRunningThresholdMs: this.thresholdMs() });
  }

  protected cancel(): void {
    this.dialogRef.close();
  }
}
