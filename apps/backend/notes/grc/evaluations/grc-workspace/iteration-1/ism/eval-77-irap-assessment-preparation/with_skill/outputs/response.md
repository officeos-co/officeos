# IRAP Assessment Preparation for a PROTECTED-Level System

This guidance covers the three areas you have asked about: artefacts to prepare, what to expect during the assessment, and what happens after the IRAP report is issued. It is based on the Australian Information Security Manual (ASD, March 2026 edition).

---

## 1. Artefacts to Prepare

Because your system handles PROTECTED-level information, an IRAP assessment is mandatory before the system can be formally authorised to operate. Your assessor will need a complete documentary package. Prepare the following before the assessment begins.

### Core Artefacts

**System Security Plan (SSP)**
This is the primary authorisation document and the centrepiece of your IRAP submission. It must contain:
- System name, owner, purpose, and operating environment
- System boundary — all in-scope hardware, software, and interfaces
- Classification level (PROTECTED)
- Security objectives — Confidentiality, Integrity, and Availability ratings
- All applicable ISM controls with justification for inclusion (for a PROTECTED system: NC + OS + P controls across all 22 guideline chapters)
- Evidence of implemented controls for each applicable control
- Excluded controls with documented risk acceptance and justification
- A current risk register
- Review schedule (minimum every 24 months or after significant change)

**Network Architecture Diagrams**
Current, accurate diagrams showing system boundaries, network zones, segmentation, interconnections, and classification boundaries. These must align with what is described in the SSP.

**Asset Register**
A complete register of all hardware and software assets within the system boundary. This underpins controls across Chapter 11 (IT Equipment), Chapter 12 (Media), and Chapter 13 (System Hardening).

**Risk Register**
A current risk register documenting identified threats, vulnerabilities, likelihood, impact ratings, and treatment decisions. Residual risks must be explicitly recorded.

**Policy Suite**
All security policies referenced in the SSP must exist and be current. Key policies for a PROTECTED system include:
- Access control policy
- Patch management policy and procedures
- Incident response plan
- Change management plan
- Business continuity and disaster recovery plan
- Media handling and sanitisation procedures
- Physical security procedures
- Acceptable use policy
- Cryptography and key management procedures
- Backup and recovery procedures

**Evidence of Implemented Controls**
For each control recorded as implemented in the SSP, you need supporting evidence. This typically includes configuration screenshots, hardening checklists, system-generated reports, training records, and test results. The assessor will sample and test this evidence.

**Incident Register (last 12 months)**
A log of cyber security incidents or near-misses. If you have no incidents to record, document that explicitly.

**Patch Management Reports**
Evidence that patches are being applied within ISM timeframes:
- Critical patches (CVSS 9–10): within 48 hours for internet-facing systems; 14 days for internal
- High patches (CVSS 7–8.9): within 14 days
- Medium patches (CVSS 4–6.9): within 30 days

**Essential Eight Evidence**
For a PROTECTED system, the assessor will also examine your Essential Eight implementation. Prepare evidence for each of the eight strategies: application control (allow-listing), patch applications, Microsoft Office macro controls, user application hardening, restriction of administrative privileges, patch operating systems, multi-factor authentication, and regular backups. ASD's 2026 recommended baseline for government entities is Maturity Level 2.

**Note on Previous Assessments**
As this is your first IRAP assessment, you will not have a previous IRAP report or Plan of Action and Milestones (POA&M). That is normal. The assessor will note this is an initial assessment.

---

## 2. What to Expect During the Assessment

### Engaging an IRAP Assessor
Before anything else, you must engage an assessor who is currently listed on the ASD IRAP Assessors Register. The assessor must be independent of your organisation and the system being assessed. You can find current assessors at:
https://www.cyber.gov.au/resources-business-and-government/assessment-and-evaluation-programs/irap/irap-assessors

### Scoping and Planning Phase
The assessor will work with you to confirm the system boundary and assessment scope. This includes agreeing which ISM controls are in scope, the assessment methodology, and the schedule. For a PROTECTED system the scope covers all NC, OS, and P controls across all 22 ISM guideline chapters relevant to your system's architecture.

### Document Review
The assessor will conduct a detailed review of all artefacts listed above. They will check that your SSP accurately reflects the system, that controls are documented with sufficient evidence, and that policies are current and approved. Gaps identified at this stage will be noted and may prompt requests for additional information.

### Technical Testing and Interviews
The assessor will conduct technical testing of the implemented controls. This typically includes:
- Configuration reviews (OS hardening, application hardening, firewall rules, cryptographic settings)
- Review of network architecture and segmentation against diagrams
- Sampling of patch management and change management records
- Testing of logging and monitoring capability (Chapter 15 — log retention of 18 months minimum for PROTECTED systems)
- Review of cryptographic implementations (AES-256 for PROTECTED data, SHA-384 minimum for hashing, TLS 1.2 minimum with TLS 1.3 preferred)
- Interviews with system administrators, the CISO, and key security personnel

### Findings Classification
The assessor will classify findings into categories (typically: satisfactory, minor finding, major finding, or significant risk). Findings represent controls that are not implemented, partially implemented, or implemented without adequate evidence.

### Draft Report Review
Before the final report is issued, you will normally be given an opportunity to review the draft IRAP Assessment Report and respond to findings. This is the appropriate time to provide any additional evidence or clarify misunderstandings. It is not the time to remediate major gaps — substantive remediation must happen before the assessment or will be recorded as outstanding findings.

---

## 3. What Happens After the IRAP Report is Issued

### IRAP Assessment Report
The assessor issues a formal IRAP Assessment Report documenting:
- Assessment scope and methodology
- All findings (satisfactory and non-compliant)
- Risk ratings for each finding
- Recommendations for remediation

This report is provided to your agency and is the primary input to the authorisation decision.

### Plan of Action and Milestones (POA&M)
Your agency must produce a POA&M addressing each finding in the IRAP report. The POA&M documents:
- Each finding and its risk rating
- The remediation action planned
- The responsible owner
- The target remediation date

The POA&M is a living document and must be maintained and updated as findings are closed.

### Authorisation to Operate (ATO)
Your Authorising Official (typically the agency head, CIO, or a delegate with appropriate authority) reviews the IRAP Assessment Report, the POA&M, and the residual risk profile. The Authorising Official makes a risk-based decision to:
- Grant an Authorisation to Operate — accepting residual risk and authorising the system for use
- Grant a conditional or time-limited ATO — with conditions tied to POA&M milestones
- Decline to authorise — if residual risk is unacceptable

The ATO is a formal sign-off that is recorded in the SSP.

### Ongoing Monitoring
After authorisation, your agency must:
- Maintain continuous monitoring of controls (Chapter 15)
- Apply patches within required timeframes (Chapter 14)
- Review and update the SSP at least every 24 months or after significant change
- Report cyber security incidents to ASD as required (Chapter 2)
- Conduct a re-assessment (new IRAP assessment) every 24 months minimum, or sooner if there is a significant architectural change, a change in classification level, or major new functionality is added

### Re-assessment Triggers
The following events require a new IRAP assessment regardless of timing:
- A major change to the system architecture or boundary
- A change in the classification level of information handled
- Addition of major new functionality or new interconnections with other systems

---

## Summary Checklist — Pre-Assessment Preparation

| Artefact | Owner | Status to Confirm |
|----------|-------|------------------|
| System Security Plan (SSP) — complete and current | System Owner / CISO | All sections complete, Authorising Official identified |
| Network architecture diagrams | IT/Architecture | Current, accurate, aligned to SSP |
| Asset register | IT | All in-scope hardware and software recorded |
| Risk register | CISO | Current risk ratings, treatments documented |
| Policy suite | CISO | All policies approved, version-controlled, within review cycle |
| Control evidence package | System Owner | Evidence for all implemented NC + OS + P controls |
| Incident register | CISO | Last 12 months documented |
| Patch management reports | IT | Reports showing compliance with ISM patch SLAs |
| Essential Eight evidence | IT / CISO | Evidence for all eight strategies at target maturity level |
| IRAP assessor engaged | CISO | Assessor confirmed on ASD IRAP register; independence confirmed |

---

## Key ISM References

- Chapter 4 — Cyber Security Documentation (SSP and policy requirements)
- Chapter 6 — Personnel Security (clearances, awareness training)
- Chapter 13 — System Hardening (OS, application, firmware hardening; Essential Eight)
- Chapter 14 — System Management (patch management, change management, backups)
- Chapter 15 — System Monitoring (logging; 18-month retention for PROTECTED systems)
- Chapter 19 — Networking (network segmentation, firewall rules)
- Chapter 20 — Cryptography (AES-256, SHA-384, TLS 1.2/1.3 for PROTECTED)
- ISM IRAP Assessors Register: https://www.cyber.gov.au/resources-business-and-government/assessment-and-evaluation-programs/irap/irap-assessors
- ISM (March 2026): https://www.cyber.gov.au/sites/default/files/2026-03/Information%20security%20manual%20(March%202026).pdf
