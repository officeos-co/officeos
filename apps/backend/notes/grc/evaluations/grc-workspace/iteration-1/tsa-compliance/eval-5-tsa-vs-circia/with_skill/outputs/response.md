# TSA Cybersecurity Requirements vs. CISA CIRCIA: What Pipeline Operators Need to Know

## Overview

Yes, pipeline operators may need to comply with **both** TSA Security Directive requirements and the forthcoming CIRCIA (Cyber Incident Reporting for Critical Infrastructure Act of 2022) requirements. They are separate regulatory frameworks with different legal bases, different responsible agencies, and different — but overlapping — obligations. Here is how they differ and how they interact.

---

## Framework Comparison

| Dimension | TSA Security Directives | CISA CIRCIA |
|-----------|------------------------|-------------|
| **Issuing authority** | Transportation Security Administration (TSA) | CISA (Cybersecurity and Infrastructure Security Agency) |
| **Legal basis** | Aviation and Transportation Security Act (ATSA); emergency authority | Cyber Incident Reporting for Critical Infrastructure Act of 2022 (Public Law 117-159) |
| **Status** | Active mandatory requirements (Security Directives in force now) | Proposed rule published April 2024; final rule not yet effective |
| **Who is covered** | Specific TSA-designated entities in pipeline, freight rail, passenger rail/transit, aviation sectors | All 16 CISA critical infrastructure sectors (much broader) |
| **Core focus** | Comprehensive cybersecurity programme (CRMP, segmentation, access controls, monitoring, patching) | Incident reporting and ransom payment reporting obligations |
| **Incident reporting timeline** | **24 hours to CISA** (under current TSA directives) | CIRCIA proposes **72 hours** for covered cyber incidents; **24 hours** for ransom payments |
| **Reporting recipient** | CISA (via 24/7 Operations Center) + TSA notification | CISA |
| **Programme requirements** | Full CRMP (CIP/COIP, IRP, ADR, CAP); four technical domains | Incident reporting; preservation of incident-related data |
| **Sector-specific** | Yes — TSA directives are tailored to transportation sectors | No — applies across all 16 critical infrastructure sectors |

---

## TSA Security Directives: What They Require

TSA's requirements for pipeline operators are grounded in the **SD Pipeline-2021 series**:

- **SD Pipeline-2021-01G** (January 2026): Immediate measures — designate a Cybersecurity Coordinator, report cybersecurity incidents to CISA within **24 hours**, and conduct a review of current cybersecurity practices
- **SD Pipeline-2021-02F**: Comprehensive CRMP — network segmentation (IT/OT), access controls (MFA), continuous monitoring, patch management, plus a Cybersecurity Implementation Plan (CIP), Incident Response Plan (IRP), Architecture Design Review (ADR), and Cybersecurity Assessment Plan (CAP)

TSA's requirements are **programme-wide** — they require building and operating a comprehensive cybersecurity programme around your Critical Cyber Systems (CCS), not just reporting incidents when they happen.

**Current incident reporting under TSA**: 24 hours to CISA from the time of identification. Report even if investigation is incomplete. Contact: 1-888-282-0870 or CISAgov@mail.dhs.gov.

---

## CIRCIA: What It Requires

The Cyber Incident Reporting for Critical Infrastructure Act of 2022 (CIRCIA) was signed into law March 15, 2022. CISA published a Notice of Proposed Rulemaking (NPRM) in April 2024 to implement CIRCIA's requirements.

CIRCIA's core obligations (as proposed):

### 1. Covered Cyber Incident Reporting — 72 Hours
Covered entities must report "covered cyber incidents" to CISA within **72 hours** of reasonably believing a covered cyber incident has occurred. A covered cyber incident includes substantial cyberattacks affecting operational availability, integrity, or confidentiality of covered systems.

### 2. Ransom Payment Reporting — 24 Hours
If a covered entity makes a ransom payment in response to a ransomware attack, it must report that payment to CISA within **24 hours** of making the payment — regardless of whether it also reports the underlying incident.

### 3. Supplemental Reports
Covered entities must file supplemental reports as new or substantially different information becomes available about a reported incident.

### 4. Data Preservation
Covered entities must preserve relevant data related to covered cyber incidents for a defined period.

### 5. Who Is a "Covered Entity"?
CIRCIA covers entities across all 16 CISA critical infrastructure sectors. CISA's proposed rule uses a sector-based, size-based, and criticality-based approach to define covered entities. Coverage is significantly broader than TSA's designation-based approach — many entities not currently under TSA mandates may be covered by CIRCIA.

---

## Key Differences for Pipeline Operators

### Reporting Timeline: 24 Hours (TSA) vs. 72 Hours (CIRCIA)
This is the most operationally significant difference. Under current TSA requirements, you must report to CISA within **24 hours** of identifying an incident. CIRCIA proposes a **72-hour** window for incident reporting.

However, the TSA 24-hour requirement is the more stringent obligation and is **currently in force**. CIRCIA's final rule is not yet effective. If you are a TSA-designated pipeline operator, you must comply with the 24-hour TSA requirement now.

**Important note**: When CIRCIA is finalised, the reporting obligations may overlap. CISA has indicated it intends to coordinate incident reports received under CIRCIA with sector-specific regulators like TSA to avoid duplicative reporting burdens where possible, but the details of that coordination are still being worked out.

### Scope: Programme vs. Reporting
- **TSA**: Requires a full cybersecurity programme — not just reporting, but building and operating controls (segmentation, MFA, monitoring, patching, annual assessments, etc.)
- **CIRCIA**: Primarily a reporting requirement — report incidents and ransom payments to CISA. It does not impose a comprehensive cybersecurity programme equivalent to the TSA CRMP.

### Coverage Basis: Designated vs. Sector-Wide
- **TSA**: Designates specific entities. Not all pipeline operators are automatically covered — you must have received a TSA designation notification.
- **CIRCIA**: Will apply to covered entities across all critical infrastructure sectors based on size and criticality thresholds defined in the final rule. Pipeline operators of meaningful size are likely to be covered.

---

## Do Pipeline Operators Need to Comply With Both?

**Yes, if you are a TSA-designated pipeline operator.** The frameworks are complementary, not mutually exclusive:

1. **TSA directives are currently in force** and require immediate compliance with all CRMP requirements, including 24-hour incident reporting to CISA
2. **CIRCIA will create a parallel obligation** once the final rule is effective — likely covering incident reporting with a 72-hour window and ransom payment reporting within 24 hours
3. **The 24-hour TSA requirement is more stringent** than CIRCIA's 72-hour window — if you report within 24 hours as TSA requires, you will also satisfy CIRCIA's 72-hour window
4. **CIRCIA's ransom payment reporting** (24 hours) is a new obligation with no direct equivalent in current TSA directives

### Practical Approach: Build One Process
Structure your incident response and reporting processes to satisfy both frameworks simultaneously:
- Report to CISA within 24 hours (satisfies TSA; also satisfies CIRCIA's 72-hour window)
- Separately track any ransom payments and report them to CISA within 24 hours of payment (CIRCIA obligation)
- Notify TSA separately per TSA directive requirements
- File supplemental reports as required by both frameworks

---

## 2024 NPRM — TSA's Own Rulemaking

Separately from CIRCIA, TSA published its own **Notice of Proposed Rulemaking (NPRM)** in November 2024 to convert current Security Directive requirements into permanent federal regulations under 49 CFR. This NPRM:
- Formally codifies the CRMP requirements (CIP, IRP, ADR, CAP, four technical domains)
- Aligns explicitly with NIST CSF 2.0 and CISA Cross-Sector Cybersecurity Performance Goals (CPGs)
- Comment period closed February 5, 2025; final rule not yet published
- Current TSA Security Directives remain fully in force until a final rule is published

---

## Summary for Pipeline Operators

| Obligation | Source | Status | Timeline |
|------------|--------|--------|---------|
| Report cybersecurity incidents to CISA | TSA SD Pipeline-2021-01G | In force NOW | 24 hours |
| Report to TSA | TSA SD Pipeline-2021-01G | In force NOW | Per directive |
| Maintain full CRMP (CIP, IRP, ADR, CAP, four domains) | TSA SD Pipeline-2021-02F | In force NOW | Ongoing |
| Report covered cyber incidents | CIRCIA (proposed) | Final rule pending | 72 hours (when effective) |
| Report ransom payments | CIRCIA (proposed) | Final rule pending | 24 hours from payment (when effective) |
| Comply with codified CRMP regulations | TSA NPRM | Proposed | When final rule published |

---

*Disclaimer: This guidance is based on publicly available information about TSA Security Directives, the CIRCIA statute, and CISA's April 2024 NPRM. CIRCIA's implementing regulations are not yet finalised. TSA Security Directives are Sensitive Security Information (SSI). Work with qualified legal counsel and cybersecurity professionals to ensure compliance with all applicable requirements.*
