export interface AppSettings {
  darkMode: boolean;
  longRunningThresholdMs: number;
  slowRequestThresholdMs: number;
  displayLimit: number;
}

export const DEFAULT_SETTINGS: AppSettings = {
  darkMode: true,
  longRunningThresholdMs: 400,
  slowRequestThresholdMs: 800,
  displayLimit: 1000,
};
