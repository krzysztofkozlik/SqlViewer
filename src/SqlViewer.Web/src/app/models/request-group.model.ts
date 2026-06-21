import { SqlCommandEvent } from './sql-command-event.model';

export interface RequestGroup {
  spanId: string;
  traceId: string;
  url: string;
  requestType: string;
  commands: SqlCommandEvent[];
  totalDurationUs: number;
  capturedAt: string;
}
