# CIS Controls v8 — Control 7: Continuous Vulnerability Management

## Overview

CIS Control 7 focuses on continuously acquiring, assessing, and taking action on new information about vulnerabilities to identify, remediate, and minimize the window of opportunity for attackers. The intent is to proactively manage the attack surface by understanding what vulnerabilities exist across the enterprise's assets and systematically reducing exposure before adversaries can exploit them.

---

## All Safeguards Under Control 7

CIS Controls v8 defines seven safeguards (7.1 through 7.7) under Control 7. Each is assigned to an Implementation Group (IG1, IG2, or IG3), which define cumulative tiers of security maturity.

---

### Safeguard 7.1 — Establish and Maintain a Vulnerability Management Process

**Implementation Group: IG1**

Establish and maintain a documented vulnerability management process for enterprise assets. Review and update the process annually, or whenever significant changes occur that could affect this Safeguard.

**Key requirements:**
- A formal, written process must exist
- It must cover the full lifecycle: discovery, prioritization, remediation, verification
- Annual review cadence (at minimum)

**Asset type:** All

---

### Safeguard 7.2 — Establish and Maintain a Remediation Process

**Implementation Group: IG1**

Establish and maintain a risk-based remediation strategy documented in a remediation process, with monthly, or more frequent, reviews.

**Key requirements:**
- Defines how vulnerabilities are triaged and prioritized (typically using CVSS scores or similar)
- Defines ownership and accountability for remediation
- Monthly review cadence (at minimum)

**Asset type:** All

---

### Safeguard 7.3 — Perform Automated Operating System Patch Management

**Implementation Group: IG1**

Perform operating system updates on enterprise assets through automated patch management on a monthly, or more frequent, basis.

**Key requirements:**
- Applies to all enterprise assets (servers, workstations, mobile devices, etc.)
- Automated tooling strongly preferred
- Monthly cadence is the floor — more frequent is better

**Asset type:** Devices (endpoints, servers)

---

### Safeguard 7.4 — Perform Automated Application Patch Management

**Implementation Group: IG1**

Perform application updates on enterprise assets through automated patch management on a monthly, or more frequent, basis.

**Key requirements:**
- Covers third-party and enterprise-developed applications
- Monthly cadence (at minimum)
- Automated patch deployment tooling required

**Asset type:** Applications

---

### Safeguard 7.5 — Perform Automated Vulnerability Scans of Internal Enterprise Assets

**Implementation Group: IG2**

Perform automated vulnerability scans of internal enterprise assets on a quarterly, or more frequent, basis. Conduct both authenticated and unauthenticated scans using a SCAP-compliant vulnerability scanning tool.

**Key requirements:**
- Authenticated scans strongly preferred (they yield far more complete results than unauthenticated)
- SCAP-compliant tooling (e.g., Tenable Nessus, Qualys, Rapid7 InsightVM, OpenVAS)
- Quarterly is the minimum frequency — not the recommended frequency
- Covers all internal assets (servers, workstations, network devices)

**Asset type:** Devices, network infrastructure

---

### Safeguard 7.6 — Perform Automated Vulnerability Scans of Externally-Exposed Enterprise Assets

**Implementation Group: IG2**

Perform automated vulnerability scans of externally exposed enterprise assets using a SCAP-compliant vulnerability scanning tool. Perform scans on a monthly, or more frequent, basis.

**Key requirements:**
- Monthly cadence (more stringent than internal scans)
- Covers internet-facing assets: web applications, APIs, VPN concentrators, mail servers, etc.
- Must use authenticated scans where technically feasible
- External attack surface is higher risk, hence the tighter cadence

**Asset type:** Network infrastructure (external-facing)

---

### Safeguard 7.7 — Remediate Detected Vulnerabilities

**Implementation Group: IG2**

Remediate detected vulnerabilities in software through processes and tooling on a monthly, or more frequent, basis, based on the remediation process.

**Key requirements:**
- Remediation must follow a defined, risk-based process (see Safeguard 7.2)
- Monthly cadence as the baseline for action
- Tracking through a vulnerability management platform or ticketing system
- Verification of remediation (re-scanning, validation)

**Asset type:** All

---

## Safeguard Summary by Implementation Group

| Safeguard | Description | IG |
|-----------|-------------|-----|
| 7.1 | Establish and maintain a vulnerability management process | IG1 |
| 7.2 | Establish and maintain a remediation process | IG1 |
| 7.3 | Perform automated OS patch management | IG1 |
| 7.4 | Perform automated application patch management | IG1 |
| 7.5 | Automated vulnerability scans — internal assets (quarterly+) | IG2 |
| 7.6 | Automated vulnerability scans — external assets (monthly+) | IG2 |
| 7.7 | Remediate detected vulnerabilities (monthly cadence) | IG2 |

**Note:** There are no IG3-exclusive safeguards in Control 7. IG3 organizations implement all seven safeguards above with greater rigor, tooling integration, and often higher scan frequency.

---

## CIS-Recommended Remediation SLAs by CVSS Severity

CIS does not publish a single rigid SLA table within the Controls v8 document itself — the Controls framework delegates SLA specifics to the remediation process defined in Safeguard 7.2. However, CIS and the broader cybersecurity community (including CIS Benchmarks guidance, CISA KEV directives, and common industry practice aligned with CIS guidance) converge on the following widely-cited SLA framework:

| CVSS Score Range | Severity | Recommended Remediation SLA |
|------------------|----------|-----------------------------|
| 9.0 – 10.0 | Critical | 15 days (often cited as 7–15 days for internet-facing assets) |
| 7.0 – 8.9 | High | 30 days |
| 4.0 – 6.9 | Medium | 60–90 days |
| 0.1 – 3.9 | Low | 180 days or next scheduled maintenance cycle |
| 0.0 | Informational | Risk acceptance / no required remediation window |

**Important caveats:**

1. **CISA KEV override:** Any vulnerability listed on CISA's Known Exploited Vulnerabilities (KEV) catalog should be treated as Critical regardless of its CVSS base score, with a 14-day remediation window for federal systems (FCEB agencies under BOD 22-01). Non-federal organizations should adopt a similar posture for KEV-listed vulnerabilities.

2. **Internet-facing assets:** CIS guidance and industry practice consistently compress SLAs for externally exposed assets. A High-severity vulnerability on an internet-facing asset should be remediated in 15 days, not 30.

3. **CVSS v3.x vs. base scores:** CVSS base scores alone are insufficient for prioritization. Organizations should incorporate temporal scores (exploit code maturity, remediation level, report confidence) and environmental scores (asset criticality, exposure) when setting actual SLAs.

4. **Exploit availability:** If a public exploit exists (e.g., via Metasploit, ExploitDB, or CISA KEV), SLAs should be compressed regardless of CVSS score.

---

## Are Quarterly Internal Scans Sufficient for IG2?

**Short answer: Technically yes, but practically insufficient and not recommended.**

### What CIS Says

Safeguard 7.5 states: "quarterly, or more frequent." So quarterly scanning of internal assets technically meets the minimum letter of the IG2 requirement for internal scans.

However, **quarterly scans alone are not sufficient to meet the full spirit of IG2**, for the following reasons:

### Why Quarterly Is Inadequate in Practice

**1. External assets require monthly scans (Safeguard 7.6)**
If your organization has any internet-facing assets — and virtually every IG2 organization does — those assets must be scanned at least monthly. Quarterly scans of external assets would be a direct compliance gap.

**2. The vulnerability landscape changes faster than quarterly**
New critical and high vulnerabilities are disclosed daily. The average time-to-exploit for critical vulnerabilities has dropped to under 15 days in recent years. A vulnerability disclosed on day 1 of a quarter may go undetected for up to 90 days if you only scan quarterly. This window directly contradicts the SLAs for Critical (15 days) and High (30 days) vulnerabilities — you cannot remediate what you have not discovered.

**3. Safeguard 7.7 requires monthly remediation cycles**
Even if scanning is quarterly, Safeguard 7.7 requires you to perform remediation on a monthly basis. If you're only scanning quarterly, you have no new findings to act on for three months at a time, which means your monthly remediation process is running on stale data.

**4. Patch management safeguards (7.3, 7.4) are monthly**
IG1 safeguards 7.3 and 7.4 require monthly automated patching. If you are patching monthly but only scanning quarterly, you have no mechanism to verify that patches were applied successfully or to catch vulnerabilities introduced between scans.

**5. Authenticated vs. unauthenticated scanning**
CIS requires both authenticated and unauthenticated scans. If your quarterly scans are unauthenticated-only, you are likely missing 40–60% of actual vulnerability findings, which is a significant compliance and risk gap.

### Recommended Posture for IG2

| Asset Category | Minimum CIS Cadence | Recommended Best Practice |
|----------------|---------------------|---------------------------|
| Internal assets | Quarterly | Monthly |
| External/internet-facing assets | Monthly | Weekly or continuous |
| Critical systems (servers, DC, databases) | Monthly | Weekly |
| Post-change scanning | As-needed | Within 24–48 hours of any significant change |

### Remediation Gap Analysis

If you run quarterly scans only, your actual exposure timeline looks like this:

- Vulnerability disclosed: Day 1
- Next scan detects it: Up to Day 90
- Remediation begins: Day 90+
- Remediation completed: Day 90 + your remediation cycle

For a Critical vulnerability, the remediation SLA (15 days) would be violated in nearly every scenario. For High vulnerabilities (30-day SLA), the same is true.

---

## Recommendations for IG2 Compliance

1. **Immediately move external asset scans to monthly** — this is a hard IG2 requirement (Safeguard 7.6), not a recommendation.

2. **Move internal scans to monthly** — quarterly is the CIS floor, not the target. Monthly internal scans dramatically reduce your exposure window and align with your monthly patching and remediation cycles.

3. **Implement authenticated scanning** — ensure all scans use credentials to get complete vulnerability coverage. Unauthenticated scans miss local privilege escalation flaws, misconfigurations, and software inventory gaps.

4. **Establish a vulnerability tracking process** — use a vulnerability management platform (Tenable, Qualys, Rapid7, etc.) or at minimum a tracked ticketing workflow that maps findings to remediation owners with due dates tied to the SLA table above.

5. **Subscribe to CISA KEV notifications** — treat KEV-listed vulnerabilities as P0 regardless of CVSS score. Integrate KEV status into your prioritization workflow.

6. **Implement continuous monitoring for critical assets** — for your most sensitive systems, consider agent-based scanning or continuous monitoring tools that provide real-time visibility rather than point-in-time scans.

7. **Close the loop with re-scanning** — after remediation, verify closure with a follow-up scan within 5–7 days. Do not mark vulnerabilities as remediated based solely on a ticket closure without technical verification.

---

## Summary

CIS Control 7 contains seven safeguards spanning IG1 (7.1–7.4) and IG2 (7.5–7.7). The framework requires monthly external scans, quarterly-minimum internal scans, and monthly remediation cycles. Recommended SLAs range from 15 days for Critical vulnerabilities to 180 days for Low. Quarterly internal scans technically satisfy the minimum IG2 bar for internal assets only, but fall short for external assets, cannot support the required remediation SLAs for Critical and High findings, and represent a significant operational risk gap. Organizations serious about IG2 compliance should target monthly internal scans and weekly or continuous external scans as their operational baseline.
