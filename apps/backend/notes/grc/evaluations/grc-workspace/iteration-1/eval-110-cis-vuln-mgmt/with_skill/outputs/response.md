# CIS Control 7: Continuous Vulnerability Management — Detailed Walkthrough

**Source:** CIS Controls v8 (published May 2021, Center for Internet Security)
**Control Reference:** CIS Control 7, Safeguards 7.1–7.7

---

## Overview

CIS Control 7 requires organizations to develop a plan to continuously assess and track vulnerabilities across all enterprise assets within the network — both internally exposed and externally facing — in order to proactively identify and remediate weaknesses before they can be exploited. The control emphasizes *continuous* and *automated* processes, not one-time or infrequent manual efforts.

The control maps to:
- **NIST CSF 2.0:** ID.RA-1 (Risk Assessment), PR.IP-12 (Vulnerability Management)
- **ISO 27001:2022:** A.8.8 (Management of technical vulnerabilities)
- **CMMC 2.0:** RA.L2-3.11.x (Risk Assessment domain)

---

## All 7 Safeguards: Detail, IG Assignment, and Implementation Notes

### Safeguard 7.1 — Establish and Maintain a Vulnerability Management Process
- **Implementation Group:** IG1
- **Asset Type:** Devices / Applications
- **Security Function:** Protect

**What it requires:** A documented, repeatable vulnerability management process that covers scanning frequency, roles and responsibilities, asset scope, tooling, and escalation paths. This policy must be reviewed and updated at least annually.

**Why it matters:** Without a defined process, vulnerability identification and remediation happen inconsistently or reactively. This safeguard creates the governance foundation for all subsequent Control 7 activities.

**How to implement:**
1. Draft a Vulnerability Management Policy that defines: scanning schedule, asset scope (on-prem, cloud, remote), responsible parties, escalation procedures, and remediation SLAs.
2. Assign ownership — typically to a Security or IT Operations team.
3. Review and update the policy annually or after significant infrastructure changes.
4. Integrate with the change management and patch management processes.

---

### Safeguard 7.2 — Establish and Maintain a Remediation Process
- **Implementation Group:** IG1
- **Asset Type:** Devices / Applications
- **Security Function:** Respond

**What it requires:** A documented remediation process that specifies how vulnerabilities are prioritized, assigned, tracked, and verified as resolved. This includes defined SLAs by severity (see SLA table below) and a process for handling exceptions when remediation cannot occur within the SLA window.

**Why it matters:** Identifying vulnerabilities without a clear remediation workflow leads to finding-fatigue and backlog accumulation. This safeguard ensures discovered vulnerabilities are acted upon systematically.

**How to implement:**
1. Define CVSS-based severity tiers and associated remediation deadlines (see SLA table in next section).
2. Establish a ticketing workflow (e.g., in Jira, ServiceNow) to track each finding from discovery through remediation and verification.
3. Define a risk acceptance / exception process for vulnerabilities that cannot be remediated within SLA (requires business risk owner sign-off and compensating controls).
4. Measure and report Mean Time to Patch (MTTP) by severity as a KPI.

---

### Safeguard 7.3 — Perform Automated Operating System Patch Management
- **Implementation Group:** IG1
- **Asset Type:** Devices
- **Security Function:** Protect

**What it requires:** Automate the application of operating system patches and updates across all enterprise endpoints and servers. Manual, ad hoc patching is insufficient for IG1 compliance.

**Why it matters:** Unpatched operating systems are among the most common attack vectors exploited in real-world breaches. Automation ensures patches are applied consistently without relying on individual administrators.

**How to implement:**
1. Enable automatic OS updates for workstations (Windows Update, macOS Software Update) or manage via a patch management platform.
2. For servers and managed endpoints, use tools such as WSUS, SCCM/MECM, Intune, Ansible, or Puppet to centrally deploy patches.
3. Define a patching cadence: critical patches applied within 15 days; routine patches within 30 days.
4. Track patch compliance rates; target 95%+ for critical patches within SLA.

**Recommended Tools:** Microsoft Intune / MECM, Jamf, Ansible, PDQ Deploy, ManageEngine Patch Manager Plus

---

### Safeguard 7.4 — Perform Automated Application Patch Management
- **Implementation Group:** IG1
- **Asset Type:** Applications
- **Security Function:** Protect

**What it requires:** Automate patching of third-party applications (browsers, productivity suites, PDF readers, runtime environments such as Java and .NET, and other installed software) across all enterprise assets.

**Why it matters:** Application vulnerabilities (particularly in browsers, Office applications, and Java runtimes) are a primary initial access vector. OS-only patching leaves a large attack surface unaddressed. This safeguard extends automated patching to the application layer.

**How to implement:**
1. Inventory all applications requiring patching (CIS Control 2 provides the foundation).
2. Use a patch management tool that covers third-party applications in addition to OS patches.
3. Include browser updates (Chrome, Edge, Firefox), Microsoft Office, Adobe Acrobat, Java, .NET, and all other commonly installed applications.
4. Confirm cloud/SaaS applications are kept current by the provider; verify SLA commitments in contracts.

**Recommended Tools:** Qualys Patch Management, Ivanti Patch for Endpoints, NinjaRMM, ManageEngine, Automox

---

### Safeguard 7.5 — Perform Automated Vulnerability Scans of Internal Enterprise Assets
- **Implementation Group:** IG2
- **Asset Type:** Devices
- **Security Function:** Identify

**What it requires:** Deploy an authenticated vulnerability scanner and run automated scans of all internally exposed enterprise assets (workstations, servers, network devices, printers, and other networked systems). CIS guidance recommends **weekly** authenticated scans of internal assets at IG2.

**Why it matters:** Automated OS and application patching (Safeguards 7.3 and 7.4) addresses known patch channels, but vulnerability scanning identifies a broader set of weaknesses including misconfigurations, missing patches that automated tools missed, default credentials, and newly discovered CVEs. Scanning provides ground-truth visibility into the actual security posture of assets.

**How to implement:**
1. Deploy a vulnerability scanner with credentialed scan capabilities (authenticated scans using domain credentials yield significantly more accurate results than unauthenticated scans).
2. Configure scan profiles to cover all asset classes: servers, workstations, network devices, and where possible, IoT/OT.
3. Schedule scans weekly at a minimum (not quarterly — see the IG2 sufficiency analysis below).
4. Feed scan results into a centralized vulnerability management platform for tracking, prioritization, and reporting.
5. Integrate scan coverage with the asset inventory (CIS Control 1) to detect unscanned assets.

**Recommended Tools:** Tenable Nessus / Tenable.io, Qualys VMDR, Rapid7 InsightVM, Microsoft Defender Vulnerability Management, CrowdStrike Spotlight

---

### Safeguard 7.6 — Perform Automated Vulnerability Scans of Externally Exposed Enterprise Assets
- **Implementation Group:** IG2
- **Asset Type:** Devices / Network
- **Security Function:** Identify

**What it requires:** Conduct automated vulnerability scans of all externally exposed assets — public-facing web servers, APIs, remote access infrastructure (VPN gateways, RDP), DNS servers, mail servers, and cloud-hosted services. CIS guidance recommends **monthly** scans of external assets at IG2.

**Why it matters:** Externally exposed assets present the most immediate risk because they are reachable by adversaries globally. A critical vulnerability on an internet-facing server can be discovered and exploited within hours of a CVE being published. Monthly external scanning is the minimum cadence that provides timely awareness of newly discovered exposures.

**How to implement:**
1. Maintain an inventory of all externally accessible IP addresses, domains, and cloud-hosted services (attack surface inventory).
2. Run monthly unauthenticated scans from an external vantage point to simulate attacker perspective.
3. Also run authenticated scans where possible (e.g., against web applications and cloud workloads).
4. Consider adding continuous external attack surface management (EASM) tools to detect newly exposed assets between scheduled scans.
5. Prioritize findings on external assets for faster remediation (treat external Critical/High findings as higher urgency).

**Recommended Tools:** Tenable.io, Qualys External Scanning, Rapid7 InsightVM, Detectify, Censys, Shodan (for exposure monitoring), UpGuard

---

### Safeguard 7.7 — Remediate Detected Vulnerabilities
- **Implementation Group:** IG2
- **Asset Type:** Devices / Applications
- **Security Function:** Respond

**What it requires:** Act on the findings produced by vulnerability scans (Safeguards 7.5 and 7.6). Vulnerabilities must be remediated, mitigated, or formally risk-accepted within the SLA timeframes defined in Safeguard 7.2, based on CVSS severity. This safeguard closes the loop between identification and remediation.

**Why it matters:** Scanning without remediating is meaningless from a security standpoint and can create legal and compliance risk (demonstrating knowledge of a vulnerability without action). This safeguard operationalizes the remediation workflow.

**How to implement:**
1. Integrate scanner output with a ticketing system for automated assignment to system owners.
2. Apply CVSS-based remediation SLAs (see table below).
3. Track open vulnerabilities aging past SLA; escalate to management for risk acceptance decisions.
4. Verify remediation by re-scanning after patches are applied.
5. Report remediation metrics (MTTP by severity, vulnerability backlog trend) to leadership monthly.

---

## Remediation SLAs by CVSS Severity

CIS Control 7 (Safeguard 7.2) requires organizations to define a remediation process with severity-based timeframes. The following SLAs represent the CIS-recommended standards and align with common industry practice and regulatory frameworks (NIST SP 800-40, PCI DSS, and others):

| CVSS Severity | Score Range | Recommended Remediation SLA | Notes |
|---------------|-------------|------------------------------|-------|
| **Critical** | 9.0 – 10.0 | **15 days** | Highest urgency; exploits frequently available within days of CVE publication; consider emergency change procedures |
| **High** | 7.0 – 8.9 | **30 days** | Significant risk; patch within one monthly patch cycle |
| **Medium** | 4.0 – 6.9 | **90 days** | Address within one quarter; compensating controls may be appropriate while patch is scheduled |
| **Low** | 0.1 – 3.9 | **180 days or risk-accepted** | Address opportunistically; document risk acceptance where remediation is deferred |

**Important notes on SLA application:**
- **Externally exposed assets:** Apply accelerated SLAs. A Critical vulnerability on an internet-facing system should be treated as a 7-day SLA, not 15 days, due to heightened exploitability.
- **CVSS vs. EPSS:** CVSS score alone does not reflect actual exploitability. Where possible, supplement CVSS with EPSS (Exploit Prediction Scoring System) scores and CISA KEV (Known Exploited Vulnerabilities) catalog membership to further prioritize. A CVSSv3 Medium vulnerability that is actively exploited in the wild may warrant Critical-level urgency.
- **SLA exceptions:** Document risk acceptance decisions with business owner sign-off, compensating controls, and a defined review date. Do not allow exceptions to accumulate silently.

**IG2 KPI Targets (from CIS guidance):**
- Mean Time to Patch (MTTP) — Critical: ≤ 15 days
- Mean Time to Patch (MTTP) — High: ≤ 30 days

---

## Complete Safeguard Summary Table

| Safeguard | Title | IG | Asset Type | Security Function |
|-----------|-------|----|-----------|------------------|
| 7.1 | Establish and Maintain a Vulnerability Management Process | **IG1** | Devices / Apps | Protect |
| 7.2 | Establish and Maintain a Remediation Process | **IG1** | Devices / Apps | Respond |
| 7.3 | Perform Automated OS Patch Management | **IG1** | Devices | Protect |
| 7.4 | Perform Automated Application Patch Management | **IG1** | Applications | Protect |
| 7.5 | Perform Automated Vulnerability Scans of Internal Assets | **IG2** | Devices | Identify |
| 7.6 | Perform Automated Vulnerability Scans of External Assets | **IG2** | Devices / Network | Identify |
| 7.7 | Remediate Detected Vulnerabilities | **IG2** | Devices / Apps | Respond |

---

## Are Quarterly Vulnerability Scans Sufficient for IG2?

**Direct answer: No. Quarterly vulnerability scans do not meet the IG2 requirements of CIS Control 7.**

Here is the specific gap analysis:

### What IG2 Requires for Scanning Frequency

| Safeguard | Asset Scope | CIS-Required Minimum Frequency |
|-----------|-------------|-------------------------------|
| 7.5 | Internal assets | **Weekly** (authenticated) |
| 7.6 | External assets | **Monthly** |

### Your Current State vs. IG2 Requirements

| Dimension | Your Current State | IG2 Requirement | Gap |
|-----------|-------------------|-----------------|-----|
| Internal scan frequency | Quarterly (every ~13 weeks) | Weekly | **Non-compliant — scanning 12x less frequently than required** |
| External scan frequency | Quarterly (assumed) | Monthly | **Non-compliant — scanning 3x less frequently than required** |
| Authenticated scanning | Unknown | Required for internal scans | Verify if scans use credentials |
| Remediation tracking | Unknown | Required (Safeguard 7.7) | Assess current tracking process |

### Why Quarterly Scanning Is Insufficient

1. **Vulnerability disclosure pace:** The NVD (National Vulnerability Database) publishes thousands of new CVEs every month. A vulnerability published on Day 1 of your quarter could remain undetected in your environment for up to 90 days with quarterly scanning.

2. **SLA compliance is impossible:** Your 15-day SLA for Critical vulnerabilities (Safeguard 7.2 / IG1) cannot be honored if scans only run quarterly — you may not discover a Critical vulnerability until it is 13 weeks old.

3. **IG2 explicitly specifies scan frequency:** The CIS implementation guidance for IG2 is unambiguous: weekly internal scans (Safeguard 7.5) and monthly external scans (Safeguard 7.6). Quarterly scanning represents a significant control gap for any IG2 compliance claim.

4. **Regulatory alignment:** Quarterly scanning also falls short of expectations in PCI DSS (quarterly *minimum* for external, but continuous monitoring recommended), HIPAA risk management requirements, and most cyber insurance baseline controls.

### Recommended Remediation Path

**Phase 1 — Immediate (0–30 days):**
- Increase internal scanning to weekly using an authenticated vulnerability scanner.
- Increase external scanning to monthly at minimum; consider continuous external attack surface management tooling for ongoing visibility.
- Ensure all scan profiles are using authenticated credentials for internal scans (unauthenticated scans miss a significant portion of vulnerabilities).

**Phase 2 — Short-term (30–60 days):**
- Integrate scanner output with a ticketing system for automated assignment and SLA tracking (supports Safeguard 7.7).
- Define and document remediation SLAs formally in a Vulnerability Management Policy (Safeguards 7.1 and 7.2).
- Establish a remediation KPI dashboard tracking MTTP by severity.

**Phase 3 — Ongoing:**
- Report monthly on vulnerability backlog, scan coverage, and MTTP metrics.
- Review the vulnerability management policy annually.
- Consider expanding to continuous scanning or agent-based approaches (e.g., Tenable.io agents, Qualys Cloud Agent, CrowdStrike Spotlight) to achieve near-real-time visibility without the overhead of scheduled network scans.

### Recommended Tooling for IG2 Vulnerability Scanning

| Tool | Strength | Deployment |
|------|---------|-----------|
| Tenable Nessus / Tenable.io | Industry standard; broad CVE coverage | On-prem scanner or cloud |
| Qualys VMDR | Cloud-native; strong asset correlation | Cloud-hosted |
| Rapid7 InsightVM | Strong remediation workflow integration | On-prem or cloud |
| Microsoft Defender Vulnerability Management | Native Windows integration; no separate agent | Microsoft ecosystem |
| CrowdStrike Spotlight | Agent-based; no network scan required | EDR-integrated |

---

## Key Takeaways

1. **CIS Control 7 has 7 safeguards:** Four are IG1 requirements (process, remediation, OS patching, app patching) and three are IG2 requirements (internal scanning, external scanning, remediation closure).

2. **IG1 organizations** must automate patching and define a remediation process, but are not required to run vulnerability scans — that requirement is introduced at IG2.

3. **IG2 organizations** must run weekly authenticated internal scans, monthly external scans, and formally track and remediate all findings within CVSS-based SLAs.

4. **Quarterly scanning fails IG2** by a significant margin: it is 12x below the internal scan frequency requirement and 3x below the external scan frequency requirement.

5. **Remediation SLAs:** Critical = 15 days, High = 30 days, Medium = 90 days, Low = 180 days. Externally exposed assets should use accelerated timelines.

6. **Scanning without remediation tracking is non-compliant:** Safeguard 7.7 requires that scan findings are acted upon, tracked, and verified — not just identified.

---

*References: CIS Controls v8, Center for Internet Security (May 2021). Safeguard details per CIS Controls v8 safeguards-detail reference. Implementation guidance per CIS Controls v8 implementation-guidance reference.*
