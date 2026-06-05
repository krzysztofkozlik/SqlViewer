export interface AppSettings {
  longRunningThresholdMs: number;
}

export const DEFAULT_SETTINGS: AppSettings = {
  longRunningThresholdMs: 1000,
};
