import { afterEach, expect, test } from "bun:test";
import { getMe } from "../features/auth/api/auth-api";
import {
  getResourceLogs,
  listModels,
  listResources,
} from "../features/control-plane/api/control-plane-api";
import { validateManifest } from "../features/manifests/api/manifests-api";

const originalFetch = globalThis.fetch;

afterEach(() => {
  globalThis.fetch = originalFetch;
});

test("control-plane API calls backend v1 resource routes", async () => {
  const requests: Array<{ url: string; method?: string }> = [];
  mockFetch(requests, []);

  await listModels("http://localhost:5000/", "token");
  await listResources("http://localhost:5000/", "token", "agents");
  await getResourceLogs("http://localhost:5000/", "token", "agents", "alice", {
    tail: 50,
    since: "10m",
    type: "message-out",
    severity: "error",
  });

  expect(requests).toEqual([
    { url: "http://localhost:5000/api/v1/models", method: "GET" },
    { url: "http://localhost:5000/api/v1/resources/agents", method: "GET" },
    {
      url: "http://localhost:5000/api/v1/resources/agents/alice/logs?tail=50&since=10m&type=message-out&severity=error",
      method: "GET",
    },
  ]);
});

test("auth API calls backend v1 identity route", async () => {
  const requests: Array<{ url: string; method?: string }> = [];
  mockFetch(requests, { id: "user-1", email: "user@example.com" });

  await getMe("http://localhost:5000", "token");

  expect(requests).toEqual([
    { url: "http://localhost:5000/api/v1/me", method: "GET" },
  ]);
});

test("API client summarizes HTML error responses", async () => {
  globalThis.fetch = (() =>
    Promise.resolve(
      new Response(
        "<!DOCTYPE html><html><head><title>officeos.co | 502: Bad gateway</title></head></html>",
        {
          status: 502,
          statusText: "Bad Gateway",
          headers: { "content-type": "text/html" },
        },
      ),
    )) as typeof fetch;

  await expect(getMe("http://localhost:5000", "token")).rejects.toThrow(
    "502 Bad Gateway (officeos.co | 502: Bad gateway)",
  );
});

test("manifest API calls backend v1 manifest route", async () => {
  const requests: Array<{ url: string; method?: string }> = [];
  mockFetch(requests, { valid: true, errors: [], resources: [] });

  await validateManifest("http://localhost:5000", "token", "kind: Agent");

  expect(requests).toEqual([
    { url: "http://localhost:5000/api/v1/manifests/validate", method: "POST" },
  ]);
});

function mockFetch(
  requests: Array<{ url: string; method?: string }>,
  body: unknown,
): void {
  globalThis.fetch = ((input: URL | RequestInfo, init?: RequestInit) => {
    requests.push({
      url: String(input),
      method: init?.method ?? "GET",
    });

    return Promise.resolve(
      new Response(JSON.stringify(body), {
        status: 200,
        headers: { "content-type": "application/json" },
      }),
    );
  }) as typeof fetch;
}
