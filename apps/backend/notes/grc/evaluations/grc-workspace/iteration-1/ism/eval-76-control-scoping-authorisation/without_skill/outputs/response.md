ISM Control Applicability, Key Controls, and Authorisation Pathway for a Cloud-Hosted Case Management System Handling OFFICIAL: Sensitive Information

OVERVIEW

This response addresses the requirements for an Australian federal government agency operating a cloud-hosted case management system that processes OFFICIAL: Sensitive information. The applicable framework is the Australian Government Information Security Manual (ISM), published and maintained by the Australian Signals Directorate (ASD). The ISM provides a risk-based, principles-driven approach to cybersecurity, and all controls should be read in conjunction with the current version of the ISM (April 2025 edition as of this writing).

---

PART 1: ISM CONTROL APPLICABILITY MARKINGS

The ISM categorises controls using applicability designations that indicate whether a control must be applied, should be applied, or is optional depending on system context. For a system handling OFFICIAL: Sensitive information, the relevant applicability categories are:

1. MUST (Mandatory Controls)
These are non-negotiable baseline requirements that apply to all systems, regardless of classification level. They represent the minimum security posture required by the ISM. All OFFICIAL: Sensitive systems must comply with every "must" control.

2. SHOULD (Recommended Controls)
These are strongly recommended controls. While agencies can choose not to implement them, any deviation must be formally documented, risk-assessed, and accepted by the Authorising Officer (AO). Deviations from "should" controls are a common area of scrutiny during security assessments.

3. SHOULD NOT / MUST NOT
These are prohibitions. "Must not" controls are absolute prohibitions; "should not" controls are strong discouragements that require formal risk acceptance if deviated from.

4. MAY (Optional Controls)
These controls are discretionary and context-dependent. Agencies implement them based on their specific threat environment and risk appetite.

For classification-level scoping, the ISM provides a sensitivity/classification matrix. OFFICIAL: Sensitive sits above OFFICIAL (unclassified) but below PROTECTED. This means:

- All baseline controls applicable to OFFICIAL systems apply.
- Additional controls specifically triggered by the "Sensitive" handling caveat apply, particularly around access control, personnel security, and physical/logical separation.
- PROTECTED-only controls generally do not apply, unless the system also hosts or connects to PROTECTED information (in which case the system must be assessed at the higher level).

For cloud-hosted systems, ASD's Cloud Computing Security for Tenants guidance and the ISM's cloud-specific controls are directly applicable. The agency must also consider whether the cloud service provider (CSP) holds an ASD-issued Cloud Services Certification (formerly IRAP-assessed listing on the Hosting Certification Framework / Digital Transformation Agency Certified Cloud Services List).

---

PART 2: KEY CONTROLS TO IMPLEMENT

The following control areas represent the most significant requirements for a cloud-hosted OFFICIAL: Sensitive case management system. Control IDs referenced below are indicative of the ISM control numbering scheme; agencies should verify exact IDs against the current ISM version.

GOVERNANCE AND RISK MANAGEMENT

- Develop and maintain a System Security Plan (SSP) that documents the system's security architecture, control implementation, residual risks, and the decisions made by the Authorising Officer.
- Conduct a formal risk assessment using the agency's risk management framework (typically aligned to PSPF and the NIST-derived approach in the ISM).
- Appoint a System Owner and ensure a qualified Information Security Registered Assessors Program (IRAP) assessor conducts an independent assessment prior to authorisation.

IDENTITY AND ACCESS MANAGEMENT

- Implement multi-factor authentication (MFA) for all users accessing OFFICIAL: Sensitive systems, particularly when accessing over the internet or through cloud infrastructure (ISM control area: "Identifying and Authenticating Personnel").
- Apply the principle of least privilege; access must be role-based and reviewed at least annually.
- Privileged access must be separately managed, logged, and subject to additional controls (e.g., Privileged Access Workstations where operationally feasible).
- Use identity federation through an agency-managed identity provider (IdP) where possible, rather than relying solely on CSP-native identity services.

SYSTEM HARDENING

- Apply the ASD Essential Eight mitigation strategies as a baseline. For OFFICIAL: Sensitive systems, the minimum target maturity level is Maturity Level Two (ML2) across all eight strategies. The eight strategies are:
  1. Application Control
  2. Patch Applications
  3. Configure Microsoft Office Macro Settings
  4. User Application Hardening
  5. Restrict Administrative Privileges
  6. Patch Operating Systems
  7. Multi-Factor Authentication
  8. Regular Backups

- Operating systems, applications, and cloud service configurations must be hardened against the ASD hardening guides and, where applicable, CIS Benchmarks.
- Disable unnecessary services, ports, and protocols on all system components.

NETWORK SECURITY

- Traffic between end users and the cloud-hosted system must be encrypted in transit using TLS 1.2 or higher (TLS 1.3 preferred).
- Data at rest must be encrypted using AES-256 or equivalent.
- Network segmentation must be implemented to isolate the case management system from other workloads.
- Web application firewalls (WAF) and intrusion detection/prevention systems (IDS/IPS) must be deployed.
- For internet-facing systems, a gateway with ASD-approved protective DNS filtering should be used.

LOGGING AND MONITORING

- Comprehensive logging must be enabled across all system components, including authentication events, privilege use, data access, and configuration changes.
- Logs must be retained for a minimum of 18 months (7 years for some records depending on Records Act obligations).
- A Security Information and Event Management (SIEM) capability or equivalent must be in place to detect and alert on anomalous activity.
- Logs must be protected from unauthorised deletion or modification.

DATA MANAGEMENT AND HANDLING

- Data must be classified and labelled correctly at all times. Systems must enforce protective markings consistent with the Protective Security Policy Framework (PSPF).
- Data sovereignty requirements: OFFICIAL: Sensitive data must be stored within Australia unless a formal risk assessment and AO approval permits offshore storage. Most cloud contracts for Australian government data stipulate Australian data residency.
- Implement Data Loss Prevention (DLP) controls to prevent unauthorised exfiltration.

VULNERABILITY AND PATCH MANAGEMENT

- Critical patches must be applied within 48 hours of release for internet-facing systems; for non-internet-facing components, within two weeks.
- Conduct regular vulnerability scanning (at least monthly) and penetration testing (at least annually or after significant change).

PERSONNEL SECURITY

- All personnel with access to OFFICIAL: Sensitive systems must hold, at minimum, a Baseline security clearance (or demonstrate an equivalent personnel security check under the PSPF).
- Third-party vendor and CSP staff with administrative access to the hosting environment should be subject to equivalent or greater vetting requirements.

PHYSICAL AND ENVIRONMENTAL (Cloud Context)

- For cloud-hosted systems, the agency does not directly control physical infrastructure. The CSP must demonstrate compliance with relevant physical security standards. This is assessed as part of the IRAP assessment of the CSP's infrastructure.
- The agency should use only CSP services listed on the ASD Evaluated Products List or assessed through IRAP at the appropriate level.

INCIDENT RESPONSE

- An Incident Response Plan (IRP) must be developed, tested, and maintained.
- Security incidents must be reported to ASD via the ASD Cyber Incident Reporting Portal in accordance with mandatory reporting thresholds.
- Business Continuity and Disaster Recovery plans must account for cloud-specific failure scenarios (e.g., availability zone outages, CSP service disruptions).

---

PART 3: AUTHORISATION PATHWAY

The ISM prescribes a formal security authorisation process before a system can be approved to handle protectively marked information. The authorisation process for OFFICIAL: Sensitive systems follows these key stages:

STAGE 1: SYSTEM CATEGORISATION AND SCOPING

- Define the system boundary, including all components (application, database, APIs, network, identity services, third-party integrations).
- Confirm the highest classification of data the system will process or store. If any OFFICIAL: Sensitive data will be handled, the entire system is assessed at that level.
- Identify applicable threat actors (from ASD's Cyber Threat Intelligence) and business impact levels.

STAGE 2: SECURITY DOCUMENTATION DEVELOPMENT

Develop the following core artefacts:

- System Security Plan (SSP): Describes the system, its security architecture, threat model, control implementation status, and residual risks.
- Standard Operating Procedures (SOPs): Operational security procedures for administrators and users.
- Incident Response Plan (IRP): How the agency will detect, respond to, and recover from security incidents.
- Business Continuity Plan (BCP) / Disaster Recovery Plan (DRP).
- Risk Assessment Report: Formal documentation of threats, vulnerabilities, likelihood, impact, and treatment decisions.

STAGE 3: IRAP ASSESSMENT

- Engage an IRAP assessor (an individual assessed and endorsed by ASD to conduct security assessments of Australian government systems).
- The IRAP assessor conducts an independent review of the SSP, system architecture, control implementation evidence, and risk posture.
- The assessment produces a Security Assessment Report (SAR) that identifies control gaps, residual risks, and recommendations.
- The IRAP assessment is not an approval — it is an independent expert opinion provided to the Authorising Officer.
- For cloud-hosted systems, the IRAP assessment should include a review of the CSP's IRAP assessment for the relevant services (tenants can leverage CSP IRAP reports to reduce duplication, but must also assess the tenant-side controls they are responsible for).

STAGE 4: RISK TREATMENT AND REMEDIATION

- Address critical and high findings from the IRAP assessment prior to authorisation where possible.
- Document accepted residual risks with formal risk acceptance decisions by the appropriate risk owner.
- Any risks that exceed the agency's risk appetite must be escalated to the Authorising Officer with full context.

STAGE 5: AUTHORISATION DECISION

- The Authorising Officer (AO) — typically a Senior Executive Service (SES) officer or equivalent, who is accountable for the system — reviews the SAR, SSP, risk register, and all risk acceptance decisions.
- The AO makes an informed risk-based decision to either:
  (a) Grant an Authority to Operate (ATO) — unconditional approval to go live, or
  (b) Grant a conditional ATO with specific remediation conditions that must be met within a defined timeframe, or
  (c) Decline authorisation and require further remediation before re-assessment.
- The ATO is time-limited. ISM guidance indicates systems should be reassessed periodically (typically every two years, or following significant change).
- The AO accepts personal accountability for the residual risks of the system in its current state.

STAGE 6: CONTINUOUS MONITORING AND REAUTHORISATION

- Post-go-live, the agency must maintain a continuous monitoring program to detect changes to the risk posture (e.g., new vulnerabilities, configuration drift, new threat intelligence).
- Significant changes to the system (new integrations, changes to data flows, major version upgrades) may trigger a requirement to re-engage IRAP and seek a new or updated ATO.
- Annual reviews of the SSP and risk register are standard practice.
- The ATO must be reviewed and reissued at least every two years, or earlier if warranted by material changes or incidents.

---

SUMMARY TABLE

| Area | Requirement |
|---|---|
| Applicable classification | OFFICIAL: Sensitive (above OFFICIAL, below PROTECTED) |
| Essential Eight target | Maturity Level 2 (minimum) |
| MFA | Required for all users |
| Encryption at rest | AES-256 or equivalent |
| Encryption in transit | TLS 1.2+ (TLS 1.3 preferred) |
| Data residency | Australia (default; exceptions require AO approval) |
| Personnel security | Baseline clearance minimum |
| CSP assessment | IRAP-assessed CSP services required |
| Authorisation type | Authority to Operate (ATO) issued by Authorising Officer |
| Independent assessment | IRAP assessor required prior to ATO |
| ATO validity | Up to 2 years; triggers reauthorisation on significant change |
| Incident reporting | Mandatory reporting to ASD |

---

REFERENCES AND FURTHER READING

- Australian Government Information Security Manual (ISM) — Australian Signals Directorate: https://www.cyber.gov.au/resources-business-and-government/essential-cyber-security/ism
- Protective Security Policy Framework (PSPF) — Attorney-General's Department: https://www.protectivesecurity.gov.au
- ASD Essential Eight Maturity Model: https://www.cyber.gov.au/resources-business-and-government/essential-cyber-security/essential-eight
- IRAP Assessors List: https://www.cyber.gov.au/about-us/irap
- Cloud Computing Security for Tenants (ASD): https://www.cyber.gov.au/resources-business-and-government/maintaining-devices-and-systems/cloud-computing-security
- ASD Certified Cloud Services: https://www.cyber.gov.au/resources-business-and-government/maintaining-devices-and-systems/cloud-computing-security/cloud-services-certification
