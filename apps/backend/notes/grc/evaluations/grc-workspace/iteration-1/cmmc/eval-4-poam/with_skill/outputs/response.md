# Plan of Action & Milestones (POA&M)
## CMMC Level 2 — C3PAO Assessment Findings

**Organization**: [Organization Name]  
**Assessment Date**: April 25, 2026  
**POA&M Prepared**: April 25, 2026  
**Remediation Deadline**: October 22, 2026 (180 days from assessment date)  
**Total Practices NOT MET**: 8  
**CMMC Level**: Level 2 (Advanced)  

---

## Critical Notice

Two of the eight findings — **AU.L2-3.3.1** (Audit Logging) and **SC.L2-3.13.8** (Encryption in Transit) — are classified as **critical practices** under CMMC 2.0. Per 32 CFR Part 170 and the C3PAO assessment rules, critical practices must be remediated before conditional certification can be finalized. These must be treated as the highest-priority items and cannot remain in POA&M status at the time of certification issuance.

---

## Priority Ordering

| Priority | Practice ID | Domain | SPRS Point Deduction | Critical Practice | Rationale |
|----------|-------------|--------|---------------------|-------------------|-----------|
| 1 | SC.L2-3.13.8 | System & Comms Protection | 5 pts | YES | Encryption in transit — critical practice; conditional certification blocked until resolved |
| 2 | AU.L2-3.3.1 | Audit & Accountability | 3 pts | YES | Audit logging — critical practice; foundational for all forensic and detection capabilities |
| 3 | AU.L2-3.3.2 | Audit & Accountability | 3 pts | No | Individual user traceability — closely dependent on AU.L2-3.3.1 infrastructure |
| 4 | AC.L2-3.1.5 | Access Control | 3 pts | No | Least privilege — high SPRS impact; excessive admin rights create broad attack surface |
| 5 | CM.L2-3.4.1 | Configuration Management | 3 pts | No | Baseline configurations — foundational for CM.L2-3.4.2; must be completed first |
| 6 | CM.L2-3.4.2 | Configuration Management | 3 pts | No | Security config settings — depends on CM.L2-3.4.1 baseline completion |
| 7 | RA.L2-3.11.2 | Risk Assessment | 3 pts | No | Vulnerability scanning — enables ongoing risk visibility; feeds CM remediation |
| 8 | CA.L2-3.12.4 | Security Assessment | 3 pts | No | SSP currency — documentation requirement; lowest operational urgency |

**Estimated Total SPRS Score Deduction from These 8 Findings**: 26 points

---

## Full POA&M Table

### POA&M Item 1 — SC.L2-3.13.8 (PRIORITY 1 — CRITICAL)

| Field | Details |
|-------|---------|
| **Practice ID** | SC.L2-3.13.8 |
| **Domain** | System and Communications Protection (SC) |
| **Requirement** | Implement cryptographic mechanisms to prevent unauthorized disclosure of CUI during transmission (FIPS 140-2/3 validated). |
| **Weakness Description** | CUI is transmitted over network connections without consistent use of FIPS 140-2/3 validated cryptographic mechanisms. Legacy protocols (TLS 1.0/1.1) remain enabled on internal servers, and some application-to-application communications transmit data in plaintext or use non-FIPS cipher suites. This exposes CUI to interception during transit. |
| **Root Cause** | No formal cryptographic policy enforced at the network/application layer; legacy application dependencies have not been reviewed for encryption compliance; FIPS mode not enabled on Windows servers. |
| **Remediation Steps** | 1. Inventory all data flows involving CUI (map source/destination, protocol, encryption status). 2. Disable TLS 1.0 and TLS 1.1 on all servers and network devices. 3. Enable TLS 1.2 minimum with FIPS-validated cipher suites (AES-256-GCM, SHA-256+). 4. Enable FIPS 140-2/3 mode on all Windows servers in scope. 5. Replace or reconfigure applications using plaintext protocols (HTTP, FTP, Telnet) with encrypted equivalents (HTTPS, SFTP, SSH). 6. Validate using TLS scanning tools (e.g., Qualys SSL Labs, testssl.sh) on all in-scope endpoints. 7. Document cryptographic settings in SSP and Configuration Baseline. |
| **Milestone 1** | Week 2 (May 9, 2026): Complete CUI data flow inventory; identify all in-scope endpoints requiring encryption. |
| **Milestone 2** | Week 4 (May 23, 2026): Disable TLS 1.0/1.1 on all externally-facing servers; FIPS mode enabled on Windows servers. |
| **Milestone 3** | Week 8 (June 20, 2026): Disable TLS 1.0/1.1 on internal servers; remediate plaintext protocols on internal data flows. |
| **Milestone 4** | Week 10 (July 4, 2026): Run authenticated TLS scans confirming FIPS-validated cipher suites only; document results. |
| **Scheduled Completion** | July 4, 2026 (70 days) |
| **Resources Required** | IT Security Engineer (80 hrs); System Administrators (40 hrs); TLS scanning tools (Qualys or equivalent). |
| **Status** | Open |
| **Evidence of Closure** | TLS configuration scan reports showing only FIPS-validated cipher suites; FIPS mode registry settings export; updated cryptographic policy; updated SSP Section SC.L2-3.13.8. |

---

### POA&M Item 2 — AU.L2-3.3.1 (PRIORITY 2 — CRITICAL)

| Field | Details |
|-------|---------|
| **Practice ID** | AU.L2-3.3.1 |
| **Domain** | Audit and Accountability (AU) |
| **Requirement** | Create and retain system audit logs and records to enable monitoring, analysis, investigation, and reporting of unlawful or unauthorized activity. |
| **Weakness Description** | Audit logging is incomplete across the CUI environment. Not all CUI-touching systems forward logs to a centralized SIEM. Log retention policy has not been formally documented or technically enforced. Several servers retain logs locally with no offsite backup, creating risk of log tampering and loss. Log retention periods do not meet minimum 3-year requirement. |
| **Root Cause** | No centralized logging architecture deployed; SIEM implementation not prioritized; log retention policy existed informally but was never technically enforced or documented in the SSP. |
| **Remediation Steps** | 1. Deploy or configure a SIEM/log aggregation platform (e.g., Splunk, Microsoft Sentinel, Elastic SIEM). 2. Identify all CUI-touching assets and configure each to forward logs to the SIEM. 3. Define and document a formal log retention policy (minimum 3 years for CUI systems). 4. Configure log storage with write-once or immutable log settings to prevent tampering. 5. Define audit events to capture: logons/logoffs, privilege use, object access, policy changes, account management, system events. 6. Test log pipeline from all in-scope endpoints to confirm completeness. 7. Update SSP Section AU.L2-3.3.1 with implementation details. |
| **Milestone 1** | Week 3 (May 16, 2026): SIEM platform selected and procurement initiated; CUI asset list finalized. |
| **Milestone 2** | Week 6 (June 6, 2026): SIEM deployed; log forwarding configured for Priority 1 assets (domain controllers, CUI file servers, VPN gateways). |
| **Milestone 3** | Week 10 (July 4, 2026): All in-scope assets forwarding logs to SIEM; retention policy technically enforced. |
| **Milestone 4** | Week 12 (July 18, 2026): Audit event catalog reviewed and confirmed; log completeness verified; documentation updated. |
| **Scheduled Completion** | July 18, 2026 (84 days) |
| **Resources Required** | IT Security Engineer (120 hrs); System Administrators (60 hrs); SIEM licensing/subscription (~$15,000–$30,000/year depending on data volume); log storage capacity. |
| **Status** | Open |
| **Evidence of Closure** | SIEM dashboard screenshots showing all in-scope assets reporting; log retention configuration export; formal audit logging policy (approved); SSP Section AU.L2-3.3.1 updated. |

---

### POA&M Item 3 — AU.L2-3.3.2 (PRIORITY 3)

| Field | Details |
|-------|---------|
| **Practice ID** | AU.L2-3.3.2 |
| **Domain** | Audit and Accountability (AU) |
| **Requirement** | Ensure that the actions of individual users can be uniquely traced to those users. |
| **Weakness Description** | Shared accounts and generic service accounts are used on several systems that process CUI. Actions performed under shared credentials cannot be attributed to individual users, making forensic tracing and accountability impossible for those sessions. Additionally, some privileged administrative tasks are performed under shared "admin" accounts with no individual identity binding. |
| **Root Cause** | Legacy systems and operational convenience led to shared account use. No formal policy prohibiting shared accounts was implemented. Service accounts not integrated with identity management for accountability. |
| **Remediation Steps** | 1. Audit all accounts on in-scope systems; identify all shared or generic accounts. 2. Eliminate shared administrative accounts; provision individual named accounts for all administrators. 3. For service accounts, document the owning individual and restrict service account login rights to prevent interactive use. 4. Implement Privileged Access Management (PAM) or privileged session recording for administrative actions. 5. Configure audit logs (built on AU.L2-3.3.1 infrastructure) to capture user identity with each logged event. 6. Update account management policy to prohibit shared account use. |
| **Milestone 1** | Week 4 (May 23, 2026): Account audit complete; list of shared/generic accounts identified. |
| **Milestone 2** | Week 8 (June 20, 2026): Individual accounts provisioned for all administrators; shared admin accounts disabled. |
| **Milestone 3** | Week 12 (July 18, 2026): Service account documentation complete; PAM or session recording implemented for privileged access. |
| **Scheduled Completion** | July 18, 2026 (84 days) |
| **Resources Required** | IT Security Engineer (40 hrs); System Administrators (60 hrs); Identity/PAM tooling (if not already in place). |
| **Status** | Open |
| **Evidence of Closure** | Account audit report showing no active shared accounts; Active Directory/IAM configuration showing named accounts; PAM session recording evidence; updated account management policy. |

---

### POA&M Item 4 — AC.L2-3.1.5 (PRIORITY 4)

| Field | Details |
|-------|---------|
| **Practice ID** | AC.L2-3.1.5 |
| **Domain** | Access Control (AC) |
| **Requirement** | Employ the principle of least privilege, including for specific security functions and privileged accounts. |
| **Weakness Description** | Numerous user accounts hold administrative or elevated privileges beyond what is required for their job functions. Several standard users have local administrator rights on workstations. Domain admin membership is broader than operationally necessary. Security functions (firewall management, log access, backup operations) are not separated from standard IT admin roles. No formal privilege review or recertification process exists. |
| **Root Cause** | Privileges were historically granted based on convenience rather than documented need-to-know. No Privileged Access Management (PAM) solution or formal least-privilege policy enforced technically. No periodic access review process established. |
| **Remediation Steps** | 1. Conduct a full privilege audit across all in-scope systems (Active Directory, servers, applications, network devices). 2. Define privilege tiers: standard user, power user, local admin, domain admin, security function roles. 3. Remove local administrator rights from standard user accounts on CUI workstations. 4. Reduce Domain Admins group to the minimum required personnel; implement tiered admin model. 5. Create role-based access groups for specific security functions; remove cross-role privilege accumulation. 6. Implement a PAM solution for management of privileged accounts (e.g., CyberArk, BeyondTrust, or Microsoft PIM). 7. Establish a quarterly privilege recertification process. 8. Document least-privilege policy and update SSP. |
| **Milestone 1** | Week 4 (May 23, 2026): Privilege audit complete; excess privilege inventory documented. |
| **Milestone 2** | Week 8 (June 20, 2026): Local admin rights removed from standard users; Domain Admins group reduced. |
| **Milestone 3** | Week 12 (July 18, 2026): Role-based access groups implemented; PAM solution deployed or procured. |
| **Milestone 4** | Week 16 (August 15, 2026): Quarterly recertification process documented and first review completed. |
| **Scheduled Completion** | August 15, 2026 (112 days) |
| **Resources Required** | IT Security Engineer (60 hrs); System Administrators (80 hrs); PAM solution licensing ($10,000–$50,000 depending on scale). |
| **Status** | Open |
| **Evidence of Closure** | Privilege audit report; Active Directory group membership exports showing reduced admin membership; PAM implementation screenshots; quarterly review procedures; updated access control policy and SSP. |

---

### POA&M Item 5 — CM.L2-3.4.1 (PRIORITY 5)

| Field | Details |
|-------|---------|
| **Practice ID** | CM.L2-3.4.1 |
| **Domain** | Configuration Management (CM) |
| **Requirement** | Establish and maintain baseline configurations and inventories of organizational systems (hardware, software, firmware, and documentation). |
| **Weakness Description** | No formal baseline configuration documentation exists for in-scope systems. The hardware and software inventory is incomplete and not maintained in a system of record. Firmware versions are not tracked. No formal process exists to update baseline documentation when system changes occur. |
| **Root Cause** | Configuration management was treated as an informal IT function. No CMDB or configuration management tooling deployed. Change management process existed informally without linkage to formal baseline documentation. |
| **Remediation Steps** | 1. Deploy or designate a Configuration Management Database (CMDB) or asset inventory tool (e.g., ServiceNow, Lansweeper, or spreadsheet-based for smaller environments). 2. Conduct a full hardware and software inventory of all in-scope CUI systems. 3. Create formal baseline configuration documents for each major OS/application category (Windows Server, Windows 10/11 workstations, network devices, firewalls). Use CIS Benchmarks or DISA STIGs as baseline templates. 4. Document firmware versions for all in-scope network and security devices. 5. Establish a process to update baselines after approved changes. 6. Integrate with the change management process (CM.L2-3.4.3) and link to SSP. |
| **Milestone 1** | Week 4 (May 23, 2026): Asset inventory tool selected; hardware/software inventory scan initiated. |
| **Milestone 2** | Week 8 (June 20, 2026): Full hardware, software, and firmware inventory documented and entered into CMDB. |
| **Milestone 3** | Week 12 (July 18, 2026): Baseline configuration documents drafted for all major system types (using CIS/STIG templates). |
| **Milestone 4** | Week 14 (August 1, 2026): Baseline documents approved by management; baseline maintenance process formalized. |
| **Scheduled Completion** | August 1, 2026 (98 days) |
| **Resources Required** | IT Administrator (80 hrs); IT Security Engineer (40 hrs); CMDB/asset management tool (free to ~$5,000 depending on tooling). |
| **Status** | Open |
| **Evidence of Closure** | Complete hardware/software/firmware inventory export; approved baseline configuration documents per system type; CMDB records; configuration management policy update; updated SSP. |

---

### POA&M Item 6 — CM.L2-3.4.2 (PRIORITY 6)

| Field | Details |
|-------|---------|
| **Practice ID** | CM.L2-3.4.2 |
| **Domain** | Configuration Management (CM) |
| **Requirement** | Establish and enforce security configuration settings for IT products employed in organizational systems. |
| **Weakness Description** | Security configuration settings are not formally established, documented, or technically enforced across in-scope systems. Default or vendor configurations remain on several servers and network devices. Security hardening has been applied inconsistently without documented standards. No automated compliance checking is performed against security configuration benchmarks. |
| **Root Cause** | Dependent on CM.L2-3.4.1 baseline gap; without documented baselines, enforcing security settings was not systematically possible. No Group Policy Objects (GPOs) formally mapped to security configuration requirements. No configuration compliance scanning tool deployed. |
| **Remediation Steps** | 1. Complete CM.L2-3.4.1 baseline documentation (prerequisite). 2. Map security configuration settings to CIS Benchmark Level 1/2 or DISA STIG for each system type. 3. Implement security settings via Group Policy (Windows) or equivalent configuration management tool (Ansible, SCCM, Intune). 4. Deploy configuration compliance scanning (e.g., Tenable.sc/Nessus, CIS-CAT, or SCAP-compliant tools). 5. Remediate critical configuration deviations identified in compliance scan. 6. Establish a process for managing configuration exceptions with documented approval. 7. Update SSP with security configuration standards references. |
| **Milestone 1** | Week 8 (June 20, 2026): CM.L2-3.4.1 baselines complete (prerequisite); security configuration standards mapped to CIS/STIG. |
| **Milestone 2** | Week 12 (July 18, 2026): GPOs and configuration policies deployed for Windows systems. |
| **Milestone 3** | Week 14 (August 1, 2026): Configuration compliance scanning tool deployed; initial scan completed. |
| **Milestone 4** | Week 18 (August 29, 2026): Critical and high deviations remediated; exception process documented. |
| **Scheduled Completion** | August 29, 2026 (126 days) |
| **Resources Required** | IT Security Engineer (80 hrs); System Administrators (60 hrs); Configuration compliance scanning tool (Nessus Professional ~$3,000/yr or CIS-CAT Pro ~$1,000/yr). |
| **Status** | Open |
| **Evidence of Closure** | Configuration compliance scan reports showing acceptable deviation rates; GPO configuration exports; approved security configuration standards documentation; exception approval records; updated SSP. |

---

### POA&M Item 7 — RA.L2-3.11.2 (PRIORITY 7)

| Field | Details |
|-------|---------|
| **Practice ID** | RA.L2-3.11.2 |
| **Domain** | Risk Assessment (RA) |
| **Requirement** | Scan for vulnerabilities in organizational systems periodically and when new vulnerabilities affecting those systems are identified. |
| **Weakness Description** | Vulnerability scanning is not performed on a defined periodic schedule. When scans are performed, they are unauthenticated, which results in incomplete vulnerability discovery. Not all in-scope CUI assets are covered by existing scans. No formal process exists for triggering scans in response to critical vulnerability disclosures (e.g., CISA KEV alerts). |
| **Root Cause** | Vulnerability scanning was conducted ad hoc without a formal program. Authenticated scan credentials not configured. Asset inventory gaps (see CM.L2-3.4.1) prevented complete scan coverage. No assigned ownership for vulnerability management program. |
| **Remediation Steps** | 1. Select and deploy a vulnerability scanning platform (e.g., Tenable Nessus, Qualys, Rapid7 InsightVM). 2. Configure authenticated scan credentials for all in-scope systems (Windows, Linux, network devices). 3. Define scan scope using the completed asset inventory (CM.L2-3.4.1 output). 4. Establish scan schedule: monthly for all in-scope assets; ad-hoc scans within 72 hours of critical vulnerability disclosure. 5. Subscribe to CISA Known Exploited Vulnerabilities (KEV) catalog notifications. 6. Define vulnerability remediation SLAs tied to severity (Critical: 15 days, High: 30 days, Medium: 90 days). 7. Document vulnerability management policy and assign ownership to a named individual. 8. Update SSP Section RA.L2-3.11.2. |
| **Milestone 1** | Week 4 (May 23, 2026): Vulnerability scanning tool procured; scan credentials configured for initial target group. |
| **Milestone 2** | Week 8 (June 20, 2026): First authenticated scan completed covering all in-scope assets; initial vulnerability findings documented. |
| **Milestone 3** | Week 12 (July 18, 2026): Vulnerability management policy approved; remediation SLAs defined; CISA KEV subscription active. |
| **Milestone 4** | Week 16 (August 15, 2026): Second monthly scan completed; critical/high findings from initial scan remediated per SLA. |
| **Scheduled Completion** | August 15, 2026 (112 days) |
| **Resources Required** | IT Security Engineer (60 hrs); System Administrators (20 hrs); Vulnerability scanner licensing (Nessus Professional ~$3,000/yr or Tenable.io ~$5,000+/yr based on asset count). |
| **Status** | Open |
| **Evidence of Closure** | Authenticated vulnerability scan reports (two cycles minimum); scan coverage report showing all in-scope assets; vulnerability management policy; remediation tracking evidence; CISA KEV subscription confirmation; updated SSP. |

---

### POA&M Item 8 — CA.L2-3.12.4 (PRIORITY 8)

| Field | Details |
|-------|---------|
| **Practice ID** | CA.L2-3.12.4 |
| **Domain** | Security Assessment (CA) |
| **Requirement** | Develop, document, and periodically update system security plans (SSPs). |
| **Weakness Description** | The existing System Security Plan is outdated, incomplete, and was not reviewed or updated within the past year. Several recently added systems are not reflected in the SSP. Practice implementation statements are either missing or describe intended (rather than actual) implementations. The SSP has not been formally approved by management and does not reflect the current system boundary. |
| **Root Cause** | No formal SSP review and update cycle established. SSP was initially created for a previous contract and not maintained as systems evolved. No assigned SSP owner responsible for ongoing maintenance. |
| **Remediation Steps** | 1. Assign a named SSP owner (typically the ISSO or IT Security Manager). 2. Conduct a full review of the existing SSP against the current system boundary and all 110 Level 2 practices. 3. Update all practice implementation statements to reflect actual (not planned) implementation status. 4. Add newly scoped systems; remove decommissioned systems from scope. 5. Ensure all 8 POA&M findings are cross-referenced in the SSP with links to POA&M items. 6. Obtain formal management approval (signature) on the updated SSP. 7. Establish an annual SSP review cycle and document in the security program procedures. 8. Store SSP under version control with documented change history. |
| **Milestone 1** | Week 2 (May 9, 2026): SSP owner designated; SSP gap review initiated. |
| **Milestone 2** | Week 6 (June 6, 2026): SSP draft updated for all currently-implemented practices; system boundary section current. |
| **Milestone 3** | Week 10 (July 4, 2026): All POA&M cross-references added to SSP; draft submitted for management review. |
| **Milestone 4** | Week 12 (July 18, 2026): Management-approved SSP finalized; annual review procedure documented. |
| **Scheduled Completion** | July 18, 2026 (84 days) |
| **Resources Required** | IT Security Manager/ISSO (120 hrs); IT Administrator support (20 hrs). Minimal tooling cost (Word/SharePoint for document management). |
| **Status** | Open |
| **Evidence of Closure** | Management-signed SSP document with current date and version; SSP change log showing recent updates; annual review procedure; all 110 practice entries populated with accurate implementation statements. |

---

## Consolidated Timeline Summary

| Practice ID | Priority | Critical | Start | Completion Target | Days to Complete |
|-------------|----------|----------|-------|-------------------|-----------------|
| SC.L2-3.13.8 | 1 | YES | Apr 28, 2026 | July 4, 2026 | 70 days |
| AU.L2-3.3.1 | 2 | YES | Apr 28, 2026 | July 18, 2026 | 84 days |
| AU.L2-3.3.2 | 3 | No | May 5, 2026 | July 18, 2026 | 84 days |
| AC.L2-3.1.5 | 4 | No | May 5, 2026 | August 15, 2026 | 112 days |
| CM.L2-3.4.1 | 5 | No | May 5, 2026 | August 1, 2026 | 98 days |
| CM.L2-3.4.2 | 6 | No | May 12, 2026 | August 29, 2026 | 126 days |
| RA.L2-3.11.2 | 7 | No | May 5, 2026 | August 15, 2026 | 112 days |
| CA.L2-3.12.4 | 8 | No | Apr 28, 2026 | July 18, 2026 | 84 days |

All 8 practices targeted for completion by **August 29, 2026** — well within the 180-day remediation window of **October 22, 2026**.

---

## Resource Summary

| Resource Category | Estimated Cost/Effort |
|-------------------|-----------------------|
| IT Security Engineer time | ~480 hours total |
| System Administrator time | ~340 hours total |
| IT Security Manager/ISSO time | ~120 hours total |
| SIEM platform | $15,000–$30,000/year |
| Vulnerability scanning tool | $3,000–$5,000/year |
| PAM solution (AC.L2-3.1.5) | $10,000–$50,000 depending on scale |
| Configuration compliance tool | $1,000–$3,000/year |
| CMDB/asset management tool | $0–$5,000 depending on tooling choice |
| **Total estimated budget** | **$29,000–$93,000** |

---

## POA&M Management Procedures

1. **Monthly Reviews**: POA&M owner (ISSO/IT Security Manager) reviews all open items monthly; updates milestone completion status and notes any blockers.
2. **Escalation Path**: Overdue milestones escalate to the CISO/IT Director within 5 business days.
3. **SSP Linkage**: Each POA&M item is cross-referenced to the corresponding SSP section.
4. **Evidence Collection**: Evidence artifacts are collected at each milestone and stored in the compliance repository with version and date tracking.
5. **C3PAO Notification**: Any material change in remediation timeline for critical practices (SC.L2-3.13.8, AU.L2-3.3.1) must be communicated to the C3PAO within 10 business days.
6. **Closure Process**: POA&M items are closed only after evidence has been reviewed and approved by the ISSO and a second reviewer.
7. **SPRS Update**: SPRS score must be updated in the SPRS portal (sprs.csd.disa.mil) as each practice is remediated and closed.

---

*This POA&M was prepared in accordance with CMMC 2.0 (32 CFR Part 170), NIST SP 800-171 Rev 2, and DoD Assessment Methodology v2.2. All remediation timelines are within the 180-day conditional certification window.*
