import { describe, it, expect } from "bun:test";

describe("repos", () => {
  describe("list_repos", () => {
    it.todo("should call /user/repos with visibility param");
    it.todo("should return mapped repo array with full_name, private, html_url");
    it.todo("should respect per_page param");
  });

  describe("get_repo", () => {
    it.todo("should call /repos/{owner}/{repo}");
    it.todo("should return full repo details including topics, stargazers_count");
  });

  describe("create_repo", () => {
    it.todo("should POST to /user/repos with name, private, auto_init");
    it.todo("should return full_name, html_url, clone_url");
  });

  describe("list_repo_topics", () => {
    it.todo("should call /repos/{owner}/{repo}/topics");
    it.todo("should return array of topic strings");
  });

  describe("set_repo_topics", () => {
    it.todo("should PUT to /repos/{owner}/{repo}/topics with names array");
    it.todo("should return updated topics array");
  });
});
