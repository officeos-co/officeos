ISM Guidance: Cloud-Hosted Case Management System — OFFICIAL: Sensitive
Australian Federal Government Agency
Reference: Australian Information Security Manual (ASD, March 2026 edition)

---

1. CONTROL APPLICABILITY MARKINGS FOR YOUR SYSTEM

Your system stores OFFICIAL: Sensitive (OS) information and is therefore classified at the OS level. Under the ISM applicability framework, the following markings apply:

Applicable Markings:
- NC (Non-Classified): All controls marked NC are mandatory. These represent the universal baseline that every government system must implement, regardless of classification level.
- OS (OFFICIAL: Sensitive): All controls marked OS are mandatory. These are additional controls specifically required for systems that store, process, or communicate OFFICIAL: Sensitive information.

Markings that do NOT apply to your system:
- P (PROTECTED): Not required unless your system is upgraded to handle PROTECTED information.
- S (SECRET): Not applicable.
- TS (TOP SECRET): Not applicable.

Stacking Rule Applied: As an OS system, your agency must implement NC + OS controls across all relevant guideline chapters. This is the standard baseline for most federal agency systems.

Cloud-hosted context: Because your system is cloud-hosted, Chapter 3 (Procurement and Outsourcing) controls apply with particular weight. Your cloud service provider (CSP) must be assessed for ISM alignment, and all relevant security responsibilities must be documented in a shared responsibility model within your System Security Plan.

---

2. KEY CONTROLS TO IMPLEMENT

The following summarises the key controls across the 22 ISM guideline chapters that apply to your NC + OS system. Controls are grouped by chapter domain.

Chapter 1 — Cyber Security Roles
- Appoint a CISO with appropriate authority and documented responsibilities.
- Designate a system owner for the case management system.
- Define and document security accountability structures.

Chapter 2 — Cyber Security Incidents
- Establish and document an incident response procedure covering detection, classification, response, and recovery.
- Define ASD notification obligations for significant incidents.
- Test incident response procedures and conduct post-incident reviews.

Chapter 3 — Procurement and Outsourcing
- Assess your cloud service provider (CSP) for ISM compliance and alignment with ASD's Cloud Security Guidance.
- Contractually require the CSP to implement ISM-aligned security controls, including for subcontractors.
- Document the shared responsibility model — which controls the CSP owns versus your agency.
- Assess supply chain risks associated with the CSP and any third-party integrations.

Chapter 4 — Cyber Security Documentation
- Produce and maintain a current System Security Plan (SSP) as the primary authorisation artefact.
- Maintain a risk register covering identified threats, vulnerabilities, likelihood, impact, and treatment.
- Document and version-control all security policies; review at least annually.

Chapter 5 — Physical Security
- If agency personnel access the system from physical premises, secure areas must have controlled access.
- Clean desk and clear screen policies must be enforced for workstations used to access the system.
- Note: Physical controls for the cloud hosting infrastructure are the CSP's responsibility — document this in the SSP.

Chapter 6 — Personnel Security
- All personnel with access to the case management system must be screened in accordance with the PSPF and hold appropriate clearances or background checks for OS-level information.
- Annual security awareness training is mandatory.
- Access must be revoked promptly upon termination or role change.

Chapter 9 — Enterprise Mobility
- If the system is accessed via mobile devices, they must be enrolled in an approved Mobile Device Management (MDM) solution.
- Remote access to the system must use an approved VPN with Multi-Factor Authentication (MFA).
- BYOD access requires containerisation or equivalent controls.

Chapter 11 — IT Equipment
- Maintain an asset register for all endpoints and devices used to access the system.
- Ensure secure disposal of any equipment holding cached or locally stored OS data.

Chapter 12 — Media
- Control any removable media used in connection with the system.
- Sanitise or destroy media before disposal.

Chapter 13 — System Hardening (Essential Eight overlap)
- Harden the operating systems and applications in the cloud environment (remove unnecessary services, disable default accounts).
- Implement application allow-listing where applicable.
- Enforce least privilege — no standing administrative access; use privileged access management (PAM).
- Restrict Microsoft Office macros and apply user application hardening (browsers, PDF readers).
- Configure Secure Boot and firmware security settings.

Chapter 14 — System Management (Essential Eight overlap)
- Apply critical patches within 14 days for internet-facing components; within 48 hours for CVSS 9–10 vulnerabilities on internet-facing assets.
- Apply high-severity patches within 14 days; medium within 30 days.
- Implement and test regular backups; store backup copies securely and separately (3-2-1 rule).
- Follow a documented change management process for all system changes.

Chapter 15 — System Monitoring
- Implement centralised event logging (SIEM or equivalent).
- Log retention for an OFFICIAL: Sensitive system: minimum 18 months.
- Logs must be stored in a tamper-evident manner, separate from the monitored system.
- All privileged access and system changes must be logged.
- Anomalous events must trigger alerts and investigation.

Chapter 16 — Software Development
- If the case management system involves custom development, implement a secure Software Development Life Cycle (SDLC).
- Conduct code reviews and vulnerability testing before deployment.
- Track and patch third-party dependencies.
- Implement a vulnerability disclosure policy if the system is externally facing.

Chapter 17 — Database Systems
- Harden the database (remove default accounts, disable unnecessary features).
- Encrypt sensitive case data at rest.
- Apply least privilege to all database access accounts.
- Log database activity.

Chapter 18 — Email
- If the system sends email notifications, configure DMARC (reject policy), DKIM, and SPF.
- Implement email gateway filtering to block malicious attachments and phishing content.

Chapter 19 — Networking
- Segment the case management system from general-purpose networks.
- Document and regularly review firewall rule sets.
- VPNs used for remote access must use TLS 1.2 minimum (TLS 1.3 preferred).
- Consider DNSSEC for DNS integrity.

Chapter 20 — Cryptography
- Data in transit must be protected using TLS 1.2 minimum (TLS 1.3 preferred); SSL and TLS 1.0/1.1 are prohibited.
- Data at rest must be encrypted using AES-128 minimum (AES-256 is best practice for OS).
- Use SHA-256 minimum for hashing; MD5 and SHA-1 are prohibited.
- Document key management procedures covering generation, storage, rotation, and destruction.

Chapter 21 — Gateways
- Implement web content filtering at internet gateways.
- Regularly assess gateway security.

Chapter 22 — Data Transfers
- Log all transfers of OFFICIAL: Sensitive data.
- Label data with its protective marking before transfer.
- Use approved transfer mechanisms.

Essential Eight — Required Maturity Level
As a federal government agency in 2026, you are expected to achieve Essential Eight Maturity Level 2 (ML2) as a minimum baseline. This covers:
1. Application control (allow-listing)
2. Patch applications
3. Configure Microsoft Office macros
4. User application hardening
5. Restrict administrative privileges
6. Patch operating systems
7. Multi-factor authentication (MFA)
8. Regular backups

ML2 means controls are mostly aligned and address sophisticated threats. Full ML3 is required only for critical infrastructure or high-risk sectors, but is a worthwhile aspiration for a case management system handling sensitive personal information.

---

3. AUTHORISATION PATHWAY BEFORE GO-LIVE

The ISM prescribes a six-step risk management cycle. For your system, the authorisation pathway to achieve an Authorisation to Operate (ATO) before go-live is as follows:

Step 1 — Define the System
Deliverable: System definition documentation (feeds into the SSP)
- Define the system boundary: all components in scope (cloud infrastructure, application, database, integrations, endpoints, user access paths).
- Identify all assets (hardware, software, data flows, third-party services).
- Confirm the classification level: OFFICIAL: Sensitive.
- Define security objectives using the CIA triad with High/Medium/Low ratings appropriate to a case management system (likely: Confidentiality = High, Integrity = High, Availability = Medium–High).

Step 2 — Select Controls
Deliverable: Control selection register (documented in SSP)
- Apply the NC + OS stacking rule to select applicable ISM controls across all 22 chapters.
- Tailor controls where needed: document any exclusions with written risk justification.
- Map the CSP's shared responsibility model to determine which controls the CSP satisfies and which remain with your agency.

Step 3 — Implement Controls
Deliverable: Evidence of control implementation
- Deploy and configure all selected controls as documented in the SSP.
- Produce evidence artefacts: configuration screenshots, policy documents, training records, patch reports, backup test results, network diagrams.

Step 4 — Assess Controls (IRAP Assessment)
Deliverable: IRAP Assessment Report
- Engage an ASD-registered IRAP assessor who is independent of your agency and the development team. Find assessors at: https://www.cyber.gov.au/resources-business-and-government/assessment-and-evaluation-programs/irap/irap-assessors
- An IRAP assessment is strongly recommended for OFFICIAL: Sensitive systems and is effectively expected for federal government systems delivering services to citizens.
- Provide the assessor with the full artefact package:
  - Current SSP
  - Network architecture diagrams
  - Asset register
  - Risk register
  - Full policy suite
  - Evidence of implemented controls
  - Incident register (last 12 months)
  - Patch management reports
  - Plan of Action and Milestones (POA&M) for any outstanding gaps
- The assessor reviews controls against ISM requirements and produces an IRAP Assessment Report with findings and recommendations.

Step 5 — Authorise the System (ATO)
Deliverable: Signed Authorisation to Operate (ATO)
- The Authorising Official (typically the Senior Responsible Officer or delegate with appropriate authority) reviews:
  - The IRAP Assessment Report
  - The SSP with all controls documented
  - The residual risk register
  - The POA&M for outstanding findings
- The Authorising Official formally accepts the residual risk and signs the ATO, authorising the system to go live.
- The ATO must record the authorisation date, the classification level, the system boundary, and the next review date.

Step 6 — Monitor Continuously
Deliverable: Ongoing monitoring programme
- Begin continuous control monitoring from go-live: log review, patch monitoring, alerting.
- Conduct a formal re-assessment every 24 months at minimum.
- Trigger an out-of-cycle re-assessment after any significant change (major architectural change, new classification level, major new functionality, or significant security incident).
- Maintain the SSP and risk register as living documents.

---

Summary: Authorisation Timeline

| Phase | Key Deliverable | Who |
|-------|----------------|-----|
| 1. Define | System boundary, classification, security objectives | Agency / System Owner |
| 2. Select | NC + OS control register (in SSP) | Agency / CISO |
| 3. Implement | Evidence package for all controls | Agency / IT team / CSP |
| 4. Assess | IRAP Assessment Report | ASD-registered IRAP assessor |
| 5. Authorise | Signed ATO | Authorising Official |
| 6. Monitor | Continuous monitoring + 24-month re-assessment | Agency / CISO |

Your system cannot go live without a signed ATO from the Authorising Official. While IRAP assessment is not legally mandatory at OS level, it is strongly recommended by ASD and is standard practice for federal government systems — particularly those handling OFFICIAL: Sensitive case data about individuals.

---

References:
- ISM (March 2026): https://www.cyber.gov.au/sites/default/files/2026-03/Information%20security%20manual%20(March%202026).pdf
- IRAP Assessors Register: https://www.cyber.gov.au/resources-business-and-government/assessment-and-evaluation-programs/irap/irap-assessors
- Essential Eight: https://www.cyber.gov.au/business-government/asds-cyber-security-frameworks/essential-eight
- Essential Eight to ISM Mapping: https://www.cyber.gov.au/business-government/asds-cyber-security-frameworks/essential-eight/essential-eight-maturity-model-and-ism-mapping
