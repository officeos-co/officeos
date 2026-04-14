import { describe, it, expect } from "bun:test";

describe("salesforce", () => {
  describe("actions", () => {
    // SOQL
    it.todo("should expose query action");
    it.todo("should expose query_all action");
    // Objects
    it.todo("should expose list_objects action");
    it.todo("should expose describe_object action");
    // Records CRUD
    it.todo("should expose get_record action");
    it.todo("should expose create_record action");
    it.todo("should expose update_record action");
    it.todo("should expose delete_record action");
    it.todo("should expose upsert_record action");
    // Leads
    it.todo("should expose list_leads action");
    it.todo("should expose create_lead action");
    it.todo("should expose convert_lead action");
    // Contacts
    it.todo("should expose list_contacts action");
    it.todo("should expose create_contact action");
    it.todo("should expose update_contact action");
    // Opportunities
    it.todo("should expose list_opportunities action");
    it.todo("should expose create_opportunity action");
    it.todo("should expose update_opportunity action");
    // Accounts
    it.todo("should expose list_accounts action");
    it.todo("should expose create_account action");
    it.todo("should expose update_account action");
    // Cases
    it.todo("should expose list_cases action");
    it.todo("should expose create_case action");
    it.todo("should expose update_case action");
    it.todo("should expose close_case action");
    // Reports
    it.todo("should expose list_reports action");
    it.todo("should expose run_report action");
    it.todo("should expose get_report_metadata action");
    // Users
    it.todo("should expose list_users action");
    it.todo("should expose get_user action");
    // Bulk
    it.todo("should expose bulk_insert action");
    it.todo("should expose bulk_update action");
    it.todo("should expose bulk_delete action");
    // Metadata
    it.todo("should expose list_metadata_types action");
    it.todo("should expose deploy_metadata action");
  });

  describe("params", () => {
    describe("query", () => {
      it.todo("should require soql");
    });

    describe("query_all", () => {
      it.todo("should require soql");
    });

    describe("list_objects", () => {
      it.todo("should require no arguments");
    });

    describe("describe_object", () => {
      it.todo("should require object_type");
    });

    describe("get_record", () => {
      it.todo("should require object_type");
      it.todo("should require record_id");
      it.todo("should accept optional fields array");
    });

    describe("create_record", () => {
      it.todo("should require object_type");
      it.todo("should require fields object");
    });

    describe("update_record", () => {
      it.todo("should require object_type");
      it.todo("should require record_id");
      it.todo("should require fields object");
    });

    describe("delete_record", () => {
      it.todo("should require object_type");
      it.todo("should require record_id");
    });

    describe("upsert_record", () => {
      it.todo("should require object_type");
      it.todo("should require external_id_field");
      it.todo("should require external_id_value");
      it.todo("should require fields object");
    });

    describe("list_leads", () => {
      it.todo("should accept optional status filter");
      it.todo("should accept optional owner filter");
      it.todo("should accept optional limit defaulting to 100");
    });

    describe("create_lead", () => {
      it.todo("should require last_name");
      it.todo("should require company");
      it.todo("should accept optional first_name");
      it.todo("should accept optional email");
      it.todo("should accept optional phone");
      it.todo("should accept optional lead_source");
      it.todo("should accept optional status");
      it.todo("should accept optional fields object");
    });

    describe("convert_lead", () => {
      it.todo("should require lead_id");
      it.todo("should require converted_status");
      it.todo("should accept optional account_id");
      it.todo("should accept optional contact_id");
      it.todo("should accept optional opportunity_name");
      it.todo("should accept optional do_not_create_opportunity");
    });

    describe("list_contacts", () => {
      it.todo("should accept optional account_id filter");
      it.todo("should accept optional limit defaulting to 100");
    });

    describe("create_contact", () => {
      it.todo("should require last_name");
      it.todo("should accept optional first_name");
      it.todo("should accept optional email");
      it.todo("should accept optional phone");
      it.todo("should accept optional account_id");
      it.todo("should accept optional title");
      it.todo("should accept optional fields object");
    });

    describe("update_contact", () => {
      it.todo("should require contact_id");
      it.todo("should require fields object");
    });

    describe("list_opportunities", () => {
      it.todo("should accept optional stage filter");
      it.todo("should accept optional account_id filter");
      it.todo("should accept optional close_date_range object");
      it.todo("should accept optional limit");
    });

    describe("create_opportunity", () => {
      it.todo("should require name");
      it.todo("should require stage");
      it.todo("should require close_date");
      it.todo("should accept optional amount");
      it.todo("should accept optional account_id");
      it.todo("should accept optional fields object");
    });

    describe("update_opportunity", () => {
      it.todo("should require opportunity_id");
      it.todo("should require fields object");
    });

    describe("list_accounts", () => {
      it.todo("should accept optional type filter");
      it.todo("should accept optional owner filter");
      it.todo("should accept optional limit");
    });

    describe("create_account", () => {
      it.todo("should require name");
      it.todo("should accept optional type");
      it.todo("should accept optional industry");
      it.todo("should accept optional website");
      it.todo("should accept optional phone");
      it.todo("should accept optional fields object");
    });

    describe("update_account", () => {
      it.todo("should require account_id");
      it.todo("should require fields object");
    });

    describe("list_cases", () => {
      it.todo("should accept optional status filter");
      it.todo("should accept optional priority filter");
      it.todo("should accept optional account_id filter");
      it.todo("should accept optional limit");
    });

    describe("create_case", () => {
      it.todo("should require subject");
      it.todo("should accept optional description");
      it.todo("should accept optional priority");
      it.todo("should accept optional status");
      it.todo("should accept optional account_id");
      it.todo("should accept optional contact_id");
      it.todo("should accept optional fields object");
    });

    describe("update_case", () => {
      it.todo("should require case_id");
      it.todo("should require fields object");
    });

    describe("close_case", () => {
      it.todo("should require case_id");
      it.todo("should accept optional resolution");
    });

    describe("list_reports", () => {
      it.todo("should accept optional limit");
    });

    describe("run_report", () => {
      it.todo("should require report_id");
      it.todo("should accept optional filters object");
    });

    describe("get_report_metadata", () => {
      it.todo("should require report_id");
    });

    describe("list_users", () => {
      it.todo("should accept optional active filter");
      it.todo("should accept optional role filter");
      it.todo("should accept optional limit");
    });

    describe("get_user", () => {
      it.todo("should require user_id");
    });

    describe("bulk_insert", () => {
      it.todo("should require object_type");
      it.todo("should require records array");
    });

    describe("bulk_update", () => {
      it.todo("should require object_type");
      it.todo("should require records array with Id fields");
    });

    describe("bulk_delete", () => {
      it.todo("should require object_type");
      it.todo("should require record_ids array");
    });

    describe("list_metadata_types", () => {
      it.todo("should require no arguments");
    });

    describe("deploy_metadata", () => {
      it.todo("should require zip_path");
      it.todo("should accept optional check_only defaulting to false");
      it.todo("should accept optional test_level");
      it.todo("should accept optional run_tests array");
    });
  });

  describe("execute", () => {
    describe("query", () => {
      it.todo("should call Salesforce REST API correctly");
      it.todo("should return totalSize, done, records");
      it.todo("should handle errors");
    });

    describe("describe_object", () => {
      it.todo("should call Salesforce REST API correctly");
      it.todo("should return name, label, fields, recordTypeInfos");
      it.todo("should handle errors");
    });

    describe("create_record", () => {
      it.todo("should call Salesforce REST API correctly");
      it.todo("should return id, success, errors");
      it.todo("should handle errors");
    });

    describe("upsert_record", () => {
      it.todo("should call Salesforce REST API correctly");
      it.todo("should return id, success, created boolean");
      it.todo("should handle errors");
    });

    describe("convert_lead", () => {
      it.todo("should call Salesforce REST API correctly");
      it.todo("should return accountId, contactId, opportunityId, success");
      it.todo("should handle errors");
    });

    describe("run_report", () => {
      it.todo("should call Salesforce REST API correctly");
      it.todo("should return reportMetadata, factMap");
      it.todo("should handle errors");
    });

    describe("bulk_insert", () => {
      it.todo("should call Salesforce Bulk API correctly");
      it.todo("should return job_id, state, results");
      it.todo("should handle errors");
    });

    describe("deploy_metadata", () => {
      it.todo("should call Salesforce Metadata API correctly");
      it.todo("should return id, status, success");
      it.todo("should handle errors");
    });
  });
});
