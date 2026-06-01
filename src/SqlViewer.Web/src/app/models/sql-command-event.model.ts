export interface SqlCommandEvent {
  traceId: string;
  url: string;
  methodName: string;
  commandType: string;
  firstTable: string;
  durationUs: number;
  rowCount: number;
  sqlText: string;
  capturedAt: string;
}
