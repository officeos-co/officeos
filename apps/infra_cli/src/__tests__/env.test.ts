import { expect, test } from "bun:test";
import { resolveApiUrl } from "../lib/env";

test("resolveApiUrl removes trailing slash", () => {
  expect(resolveApiUrl("https://api.example.com/")).toBe("https://api.example.com");
});
