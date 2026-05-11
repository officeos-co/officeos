# SWIFT CSP Architecture Scoping and CSCF v2025 Applicability Matrix

**Institution Profile:** Mid-tier regional bank  
**Connectivity:** Alliance Access software, customer-managed, on-premises servers  
**Hardware Security Modules (HSMs):** None  
**Service Bureau:** Not used  
**Cloud SWIFT:** Not used  
**Framework Version:** CSCF v2025 (effective July 2025)  
**Assessment Date:** 2026-04-25

---

## 1. Architecture Type Determination

Based on the described connectivity profile, your institution is classified as **Architecture Type A1**.

### Rationale

| Criterion | Your Environment | A1 Criteria |
|-----------|-----------------|-------------|
| Connector type | Customer connector | Customer connector |
| Management model | Customer-managed (your own staff, your own servers) | Customer-managed |
| Implementation type | Software-based (Alliance Access) | Software-based |
| HSMs | None | Not required for A1 |
| Service bureau | Not used | Not applicable |
| Cloud hosting | Not used | Not applicable |

**Conclusion:** Alliance Access running on your own on-premises servers, without HSMs, without outsourcing to a service bureau, and without SWIFT-hosted or cloud components, is the defining profile of an **A1 (Customer Connector, Customer-Managed, Software-Based)** architecture. This is the most common architecture for large and mid-tier banks with direct SWIFT connectivity.

### A1 Characteristics Relevant to Compliance Scope

As an A1 institution, you bear **full operational responsibility** for your SWIFT infrastructure. You own and manage:

- The servers running Alliance Access
- The operating systems and middleware on those servers
- Physical security of the data centre housing the servers
- All network controls protecting the SWIFT Secure Zone
- All operator access mechanisms

This means you have the **broadest mandatory control scope** among all architecture types. Unlike A3 (SWIFT-managed component) or B (service bureau), there is no shared responsibility with a third party to reduce your obligation. All 23 mandatory controls apply to you in full.

---

## 2. CSCF v2025 Applicability Matrix — Architecture Type A1

The following matrix provides the full picture of all 31 CSCF v2025 controls for an A1 institution. Controls marked as Not Applicable (N/A) for other types (A3/A4) that become applicable for A1 are highlighted.

### Legend

| Symbol | Meaning |
|--------|---------|
| **MANDATORY** | Must be implemented and attested; non-compliance must be disclosed in KYC-SA |
| **ADVISORY** | Strongly recommended best practice; not subject to mandatory attestation but assessed for maturity |
| N/A | Not applicable to this architecture type (none for A1) |

---

### Objective 1 — Secure Your Environment (Controls 1.x, 2.x, 3.x)

| Control ID | Control Name | A1 Status | Notes for A1 Institutions |
|------------|-------------|-----------|--------------------------|
| **1.1** | SWIFT Environment Protection | **MANDATORY** | Establish a dedicated Secure Zone isolating all Alliance Access servers and SWIFT workstations. Firewall with deny-by-default rules between SWIFT zone and corporate network. No dual-homing. |
| **1.2** | OS Privileged Account Control | **MANDATORY** | A1-specific: You manage your own OS, so you must control root/local admin accounts on Alliance Access servers. No routine use of privileged accounts; MFA for privileged sessions; all privileged activity logged. (Note: this control is N/A for A3 and A4 where the OS is managed by SWIFT.) |
| **1.3A** | Virtualisation Platform Security | **ADVISORY** | If Alliance Access runs on VMs, apply hypervisor hardening, patch the hypervisor, restrict management console access, and isolate SWIFT VMs from general VMs. |
| **1.4** | Restriction of Internet Access | **MANDATORY** | Alliance Access servers must have no direct internet access. SWIFT-dedicated operator workstations must have internet access blocked. Jump servers/proxies must not be internet-facing. |
| **1.5A** | Customer Environment Protection | **ADVISORY** | Extend security controls beyond the Secure Zone to protect your broader IT environment from threats that could cascade into the SWIFT zone. |
| **2.1** | Internal Data Flow Security | **MANDATORY** | All connections between Alliance Access, middleware, and back-office systems must use TLS 1.2+. Message broker (MQ etc.) connections must be authenticated and encrypted. |
| **2.2** | Security Updates | **MANDATORY** | Apply patches to Alliance Access, the underlying OS, and all middleware within CSCF v2025 SLAs: Critical/Emergency SWIFT advisory = 3 calendar days; High = 90 calendar days. Note: v2025 tightened the critical SLA from 7 days to 3 days. |
| **2.3** | System Hardening | **MANDATORY** | Apply CIS Benchmarks (or equivalent) to all Alliance Access servers and SWIFT workstations. Disable all unnecessary services, ports, and protocols. Enforce host-based firewalls. |
| **2.4A** | Back-Office Data Flow Security | **ADVISORY** | Protect SWIFT transaction data as it flows between Alliance Access and back-office/ERP systems. Encryption and authentication of back-office interfaces recommended. |
| **2.5A** | External Transmission Data Protection | **ADVISORY** | Encrypt SWIFT-related data transmitted outside your environment (e.g., to regulators, auditors, correspondent banks via non-SWIFT channels). |
| **2.6** | Operator Session Confidentiality and Integrity | **MANDATORY** | All operator sessions to Alliance Access must use TLS 1.2+ or equivalent. Session timeouts of maximum 30 minutes of inactivity. Clipboard and screen-share tools restricted during sessions. |
| **2.7** | Vulnerability Scanning | **MANDATORY** | Quarterly credentialed (authenticated) vulnerability scans of all in-scope SWIFT systems including Alliance Access servers, OS, middleware, and network devices in the Secure Zone. Results must be remediated per Control 2.2 SLAs. |
| **2.8** | Critical Activity Outsourcing | **MANDATORY** | If any SWIFT-related activity is outsourced (e.g., managed SOC, external patching teams), the outsourced party must comply with applicable CSCF controls. Contracts must include SWIFT CSP obligations. Annual review of provider compliance evidence required. |
| **2.9A** | Transaction Business Controls | **ADVISORY** | Implement business-level controls to detect and prevent fraudulent SWIFT transactions: payment value thresholds, expected transaction patterns, time-of-day restrictions, beneficiary whitelisting. |
| **2.10** | Application Hardening | **MANDATORY** | Configure Alliance Access per SWIFT's published Security Hardening Guide. Disable unused features and interfaces. Configure application accounts with least privilege. Change all default passwords. Review application accounts quarterly. |
| **2.11A** | RMA Business Controls | **ADVISORY** | Control and monitor Relationship Management Application (RMA) authorisations to restrict which counterparties can send you messages and which message types are permitted. |
| **3.1** | Physical Security | **MANDATORY** | Alliance Access servers must be housed in a locked, access-controlled facility. Access restricted to named, authorised individuals. Physical access logged electronically (badge reader or equivalent). Visitor access controlled and escorted. |

---

### Objective 2 — Know and Limit Access (Controls 4.x, 5.x)

| Control ID | Control Name | A1 Status | Notes for A1 Institutions |
|------------|-------------|-----------|--------------------------|
| **4.1** | Password Policy | **MANDATORY** | Minimum 14-character passwords; complexity required; maximum 90-day age for privileged accounts / 180 days for standard; no reuse for 12 generations; account lockout after 5 failed attempts; no shared accounts. |
| **4.2** | Multi-Factor Authentication | **MANDATORY** | MFA mandatory for all interactive logins to Alliance Access and all remote administrative access to SWIFT systems. **CSCF v2025 explicitly requires hardware OTP tokens, smart cards with PIN, or FIDO2 hardware keys. Software-based OTP authenticator apps do not satisfy this requirement for A1.** This is the most commonly failed control in assessments. |
| **5.1** | Logical Access Controls | **MANDATORY** | Individual named accounts for every SWIFT operator — no shared accounts. Role-based access with least privilege. Dual authorisation for high-risk operations. Quarterly access reviews. Remove access within 24 hours of departure. |
| **5.2** | Token Management | **MANDATORY** | Maintain a token inventory for all SWIFT operators. Lost/stolen tokens deactivated within 1 hour. Formal approval process for token issuance. Token return process for leavers. Annual inventory reconciliation. |
| **5.3A** | Staffing | **ADVISORY** | Implement personnel security measures (background screening, role separation, awareness) for staff with SWIFT access. |
| **5.4** | Physical and Logical Password Storage | **MANDATORY** | SWIFT application passwords and credentials stored in an approved password manager or PAM vault (e.g., CyberArk). No plaintext storage. Emergency/break-glass credentials in sealed, tamper-evident envelopes with access logging. Default credentials changed on installation. |

---

### Objective 3 — Detect and Respond (Controls 6.x, 7.x)

| Control ID | Control Name | A1 Status | Notes for A1 Institutions |
|------------|-------------|-----------|--------------------------|
| **6.1** | Malware Protection | **MANDATORY** | Anti-malware deployed on all Alliance Access servers and SWIFT operator workstations. Definitions updated daily (automated). Real-time scanning enabled. Malware detections on SWIFT systems treated as security incidents per Control 7.1. |
| **6.2** | Software Integrity | **MANDATORY** | Verify cryptographic hash of Alliance Access software packages before installation and after every update, comparing against SWIFT-published checksums. Unauthorised changes to SWIFT executables must trigger an incident. File integrity monitoring (FIM) recommended for SWIFT binary directories. |
| **6.3** | Database Integrity | **MANDATORY** | Restrict database access to authorised SWIFT application service accounts only. No direct operator access to production databases. Database change logging enabled. Regular integrity checks. Backups tested with documented restoration procedures. |
| **6.4** | Log and Monitoring | **MANDATORY** | All Alliance Access application logs, OS security logs, authentication logs, Secure Zone network device logs, and database audit logs must be collected. **CSCF v2025 retention: 1 year online/hot; 3 years total (online + archived).** Daily review of SWIFT transaction anomalies and authentication failures. SIEM must ingest SWIFT log sources. Log integrity protected (immutable SIEM or read-only store). |
| **6.5A** | Intrusion Detection | **ADVISORY** | Deploy network or host-based intrusion detection (IDS/IPS) for the SWIFT Secure Zone. Alerts for reconnaissance, lateral movement, and anomalous traffic patterns. |
| **7.1** | Cyber Incident Response Planning | **MANDATORY** | Maintain a documented SWIFT-specific Incident Response Plan covering detection, triage, containment, SWIFT notification, investigation, recovery, and lessons learned. **SWIFT must be notified within 24 hours of a confirmed cyber incident affecting SWIFT infrastructure or transactions; full report within 30 days.** IRP tested annually (tabletop or live drill). |
| **7.2** | Security Training and Awareness | **MANDATORY** | Annual security awareness training for all staff with SWIFT access, covering phishing, social engineering, SWIFT fraud scenarios (e.g., Bangladesh Bank-style attacks), and incident reporting. Role-specific training for SWIFT operators. Training completion tracked and evidenced. |
| **7.3A** | Penetration Testing | **ADVISORY** | Annual penetration test of SWIFT Secure Zone including network perimeter, Alliance Access application layer, and authentication mechanisms. Red-team exercise every 2–3 years is best practice. |
| **7.4A** | Scenario Risk Assessment | **ADVISORY** | Conduct scenario-based risk assessments for SWIFT-specific attack scenarios: insider fraud, compromised operator credentials, supply chain attack on SWIFT software, social engineering targeting SWIFT operators, ransomware propagation to SWIFT zone. |

---

## 3. Summary Count — A1 Architecture

| Category | Count | Controls |
|----------|-------|---------|
| **Mandatory** | **23** | 1.1, 1.2, 1.4, 2.1, 2.2, 2.3, 2.6, 2.7, 2.8, 2.10, 3.1, 4.1, 4.2, 5.1, 5.2, 5.4, 6.1, 6.2, 6.3, 6.4, 7.1, 7.2 + (see note) |
| **Advisory** | **8** | 1.3A, 1.5A, 2.4A, 2.5A, 2.9A, 2.11A, 5.3A, 6.5A, 7.3A, 7.4A |
| **Not Applicable** | **0** | None — A1 has the broadest applicability of all architecture types |

> **Note on count:** The CSCF v2025 specifies 23 mandatory controls across all architecture types. For A1, all 23 mandatory controls listed in the full framework apply — unlike A3 and A4 where Control 1.2 (OS Privileged Account Control) is Not Applicable because the OS is managed by SWIFT. As an A1 institution, Control 1.2 is fully mandatory.

---

## 4. A1-Specific Compliance Considerations

### Controls That Become Mandatory Only for A1 (vs. Other Types)

**Control 1.2 — OS Privileged Account Control**

This control is **mandatory for A1** but is **Not Applicable for A3 and A4**. The reason: in A3 (SWIFT-managed component) and A4 (cloud-based), the underlying OS is managed by SWIFT, not the customer. As an A1 institution with your own servers, you must:

- Maintain a privileged account inventory for all Alliance Access servers
- Prohibit routine use of root/local admin accounts
- Enforce MFA for privileged OS sessions where technically feasible
- Log all privileged account usage and retain logs per Control 6.4

### Highest-Risk Controls for A1

Based on the most common assessment findings for software-based on-premises deployments:

| Priority | Control | Common Gap Risk for A1 |
|----------|---------|----------------------|
| 1 | **4.2 MFA** | Software-based OTP (authenticator apps) in use rather than hardware tokens — explicitly non-compliant under v2025 |
| 2 | **1.1 SWIFT Environment Protection** | Alliance Access servers on shared network segment rather than dedicated VLAN/zone |
| 3 | **6.4 Log and Monitoring** | Alliance Access logs not ingested into SIEM; retention under 1 year; no daily review discipline |
| 4 | **2.2 Security Updates** | Critical SWIFT patches not applied within the new 3-day SLA (tightened in v2025 from 7 days) |
| 5 | **6.2 Software Integrity** | No hash verification process for Alliance Access software before installation |
| 6 | **2.3 System Hardening** | No formal hardening baseline; unused services still running on SWIFT servers |
| 7 | **1.2 OS Privileged Account Control** | Shared or default admin accounts used for routine server management |

---

## 5. Key v2025 Changes Affecting A1 Institutions

| Change | v2024 | v2025 | Impact on A1 |
|--------|-------|-------|-------------|
| Patching SLA — Critical | 7 days | **3 days** | Emergency patch processes must be capable of deploying Alliance Access and OS patches within 3 calendar days of a critical advisory |
| MFA — hardware token | Strongly recommended | **Explicitly required; app-based OTP insufficient** | If software-based OTP is in use, this is now an unambiguous mandatory finding; hardware tokens must be deployed |
| Log retention | 1 year minimum | **1 year online; 3 years total** | SIEM/log archive must retain logs accessible online for 12 months; cold/archive tier must retain for a further 2 years minimum |

---

## 6. Annual Attestation Requirements for A1

| Activity | Detail |
|----------|--------|
| **Attestation portal** | KYC Security Attestation (KYC-SA) portal at swift.com/myswift |
| **Deadline** | July 31 annually (CSCF v2025 effective; first attestation cycle under v2025 from July 2025) |
| **Assessment type** | Independent assessment required (internal audit if independent, or external SWIFT CSP assessor) |
| **Scope** | All 23 mandatory controls applicable to A1 |
| **Advisory controls** | Optionally included; assessed for maturity but not attested as pass/fail |
| **Attestation options per control** | Implemented / Partially Implemented / Not Implemented |
| **Consequence of non-attestation** | Counterparty visibility flagged after deadline; potential suspension; regulatory escalation |

**Independent Assessment Note:** Your internal audit team may act as the independent assessor provided they have no operational responsibility for the SWIFT environment. If internal audit is embedded within IT operations, an external SWIFT CSP-qualified assessor should be engaged.

---

## 7. Quick-Reference Applicability Matrix — All Architecture Types (for Benchmarking)

The following matrix shows how A1 compares to other architecture types, confirming A1 has the broadest mandatory scope:

| Control | A1 | A2 | A3 | A4 | B |
|---------|:--:|:--:|:--:|:--:|:-:|
| 1.1 SWIFT Environment Protection | MAND | MAND | MAND | MAND | MAND |
| **1.2 OS Privileged Account Control** | **MAND** | MAND | N/A | N/A | MAND |
| 1.3A Virtualisation Platform Security | ADV | ADV | ADV | ADV | ADV |
| 1.4 Restriction of Internet Access | MAND | MAND | MAND | MAND | MAND |
| 1.5A Customer Environment Protection | ADV | ADV | ADV | ADV | ADV |
| 2.1 Internal Data Flow Security | MAND | MAND | MAND | MAND | MAND |
| 2.2 Security Updates | MAND | MAND | MAND | MAND | MAND |
| 2.3 System Hardening | MAND | MAND | MAND | MAND | MAND |
| 2.4A Back-Office Data Flow Security | ADV | ADV | ADV | ADV | ADV |
| 2.5A External Transmission Data Protection | ADV | ADV | ADV | ADV | ADV |
| 2.6 Operator Session Confidentiality | MAND | MAND | MAND | MAND | MAND |
| 2.7 Vulnerability Scanning | MAND | MAND | MAND | MAND | MAND |
| 2.8 Critical Activity Outsourcing | MAND | MAND | MAND | MAND | MAND |
| 2.9A Transaction Business Controls | ADV | ADV | ADV | ADV | ADV |
| 2.10 Application Hardening | MAND | MAND | MAND | MAND | MAND |
| 2.11A RMA Business Controls | ADV | ADV | ADV | ADV | ADV |
| 3.1 Physical Security | MAND | MAND | MAND | MAND | MAND |
| 4.1 Password Policy | MAND | MAND | MAND | MAND | MAND |
| 4.2 Multi-Factor Authentication | MAND | MAND | MAND | MAND | MAND |
| 5.1 Logical Access Controls | MAND | MAND | MAND | MAND | MAND |
| 5.2 Token Management | MAND | MAND | MAND | MAND | MAND |
| 5.3A Staffing | ADV | ADV | ADV | ADV | ADV |
| 5.4 Physical and Logical Password Storage | MAND | MAND | MAND | MAND | MAND |
| 6.1 Malware Protection | MAND | MAND | MAND | MAND | MAND |
| 6.2 Software Integrity | MAND | MAND | MAND | MAND | MAND |
| 6.3 Database Integrity | MAND | MAND | MAND | MAND | MAND |
| 6.4 Log and Monitoring | MAND | MAND | MAND | MAND | MAND |
| 6.5A Intrusion Detection | ADV | ADV | ADV | ADV | ADV |
| 7.1 Cyber Incident Response Planning | MAND | MAND | MAND | MAND | MAND |
| 7.2 Security Training and Awareness | MAND | MAND | MAND | MAND | MAND |
| 7.3A Penetration Testing | ADV | ADV | ADV | ADV | ADV |
| 7.4A Scenario Risk Assessment | ADV | ADV | ADV | ADV | ADV |

**MAND** = Mandatory | **ADV** = Advisory | **N/A** = Not Applicable  
**Bold row** = Control where A1 differs from A3/A4

---

## 8. Recommended Immediate Actions

Given that this is an A1 on-premises Alliance Access deployment with no HSMs:

1. **Verify MFA method (Control 4.2):** Confirm whether hardware tokens are deployed for all SWIFT operators. Under CSCF v2025, software OTP apps are explicitly insufficient. If hardware tokens are not yet in place, this is a mandatory finding that must be disclosed in your KYC-SA attestation.

2. **Confirm Secure Zone network design (Control 1.1):** Verify that Alliance Access servers are on a dedicated, firewalled network segment with deny-by-default rules and no dual-homing to the corporate network.

3. **Assess patch readiness for 3-day SLA (Control 2.2):** The v2025 tightening of the critical patch SLA to 3 days is the most impactful procedural change. Confirm your patch deployment process can meet this requirement.

4. **Inventory all SWIFT log sources in SIEM (Control 6.4):** Ensure Alliance Access application logs, OS security logs, and authentication logs are ingested with the correct retention tiers (1 year online, 3 years total).

5. **Document OS privileged accounts (Control 1.2):** As an A1 institution, you must evidence that root/admin access to Alliance Access servers is controlled, approved, logged, and not used for routine operations.

6. **Schedule independent assessment:** Engage your internal audit team or an external SWIFT CSP assessor to conduct the assessment covering all 23 mandatory controls before the July 31 KYC-SA submission deadline.

---

*This analysis is based on SWIFT CSCF v2025. The v2025 framework is effective July 2025; KYC-SA attestations submitted from July 2025 onwards must use v2025 controls. Institutions attesting prior to July 2025 may use CSCF v2024.*
