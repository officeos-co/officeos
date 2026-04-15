export interface PromResult {
  resultType: string;
  result: any[];
}

export interface PromTarget {
  scrapePool: string;
  scrapeUrl: string;
  globalUrl?: string;
  lastError: string;
  lastScrape: string;
  lastScrapeDuration: number;
  health: string;
  labels: Record<string, string>;
}

export interface PromRule {
  name: string;
  query: string;
  duration?: number;
  health: string;
  lastEvaluation?: string;
  evaluationTime?: number;
  type: string;
  labels?: Record<string, string>;
  annotations?: Record<string, string>;
}

export interface PromRuleGroup {
  name: string;
  file: string;
  rules: PromRule[];
  interval: number;
  evaluationTime?: number;
  lastEvaluation?: string;
}

export interface PromAlert {
  labels: Record<string, string>;
  annotations: Record<string, string>;
  state: string;
  activeAt: string;
  value: string;
}
