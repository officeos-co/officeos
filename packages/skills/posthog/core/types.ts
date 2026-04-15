export interface PhCredentials {
  api_key: string;
  project_id: string;
  host: string;
}

export interface PhEvent {
  id: string;
  event: string;
  distinct_id: string;
  properties?: Record<string, any>;
  timestamp?: string;
  person?: any;
}

export interface PhPerson {
  id: number;
  uuid: string;
  distinct_ids: string[];
  properties?: Record<string, any>;
  created_at?: string;
}

export interface FeatureFlag {
  id: number;
  key: string;
  name?: string;
  active: boolean;
  rollout_percentage?: number;
  filters?: any;
  created_at?: string;
  created_by?: any;
}

export interface Insight {
  id: number;
  name?: string;
  description?: string;
  filters?: any;
  result?: any;
  created_at?: string;
  last_modified_at?: string;
}

export interface Dashboard {
  id: number;
  name: string;
  description?: string;
  pinned?: boolean;
  tiles?: any[];
  created_at?: string;
  created_by?: any;
}

export interface Cohort {
  id: number;
  name: string;
  description?: string;
  count?: number;
  filters?: any;
  is_calculating?: boolean;
  created_at?: string;
}

export interface Annotation {
  id: number;
  content: string;
  date_marker?: string;
  scope?: string;
  created_at?: string;
  created_by?: any;
}
