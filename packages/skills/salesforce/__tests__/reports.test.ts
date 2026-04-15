import { describe, it, expect } from "bun:test";

describe("reports", () => {
  describe("list_reports", () => {
    it.todo("should query Report sobject with SOQL");
    it.todo("should map Format to ReportFormat in response");
  });

  describe("run_report", () => {
    it.todo("should POST to /analytics/reports/:id");
    it.todo("should include reportMetadata body when filters provided");
    it.todo("should return factMap and reportMetadata");
  });

  describe("get_report_metadata", () => {
    it.todo("should GET /analytics/reports/:id/describe");
    it.todo("should return detailColumns and groupings");
  });

  describe("list_users", () => {
    it.todo("should build SOQL for User with IsActive and UserRole filters");
    it.todo("should map Profile.Name and UserRole.Name to flat strings");
  });

  describe("get_user", () => {
    it.todo("should query User by Id");
    it.todo("should throw when user not found");
  });
});
