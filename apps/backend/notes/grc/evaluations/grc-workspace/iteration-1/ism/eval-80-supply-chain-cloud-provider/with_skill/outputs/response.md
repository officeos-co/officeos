# ISM Compliance for Private Sector Cloud Providers Serving Australian Federal Government

## Overview

Congratulations on winning the contract. As a private sector company providing cloud services to an Australian federal government agency, you are now part of the government's supply chain and are subject to the Australian Information Security Manual (ISM), published by the Australian Signals Directorate (ASD). This document addresses your three questions: your obligations, the most critical controls for a cloud environment, and the IRAP assessment requirement.

---

## 1. Your Obligations as a Cloud Service Provider in the Government Supply Chain

### The ISM Applies to You

The ISM is not limited to government entities themselves. Chapter 3 (Procurement & Outsourcing) explicitly requires government agencies to ensure their outsourced service providers and cloud vendors implement ISM-aligned security controls. When you sign a contract requiring ISM compliance, you are contractually and practically subject to the same obligations as the agency itself for the systems and data in scope.

Your obligations flow from two directions:

- **Contractual**: The government agency's contract will specify ISM compliance requirements, classification level of information you will handle, and likely mandate a System Security Plan (SSP) and IRAP assessment as deliverables.
- **Framework-based**: The ISM itself (Chapter 3) requires agencies to hold vendors to equivalent security standards, meaning you must implement controls at the appropriate classification level.

### Determine the Classification Level First

Your first obligation is to confirm the classification level of the government information your cloud service will store, process, or transmit. This determines which controls apply:

- **OFFICIAL: Sensitive (OS)** — the most common level for federal agency systems; requires NC + OS controls
- **PROTECTED** — classified information; requires NC + OS + P controls; most stringent common level for cloud services
- **SECRET / TOP SECRET** — rare for private sector cloud; requires additional accreditation

If the contract does not state the classification explicitly, default to OFFICIAL: Sensitive and confirm with the agency. If any PROTECTED information is in scope, the full PROTECTED control set applies.

### Core Obligations Summary

| Obligation | ISM Reference | What You Must Do |
|-----------|--------------|-----------------|
| Appoint a CISO or equivalent security lead | Ch. 1 | Designate a named individual with authority over security for the service |
| Produce a System Security Plan (SSP) | Ch. 4 | Document system boundary, classification, controls implemented, and risk register |
| Implement applicable controls | All chapters | Implement NC + OS controls (and P controls if handling PROTECTED data) |
| Undergo IRAP assessment | Ch. 3, IRAP framework | Independent review before the system is authorised to operate (see Section 3 below) |
| Report incidents to ASD | Ch. 2 | Notify the agency (and ASD for significant incidents) within required timeframes |
| Maintain ongoing compliance | Ch. 14, 15 | Patch management, continuous monitoring, re-assessment every 24 months |
| Manage sub-contractors | Ch. 3 | If you sub-contract any components, those sub-contractors must meet equivalent requirements |

---

## 2. Most Critical Controls for Your Cloud Environment

For a cloud service provider handling government information, the following control domains are the highest priority. These are drawn from the ISM's 22 guideline chapters, with emphasis on controls that address the specific risk profile of cloud environments.

### Priority 1 — Identity, Access, and Privileged Access Management

Cloud environments expose management planes that, if compromised, grant access to all hosted data and infrastructure. The ISM is explicit about this risk.

**Key controls (Chapters 6, 13):**
- Multi-factor authentication (MFA) for all administrative access, remote access, and any access to government data
- No standing administrative privileges — use just-in-time (JIT) or break-glass models
- Separate administrative accounts from user accounts
- Regular access reviews; prompt revocation on role change or termination
- These map directly to the Essential Eight: "Restrict administrative privileges" and "Multi-factor authentication"

### Priority 2 — System Hardening (Chapter 13)

Every compute instance, container, and managed service component must be hardened before deployment.

**Key controls:**
- Remove or disable all unnecessary services, ports, and protocols on every VM or container image
- Implement application allow-listing where technically feasible
- Enforce Secure Boot and firmware integrity controls
- Disable default/vendor accounts
- Harden cloud management consoles (restrict API access, enforce MFA on cloud console logins)
- For OS patches: critical patches within 48 hours for internet-facing systems, 14 days for internal

### Priority 3 — Cryptography (Chapter 20)

All government data in transit and at rest must be protected with ASD-approved algorithms.

**Key controls:**
- Data at rest: AES-256 encryption (required for PROTECTED; strongly recommended for OS)
- Data in transit: TLS 1.2 minimum; TLS 1.3 preferred. SSL and TLS 1.0/1.1 must be disabled
- SSH: SSH2 only; prefer ED25519 or RSA-4096
- Key management: documented procedures for key generation, storage, rotation, and destruction
- Use ASD-approved or FIPS 140-2/3 validated cryptographic modules, especially for PROTECTED

### Priority 4 — System Monitoring and Logging (Chapter 15)

Government contracts will require evidence of comprehensive, tamper-evident logging.

**Key controls:**
- Centralised logging (SIEM or equivalent) covering all system and access events
- Log all privileged access, configuration changes, authentication events, and data access
- Minimum retention: 18 months for OFFICIAL: Sensitive and PROTECTED systems
- Logs must be stored separately from the monitored system (cannot be modified by a compromised host)
- Alerting on anomalous events with documented investigation procedures

### Priority 5 — Patch Management (Chapter 14)

**Key controls (aligned to ISM patch SLAs):**

| Patch Criticality | Internet-Facing | Internal Systems |
|------------------|-----------------|-----------------|
| Critical (CVSS 9–10) | 48 hours | 14 days |
| High (CVSS 7–8.9) | 14 days | 14 days |
| Medium (CVSS 4–6.9) | 30 days | 30 days |
| Low | Next maintenance window | Next maintenance window |

This maps to the Essential Eight strategies "Patch applications" and "Patch operating systems."

### Priority 6 — Network Segmentation (Chapter 19)

**Key controls:**
- Segment government workloads from other tenants and from your internal corporate network
- Documented, reviewed firewall rule sets (no broad allow rules)
- Restrict outbound internet access from workloads handling government data
- Use private endpoints where possible; avoid exposing management interfaces to the public internet
- DNSSEC where applicable

### Priority 7 — Security Documentation (Chapter 4)

The government agency's Authorising Official will require documentary evidence before issuing an Authorisation to Operate (ATO). You must produce and maintain:

- **System Security Plan (SSP)** — the primary document. Must cover system boundary, classification, all implemented controls, exclusions with justifications, and the risk register
- **Security Risk Assessment** — formal risk identification, analysis, and treatment
- **Incident Response Plan** — tested procedures for detecting, responding to, and reporting security incidents
- **Change Management Plan** — approval process for all changes to the cloud environment
- **Continuous Monitoring Plan** — how you will maintain and evidence ongoing compliance

### Priority 8 — Incident Response and ASD Notification (Chapter 2)

**Key obligations:**
- Define what constitutes a security incident for your cloud service
- Document and test response procedures
- For OFFICIAL: Sensitive and above: you must notify the agency immediately on discovery of a significant incident; the agency is then responsible for ASD notification
- Preserve forensic evidence; conduct post-incident reviews

### Essential Eight — Minimum Baseline

The ASD recommends all government-connected systems achieve Essential Eight Maturity Level 2 (ML2) as the 2026 baseline. As a cloud provider, you should target ML2 across all eight strategies:

1. Application control (allow-listing)
2. Patch applications
3. Configure Microsoft Office macros (if Office is in scope)
4. User application hardening
5. Restrict administrative privileges
6. Patch operating systems
7. Multi-factor authentication
8. Regular backups (3-2-1 rule; tested; offline/offsite copy)

Full Essential Eight to ISM control mapping: https://www.cyber.gov.au/business-government/asds-cyber-security-frameworks/essential-eight/essential-eight-maturity-model-and-ism-mapping

---

## 3. Do You Need an IRAP Assessment Even Though You Are Not a Government Entity?

**Yes. In practice, an IRAP assessment will almost certainly be required.**

Here is the precise picture:

### The ISM Position

The ISM sets the following requirements for IRAP assessment:

- **Mandatory** for systems handling PROTECTED and above
- **Strongly recommended** for systems handling OFFICIAL: Sensitive
- **Government policy** for any system used to deliver government services

The obligation attaches to the **system**, not the entity operating it. Because your cloud service stores, processes, or transmits Australian government information, the system must be assessed — regardless of whether you are a private company.

### What the Government Agency Will Require

The government agency cannot issue an Authorisation to Operate (ATO) for your cloud service without an IRAP assessment report to inform that decision. In practice, the contract will either:

- Require you to commission and fund an IRAP assessment of your service, or
- Specify that the agency will conduct the IRAP assessment but you must provide full access, artefacts, and cooperation

Either way, you cannot avoid IRAP. If the contract does not spell this out, raise it immediately with the agency — an ATO cannot be issued without it.

### What an IRAP Assessment Involves

An IRAP (Infosec Registered Assessors Program) assessment is an independent review by an ASD-certified assessor. The assessor must be:
- Listed on the ASD IRAP Assessors Register: https://www.cyber.gov.au/resources-business-and-government/assessment-and-evaluation-programs/irap/irap-assessors
- Independent of your organisation (cannot be your own staff or a related party)

**Artefacts you must prepare:**
- Current System Security Plan (SSP)
- Network architecture diagrams
- Asset register (hardware and software)
- Risk register with current risk ratings
- Full policy suite (all security policies referenced in the SSP)
- Evidence of implemented controls (configuration screenshots, logs, test results)
- Patch management reports
- Incident register (last 12 months)
- Plan of Action and Milestones (POA&M) for any known gaps

**Assessment cycle:**
- Initial assessment before the system is authorised to operate
- Re-assessment every 24 months minimum
- Re-assessment also required after significant changes (major architecture changes, new classification level, major new functionality)

### Practical Recommendation: Start IRAP Preparation Now

IRAP assessments typically take 8–16 weeks from engagement to final report, depending on system complexity. The government agency will need the IRAP Assessment Report before it can sign an ATO. Your steps should be:

1. **Confirm the classification level** with the agency (OFFICIAL: Sensitive vs PROTECTED drives control selection significantly)
2. **Define the system boundary** — scope only what is necessary for the government service
3. **Draft the SSP** — this is the centrepiece of the assessment
4. **Implement and evidence controls** across the priority domains above
5. **Engage an IRAP assessor** from the ASD register early — assessors are in demand
6. **Remediate findings** from the assessment via a POA&M
7. **Support the agency's ATO decision** with the completed IRAP report

---

## Summary

As a private sector cloud provider delivering services under an ISM-compliance contract with an Australian federal government agency, your obligations are substantive and non-negotiable. The ISM framework applies to your system by virtue of the government information it handles. You must implement controls at the appropriate classification level (at minimum NC + OS, and NC + OS + P if handling PROTECTED data), produce a full System Security Plan, and undergo an IRAP assessment by an ASD-certified independent assessor before the agency can authorise your service to operate. The most critical technical controls for your cloud environment are privileged access management with MFA, system hardening, ASD-approved cryptography, comprehensive logging with 18-month retention, disciplined patch management, and network segmentation. Essential Eight Maturity Level 2 should be your minimum target across all eight strategies. Begin IRAP preparation immediately, as lead times are significant and the agency's ATO — and your contract deliverables — depend on it.

---

**References**
- ISM March 2026: https://www.cyber.gov.au/sites/default/files/2026-03/Information%20security%20manual%20(March%202026).pdf
- IRAP Assessors Register: https://www.cyber.gov.au/resources-business-and-government/assessment-and-evaluation-programs/irap/irap-assessors
- Essential Eight: https://www.cyber.gov.au/business-government/asds-cyber-security-frameworks/essential-eight
- Essential Eight to ISM Mapping: https://www.cyber.gov.au/business-government/asds-cyber-security-frameworks/essential-eight/essential-eight-maturity-model-and-ism-mapping
