export interface PdRef {
  id: string;
  summary?: string;
  type?: string;
  html_url?: string;
}

export interface Incident {
  id: string;
  title: string;
  status: string;
  urgency?: string;
  service?: PdRef;
  assigned_to?: PdRef[];
  created_at?: string;
  html_url?: string;
  incident_key?: string;
  description?: string;
  last_status_change_at?: string;
  escalation_policy?: PdRef;
}

export interface Service {
  id: string;
  name: string;
  status?: string;
  description?: string;
  escalation_policy?: PdRef;
  teams?: PdRef[];
  html_url?: string;
}

export interface User {
  id: string;
  name: string;
  email: string;
  role?: string;
  teams?: PdRef[];
  html_url?: string;
}

export interface OncallEntry {
  escalation_policy?: PdRef;
  escalation_level?: number;
  schedule?: PdRef;
  user?: PdRef;
  start?: string;
  end?: string;
}

export interface Schedule {
  id: string;
  name: string;
  time_zone?: string;
  description?: string;
  html_url?: string;
}

export interface EscalationPolicy {
  id: string;
  name: string;
  description?: string;
  num_loops?: number;
  escalation_rules?: any[];
  services?: PdRef[];
  teams?: PdRef[];
  html_url?: string;
}
