import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class ErrorService {
  private readonly snackBar = inject(MatSnackBar);

  show(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 8000,
      verticalPosition: 'top',
      horizontalPosition: 'right',
      panelClass: 'error-snackbar',
    });
  }
}
