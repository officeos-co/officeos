import { describe, it } from "bun:test";

describe("jenkins", () => {
  describe("jobs", () => {
    it.todo("list_jobs should call /api/json at root when no folder_path");
    it.todo("list_jobs should call /job/Folder/api/json when folder_path given");
    it.todo("get_job should return last_build_number and buildable fields");
    it.todo("get_job should handle job with no lastBuild (returns null)");
    it.todo("build_job should POST to /job/:name/build when no params");
    it.todo("build_job should POST to /job/:name/buildWithParameters with form body");
    it.todo("get_build should use 'lastBuild' ref when build_number is 0");
    it.todo("list_builds should pass tree param with limit range");
    it.todo("stop_build should POST to /job/:name/:number/stop");
  });

  describe("log", () => {
    it.todo("get_log should call progressiveText endpoint with start offset");
    it.todo("get_log should return more_data=true when X-More-Data header is true");
    it.todo("get_log should return more_data=false when header absent");
  });

  describe("queue", () => {
    it.todo("get_queue should return id, why, task_name, in_queue_since");
    it.todo("get_queue should return empty array when queue is empty");
  });

  describe("nodes", () => {
    it.todo("list_nodes should return display_name, offline, num_executors");
    it.todo("list_nodes should map offline_cause from offlineCauseReason");
  });

  describe("views", () => {
    it.todo("list_views should call /api/json with tree=views[name,url]");
    it.todo("list_views should return name and url for each view");
  });

  describe("stages", () => {
    it.todo("get_stages should call /job/:name/:number/wfapi/describe");
    it.todo("get_stages should return duration_millis and start_time_millis");
  });

  describe("auth", () => {
    it.todo("should use Basic auth with base64(user:api_token)");
    it.todo("should fetch CSRF crumb and include it on POST requests");
    it.todo("should handle crumb fetch failure gracefully (empty crumb)");
    it.todo("should throw descriptive error on non-ok response");
  });
});
