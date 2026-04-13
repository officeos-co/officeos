export type Runner = {
  id: string;
  name: string;
  status: string;
  lastHeartbeatAt: string | null;
  version: string | null;
  createdAt: string;
};

export type CreateRunnerResult = {
  id: string;
  name: string;
  registrationToken: string;
};
