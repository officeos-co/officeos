import { describe, it } from "bun:test";

describe("azure skill", () => {
  describe("resource_groups_list", () => {
    it.todo("should POST { command: 'az', args: ['group', 'list', ...] } to proxy_url");
    it.todo("should include --subscription when param is provided");
    it.todo("should throw when proxy returns non-2xx status");
    it.todo("should throw when az exitCode is non-zero");
  });

  describe("resource_groups_create", () => {
    it.todo("should include --name and --location flags");
    it.todo("should return parsed resource group JSON");
  });

  describe("resource_groups_delete", () => {
    it.todo("should include --yes flag to skip confirmation prompt");
  });

  describe("vms_list", () => {
    it.todo("should call 'vm list' without resource group when not provided");
    it.todo("should include --resource-group when param is provided");
  });

  describe("vms_describe", () => {
    it.todo("should include vm name and resource group in args");
  });

  describe("vms_create", () => {
    it.todo("should default image to Ubuntu2204");
    it.todo("should default size to Standard_B1s");
    it.todo("should include --location when param is provided");
  });

  describe("vms_start", () => {
    it.todo("should call 'vm start' with name and resource group");
  });

  describe("vms_stop", () => {
    it.todo("should call 'vm stop' with name and resource group");
  });

  describe("vms_deallocate", () => {
    it.todo("should call 'vm deallocate' with name and resource group");
  });

  describe("webapps_list", () => {
    it.todo("should call 'webapp list' with optional resource group filter");
  });

  describe("webapps_describe", () => {
    it.todo("should include webapp name and resource group in args");
  });

  describe("webapps_create", () => {
    it.todo("should include --plan and --runtime flags");
  });

  describe("webapps_deploy", () => {
    it.todo("should default deploy_type to zip");
    it.todo("should include --type and --src-path flags");
  });

  describe("storage_accounts_list", () => {
    it.todo("should call 'storage account list' with optional resource group filter");
  });

  describe("storage_containers_list", () => {
    it.todo("should include --account-name flag");
  });

  describe("storage_blobs_list", () => {
    it.todo("should include --prefix flag when prefix is provided");
    it.todo("should include account-name and container-name flags");
  });

  describe("sql_servers_list", () => {
    it.todo("should call 'sql server list' with optional resource group filter");
  });

  describe("sql_databases_list", () => {
    it.todo("should include --server and --resource-group flags");
  });

  describe("aks_list", () => {
    it.todo("should call 'aks list' with optional resource group filter");
  });

  describe("aks_describe", () => {
    it.todo("should include cluster name and resource group in args");
  });

  describe("aks_get_credentials", () => {
    it.todo("should call 'aks get-credentials' with name and resource group");
  });

  describe("functionapp_list", () => {
    it.todo("should call 'functionapp list' with optional resource group filter");
  });

  describe("functionapp_create", () => {
    it.todo("should include --consumption-plan-location and --runtime flags");
    it.todo("should include --storage-account flag");
  });
});
