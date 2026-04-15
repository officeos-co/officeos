import { describe, it } from "bun:test";

describe("ssl", () => {
  describe("list_certificates", () => {
    it.todo("should call /zones/:id/ssl/certificate_packs");
    it.todo("should map certificate_authority to issuer");
  });

  describe("get_ssl_settings", () => {
    it.todo("should fetch ssl, min_tls_version, and tls_1_3 settings in parallel");
  });

  describe("update_ssl_settings", () => {
    it.todo("should PATCH each provided setting independently");
    it.todo("should return updated settings after patching");
  });
});
