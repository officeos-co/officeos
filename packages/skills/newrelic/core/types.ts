export interface NrCredentials {
  api_key: string;
  account_id: string;
}

export interface NrApplication {
  id: number;
  name: string;
  language?: string;
  health_status?: string;
  reporting?: boolean;
  last_reported_at?: string;
  application_summary?: Record<string, any>;
}

export interface NrAlertPolicy {
  id: string;
  name: string;
  incidentPreference?: string;
}

export interface NrAlertCondition {
  id: string;
  name: string;
  enabled: boolean;
  type?: string;
  nrql?: { query: string };
  terms?: any[];
}

export interface NrDashboard {
  guid: string;
  name: string;
  description?: string;
  pages?: any[];
  createdAt?: string;
  updatedAt?: string;
  permalink?: string;
}

export interface NrMonitor {
  guid: string;
  name: string;
  monitorType?: string;
  status?: string;
  period?: string;
  uri?: string;
}

export interface NrDeployment {
  id: string;
  version: string;
  description?: string;
  user?: string;
  timestamp?: string;
  entityGuid?: string;
  changelog?: string;
}
