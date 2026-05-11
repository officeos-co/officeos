# Plan of Action and Milestones (POA&M)
## CMMC Level 2 Assessment Findings
**Organization:** [Organization Name]
**Assessment Date:** 2026-04-25
**POA&M Creation Date:** 2026-04-25
**Remediation Deadline:** 2026-10-22 (180 days)
**Assessor:** C3PAO Assessment Team

---

## Executive Summary

This POA&M documents 8 practices identified as NOT MET during the C3PAO CMMC Level 2 assessment. Findings span five domains: Audit and Accountability (AU), Configuration Management (CM), Risk Assessment (RA), Security Assessment (CA), Access Control (AC), and System and Communications Protection (SC). Remediation must be completed within 180 days to achieve CMMC Level 2 certification.

**Priority Classification:**
- **Critical (P1):** AC.L2-3.1.5, RA.L2-3.11.2, CA.L2-3.12.4
- **High (P2):** AU.L2-3.3.1, AU.L2-3.3.2, SC.L2-3.13.8
- **Medium (P3):** CM.L2-3.4.1, CM.L2-3.4.2

---

## Priority Rationale

### Critical (P1) Findings

**AC.L2-3.1.5** (Least Privilege) and **RA.L2-3.11.2** (Vulnerability Scanning) are prioritized as Critical because they represent foundational security controls. Lack of least privilege directly enables lateral movement and privilege escalation in breach scenarios. Absent vulnerability scanning means unknown exposure cannot be measured or managed.

**CA.L2-3.12.4** (System of Records / Plan of Action) is Critical because CMMC requires an active, maintained POA&M as an artifact for certification — its absence signals systemic governance failure and can block the path to certification itself.

### High (P2) Findings

**AU.L2-3.3.1** and **AU.L2-3.3.2** (audit logging and review) are High because without audit records, incident detection and forensic capability are severely degraded. **SC.L2-3.13.8** (cryptographic key management) is High due to the risk of data exposure if keys are not properly managed.

### Medium (P3) Findings

**CM.L2-3.4.1** and **CM.L2-3.4.2** (baseline configurations and security settings) are Medium because while important, they typically involve policy and tooling work that can proceed in parallel with higher-priority items without blocking other remediation tasks.

---

## POA&M Findings Detail

### Finding 1 — AU.L2-3.3.1: Audit Log Creation
| Field | Details |
|---|---|
| **Practice ID** | AU.L2-3.3.1 |
| **Practice Description** | Create and retain system audit logs and records to the extent needed to enable monitoring, analysis, investigation, and reporting of unlawful or unauthorized system activity. |
| **Finding** | Audit logging is not consistently enabled across all systems in scope. Log retention periods are undefined or not enforced. |
| **Priority** | High (P2) |
| **Risk** | Inability to detect or investigate unauthorized access or malicious activity. |
| **Remediation Actions** | 1. Inventory all in-scope systems and identify logging gaps. 2. Enable audit logging on all endpoints, servers, and network devices. 3. Define and enforce a log retention policy (minimum 90 days online, 1 year archived). 4. Deploy centralized SIEM or log aggregation solution. 5. Document logging architecture in SSP. |
| **Responsible Party** | IT Security Manager |
| **Resources Required** | SIEM licensing, storage infrastructure, staff time (~160 hours) |
| **Milestone 1** | Day 30 — Complete system inventory and logging gap analysis |
| **Milestone 2** | Day 60 — Enable logging on all in-scope systems |
| **Milestone 3** | Day 90 — SIEM deployed and log retention policy enforced |
| **Milestone 4** | Day 120 — SSP updated; evidence package prepared |
| **Estimated Completion** | Day 120 (2026-08-23) |
| **Status** | Open |

---

### Finding 2 — AU.L2-3.3.2: Audit Log Review
| Field | Details |
|---|---|
| **Practice ID** | AU.L2-3.3.2 |
| **Practice Description** | Review and update logged events. Ensure audit logs are reviewed for anomalies or inappropriate activity. |
| **Finding** | No documented process exists for periodic review of audit logs. Logs are collected but not reviewed. No alerting or anomaly detection is in place. |
| **Priority** | High (P2) |
| **Risk** | Malicious or unauthorized activity goes undetected. |
| **Remediation Actions** | 1. Develop an Audit Log Review Policy and Procedure. 2. Configure automated alerting for critical events in SIEM. 3. Assign responsibility for daily/weekly log review. 4. Document review cadence and evidence retention. 5. Train staff on log review procedures. |
| **Responsible Party** | IT Security Manager / SOC Lead |
| **Resources Required** | SIEM configuration effort, staff time (~80 hours), possible MDR/SOC vendor |
| **Milestone 1** | Day 30 — Draft Audit Log Review Policy |
| **Milestone 2** | Day 75 — SIEM alerting rules configured |
| **Milestone 3** | Day 100 — First documented log review cycle completed |
| **Milestone 4** | Day 130 — Policy approved; evidence of ongoing review collected |
| **Estimated Completion** | Day 130 (2026-09-02) |
| **Status** | Open |
| **Dependency** | Dependent on AU.L2-3.3.1 SIEM deployment |

---

### Finding 3 — CM.L2-3.4.1: Baseline Configurations
| Field | Details |
|---|---|
| **Practice ID** | CM.L2-3.4.1 |
| **Practice Description** | Establish and maintain baseline configurations and inventories of organizational systems (including hardware, software, firmware, and documentation) throughout the respective system development life cycles. |
| **Finding** | No formal baseline configurations are documented for servers, endpoints, or network devices. No configuration management database (CMDB) or inventory exists. |
| **Priority** | Medium (P3) |
| **Risk** | Unauthorized or insecure configurations may exist undetected. Deviations cannot be identified without a defined baseline. |
| **Remediation Actions** | 1. Conduct full hardware and software inventory of in-scope systems. 2. Select and implement a CMDB or asset management tool. 3. Develop security baseline configurations (e.g., CIS Benchmarks) for each system type. 4. Document baselines in SSP and Configuration Management Plan. 5. Implement a change control process to maintain baseline integrity. |
| **Responsible Party** | IT Operations Manager |
| **Resources Required** | CMDB tool (e.g., ServiceNow, Lansweeper), staff time (~200 hours) |
| **Milestone 1** | Day 30 — Complete asset inventory |
| **Milestone 2** | Day 60 — Select CMDB tool; begin deployment |
| **Milestone 3** | Day 90 — Draft baseline configurations for all system types |
| **Milestone 4** | Day 150 — Baselines approved, documented, and applied to all systems |
| **Estimated Completion** | Day 150 (2026-09-22) |
| **Status** | Open |

---

### Finding 4 — CM.L2-3.4.2: Security Configuration Enforcement
| Field | Details |
|---|---|
| **Practice ID** | CM.L2-3.4.2 |
| **Practice Description** | Establish and enforce security configuration settings for information technology products employed in organizational systems. |
| **Finding** | Security configuration settings (e.g., CIS hardening benchmarks) are not enforced. No automated mechanism ensures configurations remain compliant. |
| **Priority** | Medium (P3) |
| **Risk** | Systems may operate with insecure default or misconfigured settings, increasing attack surface. |
| **Remediation Actions** | 1. Map baseline configurations (from CM.L2-3.4.1) to CIS Benchmark or DISA STIG standards. 2. Implement a configuration management/compliance scanning tool (e.g., Tenable.sc, Qualys, or Chef InSpec). 3. Remediate deviations from approved baselines. 4. Document exception process for any approved deviations. 5. Schedule periodic compliance scans. |
| **Responsible Party** | IT Operations Manager / Security Engineer |
| **Resources Required** | Configuration compliance tool, staff time (~120 hours) |
| **Milestone 1** | Day 60 — Identify target security configuration standards |
| **Milestone 2** | Day 90 — Deploy compliance scanning tool |
| **Milestone 3** | Day 130 — First full compliance scan completed; deviations documented |
| **Milestone 4** | Day 160 — Deviations remediated; ongoing scan schedule established |
| **Estimated Completion** | Day 160 (2026-10-02) |
| **Status** | Open |
| **Dependency** | Dependent on CM.L2-3.4.1 baseline completion |

---

### Finding 5 — RA.L2-3.11.2: Vulnerability Scanning
| Field | Details |
|---|---|
| **Practice ID** | RA.L2-3.11.2 |
| **Practice Description** | Scan for vulnerabilities in organizational systems and applications periodically and when new vulnerabilities affecting those systems are identified; remediate vulnerabilities in accordance with assessments of risk. |
| **Finding** | No vulnerability scanning program exists. Systems have not been scanned for known vulnerabilities. No remediation tracking process is in place. |
| **Priority** | Critical (P1) |
| **Risk** | Known, exploitable vulnerabilities may exist across in-scope systems. This is a fundamental gap that directly threatens CUI confidentiality and system integrity. |
| **Remediation Actions** | 1. Acquire and deploy a vulnerability scanning solution (e.g., Tenable Nessus, Qualys, or Rapid7). 2. Conduct initial authenticated scan of all in-scope systems. 3. Prioritize and remediate Critical/High vulnerabilities (CVSS 7.0+) within 30 days of identification. 4. Establish monthly scanning cadence for all systems; weekly for internet-facing. 5. Create vulnerability remediation tracking process and integrate with POA&M. 6. Document vulnerability management policy. |
| **Responsible Party** | Security Engineer / ISSO |
| **Resources Required** | Vulnerability scanner licensing (~$5,000-$30,000/year depending on scope), staff time (~100 hours initial) |
| **Milestone 1** | Day 15 — Procure vulnerability scanning tool |
| **Milestone 2** | Day 30 — Complete initial scan of all in-scope systems |
| **Milestone 3** | Day 60 — Remediate all Critical (CVSS 9.0+) findings from initial scan |
| **Milestone 4** | Day 90 — Remediate all High (CVSS 7.0-8.9) findings; monthly scan cadence established |
| **Milestone 5** | Day 120 — Vulnerability management policy documented and approved |
| **Estimated Completion** | Day 120 (2026-08-23) |
| **Status** | Open |

---

### Finding 6 — CA.L2-3.12.4: Plan of Action
| Field | Details |
|---|---|
| **Practice ID** | CA.L2-3.12.4 |
| **Practice Description** | Develop, document, and periodically update plans of action designed to correct deficiencies and reduce or eliminate vulnerabilities in organizational systems. |
| **Finding** | No formal POA&M process exists. Security deficiencies have not been tracked or managed. This current document is the first POA&M developed. |
| **Priority** | Critical (P1) |
| **Risk** | Without a POA&M, the organization cannot demonstrate it is managing security deficiencies, which is a direct prerequisite for CMMC certification. This finding also indicates systemic governance gaps. |
| **Remediation Actions** | 1. Formally adopt this POA&M as the organization's deficiency tracking mechanism. 2. Assign a POA&M owner (ISSO or Security Manager). 3. Establish a POA&M review cadence (monthly recommended). 4. Develop POA&M management procedure documenting creation, update, and closure processes. 5. Integrate POA&M management with risk management framework. 6. Update SSP to reference POA&M process. |
| **Responsible Party** | ISSO / Security Manager |
| **Resources Required** | Staff time (~40 hours), possible GRC tool investment |
| **Milestone 1** | Day 7 — This POA&M formally adopted and signed by leadership |
| **Milestone 2** | Day 30 — POA&M management procedure drafted |
| **Milestone 3** | Day 45 — First monthly POA&M review meeting held |
| **Milestone 4** | Day 60 — Procedure approved; SSP updated to reference POA&M |
| **Estimated Completion** | Day 60 (2026-06-24) |
| **Status** | Open |

---

### Finding 7 — AC.L2-3.1.5: Least Privilege
| Field | Details |
|---|---|
| **Practice ID** | AC.L2-3.1.5 |
| **Practice Description** | Employ the principle of least privilege, including for specific security functions and privileged accounts. |
| **Finding** | Privileged accounts are broadly assigned. Users have excessive permissions beyond what is required for their roles. No formal role-based access control (RBAC) model is documented or enforced. Administrative accounts are used for routine tasks. |
| **Priority** | Critical (P1) |
| **Risk** | Excessive privileges significantly increase the blast radius of account compromise. Insider threat risk is elevated. This is a top-ranked exploitable condition in breach scenarios. |
| **Remediation Actions** | 1. Conduct a full access rights review for all user and service accounts. 2. Document a role-based access control (RBAC) matrix defining permissions by job function. 3. Revoke unnecessary privileges; separate administrative from standard user accounts. 4. Implement Privileged Access Management (PAM) controls (dedicated admin accounts, just-in-time access). 5. Prohibit use of privileged accounts for day-to-day activity. 6. Implement quarterly access reviews. 7. Document least privilege policy. |
| **Responsible Party** | IT Security Manager / Identity and Access Management Lead |
| **Resources Required** | PAM tooling (e.g., CyberArk, BeyondTrust, or open-source), staff time (~160 hours) |
| **Milestone 1** | Day 21 — Complete access rights audit; identify over-privileged accounts |
| **Milestone 2** | Day 45 — RBAC matrix documented and approved |
| **Milestone 3** | Day 60 — Excessive privileges revoked; dedicated admin accounts created |
| **Milestone 4** | Day 90 — PAM controls implemented for all privileged accounts |
| **Milestone 5** | Day 120 — First quarterly access review completed; policy documented |
| **Estimated Completion** | Day 120 (2026-08-23) |
| **Status** | Open |

---

### Finding 8 — SC.L2-3.13.8: Cryptographic Protection in Transit
| Field | Details |
|---|---|
| **Practice ID** | SC.L2-3.13.8 |
| **Practice Description** | Implement cryptographic mechanisms to prevent unauthorized disclosure of CUI during transmission unless otherwise protected by alternative physical safeguards. |
| **Finding** | Not all transmission paths carrying CUI use approved cryptographic protections. Some systems transmit CUI over unencrypted or weakly encrypted channels. TLS configurations may use deprecated protocols (TLS 1.0/1.1, weak cipher suites). |
| **Priority** | High (P2) |
| **Risk** | CUI transmitted over unprotected channels is vulnerable to interception. This directly violates the confidentiality requirement for CUI and could result in contract termination. |
| **Remediation Actions** | 1. Map all data flows involving CUI transmission (internal and external). 2. Identify all transmission paths lacking adequate encryption. 3. Disable TLS 1.0 and TLS 1.1; enforce TLS 1.2 minimum (TLS 1.3 preferred). 4. Review and harden cipher suite configurations on all systems. 5. Replace or configure any systems transmitting CUI without encryption. 6. Document approved cryptographic standards in SSP. 7. Scan for unencrypted transmission using network monitoring tools. |
| **Responsible Party** | Security Engineer / Network Administrator |
| **Resources Required** | TLS certificate management, staff time (~80 hours), possible network monitoring tools |
| **Milestone 1** | Day 21 — Complete CUI data flow mapping |
| **Milestone 2** | Day 45 — Identify all unprotected or weakly protected transmission paths |
| **Milestone 3** | Day 75 — Disable deprecated TLS versions; enforce minimum TLS 1.2 |
| **Milestone 4** | Day 100 — All CUI transmission paths verified as using approved encryption |
| **Milestone 5** | Day 120 — Cryptographic standards documented in SSP; scan evidence collected |
| **Estimated Completion** | Day 120 (2026-08-23) |
| **Status** | Open |

---

## Consolidated Milestone Schedule

| Day | Date | Milestone(s) |
|---|---|---|
| Day 7 | 2026-05-02 | CA.L2-3.12.4 — POA&M formally adopted by leadership |
| Day 15 | 2026-05-10 | RA.L2-3.11.2 — Vulnerability scanner procured |
| Day 21 | 2026-05-16 | AC.L2-3.1.5 — Access rights audit complete; SC.L2-3.13.8 — CUI data flow mapping complete |
| Day 30 | 2026-05-25 | AU.L2-3.3.1 — Logging gap analysis complete; CA.L2-3.12.4 — POA&M procedure drafted; CM.L2-3.4.1 — Asset inventory complete; RA.L2-3.11.2 — Initial vulnerability scan complete |
| Day 45 | 2026-06-09 | AC.L2-3.1.5 — RBAC matrix approved; CA.L2-3.12.4 — First POA&M review held; SC.L2-3.13.8 — Unprotected paths identified |
| Day 60 | 2026-06-24 | AC.L2-3.1.5 — Excessive privileges revoked; AU.L2-3.3.1 — Logging enabled on all systems; AU.L2-3.3.2 — Audit log review policy drafted; CA.L2-3.12.4 — Procedure approved, SSP updated; CM.L2-3.4.1 — CMDB deployed; CM.L2-3.4.2 — Security configuration standards identified; RA.L2-3.11.2 — Critical vulnerabilities (CVSS 9+) remediated |
| Day 75 | 2026-07-09 | AU.L2-3.3.2 — SIEM alerting configured; SC.L2-3.13.8 — TLS 1.0/1.1 disabled |
| Day 90 | 2026-07-24 | AC.L2-3.1.5 — PAM controls implemented; AU.L2-3.3.1 — SIEM deployed, retention enforced; CM.L2-3.4.1 — Baseline configurations drafted; CM.L2-3.4.2 — Compliance scanner deployed; RA.L2-3.11.2 — High vulnerabilities remediated; monthly scan cadence active |
| Day 100 | 2026-08-03 | AU.L2-3.3.2 — First documented review cycle complete; SC.L2-3.13.8 — All CUI paths verified encrypted |
| Day 120 | 2026-08-23 | AC.L2-3.1.5 — First quarterly access review; AU.L2-3.3.1 — SSP updated; RA.L2-3.11.2 — Vulnerability management policy approved; SC.L2-3.13.8 — SSP updated, evidence collected |
| Day 130 | 2026-09-02 | AU.L2-3.3.2 — Policy approved, ongoing review evidence; CM.L2-3.4.2 — First compliance scan; deviations documented |
| Day 150 | 2026-09-22 | CM.L2-3.4.1 — Baselines applied to all systems |
| Day 160 | 2026-10-02 | CM.L2-3.4.2 — Deviations remediated; ongoing scan schedule |
| Day 175 | 2026-10-17 | Final evidence review and POA&M closure verification |
| Day 180 | 2026-10-22 | **REMEDIATION DEADLINE — All findings closed or with accepted risk** |

---

## Resource Summary

| Finding | Responsible Party | Estimated Effort | Tooling Cost |
|---|---|---|---|
| AU.L2-3.3.1 | IT Security Manager | ~160 hours | SIEM ($10K-$50K/yr) |
| AU.L2-3.3.2 | IT Security Manager / SOC Lead | ~80 hours | Included in SIEM |
| CM.L2-3.4.1 | IT Operations Manager | ~200 hours | CMDB tool ($5K-$20K/yr) |
| CM.L2-3.4.2 | IT Operations / Security Engineer | ~120 hours | Config scanner ($5K-$15K/yr) |
| RA.L2-3.11.2 | Security Engineer / ISSO | ~100 hours | Vuln scanner ($5K-$30K/yr) |
| CA.L2-3.12.4 | ISSO / Security Manager | ~40 hours | GRC tool (optional) |
| AC.L2-3.1.5 | IT Security / IAM Lead | ~160 hours | PAM tool ($10K-$50K/yr) |
| SC.L2-3.13.8 | Security Engineer / Network Admin | ~80 hours | Cert management (minimal) |
| **Total** | | **~940 hours** | **~$35K-$165K/yr** |

---

## POA&M Management and Review

- **POA&M Owner:** [ISSO Name / Title]
- **Review Cadence:** Monthly (first Monday of each month)
- **Escalation Path:** ISSO -> CISO -> Senior Leadership
- **Next Review Date:** 2026-05-04
- **Evidence Repository:** [Location — e.g., SharePoint, GRC tool]

### Status Definitions
| Status | Definition |
|---|---|
| Open | Remediation not yet begun or in early planning |
| In Progress | Remediation activities underway |
| Delayed | Behind milestone schedule; requires escalation |
| Completed | Remediation actions finished; evidence collected |
| Accepted Risk | Risk formally accepted by authorizing official |

---

## Approval and Sign-Off

| Role | Name | Signature | Date |
|---|---|---|---|
| ISSO | | | |
| CISO / Security Director | | | |
| Authorizing Official | | | |

---

*This POA&M was created on 2026-04-25 in response to C3PAO CMMC Level 2 assessment findings. All milestones are subject to revision based on resource availability and emerging risk factors. Updates must be reviewed and approved by the ISSO.*
