# NIST CSF 2.0 — Protect Function Control Mapping
## Current Controls Mapped to CSF Subcategories

### Controls Assessed:
- Okta SSO with MFA
- CrowdStrike EDR
- Qualys Vulnerability Scanning
- Druva Cloud Backup
- Written Security Awareness Training Program

---

## Overview

The **Protect (PR)** function in CSF 2.0 contains five categories:
- **PR.AA** — Identity Management, Authentication, and Access Control
- **PR.AT** — Awareness and Training
- **PR.DS** — Data Security
- **PR.PS** — Platform Security
- **PR.IR** — Technology Infrastructure Resilience

Your five controls span multiple Protect categories, with some also contributing to the **Detect (DE)** function (CrowdStrike) and **Identify (ID)** function (Qualys).

---

## Control Mapping Table

| Control | CSF Function | CSF Category | Subcategory ID | Subcategory Description | Coverage Assessment | Notes & Gaps |
|---------|-------------|--------------|----------------|------------------------|---------------------|--------------|
| **Okta SSO with MFA** | Protect | Identity Mgmt, Auth & Access Control | **PR.AA-01** | Identities and credentials for authorized users, services, and hardware are managed by the organization | Largely Implemented | Okta manages user identities; verify service accounts and non-human identities are also managed in Okta or a PAM tool |
| Okta SSO with MFA | Protect | Identity Mgmt, Auth & Access Control | **PR.AA-02** | Identities are proofed and bound to credentials based on the context of interactions | Partially Implemented | Okta handles standard identity binding; privileged accounts may need stronger proofing (e.g., PAM with session recording) |
| Okta SSO with MFA | Protect | Identity Mgmt, Auth & Access Control | **PR.AA-03** | Users, services, and hardware are authenticated using appropriate means | Fully Implemented | Okta MFA satisfies authentication requirement; confirm MFA is enforced for ALL apps (not just SSO-connected ones) |
| Okta SSO with MFA | Protect | Identity Mgmt, Auth & Access Control | **PR.AA-04** | Identity assertions are protected, conveyed, and verified | Largely Implemented | Okta handles SAML/OIDC assertions; ensure token lifetimes are appropriately short |
| Okta SSO with MFA | Protect | Identity Mgmt, Auth & Access Control | **PR.AA-05** | Access permissions and authorizations are managed, incorporating the principles of least privilege and separation of duties | Partially Implemented | SSO enables centralized access, but least privilege requires additional RBAC governance; review Okta group assignments and admin roles |
| Okta SSO with MFA | Protect | Identity Mgmt, Auth & Access Control | **PR.AA-06** | Physical access to assets is managed, monitored, and enforced commensurate with risk | Not Addressed | Okta covers logical access only; physical access controls are separate |
| **CrowdStrike EDR** | Protect | Platform Security | **PR.PS-01** | Configuration management practices are established and applied | Partially Implemented | CrowdStrike can detect misconfigurations but is not a configuration management system; complement with a CSPM or CIS benchmark tooling |
| CrowdStrike EDR | Protect | Platform Security | **PR.PS-05** | Installation and execution of unauthorized software is prevented or detected | Largely Implemented | CrowdStrike Falcon Prevent (if licensed) blocks unauthorized execution; verify prevention mode is enabled vs. detect-only |
| CrowdStrike EDR | Detect | Continuous Monitoring | **DE.CM-01** | Networks and network services are monitored to find potentially adverse events | Largely Implemented | CrowdStrike provides endpoint telemetry; network-level monitoring (NDR/SIEM) may be needed to cover full DE.CM scope |
| CrowdStrike EDR | Detect | Continuous Monitoring | **DE.CM-03** | Personnel activity and technology usage are monitored to find potentially adverse events | Largely Implemented | CrowdStrike monitors endpoint activity and process execution; extend with UEBA for comprehensive user behavior analysis |
| CrowdStrike EDR | Detect | Continuous Monitoring | **DE.CM-09** | Computing hardware and software, runtime environments, and their data are monitored to find potentially adverse events | Fully Implemented | Core CrowdStrike capability — real-time endpoint monitoring, process telemetry, and threat detection |
| CrowdStrike EDR | Detect | Adverse Event Analysis | **DE.AE-02** | Potentially adverse events are analyzed to better characterize and understand the attack | Largely Implemented | CrowdStrike Falcon Insight provides investigation capabilities; ensure analysts are trained and process exists |
| CrowdStrike EDR | Detect | Adverse Event Analysis | **DE.AE-06** | Information on adverse events is provided to authorized staff and tools | Largely Implemented | CrowdStrike integrates with SIEMs and ticketing tools; confirm alerting pipelines are configured to the right teams |
| **Qualys Vulnerability Scanning** | Identify | Risk Assessment | **ID.RA-01** | Vulnerabilities in assets are identified, validated, and recorded | Largely Implemented | Qualys performs authenticated scanning; confirm scan coverage includes cloud assets, OT where applicable, and newly provisioned systems |
| Qualys Vulnerability Scanning | Protect | Platform Security | **PR.PS-02** | Software is maintained, replaced, and removed commensurate with risk | Partially Implemented | Qualys identifies the need for patches but does not apply them; a patching process/tool (e.g., WSUS, Intune, Ansible) is required to close this |
| Qualys Vulnerability Scanning | Protect | Platform Security | **PR.PS-04** | Log records are generated and made available for continuous monitoring | Not Addressed | Qualys provides vulnerability data but not log management; log aggregation is a separate requirement |
| Qualys Vulnerability Scanning | Detect | Continuous Monitoring | **DE.CM-08** | Vulnerabilities in assets are identified to understand and manage risk | Fully Implemented | Core Qualys function — continuous or scheduled vulnerability detection is directly aligned to this subcategory |
| **Druva Cloud Backup** | Protect | Technology Infrastructure Resilience | **PR.IR-01** | Networks and environments are protected from unauthorized logical access and usage | Partially Addressed | Druva protects data through backup but does not directly address network protection; Druva itself should have MFA and restricted admin access |
| Druva Cloud Backup | Protect | Technology Infrastructure Resilience | **PR.IR-03** | Mechanisms are implemented to achieve resilience requirements in normal and adverse situations | Largely Implemented | Druva provides data resilience via cloud backup; verify RPO/RTO align with business requirements |
| Druva Cloud Backup | Protect | Technology Infrastructure Resilience | **PR.IR-04** | Adequate resource capacity to ensure availability is maintained | Largely Implemented | Druva cloud-based model provides scalable backup capacity; verify retention policies and capacity limits |
| Druva Cloud Backup | Recover | Incident Recovery Plan Execution | **RC.RP-03** | The integrity of backups and other restoration assets is verified before use in recovery | Partially Implemented | Druva provides backup integrity checking; confirm that restoration testing is conducted regularly (at least annually) |
| Druva Cloud Backup | Recover | Incident Recovery Plan Execution | **RC.RP-04** | Critical assets are prioritized for recovery to support critical services or business functions | Partially Implemented | Druva enables granular restore; however, formal recovery prioritization must be documented in a Recovery Plan — verify this exists |
| **Written Security Awareness Training** | Protect | Awareness and Training | **PR.AT-01** | Personnel are provided awareness and training so that they possess the knowledge and skills to perform general tasks with cybersecurity risks in mind | Partially Implemented | Written program exists — assess whether it includes phishing simulations, role-based training, and frequency (annual-only is insufficient per current threat landscape) |
| Written Security Awareness Training | Protect | Awareness and Training | **PR.AT-02** | Individuals in specialized roles are provided awareness and training so that they possess the knowledge and skills to perform relevant tasks with cybersecurity risks in mind | Not Implemented | Written program addresses general staff; specialized training for IT admins, developers, executives, and OT operators likely does not exist — document separately |

---

## Cross-Reference: Coverage Summary by Subcategory Category

| CSF Category | Subcategory IDs Covered | Your Controls | Coverage Level |
|-------------|------------------------|---------------|----------------|
| **PR.AA** — Identity & Access | PR.AA-01 through PR.AA-06 | Okta SSO + MFA | PR.AA-01/03/04 largely/fully covered; PR.AA-05 partial; PR.AA-06 not addressed |
| **PR.AT** — Awareness & Training | PR.AT-01, PR.AT-02 | Written Training Program | PR.AT-01 partial; PR.AT-02 not implemented |
| **PR.DS** — Data Security | Not addressed by listed controls | None mapped | Gap — no controls mapped; consider DLP, encryption, data classification |
| **PR.PS** — Platform Security | PR.PS-01, PR.PS-02, PR.PS-04, PR.PS-05 | CrowdStrike, Qualys | PR.PS-05 largely covered; PR.PS-02 partial (patching process needed) |
| **PR.IR** — Technology Resilience | PR.IR-01, PR.IR-03, PR.IR-04 | Druva | PR.IR-03/04 largely covered; PR.IR-01 not directly addressed |

---

## Key Gaps in the Protect Function

### Critical Gaps (Not Addressed by Current Controls)

| Gap Area | Subcategory | Recommended Control/Action |
|----------|-------------|---------------------------|
| **Data Security (PR.DS)** | PR.DS-01 through PR.DS-11 | No data security controls mapped. Implement: data classification policy, DLP tool (e.g., Microsoft Purview, Forcepoint), and encryption at rest/in transit |
| **Physical Access Controls** | PR.AA-06 | Okta covers logical access only. Implement physical access control system (badge readers, visitor logs, secure areas) |
| **Specialized Role Training** | PR.AT-02 | Implement separate curricula for IT admins, developers, executives, and any OT/ICS operators |
| **Patching Process** | PR.PS-02 | Qualys identifies vulnerabilities but a patching execution process is required (patch management tool + SLAs by severity) |

### Partial Gaps (Requiring Enhancement)

| Gap Area | Subcategory | Current State | Enhancement Needed |
|----------|-------------|---------------|-------------------|
| **Least Privilege / RBAC** | PR.AA-05 | Partial | Conduct Okta access review; implement formal RBAC with quarterly recertification |
| **Training Effectiveness** | PR.AT-01 | Partial | Add phishing simulations (e.g., KnowBe4, Proofpoint), increase frequency to quarterly, add role-based modules |
| **Backup Recovery Testing** | RC.RP-03 | Partial | Implement formal quarterly backup restore tests with documented results |
| **Configuration Management** | PR.PS-01 | Partial | Add CIS benchmark scanning or CSPM (e.g., Wiz, Prisma Cloud) alongside CrowdStrike |

---

## Cross-Framework Mapping (Your Controls)

| Your Control | CSF Primary Subcategories | NIST SP 800-53 Rev 5 | ISO 27001:2022 | CIS Controls v8 |
|-------------|--------------------------|---------------------|----------------|----------------|
| Okta SSO + MFA | PR.AA-01, PR.AA-03, PR.AA-05 | AC-2, IA-2, IA-8 | A.5.15, A.5.16, A.5.17 | CIS 5, 6 |
| CrowdStrike EDR | PR.PS-05, DE.CM-09, DE.AE-02 | SI-4, SI-3, IR-4 | A.8.7, A.8.15, A.8.16 | CIS 8, 10 |
| Qualys Scanning | ID.RA-01, DE.CM-08, PR.PS-02 | RA-3, RA-5, SI-2 | A.8.8 | CIS 7 |
| Druva Cloud Backup | PR.IR-03, PR.IR-04, RC.RP-03 | CP-9, CP-10 | A.8.13, A.5.30 | CIS 11 |
| Security Awareness Training | PR.AT-01, PR.AT-02 | AT-2, AT-3 | A.6.3 | CIS 14 |

---

## Recommended Next Steps

1. **Fill the PR.DS gap**: No data security controls are currently mapped. This is a significant exposure — implement data classification and encryption as a priority.
2. **Mature PR.AT**: Upgrade the written training program to include phishing simulations and role-based modules.
3. **Close PR.PS-02**: Implement a formal patch management process with defined SLAs (Critical: 24h, High: 7 days, Medium: 30 days).
4. **Review PR.AA-05**: Conduct an Okta access review and establish quarterly recertification for privileged roles.
5. **Test Druva recovery**: Implement quarterly restoration tests and document results against your RTO/RPO.

> **Note**: This mapping is based on NIST CSF 2.0 (February 2024). Control coverage assessments should be validated with evidence reviews. This does not constitute a formal audit.
