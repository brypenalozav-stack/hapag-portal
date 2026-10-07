/** Estado de un requisito de liberación (ReleaseStepStatuses del backend). */
export type ReleaseStepStatus = 'Done' | 'Pending' | 'InProgress' | 'NotRequired' | 'Unavailable';

export type ReleaseStepCode =
  | 'FREIGHT'
  | 'LOCAL_CHARGES'
  | 'RESPONSIBILITY_LETTER'
  | 'DEMURRAGE'
  | 'ADVANCE_DEMURRAGE'
  | 'NO_DEBT_CERTIFICATE'
  | 'RELEASE_LETTER';

export type ReleaseStepAction =
  | 'None'
  | 'PayFreight'
  | 'PayCharges'
  | 'IssueResponsibilityLetter'
  | 'CalculateDemurrage'
  | 'PayDemurrage'
  | 'PayAdvanceDemurrage'
  | 'RequestNoDebtCertificate'
  | 'RequestReleaseLetter';

export interface ReleaseAmount {
  currency: string;
  total: number;
}

export interface ReleaseStepItem {
  code: string;
  label: string;
  reference: string | null;
  status: string;
  satisfied: boolean;
  amount: number | null;
  currency: string | null;
}

export interface ReleaseStep {
  code: ReleaseStepCode;
  status: ReleaseStepStatus;
  reason: string | null;
  action: ReleaseStepAction;
  actionAllowed: boolean;
  pendingAmounts: ReleaseAmount[];
  items: ReleaseStepItem[];
}

export interface ReleaseContainer {
  containerNumber: string;
  containerType: string;
  isShipperOwned: boolean;
  demurrageStatus: string | null;
  demurrageAmount: number | null;
  demurrageCurrency: string | null;
  tatcNumber: string | null;
  tatcStatus: string | null;
  tatcIssuedAt: string | null;
  warehouseCode: string | null;
  tatcPendingReasons: string[];
}

export interface ReleaseTatc {
  unlocked: boolean;
  available: boolean;
  status: string | null;
  errorCode: string | null;
  sourceUpdatedAt: string | null;
  retrievedAt: string;
  windowHours: number;
  availableFrom: string | null;
  windowOpen: boolean;
  canRequest: boolean;
  lastRequestedAt: string | null;
  lastRequestStatus: string | null;
  lastRequestReason: string | null;
}

/** Consulta de BL y TATC con el estado de liberación (M2-09, CL-IMP-13, BO-IMP-13). */
export interface ReleaseStatus {
  blId: string;
  blNumber: string;
  bookingNumber: string | null;
  country: 'CL' | 'BO' | string;
  operation: string;
  status: string;
  vessel: string | null;
  voyage: string | null;
  portOfLoading: string | null;
  portOfDischarge: string | null;
  portOfDischargeCode: string | null;
  finalDestinationCode: string | null;
  eta: string | null;
  consignee: string | null;
  timeZone: string;
  applicable: boolean;
  steps: ReleaseStep[];
  completedSteps: number;
  totalSteps: number;
  released: boolean;
  containers: ReleaseContainer[];
  tatc: ReleaseTatc;
  notices: string[];
  evaluatedAt: string;
}
