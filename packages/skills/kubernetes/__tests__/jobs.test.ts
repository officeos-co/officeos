import { describe, it } from "bun:test";

describe("jobs", () => {
  describe("list_jobs", () => {
    it.todo("should list batch/v1 jobs in namespace");
    it.todo("should compute duration from startTime and completionTime");
    it.todo("should format completions as succeeded/spec.completions");
  });

  describe("create_job", () => {
    it.todo("should POST job manifest with /bin/sh -c wrapper for command");
    it.todo("should use default backoffLimit=6 and restartPolicy=Never");
  });

  describe("list_cronjobs", () => {
    it.todo("should list batch/v1 cronjobs with schedule and active count");
  });

  describe("create_cronjob", () => {
    it.todo("should POST cronjob with schedule expression");
    it.todo("should set successfulJobsHistoryLimit and failedJobsHistoryLimit");
  });
});
