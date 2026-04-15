export interface DdCredentials {
  api_key: string;
  app_key: string;
  site: string;
}

export interface MetricSeries {
  metric: string;
  displayName?: string;
  pointlist: Array<[number, number]>;
  scope?: string;
  unit?: any[];
}

export interface Monitor {
  id: number;
  name: string;
  type: string;
  status?: string;
  query: string;
  message?: string;
  tags?: string[];
  options?: Record<string, any>;
  created?: string;
  modified?: string;
}

export interface Dashboard {
  id: string;
  title: string;
  layout_type: string;
  url?: string;
  created_at?: string;
  modified_at?: string;
  author_handle?: string;
  description?: string;
  widgets?: any[];
  template_variables?: any[];
}

export interface Host {
  name: string;
  id?: number;
  aliases?: string[];
  apps?: string[];
  tags_by_source?: Record<string, string[]>;
  is_muted?: boolean;
  up?: boolean;
  last_reported_time?: number;
}

export interface DdEvent {
  id?: number;
  title: string;
  text: string;
  date_happened?: number;
  host?: string;
  tags?: string[];
  alert_type?: string;
  priority?: string;
  source?: string;
  url?: string;
}

export interface Downtime {
  id: number;
  scope: string;
  message?: string;
  start?: number;
  end?: number;
  monitor_id?: number;
  monitor_tags?: string[];
  active?: boolean;
  disabled?: boolean;
  creator_id?: number;
}
