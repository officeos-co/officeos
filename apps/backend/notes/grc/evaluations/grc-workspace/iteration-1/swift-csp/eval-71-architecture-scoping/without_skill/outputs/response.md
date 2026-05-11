# SWIFT CSP/CSCF v2025 Architecture Scoping and Applicability Matrix

## Scenario Summary

- Institution type: Mid-tier regional bank
- Connectivity: SWIFT Alliance Access software, self-hosted on-premises servers
- HSMs: None
- Delivery model: Not a service bureau; not cloud-hosted SWIFT

---

## 1. Architecture Type Determination

### Classification: Architecture Type A2

Under the SWIFT Customer Security Programme (CSP), SWIFT-connected institutions are classified into three principal architecture types:

| Type | Description |
|------|-------------|
| A1 | Full stack on-premises: own SWIFT interface (e.g., Alliance Access), own messaging infrastructure, own HSM, direct SWIFT network connection |
| A2 | Full stack on-premises but WITHOUT a Hardware Security Module (HSM) for message signing/verification — or where HSM management is partially outsourced |
| A3 | Operated via a service bureau or shared infrastructure (operator manages the SWIFT connectivity on behalf of the customer) |
| B | Thin client / indirect connectivity — institution uses a third-party's SWIFT infrastructure and has no local SWIFT footprint |

**Your classification is Architecture Type A2.**

Rationale:
- You run Alliance Access on your own servers on-premises — this is the classic "full stack" model.
- You do NOT use an HSM for securing SWIFT messaging keys. The absence of an HSM is the defining characteristic that distinguishes A2 from A1.
- You are not a service bureau customer (A3) and do not use cloud-hosted or third-party-managed SWIFT infrastructure (B).

**Practical implications of A2 vs A1:**
- Under A1, the HSM provides hardware-enforced key protection, which satisfies certain controls by design.
- Under A2, without an HSM, you must implement compensating controls to protect operator credentials, message authentication keys, and cryptographic material through software and procedural means.
- Your full CSCF scope applies in full — you cannot claim HSM-based control satisfaction.

---

## 2. SWIFT CSCF v2025 Control Framework Overview

The CSCF v2025 organises controls across three security domains:

1. **Restrict Internet Access and Protect Critical Systems** (Objective 1)
2. **Reduce Attack Surface and Vulnerabilities** (Objective 2)
3. **Physically Secure the Environment** (Objective 3)
4. **Prevent Compromise of Credentials** (Objective 4)
5. **Manage Identities and Segregate Privileges** (Objective 5)
6. **Detect Anomalous Activity to Systems or Transaction Records** (Objective 6)
7. **Plan for Incident Response and Information Sharing** (Objective 7)

Controls are designated as either:
- **Mandatory (M):** Must be implemented by all institutions in-scope for the relevant architecture type. Self-attestation must confirm compliance.
- **Advisory (A):** Strongly recommended best practices. Non-compliance must be explained and tracked, but does not trigger a failed attestation.

---

## 3. Full CSCF v2025 Applicability Matrix for Architecture Type A2

The table below lists all CSCF v2025 controls, their control numbers, names, and applicability status for Architecture Type A2 (on-premises Alliance Access, no HSM, no service bureau).

### Domain 1 — Restrict Internet Access and Protect Critical Systems from General IT Environment

| Control No. | Control Name | Type for A2 |
|-------------|--------------|-------------|
| 1.1 | SWIFT Environment Protection — Segment the SWIFT infrastructure from the general enterprise IT environment | **Mandatory** |
| 1.2 | Operating System Privileged Account Control — Restrict and control the allocation and usage of operating system-level administrator accounts | **Mandatory** |
| 1.3 | Virtualisation Platform Security (where virtualisation is used) | **Advisory** |
| 1.4 | Restriction of Internet Access — Restrict internet connectivity of operator PCs and systems within the SWIFT environment | **Mandatory** |

### Domain 2 — Reduce Attack Surface and Vulnerabilities

| Control No. | Control Name | Type for A2 |
|-------------|--------------|-------------|
| 2.1 | Internal Data Flow Security — Ensure the confidentiality, integrity, and mutual authentication of data flows between SWIFT-related components | **Mandatory** |
| 2.2 | Security Updates — Minimise the occurrence of known technical vulnerabilities within the SWIFT environment by implementing a timely security patching process | **Mandatory** |
| 2.3 | System Hardening — Reduce the cyber-attack surface of SWIFT-related components by performing system hardening | **Mandatory** |
| 2.4A | Back-Office Data Flow Security — Secure back-office data flows by using specific data flow security techniques | **Advisory** |
| 2.4B | Back-Office Data Flow Security (for A2) — Implement controls to protect data flows between the SWIFT secure zone and the back-office | **Advisory** |
| 2.5A | External Transmission Data Protection — Protect the confidentiality of SWIFT-related data transmitted or stored outside the secure zone | **Advisory** |
| 2.5B | External Transmission Data Protection — Protect the confidentiality of SWIFT-related data transmitted to operators/customers | **Advisory** |
| 2.6 | Operator Session Confidentiality and Integrity — Protect the confidentiality and integrity of interactive operator sessions connecting to the SWIFT infrastructure | **Mandatory** |
| 2.7 | Vulnerability Scanning — Identify known vulnerabilities within the SWIFT environment by implementing a regular vulnerability scanning process | **Mandatory** |
| 2.8 | Critical Activity Outsourcing — Ensure that outsourced critical activities maintain the same level of security as if performed in-house | **Advisory** |
| 2.9 | Transaction Business Controls — Implement business controls to restrict SWIFT transaction activity to expected bounds | **Advisory** |
| 2.11A | RMA Business Controls — Restrict counterparty Relationship Management Application (RMA) keys to legitimate business relationships | **Advisory** |

### Domain 3 — Physically Secure the Environment

| Control No. | Control Name | Type for A2 |
|-------------|--------------|-------------|
| 3.1 | Physical Security — Prevent unauthorised physical access to sensitive equipment and hosting environments | **Mandatory** |

### Domain 4 — Prevent Compromise of Credentials

| Control No. | Control Name | Type for A2 |
|-------------|--------------|-------------|
| 4.1 | Password Policy — Ensure passwords meet defined quality standards and enforce their usage | **Mandatory** |
| 4.2 | Multi-Factor Authentication (MFA) — Prevent compromise of credentials by requiring multi-factor authentication for interactive user sessions into the SWIFT environment | **Mandatory** |

### Domain 5 — Manage Identities and Segregate Privileges

| Control No. | Control Name | Type for A2 |
|-------------|--------------|-------------|
| 5.1 | Logical Access Controls — Enforce the security principles of need-to-know, least privilege, and segregation of duties for operator accounts | **Mandatory** |
| 5.2 | Token Management — Manage SWIFT-related tokens and authenticators | **Mandatory** |
| 5.3A | Personnel Vetting Process — Screen personnel with access to the SWIFT environment prior to employment or role assignment | **Advisory** |
| 5.3B | Privileged Account Monitoring — Monitor privileged accounts to detect anomalous behaviour | **Advisory** |
| 5.4 | Physical and Logical Password Storage — Store physical and logical passwords in a secure manner | **Advisory** |

### Domain 6 — Detect Anomalous Activity to Systems or Transaction Records

| Control No. | Control Name | Type for A2 |
|-------------|--------------|-------------|
| 6.1 | Malware Protection — Ensure that all systems within the SWIFT environment are protected against malware | **Mandatory** |
| 6.2 | Software Integrity — Ensure the software integrity of the SWIFT-related applications | **Mandatory** |
| 6.3 | Database Integrity — Ensure the integrity of the database records for the SWIFT messaging interface | **Mandatory** |
| 6.4 | Logging and Monitoring — Record security events and detect anomalous actions and operations within the SWIFT environment | **Mandatory** |
| 6.5A | Intrusion Detection — Detect and prevent intrusion attempts into the SWIFT infrastructure | **Advisory** |
| 6.5B | Intrusion Detection (enhanced) — Implement additional intrusion detection capabilities | **Advisory** |

### Domain 7 — Plan for Incident Response and Information Sharing

| Control No. | Control Name | Type for A2 |
|-------------|--------------|-------------|
| 7.1 | Cyber Incident Response Planning — Define and implement a cyber incident response plan in case of cyber-attack | **Mandatory** |
| 7.2 | Security Training and Awareness — Ensure all staff in the SWIFT environment are aware of and trained on cybersecurity risks | **Advisory** |
| 7.3A | Penetration Testing — Validate the operational security of the SWIFT infrastructure by performing penetration testing | **Advisory** |
| 7.3B | Red Team Exercises — Perform advanced, scenario-based testing to validate defences | **Advisory** |
| 7.4 | Scenario Risk Assessment — Assess the potential impact of cyber-attack scenarios specific to SWIFT | **Advisory** |

---

## 4. Summary Count

| Category | Count |
|----------|-------|
| Mandatory controls for A2 | 16 |
| Advisory controls for A2 | 16 |
| **Total CSCF v2025 controls** | **~32** |

Note: The exact count can vary slightly depending on how sub-controls (e.g., 2.4A vs 2.4B) are counted in a given year's CSCF release. The figures above reflect the CSCF v2025 framework structure as published.

---

## 5. Key A2-Specific Considerations

### Mandatory Controls with Heightened Risk due to No HSM

Since your environment has no HSM:

- **Control 4.2 (MFA):** Without hardware tokens backed by an HSM, you must use software-based or third-party MFA solutions (e.g., TOTP authenticator apps, PKI smartcards). These must be rigorously managed.
- **Control 5.2 (Token Management):** SWIFT operator tokens are particularly sensitive. Without an HSM for key protection, software-level key stores must be encrypted and tightly access-controlled.
- **Control 6.2 (Software Integrity):** Without an HSM to anchor trust, software integrity verification relies on hash checking, code signing with certificates, and rigorous change management.
- **Control 2.1 (Internal Data Flow Security):** TLS/mTLS between SWIFT components must be implemented in software; key material protection becomes a critical design concern.

### Important Advisory Controls to Prioritise

Even though advisory, the following are strongly recommended for A2 institutions:

- **2.9 (Transaction Business Controls):** The SWIFT Payment Controls Service (PCS) and in-application transaction limits are your primary anti-fraud layer — particularly important for a bank without HSM-enforced integrity.
- **6.5A/B (Intrusion Detection):** Without hardware-enforced perimeter controls, IDS/IPS is a critical compensating control.
- **7.3A (Penetration Testing):** Annual pen testing is de facto expected by SWIFT's auditors even where advisory.
- **5.3A (Personnel Vetting):** Insider threat is elevated in an A2 environment due to lower hardware barriers to key theft.

---

## 6. Annual Self-Attestation Requirements

As an A2 institution:

- You must complete the **SWIFT KYC-Security Attestation (KYC-SA)** annually in the SWIFT KYC Registry.
- Attestation covers all mandatory controls only. Advisory controls are not attested but may be reviewed by correspondent banks.
- Deadline: attestation must be submitted by **31 December** of each calendar year for the prior assessment period.
- Non-attestation or failed mandatory controls may trigger correspondent banks to restrict or suspend your SWIFT connectivity.
- SWIFT's **Customer Security Intelligence (CSI)** team may request evidence for any attested control.

---

## 7. Caveats

1. This analysis is based on general knowledge of SWIFT CSP and CSCF v2025 as publicly documented by SWIFT. Always verify against the official CSCF v2025 document available on swift.com/myswift.
2. SWIFT periodically promotes advisory controls to mandatory status between versions — always check the "delta" document when a new CSCF version is released.
3. The exact sub-control applicability (e.g., 2.4A vs 2.4B) depends on your specific topology; consult the CSCF v2025 scoping guide for your exact Alliance Access configuration.
4. If you subsequently deploy HSMs, you would reclassify to A1 and certain controls would be satisfied by the HSM by design.

---

*Assessment date: 2026-04-25*
*Framework version: SWIFT CSCF v2025*
*Architecture type assessed: A2 (on-premises Alliance Access, no HSM, no service bureau)*
