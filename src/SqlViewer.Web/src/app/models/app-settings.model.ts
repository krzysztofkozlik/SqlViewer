export interface AppSettings {
  longRunningThresholdMs: number;
  slowRequestThresholdMs: number;
}

export const DEFAULT_SETTINGS: AppSettings = {
  longRunningThresholdMs: 400,
  slowRequestThresholdMs: 800,
};
