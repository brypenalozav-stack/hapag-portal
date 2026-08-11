export interface Manifest {
  id: string;
  vesselImo: string;
  voyage: string;
  port: string;
  direction: string;
  estimatedArrival?: string;
  estimatedDeparture?: string;
  transmissionCount: number;
}

export interface CreateManifestRequest {
  vesselImo: string;
  voyage: string;
  port: string;
  direction: string;
  estimatedArrival?: string;
  estimatedDeparture?: string;
}

export interface Transmission {
  id: string;
  manifestId?: string;
  billOfLadingId?: string;
  blNumber?: string;
  stage: string;
  kind: string;
  status: string;
  reference?: string;
  responseCode?: string;
  responseMessage?: string;
  attemptCount: number;
  submittedAt?: string;
  respondedAt?: string;
}
