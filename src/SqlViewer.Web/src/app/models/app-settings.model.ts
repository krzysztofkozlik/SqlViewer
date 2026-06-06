export interface AppSettings {
  longRunningThresholdMs: number;
  slowRequestThresholdMs: number;
  displayLimit: number;
}

export const DEFAULT_SETTINGS: AppSettings = {
  longRunningThresholdMs: 400,
  slowRequestThresholdMs: 800,
  displayLimit: 1000,
};
