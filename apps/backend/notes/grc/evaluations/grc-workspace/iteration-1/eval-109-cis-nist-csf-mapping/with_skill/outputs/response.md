# CIS Controls v8 → NIST CSF 2.0 Mapping: Controls 1–8

## Overview

This mapping supports organizations using **CIS Controls v8** as their primary security framework while demonstrating alignment with **NIST CSF 2.0** for government contract requirements. The tables below provide a side-by-side mapping for CIS Controls 1 through 8, including the applicable NIST CSF 2.0 Function, Categories, and a summary of coverage.

---

## CIS Controls 1–8 → NIST CSF 2.0 Mapping Table

| CIS Control | Control Name | NIST CSF 2.0 Function | NIST CSF 2.0 Categories | Notes |
|-------------|-------------|----------------------|------------------------|-------|
| **CIS Control 1** | Inventory and Control of Enterprise Assets | **Identify** | ID.AM-1, ID.AM-2 | Establishes the asset inventory foundation required for CSF asset management. Safeguard 1.1 directly addresses ID.AM-1 (physical device inventory). |
| **CIS Control 2** | Inventory and Control of Software Assets | **Identify** | ID.AM-2, ID.AM-5 | Software inventory maps to ID.AM-2 (software platform/application inventory) and ID.AM-5 (resource prioritization). |
| **CIS Control 3** | Data Protection | **Protect** | PR.DS-1, PR.DS-2, PR.DS-5 | Data classification, access controls, encryption at rest (Safeguard 3.11), and DLP (Safeguard 3.13) align with CSF data security categories. |
| **CIS Control 4** | Secure Configuration of Enterprise Assets and Software | **Protect** | PR.IP-1, PR.IP-3 | Secure baselines (Safeguard 4.1) and configuration management align with PR.IP-1 (baseline configuration) and PR.IP-3 (configuration change control). |
| **CIS Control 5** | Account Management | **Protect** | PR.AC-1, PR.AC-4 | Credential management, dormant account disabling (Safeguard 5.3), and admin privilege restriction (Safeguard 5.4) align with identity management categories. |
| **CIS Control 6** | Access Control Management | **Protect** | PR.AC-3, PR.AC-6, PR.AC-7 | MFA requirements (Safeguards 6.3–6.5), least privilege, and access granting/revoking processes align with remote access, least privilege, and authentication categories. |
| **CIS Control 7** | Continuous Vulnerability Management | **Identify / Protect** | ID.RA-1, PR.IP-12 | Vulnerability scanning and remediation (Safeguards 7.6, 7.7) map to risk assessment (ID.RA-1) and vulnerability management plan (PR.IP-12). Dual-function coverage. |
| **CIS Control 8** | Audit Log Management | **Detect** | DE.AE-3, DE.CM-1, DE.CM-7 | Log collection (Safeguard 8.2), audit log reviews (Safeguard 8.11), and alerting map directly to anomalous event detection and continuous monitoring categories. |

---

## Detailed Control-by-Control Breakdown

### CIS Control 1: Inventory and Control of Enterprise Assets
**NIST CSF 2.0 Function: Identify**

| Safeguard | Description | NIST CSF Category |
|-----------|-------------|-------------------|
| 1.1 | Establish/maintain detailed enterprise asset inventory | ID.AM-1 |
| 1.2 | Address unauthorized assets | ID.AM-1 |
| 1.3 | Utilize DHCP logging to update enterprise asset inventory | ID.AM-1 |
| 1.4 | Use DHCP logging | ID.AM-1 |
| 1.5 (IG2+) | Use a passive asset discovery tool | ID.AM-2 |

**CSF Alignment Notes:** CIS Control 1 is the foundational control for the NIST CSF "Identify" function. Without a complete asset inventory (ID.AM-1), no other CSF function can be effectively implemented. This control is universally required at IG1.

---

### CIS Control 2: Inventory and Control of Software Assets
**NIST CSF 2.0 Function: Identify**

| Safeguard | Description | NIST CSF Category |
|-----------|-------------|-------------------|
| 2.1 | Establish/maintain a software inventory | ID.AM-2 |
| 2.2 | Ensure authorized software is currently supported | ID.AM-5 |
| 2.3 | Address unauthorized software | ID.AM-2 |
| 2.5 (IG2+) | Allowlist authorized software | ID.AM-2 |
| 2.6 (IG2+) | Allowlist authorized libraries | ID.AM-2 |
| 2.7 (IG2+) | Allowlist authorized scripts | ID.AM-2 |

**CSF Alignment Notes:** CIS Control 2 strengthens the Identify function by ensuring software assets are known, authorized, and supported. Unsupported software creates unmanaged risk that cannot be captured in CSF risk assessment (ID.RA) without this inventory.

---

### CIS Control 3: Data Protection
**NIST CSF 2.0 Function: Protect**

| Safeguard | Description | NIST CSF Category |
|-----------|-------------|-------------------|
| 3.1 | Establish/maintain a data management process | PR.DS-1, PR.DS-2 |
| 3.2 | Establish/maintain a data inventory | PR.DS-1 |
| 3.3 | Configure data access control lists | PR.DS-5 |
| 3.4 | Enforce data retention | PR.DS-1 |
| 3.5 | Securely dispose of data | PR.DS-1 |
| 3.6 (IG2+) | Encrypt data on end-user devices | PR.DS-1 |
| 3.11 (IG2+) | Encrypt sensitive data at rest | PR.DS-1 |
| 3.13 (IG3) | Deploy a data loss prevention solution | PR.DS-5 |

**CSF Alignment Notes:** CIS Control 3 maps to the CSF Protect function's data security categories. PR.DS-1 (data at rest protection), PR.DS-2 (data in transit protection), and PR.DS-5 (data leak protections) are all addressed through this control's safeguards.

---

### CIS Control 4: Secure Configuration of Enterprise Assets and Software
**NIST CSF 2.0 Function: Protect**

| Safeguard | Description | NIST CSF Category |
|-----------|-------------|-------------------|
| 4.1 | Establish/maintain a secure configuration process | PR.IP-1 |
| 4.2 | Establish/maintain a secure configuration process for network infrastructure | PR.IP-1 |
| 4.3 | Configure automatic session locking | PR.IP-1 |
| 4.4 | Implement/manage a firewall on servers | PR.IP-1 |
| 4.5 | Implement/manage a firewall on end-user devices | PR.IP-1 |
| 4.8 (IG2+) | Uninstall or disable unnecessary services | PR.IP-3 |

**CSF Alignment Notes:** CIS Control 4 directly supports PR.IP-1 (baseline configuration of information technology/industrial control systems) and PR.IP-3 (configuration change control processes). Use of CIS Benchmarks for configuration baselines is explicitly recommended for both frameworks.

---

### CIS Control 5: Account Management
**NIST CSF 2.0 Function: Protect**

| Safeguard | Description | NIST CSF Category |
|-----------|-------------|-------------------|
| 5.1 | Establish/maintain an inventory of accounts | PR.AC-1 |
| 5.2 | Use unique passwords | PR.AC-1 |
| 5.3 | Disable dormant accounts | PR.AC-1, PR.AC-4 |
| 5.4 | Restrict administrator privileges to dedicated admin accounts | PR.AC-4 |
| 5.5 (IG2+) | Establish/maintain an inventory of service accounts | PR.AC-1 |
| 5.6 (IG2+) | Centralize account management | PR.AC-1 |

**CSF Alignment Notes:** CIS Control 5 maps to CSF PR.AC-1 (identities and credentials are issued, managed, verified, revoked, and audited) and PR.AC-4 (access permissions and authorizations are managed, incorporating least privilege). This is foundational to identity governance.

---

### CIS Control 6: Access Control Management
**NIST CSF 2.0 Function: Protect**

| Safeguard | Description | NIST CSF Category |
|-----------|-------------|-------------------|
| 6.1 | Establish an access granting process | PR.AC-3, PR.AC-6 |
| 6.2 | Establish an access revoking process | PR.AC-3, PR.AC-6 |
| 6.3 (IG2+) | Require MFA for externally-exposed applications | PR.AC-7 |
| 6.4 (IG2+) | Require MFA for remote network access | PR.AC-3, PR.AC-7 |
| 6.5 (IG2+) | Require MFA for admin access | PR.AC-7 |
| 6.8 (IG2+) | Define/maintain role-based access control | PR.AC-4, PR.AC-6 |

**CSF Alignment Notes:** CIS Control 6 addresses three CSF access management categories: PR.AC-3 (remote access management), PR.AC-6 (identities are proofed and bound to credentials), and PR.AC-7 (users, devices, and other assets are authenticated). MFA safeguards directly satisfy the authentication assurance requirements implicit in PR.AC-7.

---

### CIS Control 7: Continuous Vulnerability Management
**NIST CSF 2.0 Functions: Identify AND Protect**

| Safeguard | Description | NIST CSF Category |
|-----------|-------------|-------------------|
| 7.1 | Establish/maintain a vulnerability management process | ID.RA-1, PR.IP-12 |
| 7.2 | Establish/maintain a remediation process | PR.IP-12 |
| 7.3 | Perform automated OS patch management | PR.IP-12 |
| 7.4 | Perform automated application patch management | PR.IP-12 |
| 7.6 (IG2+) | Perform automated vulnerability scans of internal assets | ID.RA-1 |
| 7.7 (IG2+) | Remediate detected vulnerabilities | PR.IP-12 |

**CSF Alignment Notes:** CIS Control 7 spans two NIST CSF functions. The scanning and discovery aspects (Safeguard 7.6) map to the **Identify** function under ID.RA-1 (vulnerabilities in assets are identified and documented). The remediation and patching aspects map to the **Protect** function under PR.IP-12 (a vulnerability management plan is developed and implemented). This dual coverage is important to document for government contract purposes.

---

### CIS Control 8: Audit Log Management
**NIST CSF 2.0 Function: Detect**

| Safeguard | Description | NIST CSF Category |
|-----------|-------------|-------------------|
| 8.1 | Establish/maintain an audit log management process | DE.CM-1, DE.AE-3 |
| 8.2 | Collect audit logs | DE.AE-3, DE.CM-7 |
| 8.3 (IG2+) | Ensure adequate audit log storage | DE.AE-3 |
| 8.5 (IG2+) | Collect detailed audit logs | DE.CM-1, DE.AE-3 |
| 8.11 (IG2+) | Conduct audit log reviews | DE.CM-1, DE.AE-3 |
| 8.12 (IG2+) | Collect service provider logs | DE.CM-7 |

**CSF Alignment Notes:** CIS Control 8 is the primary IG1-eligible control that maps to the **Detect** function. It covers:
- **DE.AE-3:** Event data are collected and correlated from multiple sources and sensors
- **DE.CM-1:** The network is monitored to detect potential cybersecurity events
- **DE.CM-7:** Monitoring for unauthorized personnel, connections, devices, and software is performed

---

## NIST CSF Function Coverage Summary: CIS Controls 1–8

| NIST CSF Function | CIS Controls (1–8) That Map to It |
|-------------------|----------------------------------|
| **Govern** | None directly (Controls 1–8 focus on operational controls) |
| **Identify** | CIS Control 1, CIS Control 2, CIS Control 7 (partial) |
| **Protect** | CIS Control 3, CIS Control 4, CIS Control 5, CIS Control 6, CIS Control 7 (partial) |
| **Detect** | **CIS Control 8** |
| **Respond** | None (CIS Controls 17 covers this — outside scope of Controls 1–8) |
| **Recover** | None (CIS Control 11 covers this — outside scope of Controls 1–8) |

---

## CIS Controls That Cover the NIST CSF "Detect" Function

**Within CIS Controls 1–8, only CIS Control 8 (Audit Log Management) maps to the NIST CSF "Detect" function.**

CIS Control 8 covers the Detect function through three categories:

| NIST CSF Category | Category Description | Covered By |
|-------------------|---------------------|------------|
| **DE.AE-3** | Event data are collected and correlated from multiple sources | Safeguards 8.1, 8.2, 8.3, 8.5, 8.11 |
| **DE.CM-1** | The network is monitored to detect potential cybersecurity events | Safeguards 8.1, 8.5, 8.11 |
| **DE.CM-7** | Monitoring for unauthorized personnel, connections, devices, and software | Safeguards 8.2, 8.12 |

**Important Note for Program Planning:** If robust Detect function coverage is required for the government contract, CIS Controls 1–8 alone provide only foundational detection through audit logging (CIS Control 8). Organizations should also implement:

- **CIS Control 13 (Network Monitoring and Defense)** — Adds DE.CM-1, DE.CM-7, DE.AE-2 coverage through IDS/IPS and SIEM capabilities (IG2+)
- **CIS Control 18 (Penetration Testing)** — Adds DE.CM-8 coverage through active detection testing (IG2+)

These controls beyond the initial 8 significantly strengthen the Detect function and are likely expected for a government contract requiring NIST CSF 2.0 alignment.

---

## Implementation Recommendations for Dual-Framework Compliance

### Prioritization by Implementation Group

For organizations building toward a government contract, the recommended path is:

1. **Start with IG1 Controls 1–8** — Establishes the Identify and Protect functions and foundational Detect capability
2. **Add IG2 safeguards for Controls 7, 8, and 13** — Expands Detect function coverage significantly
3. **Document each CIS safeguard against its NIST CSF category** — Creates the evidence trail needed for CSF compliance demonstration

### Evidence Artifacts for Each Mapping

| CIS Control | NIST CSF Evidence Needed |
|-------------|-------------------------|
| Control 1 | Asset inventory documentation, DHCP logs |
| Control 2 | Software inventory, allowlisting records |
| Control 3 | Data classification policy, encryption certificates, DLP reports |
| Control 4 | Configuration baselines (CIS Benchmarks recommended), change management records |
| Control 5 | Account inventory, access reviews, privileged account documentation |
| Control 6 | Access request/revocation records, MFA enrollment reports, RBAC documentation |
| Control 7 | Vulnerability scan reports, patch management records, remediation tracking |
| Control 8 | Log collection architecture, log retention documentation, audit review records |

---

## References

- Center for Internet Security. *CIS Controls v8.* May 2021.
- National Institute of Standards and Technology. *Cybersecurity Framework 2.0.* February 2024.
- CIS Controls v8 Safeguard assignments per Implementation Group (IG1/IG2/IG3) as published by CIS.
- NIST CSF 2.0 Core Functions: Govern (GV), Identify (ID), Protect (PR), Detect (DE), Respond (RS), Recover (RC).
