import { describe, it } from "bun:test";

describe("gcp skill", () => {
  describe("instances_list", () => {
    it.todo("should POST { command: 'gcloud', args: ['compute', 'instances', 'list', ...] } to proxy_url");
    it.todo("should include --zones flag when zone param is provided");
    it.todo("should include --filter flag when filter param is provided");
    it.todo("should throw when proxy returns non-2xx status");
    it.todo("should throw when gcloud exitCode is non-zero");
  });

  describe("instances_describe", () => {
    it.todo("should include instance name, project, and zone in args");
    it.todo("should return parsed JSON from stdout");
  });

  describe("instances_create", () => {
    it.todo("should pass machine_type, image_family, image_project as flags");
    it.todo("should default to e2-micro machine type");
  });

  describe("instances_delete", () => {
    it.todo("should include --quiet flag to skip confirmation");
  });

  describe("instances_start", () => {
    it.todo("should call 'compute instances start' with correct project and zone");
  });

  describe("instances_stop", () => {
    it.todo("should call 'compute instances stop' with correct project and zone");
  });

  describe("clusters_list", () => {
    it.todo("should call 'container clusters list' with project flag");
    it.todo("should include --region flag when region param is provided");
  });

  describe("clusters_describe", () => {
    it.todo("should include cluster name, project, and region in args");
  });

  describe("services_list", () => {
    it.todo("should call 'run services list' with project and region flags");
  });

  describe("services_describe", () => {
    it.todo("should include service name in args");
  });

  describe("services_deploy", () => {
    it.todo("should include --allow-unauthenticated when flag is true");
    it.todo("should include --no-allow-unauthenticated when flag is false");
    it.todo("should pass image URL as --image flag");
  });

  describe("functions_list", () => {
    it.todo("should call 'functions list' with project flag");
    it.todo("should include --regions flag when region is provided");
  });

  describe("functions_describe", () => {
    it.todo("should include function name, project, and region in args");
  });

  describe("functions_deploy", () => {
    it.todo("should include --entry-point flag when entry_point is provided");
    it.todo("should include --trigger-http flag");
  });

  describe("buckets_list", () => {
    it.todo("should call 'storage buckets list' with project flag");
  });

  describe("objects_list", () => {
    it.todo("should construct gs://BUCKET/PREFIX URI");
    it.todo("should use gs://BUCKET/ URI when prefix is omitted");
  });

  describe("datasets_list", () => {
    it.todo("should call 'bq ls' with project_id flag");
  });

  describe("query", () => {
    it.todo("should pass SQL query as last positional argument");
    it.todo("should include --max_rows flag with default 100");
    it.todo("should set --use_legacy_sql=false");
  });
});
