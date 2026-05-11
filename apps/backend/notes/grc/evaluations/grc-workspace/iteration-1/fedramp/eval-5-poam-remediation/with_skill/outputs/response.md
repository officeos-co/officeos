# FedRAMP POA&M: Vulnerability Remediation Timeframes and Entry Guidance

*Use official FedRAMP templates from fedramp.gov — this content should be inserted into the official FedRAMP POA&M template (SSP Appendix O).*

---

## FedRAMP Vulnerability Remediation SLAs

FedRAMP mandates specific remediation timeframes based on vulnerability severity. These are not guidelines — they are enforceable SLAs, and exceeding them without an approved Deviation Request (DR) constitutes a compliance finding.

| Severity Level | CVSS Score Range | Remediation SLA | Notes |
|---|---|---|---|
| **Critical** | 9.0–10.0 | **30 calendar days** | Most urgent; active exploitation likely |
| **High** | 7.0–8.9 | **90 calendar days** | Significant risk; monitored closely by AO |
| **Moderate** | 4.0–6.9 | **180 calendar days** | 6 months from date of discovery |
| **Low** | 0.1–3.9 | **365 calendar days** | 1 year from date of discovery |

**Reference**: These SLAs are documented in the FedRAMP Continuous Monitoring Performance Management Guide.

### Key Points on SLA Calculation

- The clock starts from the **date of discovery** (typically the date the vulnerability scan first identifies the finding)
- SLAs apply to the **remediation date**, not the date a patch is released
- If a vendor has not released a patch (Vendor Dependency), document as a Vendor Dependency (VD) in the POA&M — this does not stop the SLA clock but provides context for the AO
- Exceeding an SLA without an approved Deviation Request (DR) requires immediate escalation to the AO

---

## Your Specific POA&M Item: Analysis

The finding "Vulnerability scan findings not remediated within required timeframes for Critical vulnerabilities" is itself a process/operational finding — it indicates that your vulnerability management program failed to remediate Critical findings within 30 days. This creates two distinct POA&M items:

1. **The original Critical vulnerability/vulnerabilities** that were not remediated — each should have its own POA&M row
2. **The process finding** — failure to meet the 30-day SLA — which may be raised as a separate control finding under RA-5 (Vulnerability Scanning) or SI-2 (Flaw Remediation)

---

## How to Write a Proper FedRAMP POA&M Entry

A FedRAMP POA&M entry must be a complete row in the official POA&M spreadsheet template. Below is a properly structured entry for both the process finding and an example Critical vulnerability finding.

### POA&M Entry: Process Finding (SLA Miss)

| Field | Value |
|---|---|
| **POA&M ID** | POA-2025-001 |
| **Control Identifier** | SI-2, RA-5 |
| **Weakness Name** | Critical vulnerability scan findings not remediated within FedRAMP-required 30-day timeframe |
| **Weakness Description** | Vulnerability scan results conducted on [scan date] identified Critical-severity findings (CVE-XXXX-XXXX) on [affected components]. These findings were not remediated within the FedRAMP-mandated 30-day SLA for Critical vulnerabilities, resulting in a remediation schedule overrun of [N] days. This represents a deficiency in the organization's vulnerability management process and flaw remediation procedures under SI-2 and RA-5. |
| **Source of Discovery** | Vulnerability Scan / 3PAO Annual Assessment / ConMon Review |
| **Date Discovered** | [YYYY-MM-DD] |
| **Original Due Date** | [Date discovered + 30 days] |
| **Revised Due Date** | [Updated realistic remediation date — must be approved by AO if past original SLA] |
| **Risk Level** | High |
| **Risk Adjustment** | None / Vendor Dependency / False Positive [select as applicable] |
| **Point of Contact** | [Name, Title] |
| **Resources Required** | Engineering team availability; patch testing environment; change management approval |
| **Milestone with Completion Dates** | 1. Root cause analysis complete — [date]; 2. Patches applied to Dev — [date]; 3. Patches applied to Prod — [date]; 4. Rescan confirming remediation — [date] |
| **Status** | Open — In Progress |
| **Comments / Vendor Dependency** | [If applicable: "Awaiting patch release from [Vendor] for CVE-XXXX-XXXX. Compensating control implemented: [describe control]."] |

---

### POA&M Entry: Individual Critical Vulnerability (Example)

| Field | Value |
|---|---|
| **POA&M ID** | POA-2025-002 |
| **Control Identifier** | SI-2 |
| **Weakness Name** | Critical vulnerability: [CVE-XXXX-XXXX] — [Affected component] |
| **Weakness Description** | A Critical-severity vulnerability (CVE-XXXX-XXXX, CVSS 9.8) was identified in [component/package] version [X.X] on [affected asset(s)]. This vulnerability allows [describe attack vector and impact]. The patch was available as of [date] but was not applied within the required 30-day remediation window. The affected component is [in/not in] the authorization boundary and processes/stores/transmits federal data. |
| **Source of Discovery** | Authenticated vulnerability scan — [scanner name, e.g., Tenable.sc, Qualys] |
| **Date Discovered** | [YYYY-MM-DD] |
| **Original Due Date** | [Date discovered + 30 days] |
| **Revised Due Date** | [Updated date; requires AO approval if past original SLA] |
| **Risk Level** | Critical |
| **Risk Adjustment** | None |
| **Point of Contact** | [Name, Title, Email] |
| **Resources Required** | Infrastructure team; patch testing; change control board approval; maintenance window scheduling |
| **Milestone with Completion Dates** | 1. Compensating control implemented — [date]; 2. Patch validated in staging — [date]; 3. Patch deployed to production — [date]; 4. Authenticated rescan confirming closure — [date] |
| **Compensating Controls** | [Describe interim controls: network segmentation, WAF rule, IDS signature, increased monitoring, etc.] |
| **Status** | Open — In Progress / Closed [select] |
| **Closure Evidence** | [When closed]: Rescan report date [date], scan reference [report ID], confirming CVE-XXXX-XXXX no longer detected on [asset(s)]. |

---

## Required Actions for This Finding

1. **Conduct root cause analysis** — Determine why Critical vulnerabilities were not remediated within 30 days (missing patch process, resource constraints, change management delays, etc.)

2. **Implement compensating controls immediately** — Until patches are applied, document compensating controls (e.g., network isolation of affected system, WAF rules blocking exploitation vectors, enhanced monitoring). Compensating controls do not stop the SLA clock but demonstrate good faith to the AO.

3. **Notify the AO** — SLA overruns for Critical vulnerabilities must be proactively reported to the Authorizing Official. Do not wait for the monthly ConMon report — contact the AO directly.

4. **Submit a Deviation Request (DR) if needed** — If remediation cannot be completed within the original timeframe (e.g., pending vendor patch), submit a formal DR to the AO for approval.

5. **Update the POA&M** — All open Critical findings must appear in the POA&M with updated milestone dates, status, and compensating controls. Submit with the next monthly ConMon package.

6. **Perform authenticated rescans** — After patching, conduct an authenticated vulnerability scan to confirm closure. Include the rescan report as closure evidence in the POA&M.

7. **Process improvement** — Document corrective actions to prevent recurrence: automated patch deployment pipelines, shorter change management SLAs for Critical patches, weekly vulnerability review meetings, etc.

---

## Deviation Request (DR) Process

If you cannot remediate within the required SLA:

| DR Type | When to Use |
|---|---|
| **Vendor Dependency (VD)** | No patch available from the vendor; you are waiting on a third-party fix |
| **Operational Requirement (OR)** | Remediation would break a critical operational function; risk accepted temporarily |
| **False Positive (FP)** | The scanner identified a vulnerability that does not actually exist on the system |
| **Risk Adjustment (RA)** | The finding's risk is overstated due to compensating controls or environmental factors |

All DRs require AO approval and must be documented in the POA&M with supporting evidence.
