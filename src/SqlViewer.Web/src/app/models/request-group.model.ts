import { SqlCommandEvent } from './sql-command-event.model';

export interface RequestGroup {
  spanId: string;
  url: string;
  methodName: string;
  commands: SqlCommandEvent[];
  totalDurationUs: number;
  capturedAt: string;
}
