import { describe, it } from "bun:test";

describe("oncalls", () => {
  describe("list_oncalls", () => {
    it.todo("should call /oncalls");
    it.todo("should pass schedule_ids[] and user_ids[] params");
    it.todo("should pass earliest=true as string param");
    it.todo("should return escalation_policy, user, start, end per entry");
  });
});

describe("schedules", () => {
  describe("list_schedules", () => {
    it.todo("should call /schedules with limit");
    it.todo("should filter by query");
  });

  describe("get_schedule", () => {
    it.todo("should call /schedules/{id}");
    it.todo("should pass since/until window params");
    it.todo("should return final_schedule with rendered entries");
  });
});

describe("escalation_policies", () => {
  describe("list_escalation_policies", () => {
    it.todo("should call /escalation_policies");
    it.todo("should pass user_ids[] and team_ids[] params");
  });

  describe("get_escalation_policy", () => {
    it.todo("should call /escalation_policies/{id}");
    it.todo("should return escalation_rules, services, teams");
  });
});
