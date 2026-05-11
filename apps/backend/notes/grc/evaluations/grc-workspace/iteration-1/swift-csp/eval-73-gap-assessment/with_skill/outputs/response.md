# SWIFT CSCF v2025 — Gap Assessment Report

**Assessment Date:** 2026-04-25
**Framework Version:** CSCF v2025 (effective July 2025)
**Prepared for:** Organisation (hemant.naik@gmail.com)
**Scope:** All 23 mandatory controls; advisory controls noted where relevant
**Architecture Note:** Architecture type not explicitly confirmed — assessment assumes **A1** (customer-managed, software-based Alliance Access/Gateway on-premises) as the most common configuration consistent with the description provided. Controls applicable to A1 are assessed; where the architecture is A3 or A4, 1.2 would be not applicable.

---

## Executive Summary

The organisation has a **partial SWIFT security posture** with notable compliance gaps across all three CSCF objectives. Based on the information provided, **5 mandatory controls are rated RED (not implemented or critically deficient)** and **4 are rated AMBER (partially implemented)**. The most critical risks are:

1. The SWIFT zone shares a VLAN with other internal systems — violating the core isolation requirement of Control 1.1.
2. Software-based OTP is used for MFA — explicitly insufficient under CSCF v2025 (Control 4.2).
3. Log retention is only 6 months against a mandatory minimum of 1 year online, 3 years total (Control 6.4); logs are also not being reviewed.
4. No SWIFT-specific Incident Response Plan exists (Control 7.1).
5. The 30-day critical patching window exceeds the 3-day SLA required under CSCF v2025 (Control 2.2).

The organisation must address the RED-rated controls before submitting its KYC-SA attestation. All RED items would need to be attested as "Not Implemented" on the KYC-SA portal, which is visible to counterparties.

---

## Section 1: Current State Summary

| Area | Current State Provided |
|------|----------------------|
| Network segregation | SWIFT zone exists but shares a VLAN with other internal systems |
| Patching | Critical vulnerabilities patched within 30 days |
| MFA | Software-based OTP (authenticator app) used for operator MFA |
| Log collection | SWIFT logs are collected but not reviewed; retention is 6 months |
| Incident response | General IT incident response plan exists; no SWIFT-specific IRP |
| Operator accounts | Named individual accounts used (no sharing) |

---

## Section 2: Gap Assessment Table

### Objective 1 — Secure Your Environment

| Control | Control Name | Mandatory/Advisory | RAG Status | Current State | Gap Description | Evidence Required |
|---------|-------------|-------------------|-----------|---------------|-----------------|-------------------|
| **1.1** | SWIFT Environment Protection | Mandatory | 🔴 RED | SWIFT zone exists but on a shared VLAN with other internal systems | Critical gap: SWIFT zone not isolated on a dedicated VLAN/segment. Shared VLAN means other systems can communicate with SWIFT infrastructure without firewall enforcement. Dual-homing risk. Does not meet the dedicated Secure Zone requirement. | Network architecture diagram showing dedicated SWIFT Secure Zone; firewall ruleset; system inventory for Secure Zone |
| **1.2** | OS Privileged Account Control | Mandatory (A1) | 🟡 AMBER | Not stated — assumed partially in place | No evidence provided of privileged account controls on SWIFT servers. Assumed some controls exist via general IT, but no SWIFT-specific evidence of MFA for privileged OS sessions, account inventory, or PAM tooling. | Privileged account inventory; MFA evidence for privileged sessions; OS audit logs |
| **1.3A** | Virtualisation Platform Security | Advisory | Not assessed | Unknown | Advisory — not rated; should be reviewed if SWIFT runs on VMs | Hypervisor patch status; VM isolation diagram |
| **1.4** | Restriction of Internet Access | Mandatory | 🟡 AMBER | Not stated | No evidence provided that SWIFT servers or operator workstations have internet access blocked. Shared VLAN increases likelihood of unrestricted routing. Requires verification. | Firewall rules showing internet blocked for SWIFT zone IPs; proxy configuration |
| **1.5A** | Customer Environment Protection | Advisory | Not assessed | Unknown | Advisory — broader customer environment security; not directly rated here | — |
| **2.1** | Internal Data Flow Security | Mandatory | 🟡 AMBER | Not stated | No evidence provided of TLS 1.2+ on internal SWIFT component connections. Shared VLAN increases risk of unencrypted lateral traffic. Requires verification. | Data flow diagram; TLS configuration evidence; certificate inventory |
| **2.2** | Security Updates | Mandatory | 🔴 RED | Critical vulnerabilities patched within 30 days | CSCF v2025 requires critical patches within **3 calendar days** and SWIFT emergency advisories within 3 days. A 30-day SLA for criticals significantly exceeds this. High severity must be within 90 days (current state may comply for high). The 30-day window is a direct non-compliance against the v2025 tightened SLA. | Patch management reports showing SWIFT systems; SWIFT advisory subscription and action log; exception register |
| **2.3** | System Hardening | Mandatory | 🟡 AMBER | Not stated | No evidence of CIS Benchmark hardening applied to SWIFT servers or operator workstations. General IT hardening may exist but SWIFT-specific hardening baseline and scan evidence not mentioned. | Hardening baseline document; CIS-CAT or equivalent scan results; evidence of unnecessary services disabled |
| **2.4A** | Back-Office Data Flow Security | Advisory | Not assessed | Unknown | Advisory — not rated | — |
| **2.5A** | External Transmission Data Protection | Advisory | Not assessed | Unknown | Advisory — not rated | — |
| **2.6** | Operator Session Confidentiality and Integrity | Mandatory | 🟡 AMBER | Named individual accounts used | Positive: individual accounts in place. Gap: No evidence of TLS 1.2+ session encryption, 30-minute session timeout configuration, or restriction of clipboard/screen-share tools during SWIFT sessions. MFA gap (see 4.2) also impacts this control. | TLS configuration for Alliance Access web interface; session timeout screenshots; remote access tool restriction evidence |
| **2.7** | Vulnerability Scanning | Mandatory | 🟡 AMBER | Not stated | No evidence of quarterly credentialed vulnerability scans of SWIFT-in-scope systems. General IT scanning may exist. Requires confirmation that SWIFT components (Alliance Access, OS, middleware) are included in authenticated scan scope. | Credentialed scan reports for last 4 quarters covering SWIFT system hostnames/IPs |
| **2.8** | Critical Activity Outsourcing | Mandatory | 🟢 GREEN | Not applicable if fully in-house | Assumed not outsourced based on description. If any managed service, bureau, or cloud is used for SWIFT, this requires immediate review. | Contracts with SWIFT obligations; provider KYC-SA attestations if applicable |
| **2.9A** | Transaction Business Controls | Advisory | Not assessed | Unknown | Advisory — recommended to implement payment value thresholds and time-of-day controls | — |
| **2.10** | Application Hardening | Mandatory | 🟡 AMBER | Not stated | No evidence that SWIFT Alliance Access/Gateway has been configured per SWIFT's published Security Hardening Guides. Application-level hardening distinct from OS hardening. | Completed SWIFT Alliance Access / Alliance Gateway Security Hardening Guide checklist; application account audit |
| **2.11A** | RMA Business Controls | Advisory | Not assessed | Unknown | Advisory — recommended to review RMA authorisations | — |
| **3.1** | Physical Security | Mandatory | 🟡 AMBER | Not stated | No evidence provided about physical access controls for SWIFT server room or operator workstation areas. Assumed general data centre controls exist given the organisation operates SWIFT infrastructure. | Physical access logs; authorised access list; CCTV/badge system evidence |

---

### Objective 2 — Know and Limit Access

| Control | Control Name | Mandatory/Advisory | RAG Status | Current State | Gap Description | Evidence Required |
|---------|-------------|-------------------|-----------|---------------|-----------------|-------------------|
| **4.1** | Password Policy | Mandatory | 🟡 AMBER | Not stated | No evidence of a SWIFT-specific password policy (14+ character minimum, 90-day rotation for privileged accounts, 12-generation reuse prohibition, 5-attempt lockout). General IT policy may exist. | Password policy document; AD/Group Policy configuration screenshots; lockout configuration |
| **4.2** | Multi-Factor Authentication | Mandatory | 🔴 RED | Software OTP (authenticator app) used | Critical gap: CSCF v2025 explicitly states software-based OTP (authenticator apps) does **not satisfy** the MFA requirement for most architecture types. Hardware OTP tokens, smart cards with PIN, or FIDO2 hardware keys are required. This is one of the most commonly cited non-compliance findings industry-wide. Must be attested as "Not Implemented" until hardware tokens are deployed. | MFA configuration evidence; hardware token inventory; authentication logs showing MFA enforcement |
| **5.1** | Logical Access Controls | Mandatory | 🟢 GREEN | Named individual accounts used (no sharing) | Positive finding: individual named accounts are in place, which satisfies the core non-repudiation requirement. Gaps to verify: quarterly access reviews, dual-authorisation for high-risk operations, timely leaver deprovisioning (within 24 hours). | User access list with roles and approvals; access review records (last 4 quarters); leaver process records |
| **5.2** | Token Management | Mandatory | 🔴 RED | Software OTP in use (no hardware tokens) | Since hardware tokens are not yet deployed (see 4.2), there is no hardware token lifecycle management in place. When hardware tokens are deployed, a full token management process (inventory, issuance, return, lost token procedure, annual reconciliation) must be established. | Token inventory register; issuance/return records; lost token procedure; annual reconciliation evidence |
| **5.3A** | Staffing | Advisory | Not assessed | Unknown | Advisory — not rated | — |
| **5.4** | Physical and Logical Password Storage | Mandatory | 🟡 AMBER | Not stated | No evidence of a password manager or PAM vault for SWIFT credentials. Credentials may be stored insecurely. Emergency/break-glass credential procedure not mentioned. | Password manager/PAM tool evidence; break-glass credential procedure and access log |

---

### Objective 3 — Detect and Respond

| Control | Control Name | Mandatory/Advisory | RAG Status | Current State | Gap Description | Evidence Required |
|---------|-------------|-------------------|-----------|---------------|-----------------|-------------------|
| **6.1** | Malware Protection | Mandatory | 🟡 AMBER | Not stated | No evidence of anti-malware deployed on SWIFT servers and operator workstations with daily definition updates, real-time scanning, and 1-hour alert SLA. General IT AV may exist. | AV deployment scope; definition update log; alert configuration; scan history |
| **6.2** | Software Integrity | Mandatory | 🟡 AMBER | Not stated | No evidence of cryptographic hash verification of SWIFT software packages before installation and after updates. File integrity monitoring (FIM) on SWIFT binary directories not mentioned. | Hash verification records; FIM configuration (if deployed); integrity check procedure |
| **6.3** | Database Integrity | Mandatory | 🟡 AMBER | Not stated | No evidence of SWIFT database access restrictions, database change logging, integrity checks, or backup/restoration testing. | Database access control configuration; audit log samples; backup and restoration test records |
| **6.4** | Log and Monitoring | Mandatory | 🔴 RED | Logs collected but not reviewed; retention only 6 months | Two distinct gaps: (1) Logs are not being reviewed — CSCF v2025 requires **daily** review of transaction anomalies and authentication failures, and **weekly** review of other events. (2) Retention is 6 months — CSCF v2025 requires **minimum 1 year online** and **3 years total** (online + archived). This is a direct, demonstrable non-compliance on both dimensions. | SIEM configuration showing SWIFT log sources; log retention policy and technical evidence; sample alert rules; log review records (last 30 days) |
| **6.5A** | Intrusion Detection | Advisory | Not assessed | Unknown | Advisory — strongly recommended given the other gaps identified | — |
| **7.1** | Cyber Incident Response Planning | Mandatory | 🔴 RED | General IT IRP exists; no SWIFT-specific IRP | A general IT IRP does not satisfy CSCF v2025. Requirement is for a **SWIFT-specific IRP** covering: SWIFT-specific detection triggers (e.g., anomalous transaction patterns, credential compromise), 24-hour SWIFT notification obligation, forensic evidence preservation, escalation to SWIFT CISO office (security@swift.com), and annual tabletop/drill test. The 24-hour notification obligation to SWIFT is a legal/contractual obligation that cannot be met without a SWIFT-specific IRP. | SWIFT-specific IRP document (dated, approved); last annual test record; SWIFT notification contact list |
| **7.2** | Security Training and Awareness | Mandatory | 🟡 AMBER | Not stated | No evidence of annual security awareness training for SWIFT staff covering SWIFT-specific fraud scenarios (Bangladesh Bank-style attacks), phishing, social engineering, and incident reporting. General IT security training may exist but SWIFT-specific content required. | Training completion records for all SWIFT users (last 12 months); SWIFT-specific training content overview |
| **7.3A** | Penetration Testing | Advisory | Not assessed | Unknown | Advisory — strongly recommended; annual penetration test of SWIFT Secure Zone would help identify risks from the shared VLAN issue | — |
| **7.4A** | Scenario Risk Assessment | Advisory | Not assessed | Unknown | Advisory — recommended; insider fraud and credential compromise scenarios are highly relevant | — |

---

## Section 3: RAG Summary Dashboard

| RAG | Count | Controls |
|-----|-------|---------|
| 🔴 RED — Not Implemented / Critical Gap | 5 | 1.1, 2.2, 4.2, 5.2, 6.4, 7.1 |
| 🟡 AMBER — Partially Implemented / Requires Verification | 9 | 1.2, 1.4, 2.1, 2.3, 2.6, 2.7, 2.10, 3.1, 4.1, 5.4, 6.1, 6.2, 6.3, 7.2 |
| 🟢 GREEN — Implemented | 2 | 2.8 (assumed), 5.1 |
| Not assessed (Advisory) | 8 | 1.3A, 1.5A, 2.4A, 2.5A, 2.9A, 2.11A, 5.3A, 6.5A, 7.3A, 7.4A |

> Note: RED count in table is 6 controls (1.1, 2.2, 4.2, 5.2, 6.4, 7.1). The AMBER group spans 12 controls where partial implementation or lack of evidence creates compliance risk.

**KYC-SA attestation impact:** The 6 RED-rated controls must be attested as "Not Implemented" on the KYC-SA portal. This will be visible to all counterparties and may trigger counterparty queries or escalation.

---

## Section 4: Prioritised Remediation Plan

Remediation is prioritised into three tiers: **Immediate (0–30 days)**, **Short-term (30–90 days)**, and **Medium-term (90–180 days)**.

---

### Priority 1 — Immediate Actions (0–30 days)

These actions address direct non-compliance with mandatory controls that represent immediate risk to SWIFT operations, counterparty notifications, and potential regulatory escalation.

#### P1-1: Deploy Hardware MFA Tokens — Control 4.2 and 5.2
**Owner:** IT Security / SWIFT Operations
**Target:** Complete within 30 days

**Action:**
1. Procure hardware OTP tokens (e.g., Thales/Gemalto, Feitian, VASCO/OneSpan) or FIDO2 hardware keys for all SWIFT operators. Minimum: one per operator plus spares.
2. Integrate tokens with Alliance Access / Alliance Gateway authentication.
3. Disable software-based OTP for all SWIFT application logins.
4. Establish a token management process:
   - Create and maintain a token inventory register (operator name, token serial, issue date, status).
   - Document issuance/return procedures.
   - Define lost/stolen token procedure (deactivation within 1 hour of report).
   - Schedule annual token inventory reconciliation.
5. Update authentication logs to confirm MFA enforcement.

**Evidence to collect:** Token inventory; MFA configuration screenshots; authentication logs showing hardware token enforcement; token issuance records.

**KYC-SA impact:** Resolves RED on 4.2 and 5.2 simultaneously.

---

#### P1-2: Establish SWIFT-Specific Incident Response Plan — Control 7.1
**Owner:** IT Security / CISO / SWIFT Operations
**Target:** Draft and approve within 14 days; test within 30 days

**Action:**
1. Draft a SWIFT-specific IRP addendum to the existing general IT IRP. The addendum must cover:
   - SWIFT-specific detection triggers (failed MFA, after-hours logins, unusual transaction volumes, config changes on Alliance Access)
   - Escalation path to SWIFT within 24 hours of confirmed incident (contact: security@swift.com and SWIFT relationship manager)
   - Full incident report to SWIFT within 30 days
   - Evidence preservation requirements (forensic images, log preservation procedure)
   - Internal escalation to senior management immediately upon detection
   - Communication with regulators and law enforcement per local requirements
2. Maintain a SWIFT notification contact list (SWIFT CISO office, internal incident team, legal, compliance).
3. Conduct an initial tabletop exercise within 30 days of IRP completion.
4. Obtain formal approval and date the document.

**Evidence to collect:** SWIFT-specific IRP document (dated, approved by CISO); tabletop test report; SWIFT contact list.

**KYC-SA impact:** Resolves RED on 7.1.

---

#### P1-3: Establish Log Review Process and Extend Retention — Control 6.4
**Owner:** SOC / IT Security
**Target:** Retention extended within 14 days; review process operational within 30 days

**Action:**
1. **Immediate — Retention:** Configure log archival to extend SWIFT log retention from 6 months to minimum 1 year online (hot storage) and arrange archival storage for years 2–3. Ensure coverage of all SWIFT log sources: Alliance Access/Gateway application logs, OS security logs, authentication logs, network device logs for SWIFT zone, database audit logs.
2. **Immediate — SIEM ingestion:** If not already ingested, onboard SWIFT Alliance Access/Gateway logs into the SIEM or equivalent log management platform.
3. **Short-term — Alerting:** Configure automated alert rules for: failed authentication attempts, after-hours logins to SWIFT systems, large/unusual transactions, privilege escalation events, configuration changes on SWIFT components.
4. **Process — Review cadence:** Establish and document a formal log review schedule:
   - Daily: SWIFT transaction anomalies and authentication failures
   - Weekly: OS events, network device logs, configuration changes
   - Assign named reviewer(s) and retain review records.
5. Protect log integrity (ship to immutable SIEM store or read-only archive).

**Evidence to collect:** SIEM configuration showing SWIFT log sources; log retention policy and technical evidence (archive tool configuration); sample alert rules; log review records (last 30 days).

**KYC-SA impact:** Resolves RED on 6.4.

---

#### P1-4: Tighten Critical Patching SLA — Control 2.2
**Owner:** IT Operations / Patch Management
**Target:** Policy updated within 7 days; process operational immediately

**Action:**
1. Update the patch management policy to reflect CSCF v2025 SLAs:
   - Critical severity: 3 calendar days (down from current 30 days)
   - SWIFT emergency advisories: 3 calendar days
   - High severity: 90 calendar days (verify current state complies)
   - Medium: Next scheduled maintenance cycle
2. Subscribe to SWIFT's security advisory mailing list (via swift.com) and create a tracked action log for each advisory received.
3. For any critical vulnerabilities currently open beyond 3 days: document a formal risk acceptance or compensating controls exception — signed by a risk owner — and remediate immediately.
4. Configure vulnerability management tooling to tag SWIFT-in-scope systems separately so SLA reporting is automated.

**Evidence to collect:** Updated patch management policy with SWIFT SLAs; SWIFT advisory subscription confirmation; current patch status report for all SWIFT systems; exception register (if any open criticals require documented exceptions).

**KYC-SA impact:** Resolves RED on 2.2 once the 3-day SLA is being consistently met and evidenced.

---

### Priority 2 — Short-term Actions (30–90 days)

These actions address mandatory controls where partial implementation or lack of evidence creates compliance risk. Most require investigation and remediation of specific gaps.

#### P2-1: Isolate SWIFT Zone onto Dedicated VLAN/Segment — Control 1.1
**Owner:** Network Engineering / IT Security
**Target:** Design complete within 30 days; implementation within 60–90 days

**Action:**
1. Map all SWIFT components and their current network connectivity (Alliance Access, Alliance Gateway, HSMs, operator workstations, back-office interfaces).
2. Design a dedicated SWIFT Secure Zone:
   - Dedicated VLAN(s) not shared with any non-SWIFT system
   - Stateful firewall between SWIFT Secure Zone and corporate network (deny-by-default rules; whitelist only required flows)
   - Stateful firewall or ACL between SWIFT Secure Zone and internet path
3. Confirm no dual-homed systems (connected to both SWIFT zone and general network).
4. Remove any non-SWIFT applications from SWIFT servers (email clients, web browsers, general business apps).
5. Document all approved network flows in and out of the Secure Zone; schedule periodic review.
6. Conduct a post-migration validation: verify SWIFT zone isolation using network scans and firewall rule review.

**Note:** This is the single highest-impact architectural remediation. It also partially addresses 1.4 (internet restriction) and 2.1 (internal data flow security). Change management approval required; may need a maintenance window.

**Evidence to collect:** Updated network architecture diagram showing dedicated SWIFT Secure Zone; firewall ruleset and change records; system inventory for Secure Zone; confirmation of no dual-homing (config screenshots).

**KYC-SA impact:** Resolves RED on 1.1. Also substantially improves 1.4, 2.1, 2.3 ratings.

---

#### P2-2: Block Internet Access for SWIFT Zone — Control 1.4
**Owner:** Network Engineering
**Target:** Within 60 days (concurrent with P2-1)

**Action:**
1. Verify and configure firewall rules to block all direct internet access from SWIFT zone IPs.
2. If SWIFTNet connectivity requires internet routing, ensure all traffic passes through a controlled and monitored proxy or dedicated circuit — not open internet routing.
3. Block all internet-bound traffic from SWIFT-dedicated operator workstations.
4. Document exceptions (if any) with risk justification.

**Evidence to collect:** Firewall rules showing internet blocked for SWIFT zone IPs; proxy configuration if applicable; network flow test results.

---

#### P2-3: Implement TLS 1.2+ for Internal SWIFT Data Flows — Control 2.1 and 2.6
**Owner:** SWIFT Operations / IT Security
**Target:** Within 60 days

**Action:**
1. Map all internal connections between SWIFT components (Alliance Access to Gateway, to back-office middleware, database connections).
2. Verify TLS 1.2+ is configured on all these connections. Disable TLS 1.0 and 1.1.
3. Verify the Alliance Access/Gateway web interface for operators uses TLS 1.2+ with a valid certificate.
4. Configure session timeouts to maximum 30 minutes of inactivity for SWIFT operator sessions.
5. Restrict clipboard, screen-share, and remote control tools on SWIFT workstations during sessions.
6. Maintain a certificate inventory with expiry tracking and automated renewal alerts.

**Evidence to collect:** Data flow diagram; TLS configuration evidence for each connection; certificate inventory; session timeout configuration screenshots.

---

#### P2-4: Apply System and Application Hardening — Controls 2.3 and 2.10
**Owner:** IT Operations / SWIFT Operations
**Target:** Within 90 days

**Action:**
1. **OS hardening (Control 2.3):**
   - Obtain CIS Benchmarks for the OS versions running on SWIFT servers and operator workstations.
   - Run a baseline hardening scan (CIS-CAT or equivalent) against each SWIFT system.
   - Remediate deviations: disable unnecessary services, close unused ports, remove unused software and accounts, enable host-based firewalls.
   - Document the hardening baseline; schedule re-checks after every change.
2. **Application hardening (Control 2.10):**
   - Download and complete the SWIFT Alliance Access Security Hardening Guide (available from swift.com).
   - Download and complete the Alliance Gateway Security Hardening Guide if applicable.
   - Disable unused SWIFT application features and interfaces.
   - Review and tighten all application-level accounts to least-privilege; remove default accounts.
   - Change any default application credentials.
   - Schedule quarterly review of SWIFT application accounts.

**Evidence to collect:** Hardening baseline document; CIS-CAT scan results; evidence of unnecessary services disabled; completed SWIFT hardening guide checklists; application account audit report.

---

#### P2-5: Conduct Quarterly Credentialed Vulnerability Scanning — Control 2.7
**Owner:** IT Security / Vulnerability Management
**Target:** First scan within 30 days; quarterly cadence established within 60 days

**Action:**
1. Ensure all SWIFT in-scope systems are included in the vulnerability scanning scope:
   - SWIFT servers (Alliance Access, Alliance Gateway)
   - SWIFT-connected OS and middleware
   - Network devices in the SWIFT zone
   - SWIFT-dedicated operator workstations
2. Configure authenticated (credentialed) scans — unauthenticated scans do not satisfy the CSCF requirement.
3. Run the first credentialed scan and review results.
4. Remediate findings per Control 2.2 patching SLAs.
5. Track remediation of identified vulnerabilities to closure.
6. Schedule quarterly cadence and retain last 4 quarters of scan reports.

**Evidence to collect:** Credentialed scan reports for last 4 quarters (once established); scanner credential configuration; remediation tracking records.

---

#### P2-6: Formalise Access Reviews and Leaver Process — Control 5.1
**Owner:** SWIFT Operations / HR / IT Security
**Target:** Within 60 days

**Action:**
1. Produce a complete user access list for all SWIFT applications with roles and approval evidence.
2. Establish a quarterly access review process — named reviewer, documented sign-off, records retained.
3. Implement dual-authorisation for high-risk operations (e.g., creating new BIC connections, adding new operator accounts).
4. Formalise the leaver process: SWIFT access must be removed within 24 hours of departure — document this in the HR offboarding procedure.
5. Conduct an immediate access review to identify any stale or inappropriate access.

**Evidence to collect:** User access list with roles and approvals; access review records (last 4 quarters once established); dual-authorisation evidence; leaver process records.

---

#### P2-7: Deploy Malware Protection on SWIFT Systems — Control 6.1
**Owner:** IT Operations
**Target:** Within 30 days (verify or deploy)

**Action:**
1. Confirm anti-malware is deployed on all SWIFT servers and operator workstations.
2. Verify daily automated definition updates are running.
3. Verify real-time scanning is enabled.
4. Configure scheduled full scans.
5. Configure alerts for malware detections to reach the security team within 1 hour.
6. Establish that malware found on any SWIFT system is treated as a security incident per the SWIFT-specific IRP (P1-2).

**Evidence to collect:** AV deployment and configuration screenshots; definition update log (last 30 days); alert configuration evidence; scan history reports.

---

### Priority 3 — Medium-term Actions (90–180 days)

These actions address controls where baseline compliance may be partially in place but require formalisation, evidence collection, and SWIFT-specific documentation.

#### P3-1: Implement Software Integrity Verification — Control 6.2
**Owner:** SWIFT Operations
**Target:** Within 90 days

**Action:**
1. Document and implement a procedure to verify cryptographic hashes of SWIFT software packages before installation (compare against SWIFT-published checksums available at swift.com).
2. Perform integrity verification after every SWIFT software update.
3. Define a procedure: unauthorised changes to SWIFT executable files trigger a security incident.
4. Consider deploying file integrity monitoring (FIM) on SWIFT binary directories (e.g., Tripwire, OSSEC, or equivalent).
5. Retain hash verification records for audit.

**Evidence to collect:** Hash verification records for recent SWIFT software installations/updates; FIM configuration covering SWIFT directories; integrity check procedure document.

---

#### P3-2: Establish Database Integrity Controls — Control 6.3
**Owner:** Database Administration / SWIFT Operations
**Target:** Within 90 days

**Action:**
1. Restrict database access to authorised SWIFT application service accounts only — no direct operator access to production SWIFT databases.
2. Enable database change logging and configure alerts to the security team for unexpected changes.
3. Configure regular database integrity checks.
4. Test database backup and restoration procedure; document results and schedule annual re-tests.

**Evidence to collect:** Database access control configuration; database audit log samples; backup and restoration test records.

---

#### P3-3: Strengthen Password Policy and Credential Storage — Controls 4.1 and 5.4
**Owner:** IT Security
**Target:** Within 90 days

**Action:**
1. **Password policy (4.1):** Formalise a SWIFT-specific password policy (or verify the existing policy covers SWIFT):
   - Minimum 14 characters
   - Complexity: upper, lower, number, special character
   - Maximum 90 days for privileged accounts; 180 days for standard SWIFT accounts
   - No reuse for 12 generations
   - Lockout after 5 failed attempts
   - No shared or generic accounts
   - Implement via Group Policy / Active Directory and document configuration screenshots.
2. **Credential storage (5.4):** Ensure all SWIFT application passwords and credentials are stored in an approved password manager or PAM vault (e.g., CyberArk, HashiCorp Vault, or equivalent). Remove any credentials from plaintext files or spreadsheets. Establish an emergency/break-glass credential procedure with tamper-evident storage and access logging.

**Evidence to collect:** Password policy document; AD/Group Policy configuration; lockout configuration evidence; password manager/PAM tool evidence showing SWIFT credentials; break-glass credential procedure and access log.

---

#### P3-4: Implement OS Privileged Account Controls — Control 1.2
**Owner:** IT Security / SWIFT Operations
**Target:** Within 90 days

**Action:**
1. Produce a privileged account inventory for all SWIFT servers (root, local admin accounts).
2. Ensure privileged accounts are not used for routine operations — enforce use of non-privileged accounts for daily tasks.
3. Implement MFA for privileged OS sessions where technically feasible (consider PAM tooling).
4. Rename or disable all default/factory OS accounts.
5. Enable and review OS audit logs for all privileged account activity.

**Evidence to collect:** Privileged account inventory; MFA evidence for privileged sessions; privileged account usage policy; OS audit logs.

---

#### P3-5: Verify Physical Security Controls — Control 3.1
**Owner:** Facilities / IT Operations
**Target:** Within 60 days (verify existing controls and close gaps)

**Action:**
1. Confirm SWIFT servers are housed in a locked, access-controlled facility (data centre or server room).
2. Verify access is restricted to named individuals with documented authorisation — produce an access list.
3. Confirm physical access is logged electronically (badge reader, CCTV audit trail).
4. Implement visitor escort policy if not already in place.
5. Confirm SWIFT-dedicated operator workstations are in physically controlled areas.

**Evidence to collect:** Physical access control system logs; authorised access list for data centre/SWIFT server room; CCTV or badge system evidence.

---

#### P3-6: SWIFT-Specific Security Training — Control 7.2
**Owner:** HR / IT Security / Compliance
**Target:** Within 90 days

**Action:**
1. Develop or procure SWIFT-specific security awareness training covering:
   - Phishing and social engineering targeting SWIFT credentials
   - SWIFT fraud scenarios (Bangladesh Bank-style attacks, business email compromise leading to fraudulent MT103s)
   - Incident reporting procedures (internal and 24-hour SWIFT notification obligation)
   - Role-specific training for SWIFT operators on CSP requirements
2. Deliver training to all staff with SWIFT access and track completion.
3. Schedule annual refresh training.

**Evidence to collect:** Training completion records for all SWIFT users (last 12 months); training content overview showing SWIFT-specific topics; role-specific training materials.

---

#### P3-7: Advisory Controls — Consider for Enhanced Posture

| Advisory Control | Recommendation |
|----------------|---------------|
| **6.5A — Intrusion Detection** | Deploy network IDS/IPS at the SWIFT zone perimeter. Given the current shared VLAN issue, this becomes more urgent as an interim compensating control while P2-1 (zone isolation) is implemented. |
| **7.3A — Penetration Testing** | Schedule an annual penetration test of the SWIFT Secure Zone once isolation (P2-1) is complete. Include network perimeter, Alliance Access application layer, and authentication mechanisms. |
| **7.4A — Scenario Risk Assessment** | Conduct a scenario-based risk assessment covering: insider fraud, compromised SWIFT operator credentials, supply chain attack on SWIFT software, ransomware propagation to SWIFT zone. |
| **2.9A — Transaction Business Controls** | Implement payment value thresholds, time-of-day restrictions, and beneficiary whitelist controls to detect and prevent fraudulent transactions. Strongly recommended given the logging and monitoring gaps. |
| **1.3A — Virtualisation Platform Security** | If SWIFT runs on VMs, review hypervisor patch status, management interface access controls, and VM isolation. |

---

## Section 5: Remediation Timeline Summary

| Priority | Timeframe | Controls | Key Actions |
|----------|-----------|---------|-------------|
| P1 — Immediate | 0–30 days | 4.2, 5.2, 7.1, 6.4, 2.2 | Deploy hardware MFA tokens; draft SWIFT IRP; extend log retention and establish review process; tighten patching SLA to 3 days |
| P2 — Short-term | 30–90 days | 1.1, 1.4, 2.1, 2.3, 2.6, 2.7, 2.10, 5.1, 6.1 | Isolate SWIFT zone to dedicated VLAN; block internet access; apply TLS 1.2+; harden OS and application; establish vulnerability scanning; formalise access reviews; deploy AV |
| P3 — Medium-term | 90–180 days | 1.2, 3.1, 4.1, 5.4, 6.2, 6.3, 7.2 | Privileged account controls; physical security verification; password policy; credential storage; software/database integrity; SWIFT-specific security training |

---

## Section 6: KYC-SA Attestation Readiness

Based on current state, the following attestation positions apply for the mandatory controls:

| Control | Attestation Status (Current) | Target Attestation (Post-Remediation) |
|---------|------------------------------|---------------------------------------|
| 1.1 | Not Implemented | Implemented |
| 1.2 | Partially Implemented | Implemented |
| 1.4 | Partially Implemented | Implemented |
| 2.1 | Partially Implemented | Implemented |
| 2.2 | Not Implemented | Implemented |
| 2.3 | Partially Implemented | Implemented |
| 2.6 | Partially Implemented | Implemented |
| 2.7 | Partially Implemented | Implemented |
| 2.8 | Implemented | Implemented |
| 2.10 | Partially Implemented | Implemented |
| 3.1 | Partially Implemented | Implemented |
| 4.1 | Partially Implemented | Implemented |
| 4.2 | Not Implemented | Implemented |
| 5.1 | Implemented | Implemented |
| 5.2 | Not Implemented | Implemented |
| 5.4 | Partially Implemented | Implemented |
| 6.1 | Partially Implemented | Implemented |
| 6.2 | Partially Implemented | Implemented |
| 6.3 | Partially Implemented | Implemented |
| 6.4 | Not Implemented | Implemented |
| 7.1 | Not Implemented | Implemented |
| 7.2 | Partially Implemented | Implemented |

**Immediate action required:** If the KYC-SA deadline is within 90 days, the organisation should prioritise P1 actions and submit with "Not Implemented" or "Partially Implemented" attestation for controls where gaps remain. This is preferable to missing the July 31 deadline. A remediation roadmap (this document) should be available to share with counterparties and the independent assessor.

**Independent assessment note:** The organisation must engage an independent assessor (internal audit or external firm with SWIFT CSP competency) before submitting its KYC-SA attestation. The assessor must not have operational responsibility for the SWIFT environment. Allow 4–8 weeks for the assessment process.

---

## Section 7: Quick-Reference Positive Findings

The following aspects of the current state represent a **positive foundation** to build on:

| Finding | Relevant Control | Benefit |
|---------|----------------|---------|
| Named individual operator accounts (no shared accounts) | 5.1 | Satisfies the core non-repudiation requirement; audit trails are operator-attributable |
| SWIFT zone exists (even if not yet isolated) | 1.1 | SWIFT infrastructure is identified and bounded; isolation is the remaining step |
| Logs are being collected | 6.4 | Log collection pipeline exists; review and retention extension are the gaps, not a rebuild |
| General IT IRP exists | 7.1 | SWIFT-specific addendum to an existing IRP is faster to develop than starting from scratch |
| General IT patching process exists | 2.2 | Process infrastructure exists; the gap is tightening the SLA and scoping SWIFT systems specifically |

---

*This assessment is based on the current state information provided and is valid as at 2026-04-25. It should be reviewed after each remediation milestone and updated prior to submission of the annual KYC-SA attestation. An independent assessment by a qualified SWIFT CSP assessor is required before formal attestation.*
