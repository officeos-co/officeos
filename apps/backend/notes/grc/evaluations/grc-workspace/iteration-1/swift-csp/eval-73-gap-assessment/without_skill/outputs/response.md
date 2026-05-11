# SWIFT CSCF v2025 Gap Assessment

**Organisation Assessment Date:** 2026-04-25
**Framework:** SWIFT Customer Security Controls Framework (CSCF) v2025
**Assessment Type:** Gap Assessment with Prioritised Remediation Plan

---

## Executive Summary

Based on the current state provided, the organisation has partial compliance with the SWIFT CSCF v2025. Critical gaps exist across network segregation, MFA strength, log management, and incident response. The organisation is at elevated risk of non-compliance and potential SWIFT suspension pending remediation. Five controls are rated as non-compliant (Red), two as partially compliant (Amber), and two as compliant (Green).

---

## Current State Input Summary

| Area | Current State |
|------|--------------|
| Network Segregation | SWIFT zone exists but on shared VLAN with other internal systems |
| Vulnerability Management | Critical vulnerabilities patched within 30 days |
| Multi-Factor Authentication | Software OTP used for operator MFA |
| Logging & Monitoring | Logs collected but not reviewed; 6-month retention |
| Incident Response | General IT IR plan; no SWIFT-specific plan |
| Account Management | Operators use named individual accounts |

---

## Control-by-Control Gap Assessment

### Control 1.1 — SWIFT Environment Protection (Mandatory)

**Requirement:** The SWIFT infrastructure must be protected from the broader IT environment through a secure zone with strict access controls and network segmentation. The SWIFT zone must be logically and/or physically isolated from other parts of the network, including general corporate IT systems.

**Current State:** SWIFT zone exists but is on a shared VLAN with other internal systems.

**Gap:** Sharing a VLAN with non-SWIFT internal systems violates the fundamental zone isolation requirement. A VLAN without additional micro-segmentation controls (firewalls, access control lists scoped exclusively to SWIFT flows) does not constitute adequate isolation. Lateral movement from a compromised internal host to SWIFT infrastructure is a realistic threat vector.

**Rating: NON-COMPLIANT (Red)**

**Evidence Required to Close:**
- Network diagram showing SWIFT zone physically or logically isolated
- Firewall rule-set restricting all inbound/outbound flows to only required SWIFT communication paths
- No shared VLAN membership between SWIFT and non-SWIFT assets
- Third-party penetration test confirming zone isolation

---

### Control 1.2 — Privileged Account Control (Mandatory)

**Requirement:** Operating system and application privileged accounts on SWIFT infrastructure must be restricted and controlled. Generic/shared accounts are prohibited.

**Current State:** Operators use named individual accounts.

**Gap:** No gap identified. Named individual accounts satisfy the requirement that operators are identifiable and accounts are not shared. Provided that privileged access is appropriately restricted and access rights are reviewed periodically, this control is met.

**Rating: COMPLIANT (Green)**

**Recommendations (Advisory):**
- Ensure periodic access reviews (at least annually) are documented
- Confirm that account provisioning/de-provisioning processes are tied to HR joiners/movers/leavers workflows
- Maintain an authorised user register for the SWIFT environment

---

### Control 2.2 — Security Updates (Mandatory)

**Requirement:** SWIFT-related components (operating systems, applications, network devices) must be kept up to date with security patches. Critical vulnerabilities must be patched within a defined timescale — CSCF v2025 requires critical patches to be applied within one month (approximately 30 days) for mandatory components.

**Current State:** Critical vulnerabilities patched within 30 days.

**Gap:** The 30-day patching cycle meets the CSCF v2025 mandatory threshold for critical vulnerabilities. However, this rating assumes the 30-day cycle applies specifically to SWIFT infrastructure components and is consistently achieved and evidenced. If the 30-day clock is an aspiration rather than a demonstrated, tracked metric, there is a documentation gap.

**Rating: COMPLIANT (Green) — with caveat**

**Recommendations (Advisory):**
- Maintain a SWIFT-scoped vulnerability tracking register with patch dates evidenced
- Confirm that the 30-day cycle is measured from vulnerability disclosure or vendor patch release, not from internal discovery
- Consider tightening the target for zero-day or actively exploited vulnerabilities

---

### Control 4.2 — Multi-Factor Authentication (Mandatory)

**Requirement:** MFA must be enforced for all operator accounts accessing the SWIFT environment. CSCF v2025 strengthens MFA requirements: software-based OTP (e.g., authenticator apps generating TOTP codes) no longer satisfies the mandatory MFA requirement for SWIFT operator access in isolation. Hardware-based authentication tokens or hardware security keys (e.g., FIDO2/WebAuthn, PKI smart cards, or SWIFT-approved hardware tokens) are required for mandatory compliance.

**Current State:** Software OTP used for operator MFA.

**Gap:** Software OTP (TOTP via authenticator app) is considered a weaker form of MFA under CSCF v2025. The framework explicitly requires that MFA for SWIFT operator access uses a hardware-based second factor or a solution that cannot be compromised by malware on the operator workstation. Software OTP running on the same endpoint as the SWIFT client does not provide sufficient separation. This is a mandatory control failure.

**Rating: NON-COMPLIANT (Red)**

**Evidence Required to Close:**
- Deployment of hardware authentication tokens (e.g., RSA SecurID, FIDO2 hardware keys, smart card/PKI) for all SWIFT operators
- Configuration documentation showing hardware MFA enforced at the application/system level
- Enrolment records for all operator accounts

---

### Control 6.1 — Operator Session Confidentiality and Integrity (Advisory — but risk-relevant)

This control is implicitly impacted by the MFA weakness noted under 4.2 and is noted for completeness. If software OTP is running on the operator workstation, the session integrity guarantee is weakened. No separate rating issued — remediation of 4.2 addresses this.

---

### Control 6.4 — Logging and Monitoring (Mandatory)

**Requirement:** SWIFT-related event logs must be recorded, protected, and reviewed. CSCF v2025 requires:
- Logs must be actively reviewed (not just collected)
- Anomalous activity must be detected and investigated
- Log retention must be a minimum of 12 months (with at least 3 months readily available online)

**Current State:** SWIFT logs are collected but not reviewed; retention is only 6 months.

**Gap:** Two distinct failures:
1. **No log review:** Collecting logs without reviewing them provides zero detection capability. This is a fundamental failure of the monitoring requirement. Unreviewed logs cannot satisfy the detective control objective.
2. **Insufficient retention:** 6-month retention falls short of the 12-month mandatory minimum. This creates forensic investigation risk and potential non-compliance in the event of a SWIFT-notified incident.

**Rating: NON-COMPLIANT (Red)**

**Evidence Required to Close:**
- Documented log review process (automated alerting via SIEM, or defined manual review schedule — minimum daily review of critical alerts)
- Evidence of log reviews performed (review logs, SIEM alert records)
- Log retention policy updated to minimum 12 months
- Technical confirmation that log storage capacity and archival processes support 12-month retention
- Logs must be tamper-evident and access-controlled

---

### Control 6.5 — Intrusion Detection (Advisory in v2025 for some architectures, Mandatory for others)

**Current State:** Not explicitly stated. Assumed not deployed given the broader monitoring gap.

**Gap (Assumed):** If no IDS/IPS or SIEM-based anomaly detection is in place on the SWIFT zone boundary, this represents an advisory gap that should be addressed in conjunction with the log review remediation.

**Rating: PARTIALLY COMPLIANT (Amber) — assumed**

**Recommendations:**
- Deploy network-based intrusion detection on SWIFT zone boundaries
- Integrate SWIFT logs into a SIEM with correlation rules for known SWIFT attack patterns (e.g., fraudulent payment message injection, bulk transfer anomalies)

---

### Control 7.1 — Cyber Incident Response Planning (Mandatory)

**Requirement:** Organisations must have a documented, tested cyber incident response plan that specifically covers SWIFT-related incidents, including procedures for contacting SWIFT, isolating the SWIFT infrastructure, preserving evidence, and notifying relevant parties.

**Current State:** General IT incident response plan exists; no SWIFT-specific plan.

**Gap:** A generic IT IR plan does not satisfy this control. SWIFT incidents have specific characteristics — fraudulent payment messages, potential backdoors in the SWIFT messaging layer, mandatory SWIFT notification obligations, and coordination with correspondent banks and regulators. A SWIFT-specific annex or standalone IR plan is required, covering:
- SWIFT-specific escalation contacts (SWIFT ISAC, local SWIFT support)
- Payment message recall and freezing procedures
- Evidence preservation for SWIFT logs and messaging databases
- Regulatory and law enforcement notification thresholds

**Rating: NON-COMPLIANT (Red)**

**Evidence Required to Close:**
- SWIFT-specific IR plan documented and approved
- IR plan tested via tabletop exercise (at minimum annually)
- Exercise records and lessons learned documented
- Staff trained on SWIFT-specific IR procedures

---

### Control 2.4A — Back Office Data Flow Security (Advisory)

**Current State:** Not explicitly addressed; shared VLAN environment creates implicit risk for back-office data flows.

**Gap (Inferred):** If back-office systems share the VLAN with SWIFT systems, data flow security between SWIFT and back-office cannot be adequately controlled.

**Rating: PARTIALLY COMPLIANT (Amber) — inferred from network state**

---

## Consolidated Control Ratings Summary

| Control | Control Name | Mandatory/Advisory | Rating |
|---------|-------------|-------------------|--------|
| 1.1 | SWIFT Environment Protection | Mandatory | NON-COMPLIANT (Red) |
| 1.2 | Privileged Account Control | Mandatory | COMPLIANT (Green) |
| 2.2 | Security Updates | Mandatory | COMPLIANT (Green) |
| 4.2 | Multi-Factor Authentication | Mandatory | NON-COMPLIANT (Red) |
| 6.4 | Logging and Monitoring | Mandatory | NON-COMPLIANT (Red) |
| 6.5 | Intrusion Detection | Advisory/Mandatory | PARTIALLY COMPLIANT (Amber) |
| 7.1 | Cyber Incident Response | Mandatory | NON-COMPLIANT (Red) |
| 2.4A | Back Office Data Flow Security | Advisory | PARTIALLY COMPLIANT (Amber) |

**Overall Compliance Score: 2 Green / 2 Amber / 4 Red**

---

## Prioritised Remediation Plan

Priorities are assigned based on: (1) mandatory vs advisory status, (2) exploitability and potential financial/reputational impact if control is absent, and (3) remediation complexity.

---

### Priority 1 — CRITICAL: Network Segregation (Control 1.1)

**Priority Rationale:** This is the foundational security control for the entire SWIFT architecture. All other controls are undermined if the SWIFT zone is not properly isolated. A compromised internal system on the shared VLAN can directly access SWIFT infrastructure.

**Remediation Actions:**

1. Engage network architecture team to design dedicated SWIFT VLAN/segment with no shared membership with corporate IT VLANs.
2. Deploy dedicated firewall (or firewall policy partition) at the SWIFT zone perimeter with default-deny inbound/outbound rules.
3. Whitelist only required communication flows: SWIFT messaging interfaces (SWIFTNet Link, Alliance Gateway), approved operator workstations, and authorised management hosts.
4. Remove SWIFT assets from the shared VLAN.
5. Commission a penetration test or network segmentation review to validate isolation.
6. Update network documentation and maintain a current network diagram.

**Target Timeline:** 60 days (immediate initiation, phased cutover)
**Owner:** Network/Infrastructure Team + SWIFT IT Owner
**Effort:** High (infrastructure change with potential downtime window)

---

### Priority 2 — CRITICAL: Multi-Factor Authentication Upgrade (Control 4.2)

**Priority Rationale:** Software OTP can be compromised by malware on the operator workstation — the exact attack vector used in multiple historical SWIFT fraud cases (e.g., Bangladesh Bank). Upgrading to hardware MFA is mandatory and directly mitigates credential theft attacks.

**Remediation Actions:**

1. Select and procure hardware authentication tokens or FIDO2 hardware security keys for all SWIFT operators (e.g., YubiKey, Thales SafeNet, RSA SecurID hardware token).
2. Enrol all SWIFT operator accounts with hardware MFA.
3. Update authentication policy to require hardware MFA at SWIFT application login — disable software OTP fallback.
4. Test authentication flow to confirm hardware MFA enforced end-to-end.
5. Document MFA configuration and maintain enrolment register.
6. Define a procedure for lost/damaged token replacement without creating a bypass window.

**Target Timeline:** 45 days
**Owner:** Identity & Access Management Team + SWIFT Application Owner
**Effort:** Medium (procurement lead time is primary constraint)

---

### Priority 3 — HIGH: Log Review Process and Retention Extension (Control 6.4)

**Priority Rationale:** Without log review, the organisation has no ability to detect a SWIFT compromise in progress. The Bangladesh Bank fraud and subsequent SWIFT incidents persisted for extended periods precisely because monitoring was absent. Extending retention also closes a forensic gap.

**Remediation Actions:**

1. Implement automated log ingestion of SWIFT logs into a SIEM (e.g., Splunk, Microsoft Sentinel, IBM QRadar) or centralised log management platform.
2. Develop SWIFT-specific detection rules, including: unusual transaction volumes, after-hours operator logins, failed authentication spikes, configuration changes on SWIFT infrastructure.
3. Define a daily (or real-time alert-based) log review process with documented responsibilities.
4. Increase log retention to minimum 12 months; ensure at least 3 months of logs are readily searchable online.
5. Implement log integrity controls (write-once storage or cryptographic signing) to prevent tampering.
6. Document the log review process and evidence retention schedule in a formal policy.

**Target Timeline:** 60 days (SIEM integration may require phased approach)
**Owner:** Security Operations / SIEM Team + SWIFT IT Owner
**Effort:** High (SIEM onboarding and rule development)

---

### Priority 4 — HIGH: SWIFT-Specific Incident Response Plan (Control 7.1)

**Priority Rationale:** Without a SWIFT-specific IR plan, the organisation will lose critical hours in the event of a fraud incident. Payment message recall windows are extremely short (often hours). A tested plan is mandatory under CSCF v2025.

**Remediation Actions:**

1. Draft a SWIFT-specific IR plan as a standalone document or formal annex to the existing IT IR plan, covering:
   - Roles and responsibilities (SWIFT IR Lead, CISO, Finance/Treasury, Legal)
   - Detection and initial triage procedures for SWIFT fraud indicators
   - Isolation procedures for SWIFT infrastructure (network isolation steps, documented and pre-approved)
   - Payment recall procedures and escalation to correspondent banks
   - SWIFT mandatory notification procedures (SWIFT ISAC, local SWIFT support contact)
   - Evidence preservation: SWIFT messaging database, logs, operator session records
   - Regulatory notification thresholds (local financial regulator, law enforcement)
   - Post-incident review and lessons learned process
2. Obtain management sign-off on the plan.
3. Conduct a tabletop exercise simulating a SWIFT fraud scenario within 30 days of plan completion.
4. Document exercise findings and update the plan accordingly.
5. Schedule annual review and re-testing.

**Target Timeline:** 45 days (plan draft + exercise)
**Owner:** CISO + Compliance + Treasury + Legal
**Effort:** Medium (primarily process and documentation effort)

---

### Priority 5 — MEDIUM: Intrusion Detection on SWIFT Zone Boundary (Control 6.5)

**Priority Rationale:** Once the SWIFT zone is properly segregated (Priority 1) and log monitoring is implemented (Priority 3), deploying IDS/IPS at the SWIFT zone boundary provides an additional detection layer for network-based attacks.

**Remediation Actions:**

1. Deploy network IDS/IPS (or configure existing capability) at the SWIFT zone firewall/perimeter to inspect traffic entering and leaving the SWIFT zone.
2. Enable SWIFT-relevant signatures and anomaly detection rules.
3. Integrate IDS alerts into the SIEM/log review process established under Priority 3.
4. Test IDS coverage and tune to reduce false positives.

**Target Timeline:** 90 days (dependent on Priority 1 network redesign)
**Owner:** Network Security / SOC Team
**Effort:** Medium

---

### Priority 6 — MEDIUM: Back-Office Data Flow Review (Control 2.4A)

**Priority Rationale:** Advisory control but directly relevant given the shared VLAN environment. Once network segregation is remediated, data flow paths between SWIFT and back-office systems should be formally documented and secured.

**Remediation Actions:**

1. Map all data flows between SWIFT infrastructure and back-office/ERP systems.
2. Ensure all flows traverse the SWIFT zone firewall and are explicitly whitelisted.
3. Apply encryption in transit for back-office data flows where applicable.
4. Document approved data flows in the network architecture documentation.

**Target Timeline:** 90 days (dependent on Priority 1)
**Owner:** Network/Infrastructure Team + Application Owner
**Effort:** Low-Medium

---

## Remediation Timeline Summary

| Priority | Control | Action | Target Completion | Effort |
|----------|---------|--------|------------------|--------|
| 1 | 1.1 | Network Segregation — dedicated SWIFT VLAN + firewall | Day 60 | High |
| 2 | 4.2 | Hardware MFA deployment for all operators | Day 45 | Medium |
| 3 | 6.4 | SIEM log ingestion, daily review, 12-month retention | Day 60 | High |
| 4 | 7.1 | SWIFT-specific IR plan + tabletop exercise | Day 45 | Medium |
| 5 | 6.5 | IDS/IPS on SWIFT zone boundary | Day 90 | Medium |
| 6 | 2.4A | Back-office data flow review and documentation | Day 90 | Low-Medium |

---

## Immediate Actions (Within 7 Days)

The following actions should be initiated immediately regardless of the broader remediation timeline:

1. **Escalate to CISO and senior management** — the volume of mandatory control gaps represents a material compliance risk and potential grounds for SWIFT to restrict messaging access.
2. **Initiate hardware MFA procurement** — ordering hardware tokens has a lead time; procurement should begin immediately.
3. **Assign a SWIFT compliance owner** — a named individual should own CSCF compliance and coordinate the remediation programme.
4. **Begin network architecture design** — engage network team to design the isolated SWIFT VLAN before any other infrastructure changes are made.
5. **Extend log retention immediately** — as a short-term measure, adjust log rotation settings to cease deletion of logs older than 6 months while the full 12-month retention architecture is designed.

---

## Assumptions and Caveats

- This assessment is based solely on the current state information provided. A full assessment would require review of technical documentation, network diagrams, system configurations, and interviews with SWIFT operations staff.
- CSCF v2025 control applicability depends on the organisation's SWIFT architecture type (A1, A2, A3, A4, B). Controls assessed here are broadly applicable across architecture types, but specific mandatory/advisory designations may vary.
- The "Compliant" rating for Control 2.2 (patching) assumes the 30-day metric is consistently achieved and evidenced for SWIFT-specific components. A documentation review would be needed to confirm.
- Control 1.2 (named accounts) is rated Compliant based on the information provided; this does not account for whether access rights are appropriately scoped or whether periodic reviews occur.
- SWIFT KYC-SA (Know Your Customer — Security Attestation) submissions are due annually. The gaps identified here will need to be reflected accurately in the next KYC-SA attestation and remediation tracked accordingly.

---

*Assessment completed: 2026-04-25*
*Framework version: SWIFT CSCF v2025*
*Assessment method: General knowledge assessment (no skill files referenced)*
