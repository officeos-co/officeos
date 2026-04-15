import { describe, it } from "bun:test";

describe("services", () => {
  describe("list_services", () => {
    it.todo("should list services and map external IPs from loadBalancer ingress");
    it.todo("should format ports as port/protocol strings");
  });

  describe("get_service", () => {
    it.todo("should fetch service and its endpoints");
    it.todo("should return empty endpoints array when endpoint fetch fails");
  });

  describe("create_service", () => {
    it.todo("should POST ClusterIP service manifest with parsed selector");
    it.todo("should default targetPort to port when not provided");
  });

  describe("delete_service", () => {
    it.todo("should DELETE /api/v1/namespaces/{ns}/services/{name}");
  });
});
