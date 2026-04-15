import { describe, it, expect } from "bun:test";

describe("todoist", () => {
  describe("actions", () => {
    // Tasks
    it.todo("should expose list_tasks action");
    it.todo("should expose get_task action");
    it.todo("should expose create_task action");
    it.todo("should expose update_task action");
    it.todo("should expose close_task action");
    it.todo("should expose reopen_task action");
    it.todo("should expose delete_task action");
    // Projects
    it.todo("should expose list_projects action");
    it.todo("should expose get_project action");
    it.todo("should expose create_project action");
    it.todo("should expose update_project action");
    it.todo("should expose delete_project action");
    // Sections
    it.todo("should expose list_sections action");
    it.todo("should expose get_section action");
    it.todo("should expose create_section action");
    it.todo("should expose update_section action");
    it.todo("should expose delete_section action");
    // Comments
    it.todo("should expose list_comments action");
    it.todo("should expose get_comment action");
    it.todo("should expose create_comment action");
    it.todo("should expose update_comment action");
    it.todo("should expose delete_comment action");
    // Labels
    it.todo("should expose list_labels action");
    it.todo("should expose create_label action");
    it.todo("should expose update_label action");
    it.todo("should expose delete_label action");
    // Collaborators
    it.todo("should expose list_collaborators action");
  });

  describe("params", () => {
    describe("list_tasks", () => {
      it.todo("should accept optional project_id param");
      it.todo("should accept optional filter param");
      it.todo("should accept optional label param");
      it.todo("should accept optional priority param");
    });

    describe("get_task", () => {
      it.todo("should require task_id param");
    });

    describe("create_task", () => {
      it.todo("should require content param");
      it.todo("should accept optional description param");
      it.todo("should accept optional project_id param");
      it.todo("should accept optional section_id param");
      it.todo("should accept optional parent_id param");
      it.todo("should accept optional priority param");
      it.todo("should accept optional due_string param");
      it.todo("should accept optional due_date param");
      it.todo("should accept optional labels param");
      it.todo("should accept optional assignee_id param");
    });

    describe("update_task", () => {
      it.todo("should require task_id param");
      it.todo("should accept optional content param");
      it.todo("should accept optional description param");
      it.todo("should accept optional priority param");
      it.todo("should accept optional due_string param");
      it.todo("should accept optional due_date param");
      it.todo("should accept optional labels param");
      it.todo("should accept optional assignee_id param");
    });

    describe("close_task", () => {
      it.todo("should require task_id param");
    });

    describe("reopen_task", () => {
      it.todo("should require task_id param");
    });

    describe("delete_task", () => {
      it.todo("should require task_id param");
    });

    describe("get_project", () => {
      it.todo("should require project_id param");
    });

    describe("create_project", () => {
      it.todo("should require name param");
      it.todo("should accept optional color param");
      it.todo("should accept optional parent_id param");
      it.todo("should accept optional is_favorite param");
    });

    describe("update_project", () => {
      it.todo("should require project_id param");
      it.todo("should accept optional name param");
      it.todo("should accept optional color param");
      it.todo("should accept optional is_favorite param");
    });

    describe("delete_project", () => {
      it.todo("should require project_id param");
    });

    describe("list_sections", () => {
      it.todo("should accept optional project_id param");
    });

    describe("get_section", () => {
      it.todo("should require section_id param");
    });

    describe("create_section", () => {
      it.todo("should require name param");
      it.todo("should require project_id param");
    });

    describe("update_section", () => {
      it.todo("should require section_id param");
      it.todo("should require name param");
    });

    describe("delete_section", () => {
      it.todo("should require section_id param");
    });

    describe("list_comments", () => {
      it.todo("should accept optional task_id param");
      it.todo("should accept optional project_id param");
    });

    describe("get_comment", () => {
      it.todo("should require comment_id param");
    });

    describe("create_comment", () => {
      it.todo("should require content param");
      it.todo("should accept optional task_id param");
      it.todo("should accept optional project_id param");
      it.todo("should accept optional attachment param");
    });

    describe("update_comment", () => {
      it.todo("should require comment_id param");
      it.todo("should require content param");
    });

    describe("delete_comment", () => {
      it.todo("should require comment_id param");
    });

    describe("create_label", () => {
      it.todo("should require name param");
      it.todo("should accept optional color param");
      it.todo("should accept optional order param");
    });

    describe("update_label", () => {
      it.todo("should require label_id param");
      it.todo("should accept optional name param");
      it.todo("should accept optional color param");
      it.todo("should accept optional order param");
    });

    describe("delete_label", () => {
      it.todo("should require label_id param");
    });

    describe("list_collaborators", () => {
      it.todo("should require project_id param");
    });
  });

  describe("execute", () => {
    describe("list_tasks", () => {
      it.todo("should return tasks with id, content, priority, due, labels");
      it.todo("should filter by project_id");
      it.todo("should filter by label");
      it.todo("should filter by priority");
      it.todo("should handle errors");
    });

    describe("get_task", () => {
      it.todo("should return full task details");
      it.todo("should handle non-existent task");
    });

    describe("create_task", () => {
      it.todo("should create task and return id, content, url");
      it.todo("should set priority");
      it.todo("should set due date from due_string");
      it.todo("should set due date from due_date");
      it.todo("should assign labels");
      it.todo("should handle invalid project_id");
    });

    describe("update_task", () => {
      it.todo("should update task fields");
      it.todo("should return updated task");
      it.todo("should handle non-existent task");
    });

    describe("close_task", () => {
      it.todo("should mark task as completed");
      it.todo("should handle already completed task");
    });

    describe("reopen_task", () => {
      it.todo("should reopen completed task");
      it.todo("should handle already open task");
    });

    describe("delete_task", () => {
      it.todo("should permanently delete task");
      it.todo("should handle non-existent task");
    });

    describe("list_projects", () => {
      it.todo("should return projects with id, name, color, url");
    });

    describe("get_project", () => {
      it.todo("should return full project details");
      it.todo("should handle non-existent project");
    });

    describe("create_project", () => {
      it.todo("should create project and return id, name, url");
      it.todo("should set color and favorite");
    });

    describe("update_project", () => {
      it.todo("should update project fields");
      it.todo("should handle non-existent project");
    });

    describe("delete_project", () => {
      it.todo("should permanently delete project");
      it.todo("should handle non-existent project");
    });

    describe("list_sections", () => {
      it.todo("should return sections with id, name, project_id, order");
      it.todo("should filter by project_id");
    });

    describe("get_section", () => {
      it.todo("should return section details");
      it.todo("should handle non-existent section");
    });

    describe("create_section", () => {
      it.todo("should create section and return id, name");
    });

    describe("update_section", () => {
      it.todo("should update section name");
    });

    describe("delete_section", () => {
      it.todo("should permanently delete section");
    });

    describe("list_comments", () => {
      it.todo("should return comments with id, content, posted_at");
      it.todo("should filter by task_id");
      it.todo("should filter by project_id");
    });

    describe("get_comment", () => {
      it.todo("should return full comment details");
      it.todo("should handle non-existent comment");
    });

    describe("create_comment", () => {
      it.todo("should create comment and return id, content, posted_at");
      it.todo("should support task comments");
      it.todo("should support project comments");
    });

    describe("update_comment", () => {
      it.todo("should update comment content");
      it.todo("should handle non-existent comment");
    });

    describe("delete_comment", () => {
      it.todo("should permanently delete comment");
    });

    describe("list_labels", () => {
      it.todo("should return labels with id, name, color, order");
    });

    describe("create_label", () => {
      it.todo("should create label and return id, name, color");
    });

    describe("update_label", () => {
      it.todo("should update label fields");
    });

    describe("delete_label", () => {
      it.todo("should permanently delete label");
    });

    describe("list_collaborators", () => {
      it.todo("should return collaborators with id, name, email");
      it.todo("should handle project with no collaborators");
    });
  });
});
