export interface ReportResult {
  title: string;
  columns: string[];
  rows: string[][];
}

export type ReportType = 'transmissions' | 'overdue-deadlines';
