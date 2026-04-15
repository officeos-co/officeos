import { describe, it } from "bun:test";

describe("alerts", () => {
  describe("list_alert_policies", () => {
    it.todo("should POST policiesSearch NerdGraph query");
    it.todo("should pass name filter when provided");
    it.todo("should return policies array and nextCursor");
  });

  describe("create_alert_policy", () => {
    it.todo("should POST alertsPolicyCreate mutation");
    it.todo("should default incidentPreference to PER_POLICY");
    it.todo("should return id, name, incidentPreference");
  });

  describe("list_alert_conditions", () => {
    it.todo("should POST nrqlConditionsSearch query");
    it.todo("should pass policyId filter when provided");
    it.todo("should return conditions with nrql.query field");
  });

  describe("create_alert_condition", () => {
    it.todo("should POST alertsNrqlConditionStaticCreate mutation");
    it.todo("should build CRITICAL term from critical_threshold");
    it.todo("should add WARNING term when warning_threshold is provided");
    it.todo("should default operator to ABOVE");
  });
});
