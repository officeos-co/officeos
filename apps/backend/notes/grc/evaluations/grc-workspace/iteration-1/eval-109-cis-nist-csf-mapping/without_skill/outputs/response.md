# CIS Controls v8 (1–8) Mapped to NIST CSF 2.0

## Executive Summary

This document maps CIS Controls 1 through 8 to the NIST Cybersecurity Framework (CSF) 2.0 functions and categories to support dual-framework alignment for government contract purposes. NIST CSF 2.0 introduces six functions: **Govern (GV)**, **Identify (ID)**, **Protect (PR)**, **Detect (DE)**, **Respond (RS)**, and **Recover (RC)**. CIS Controls v8 are operationally focused and map most heavily to the Identify and Protect functions, with select controls also addressing Detect and Govern.

---

## NIST CSF 2.0 Function Overview

| Function | Abbreviation | Purpose |
|---|---|---|
| Govern | GV | Organizational cybersecurity risk strategy, expectations, and policy |
| Identify | ID | Understanding assets, risks, and context |
| Protect | PR | Safeguards to deliver critical services |
| Detect | DE | Identifying cybersecurity events |
| Respond | RS | Actions regarding detected incidents |
| Recover | RC | Restoring capabilities after incidents |

---

## CIS Controls 1–8: Detailed Mapping to NIST CSF 2.0

---

### CIS Control 1: Inventory and Control of Enterprise Assets

**Description:** Actively manage (inventory, track, and correct) all enterprise assets—end-user devices, network devices, IoT, and servers—connected to the infrastructure.

| NIST CSF 2.0 Function | NIST CSF 2.0 Category | Category Description |
|---|---|---|
| **Identify (ID)** | ID.AM-01 | Inventories of hardware assets are maintained |
| **Identify (ID)** | ID.AM-02 | Inventories of software, services, and systems are maintained |
| **Identify (ID)** | ID.AM-05 | Assets are prioritized based on classification, criticality, resources, and impact |
| **Govern (GV)** | GV.OC-04 | Legal, regulatory, and contractual requirements regarding cybersecurity are understood |

**Primary Function:** Identify
**Secondary Function:** Govern

**Rationale:** Asset inventory is foundational to understanding what an organization has and needs to protect. This directly supports the Identify function's asset management (ID.AM) category. The governance overlay applies because asset management policies must be established and maintained.

---

### CIS Control 2: Inventory and Control of Software Assets

**Description:** Actively manage (inventory, track, and correct) all software on the network so that only authorized software is installed and can execute.

| NIST CSF 2.0 Function | NIST CSF 2.0 Category | Category Description |
|---|---|---|
| **Identify (ID)** | ID.AM-02 | Inventories of software, services, and systems are maintained |
| **Protect (PR)** | PR.PS-01 | Configuration management practices are established and applied |
| **Protect (PR)** | PR.PS-02 | Software is maintained, replaced, and removed commensurate with risk |
| **Govern (GV)** | GV.PO-01 | Policy for managing cybersecurity risks is established, communicated, and enforced |

**Primary Function:** Identify
**Secondary Function:** Protect

**Rationale:** Software inventory aligns to ID.AM-02 for asset management. Application whitelisting and allowlisting controls—core to CIS Control 2—also align to Protect (PR.PS) as they prevent unauthorized software execution. The allowlisting policy element maps to Govern.

---

### CIS Control 3: Data Protection

**Description:** Develop processes and technical controls to identify, classify, securely handle, retain, and dispose of data.

| NIST CSF 2.0 Function | NIST CSF 2.0 Category | Category Description |
|---|---|---|
| **Identify (ID)** | ID.AM-07 | Inventories of data and corresponding metadata for designated data are maintained |
| **Protect (PR)** | PR.DS-01 | The confidentiality, integrity, and availability of data-at-rest are protected |
| **Protect (PR)** | PR.DS-02 | The confidentiality, integrity, and availability of data-in-transit are protected |
| **Protect (PR)** | PR.DS-10 | The confidentiality, integrity, and availability of data-in-use are protected |
| **Protect (PR)** | PR.DS-11 | Backups of data are created, protected, maintained, and tested |
| **Govern (GV)** | GV.PO-01 | Policy for managing cybersecurity risks is established, communicated, and enforced |

**Primary Function:** Protect
**Secondary Function:** Identify

**Rationale:** Data protection controls are the quintessential Protect-function activity. Data classification (ID.AM-07) belongs to Identify, while encryption, DLP, and secure disposal map to PR.DS categories. Data retention policies map to Govern.

---

### CIS Control 4: Secure Configuration of Enterprise Assets and Software

**Description:** Establish and maintain the secure configuration of enterprise assets (end-user devices, network devices, servers) and software.

| NIST CSF 2.0 Function | NIST CSF 2.0 Category | Category Description |
|---|---|---|
| **Protect (PR)** | PR.PS-01 | Configuration management practices are established and applied |
| **Protect (PR)** | PR.PS-04 | Logs of events are generated and retained |
| **Identify (ID)** | ID.RA-01 | Vulnerabilities in assets are identified, validated, and recorded |
| **Govern (GV)** | GV.PO-02 | Cybersecurity roles, responsibilities, and authorities are established, communicated, and enforced |

**Primary Function:** Protect
**Secondary Function:** Identify

**Rationale:** Hardening and baseline configuration management are core Protect activities (PR.PS-01). Secure configuration also reduces the attack surface, indirectly supporting risk identification (ID.RA-01). Logging configurations (PR.PS-04) bridge toward Detect but are considered a Protect activity in the context of secure configuration.

---

### CIS Control 5: Account Management

**Description:** Use processes and tools to assign and manage authorization to credentials for user accounts, including administrator accounts, as well as service accounts.

| NIST CSF 2.0 Function | NIST CSF 2.0 Category | Category Description |
|---|---|---|
| **Protect (PR)** | PR.AA-01 | Identities and credentials for authorized users, services, and hardware are managed by the organization |
| **Protect (PR)** | PR.AA-02 | Identities are proofed and bound to credentials based on the context of interactions |
| **Protect (PR)** | PR.AA-05 | Access permissions, entitlements, and authorizations are defined in a policy, managed, enforced, and reviewed |
| **Govern (GV)** | GV.PO-01 | Policy for managing cybersecurity risks is established, communicated, and enforced |

**Primary Function:** Protect
**Secondary Function:** Govern

**Rationale:** Account and identity lifecycle management is a direct implementation of NIST CSF 2.0's PR.AA (Identity Management, Authentication, and Access Control) category. Privileged account management maps specifically to PR.AA-05. Account management policy requirements map to Govern.

---

### CIS Control 6: Access Control Management

**Description:** Use processes and tools to create, assign, manage, and revoke access credentials and privileges for user, administrator, and service accounts.

| NIST CSF 2.0 Function | NIST CSF 2.0 Category | Category Description |
|---|---|---|
| **Protect (PR)** | PR.AA-03 | Users, services, and hardware are authenticated |
| **Protect (PR)** | PR.AA-04 | Identity assertions are protected, conveyed, and verified |
| **Protect (PR)** | PR.AA-05 | Access permissions, entitlements, and authorizations are defined in a policy, managed, enforced, and reviewed |
| **Protect (PR)** | PR.AA-06 | Physical access to assets is managed, monitored, and controlled |
| **Identify (ID)** | ID.AM-03 | Representations of the organization's authorized network communication and internal and external network data flows are maintained |

**Primary Function:** Protect
**Secondary Function:** Identify

**Rationale:** CIS Control 6 focuses on least-privilege access, role-based access control, and MFA—all of which map squarely to PR.AA categories in NIST CSF 2.0. Network segmentation and access path documentation support ID.AM-03 (network flow inventories). Note: CIS Controls 5 and 6 are closely related and together form the full IAM picture mapped to PR.AA.

---

### CIS Control 7: Continuous Vulnerability Management

**Description:** Develop a plan to continuously assess and track vulnerabilities on all enterprise assets within the organization's infrastructure, in order to remediate and minimize the window of opportunity for attackers.

| NIST CSF 2.0 Function | NIST CSF 2.0 Category | Category Description |
|---|---|---|
| **Identify (ID)** | ID.RA-01 | Vulnerabilities in assets are identified, validated, and recorded |
| **Identify (ID)** | ID.RA-07 | Changes and exceptions are managed, assessed for risk impact, informed by prior risk analyses, and recorded |
| **Protect (PR)** | PR.PS-02 | Software is maintained, replaced, and removed commensurate with risk |
| **Govern (GV)** | GV.RM-05 | Risks with shared characteristics are aggregated and managed |
| **Detect (DE)** | DE.CM-09 | Computing hardware and software, runtime environments, and their data are monitored to find potentially adverse events |

**Primary Function:** Identify
**Secondary Function:** Protect, Detect

**Rationale:** Vulnerability scanning, patch management, and risk-based remediation prioritization are predominantly Identify-function activities (ID.RA). Patching maps to Protect (PR.PS-02). Continuous scanning that detects newly emerged vulnerabilities in real time has a secondary alignment to Detect (DE.CM-09), since authenticated vulnerability scans can surface active exploitation indicators.

---

### CIS Control 8: Audit Log Management

**Description:** Collect, alert, review, and retain audit logs of events that could help detect, understand, or recover from an attack.

| NIST CSF 2.0 Function | NIST CSF 2.0 Category | Category Description |
|---|---|---|
| **Detect (DE)** | DE.CM-03 | Personnel activity and technology usage are monitored to find potentially adverse events |
| **Detect (DE)** | DE.CM-06 | External service provider activities and services are monitored to find potentially adverse events |
| **Detect (DE)** | DE.CM-09 | Computing hardware and software, runtime environments, and their data are monitored to find potentially adverse events |
| **Detect (DE)** | DE.AE-02 | Potentially adverse events are analyzed to better understand associated activities |
| **Detect (DE)** | DE.AE-03 | Information is correlated from multiple sources |
| **Protect (PR)** | PR.PS-04 | Logs of events are generated and retained |
| **Govern (GV)** | GV.PO-01 | Policy for managing cybersecurity risks is established, communicated, and enforced |

**Primary Function:** Detect
**Secondary Function:** Protect

**Rationale:** Audit log management is the most direct mapping to the Detect function among CIS Controls 1–8. Log generation maps to Protect (PR.PS-04—a prerequisite), while log review, alerting, correlation, and analysis map to DE.CM and DE.AE categories. Log retention policies map to Govern. CIS Control 8 is the primary vehicle for demonstrating Detect function coverage within the first eight CIS Controls.

---

## Summary Mapping Table

| CIS Control | Primary CSF Function | Secondary CSF Function(s) | Key CSF Categories |
|---|---|---|---|
| 1 – Enterprise Asset Inventory | Identify (ID) | Govern (GV) | ID.AM-01, ID.AM-02, ID.AM-05 |
| 2 – Software Asset Inventory | Identify (ID) | Protect (PR), Govern (GV) | ID.AM-02, PR.PS-01, PR.PS-02 |
| 3 – Data Protection | Protect (PR) | Identify (ID), Govern (GV) | PR.DS-01, PR.DS-02, PR.DS-10, PR.DS-11, ID.AM-07 |
| 4 – Secure Configuration | Protect (PR) | Identify (ID), Govern (GV) | PR.PS-01, PR.PS-04, ID.RA-01 |
| 5 – Account Management | Protect (PR) | Govern (GV) | PR.AA-01, PR.AA-02, PR.AA-05 |
| 6 – Access Control Management | Protect (PR) | Identify (ID) | PR.AA-03, PR.AA-04, PR.AA-05, PR.AA-06 |
| 7 – Vulnerability Management | Identify (ID) | Protect (PR), Detect (DE) | ID.RA-01, ID.RA-07, PR.PS-02, DE.CM-09 |
| 8 – Audit Log Management | **Detect (DE)** | Protect (PR), Govern (GV) | DE.CM-03, DE.CM-06, DE.CM-09, DE.AE-02, DE.AE-03, PR.PS-04 |

---

## Which CIS Controls Cover the NIST CSF "Detect" Function?

Among CIS Controls 1 through 8, the Detect function is addressed primarily by:

### Primary Coverage: CIS Control 8 – Audit Log Management

CIS Control 8 is the dominant Detect-function control in this set. It maps to multiple DE categories:

- **DE.CM (Continuous Monitoring):** Log collection and review activities directly implement continuous monitoring of systems, users, and external services (DE.CM-03, DE.CM-06, DE.CM-09).
- **DE.AE (Adverse Event Analysis):** Log correlation, alerting, and event analysis activities implement DE.AE-02 (event analysis) and DE.AE-03 (multi-source correlation).

The specific CIS Control 8 safeguards that drive Detect coverage include:
- **8.2** – Collect audit logs from all enterprise assets
- **8.5** – Collect detailed audit logs (authentication, privilege use, data access)
- **8.6** – Collect DNS query audit logs
- **8.7** – Collect URL request audit logs
- **8.9** – Centralize log management (SIEM or equivalent)
- **8.11** – Conduct audit log reviews (alerts on anomalies)

### Secondary Coverage: CIS Control 7 – Continuous Vulnerability Management

CIS Control 7 has a secondary alignment to Detect (specifically **DE.CM-09**) because:
- Authenticated vulnerability scanning tools actively probe systems to identify exposed vulnerabilities and configuration weaknesses in real time.
- When integrated with a continuous monitoring program, vulnerability scan results feed into detection pipelines.
- However, this is a supporting rather than primary alignment—vulnerability management's core function is risk identification (ID.RA), not event detection.

### Gap Analysis: Detect Function Coverage in CIS Controls 1–8

It is important to note that CIS Controls 1–8 provide **limited Detect function coverage**. The bulk of Detect-function capabilities in CIS Controls v8 live in higher-numbered controls:

| CIS Control | Detect Relevance |
|---|---|
| Control 9 | Email and Web Browser Protections — partial DE.CM alignment |
| Control 10 | Malware Defenses — DE.CM-09 (endpoint monitoring) |
| Control 13 | Network Monitoring and Defense — **core DE.CM** coverage |
| Control 17 | Incident Response Management — DE.AE, RS function |

**For government contract alignment:** When demonstrating NIST CSF 2.0 Detect function coverage using only CIS Controls 1–8, organizations should document CIS Control 8 as the primary mechanism and supplement with evidence from CIS Controls 9–13 for complete Detect coverage. A crosswalk document noting that full DE function implementation requires controls beyond the first eight is recommended for assessor transparency.

---

## Recommendations for Government Contract Alignment

1. **Document the mapping formally:** Produce a crosswalk table (similar to this document) that assessors can review, showing each CSF category and the CIS Control safeguard that satisfies it.

2. **Address the Detect gap explicitly:** CIS Controls 1–8 are heavily weighted toward Identify and Protect. For full Detect coverage, include Controls 9, 10, and 13 in your program scope and crosswalk.

3. **Leverage CISA's CIS-to-NIST mappings:** CISA and the Center for Internet Security publish official crosswalk documents. Reference these as authoritative sources in your contract deliverables.

4. **Implement CIS Control 8 as your SIEM/logging strategy:** A centralized logging platform (SIEM) implementing Control 8 safeguards provides the most direct evidence of Detect function compliance for government auditors.

5. **Respond and Recover gap:** CIS Controls 1–8 do not meaningfully address Respond (RS) or Recover (RC) functions. Controls 17 (Incident Response) and 11 (Data Recovery) must be cited for those functions.

---

*Document prepared as part of security program framework alignment initiative. Based on CIS Controls v8 and NIST Cybersecurity Framework 2.0 (February 2024).*
