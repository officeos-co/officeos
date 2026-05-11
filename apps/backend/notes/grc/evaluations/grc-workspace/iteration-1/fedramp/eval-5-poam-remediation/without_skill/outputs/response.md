# FedRAMP POA&M: Vulnerability Remediation Timeframes and Entry Guidance

## FedRAMP Vulnerability Remediation Timeframes

FedRAMP specifies mandatory remediation timeframes for vulnerabilities based on severity. These are binding SLAs that apply to all authorized Cloud Service Providers (CSPs).

| Severity | Remediation Timeframe | Notes |
|---|---|---|
| **Critical** | **30 days** | From date of discovery; must be prioritized immediately |
| **High** | **90 days** | Must be tracked in POA&M if not remediated within this window |
| **Moderate** | **180 days** | Six months from discovery |
| **Low** | **365 days** | One year from discovery |

**Important notes on timeframes:**
- The clock starts from the date the vulnerability is first identified (typically the scan date)
- Remediation means the vulnerability is actually fixed and confirmed via rescan — not just that a patch was scheduled
- If a vendor patch is not yet available, document as a Vendor Dependency but this does not pause the SLA
- Exceeding the remediation SLA without an approved Deviation Request is a compliance violation that must be reported to the Authorizing Official (AO)

---

## Understanding Your POA&M Item

Your POA&M item — "Vulnerability scan findings not remediated within required timeframes for Critical vulnerabilities" — indicates that one or more Critical vulnerabilities went beyond the 30-day remediation window. This is a significant finding because:

1. It represents an actual security risk (unpatched Critical vulnerabilities)
2. It represents a process failure (vulnerability management program not meeting FedRAMP SLAs)
3. It must be reported to and managed with your Authorizing Official

---

## How to Write a Proper POA&M Entry

FedRAMP POA&M entries must use the official FedRAMP POA&M template. Each finding gets its own row with specific required fields.

### Required POA&M Fields

**For your Critical vulnerability SLA miss, here is a sample entry:**

---

**POA&M Item ID**: POA-2025-001

**Control Number**: SI-2 (Flaw Remediation), RA-5 (Vulnerability Scanning)

**Weakness Name**: Critical vulnerability not remediated within 30-day FedRAMP SLA

**Weakness Description**: Vulnerability scan conducted on [scan date] identified a Critical-severity vulnerability (CVE-[YEAR]-[NUMBER]) affecting [component/asset name, e.g., "web application servers running Apache version X.X"]. The vulnerability has a CVSS score of [score] and allows [brief description of attack, e.g., "remote code execution by unauthenticated attackers"]. The 30-day remediation SLA expired on [due date] without the vulnerability being remediated due to [reason, e.g., "patch testing delays and change management scheduling constraints"]. The affected component processes and stores federal data and is within the authorization boundary.

**Source of Discovery**: Vulnerability scan — [scanner name and version, e.g., Tenable Nessus, Qualys]

**Date Discovered**: [YYYY-MM-DD]

**Scheduled Completion Date**: [New realistic completion date — must be reviewed/approved by AO]

**Milestones with Completion Dates**:
- Milestone 1: Apply patch to development/staging environment — [date]
- Milestone 2: Complete patch validation testing — [date]
- Milestone 3: Apply patch to production environment — [date]
- Milestone 4: Conduct authenticated vulnerability rescan to confirm remediation — [date]

**Risk Level**: Critical

**Point of Contact**: [Name, Title, Email]

**Resources Required**: [Engineering hours, testing environment, change management approval, maintenance window]

**Status**: Open — In Progress

**Compensating Controls**: [Describe any interim mitigations applied while remediation is pending, e.g., "Network ACLs implemented to restrict access to the affected service from external sources; IDS signatures deployed to detect exploitation attempts; increased log monitoring for signs of exploitation."]

---

## Steps to Address This Finding

1. **Immediate notification**: Contact your AO immediately. Do not wait for the monthly ConMon report — Critical SLA misses should be proactively disclosed.

2. **Implement compensating controls now**: Until the patch is applied, put mitigating controls in place and document them in the POA&M.

3. **Prioritize remediation**: Schedule emergency patching if needed. For Critical vulnerabilities, standard change management processes should have an expedited track.

4. **Submit a Deviation Request if needed**: If a vendor patch is not available, submit a Deviation Request (Vendor Dependency type) to your AO documenting the situation and your compensating controls.

5. **Rescan after remediation**: After applying the patch, run an authenticated vulnerability scan to confirm the vulnerability is gone. Include the rescan report as closure evidence.

6. **Root cause and process improvement**: Document what went wrong in your vulnerability management process and what changes you are making to prevent recurrence. Your AO will likely ask for this.

7. **Update monthly ConMon reports**: Include this finding and its status in every monthly ConMon package until it is closed.
