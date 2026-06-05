import { Injectable, signal } from '@angular/core';
import { AppSettings, DEFAULT_SETTINGS } from '../models/app-settings.model';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly STORAGE_KEY = 'sqlviewer-settings';

  readonly settings = signal<AppSettings>(this.load());

  save(settings: AppSettings): void {
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(settings));
    this.settings.set(settings);
  }

  private load(): AppSettings {
    try {
      const raw = localStorage.getItem(this.STORAGE_KEY);
      if (raw) return { ...DEFAULT_SETTINGS, ...JSON.parse(raw) };
    } catch {}
    return { ...DEFAULT_SETTINGS };
  }
}
