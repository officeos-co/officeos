# Security Policy

## Supported Versions

The `main` branch receives security fixes. Released versions are supported only when they are documented as active releases.

## Reporting a Vulnerability

Do not report vulnerabilities in public GitHub issues, discussions, or pull requests.

Report security issues privately through GitHub's private vulnerability reporting if it is enabled for this repository. If that is unavailable, contact the repository maintainers privately and include enough detail to reproduce and assess the issue.

Useful report details:

- Affected component, package, or deployment mode.
- Impact and realistic attack scenario.
- Reproduction steps or proof of concept.
- Relevant logs, screenshots, or configuration snippets with secrets removed.
- Suggested fix or mitigation, if known.

## Scope

Security-sensitive areas include:

- Agent sandboxing, pod execution, shell access, and workspace isolation.
- Credential storage, provider keys, MCP server credentials, and integration secrets.
- Authentication, authorization, tenant boundaries, and team access.
- Browser automation, network access, and SSRF-style paths.
- Structured logs, memory, transcripts, files, and other sensitive agent data.
- Kubernetes, Docker, and deployment manifests.

## Response

Maintainers will acknowledge valid reports as soon as practical, investigate the impact, and coordinate a fix before public disclosure when needed. Please avoid public disclosure until maintainers have had a reasonable opportunity to ship a mitigation.
