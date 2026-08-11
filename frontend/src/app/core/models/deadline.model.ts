export interface DeadlineItem {
  id: string;
  ruleCode: string;
  ruleName: string;
  severity: string;
  manifestId?: string;
  billOfLadingId?: string;
  blNumber?: string;
  baseEventAt: string;
  dueAt: string;
  completedAt?: string;
  status: string;
}

export interface DeadlineRule {
  id: string;
  code: string;
  name: string;
  baseEvent: string;
  offsetHours: number;
  atRiskWindowHours: number;
  direction?: string;
  country?: string;
  blType?: string;
  severity: string;
  source?: string;
  certainty: string;
  isActive: boolean;
}
