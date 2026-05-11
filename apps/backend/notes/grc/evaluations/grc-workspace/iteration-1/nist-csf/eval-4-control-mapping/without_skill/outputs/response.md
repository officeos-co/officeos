# NIST CSF Protect Function — Control Mapping
## Controls: Okta SSO + MFA, CrowdStrike EDR, Qualys, Druva, Security Awareness Training

---

## Overview

This document maps your five current security controls to the NIST Cybersecurity Framework (CSF) Protect function and related functions. The Protect function covers safeguards to manage cybersecurity risk, including access controls, awareness training, data security, maintenance, and protective technologies.

The five controls you listed span multiple areas of the framework, with strong coverage in some areas and notable gaps in others.

---

## Control-by-Control Mapping

### 1. Okta SSO with MFA

**Primary CSF Mapping: Protect → Identity Management and Access Control**

| CSF Area | Coverage | Assessment |
|----------|----------|------------|
| Identity Management | Strong | Okta provides centralized identity management, lifecycle management, and single sign-on across applications |
| Authentication | Strong | MFA enforcement through Okta directly addresses the requirement for multi-factor authentication |
| Access Control | Moderate | Okta enables centralized access control; however, least privilege and role-based access control (RBAC) governance depends on how Okta groups and permissions are configured and maintained |
| Privileged Access | Partial | Standard Okta does not typically cover privileged access management (PAM); consider whether admin accounts have additional protections |

**What Okta + MFA covers well:**
- User authentication with multiple factors
- Centralized identity lifecycle (provisioning and deprovisioning)
- Single sign-on reducing credential sprawl
- Adaptive authentication policies

**What may still be needed:**
- Privileged Access Management (PAM) for admin accounts (e.g., CyberArk, BeyondTrust)
- Access certification / recertification processes (quarterly reviews of who has access to what)
- Formal least-privilege role definitions and RBAC governance

---

### 2. CrowdStrike EDR

**Primary CSF Mapping: Protect → Protective Technology / Detect → Security Continuous Monitoring**

| CSF Area | Coverage | Assessment |
|----------|----------|------------|
| Malware/Threat Protection | Strong | CrowdStrike Falcon provides next-gen antivirus and behavioral threat detection |
| Endpoint Monitoring | Strong | Real-time endpoint telemetry and event collection |
| Anomaly Detection | Strong | AI-driven behavioral analytics detect threats that signature-based tools miss |
| Incident Investigation | Moderate | Falcon Insight provides investigation capabilities, though analyst training is required |

**What CrowdStrike covers well:**
- Endpoint protection across laptops, servers, and workstations
- Real-time threat detection and prevention
- Threat hunting and forensic investigation capabilities
- Integration with SIEM/SOAR tools

**CSF Functions Addressed:**
- **Protect**: Protective technology for endpoints (malware prevention, application control)
- **Detect**: Continuous monitoring of endpoints for security events
- **Respond**: Supports incident analysis and investigation

**What may still be needed:**
- Network-level monitoring (NDR or SIEM) — CrowdStrike alone doesn't cover network traffic analysis
- Coverage verification — ensure all endpoints are enrolled (laptops, servers, any OT/ICS endpoints)
- Prevention mode validation — confirm Falcon is in prevention mode, not detect-only

---

### 3. Qualys Vulnerability Scanning

**Primary CSF Mapping: Identify → Risk Assessment / Protect → Maintenance**

| CSF Area | Coverage | Assessment |
|----------|----------|------------|
| Vulnerability Identification | Strong | Qualys provides comprehensive vulnerability scanning across IT assets |
| Risk Assessment | Strong | Vulnerability data feeds directly into your risk assessment process |
| Patch/Remediation Tracking | Partial | Qualys identifies vulnerabilities but doesn't remediate them; a patching process is required |
| Asset Discovery | Moderate | Qualys can discover assets during scanning, supplementing asset inventory |

**What Qualys covers well:**
- Regular vulnerability discovery across servers, workstations, and network devices
- Prioritization of vulnerabilities by severity (CVSS scoring)
- Compliance scanning against benchmarks (CIS, PCI DSS, etc.)
- Trend reporting on vulnerability remediation over time

**CSF Functions Addressed:**
- **Identify**: Vulnerability identification and risk assessment inputs
- **Protect**: Maintenance — knowing what needs to be patched
- **Detect**: Identifying vulnerable systems before attackers exploit them

**What may still be needed:**
- Patch management process with defined SLAs (e.g., critical patches within 48 hours)
- Coverage of cloud assets and SaaS applications (Qualys may not scan all cloud workloads without agents)
- Web application scanning if you have customer-facing web applications

---

### 4. Druva Cloud Backup

**Primary CSF Mapping: Protect → Data Protection and Resilience**

| CSF Area | Coverage | Assessment |
|----------|----------|------------|
| Data Backup | Strong | Druva provides cloud-based backup for endpoints, servers, and cloud data |
| Data Recovery | Strong | Enables restoration of files, systems, and data after incidents |
| Ransomware Protection | Strong | Cloud backups are typically isolated from on-premises ransomware attacks |
| Data Retention | Moderate | Retention policies need to be aligned with legal and business requirements |

**What Druva covers well:**
- Automated backup for endpoint and cloud data
- Protection against data loss from ransomware, hardware failure, or accidental deletion
- Centralized management with reporting on backup coverage and compliance
- Fast restore capabilities to minimize downtime

**CSF Functions Addressed:**
- **Protect**: Data protection, resilience, and backup
- **Recover**: Enables recovery of data after a cybersecurity incident

**What may still be needed:**
- Recovery testing — are you regularly testing restores? (At minimum annually)
- Recovery Time Objectives (RTOs) — how fast can you restore critical systems?
- Coverage gaps — are all critical systems, databases, and cloud workloads backed up?
- Offsite/air-gapped copies for highly sensitive or critical systems (3-2-1 backup rule)

---

### 5. Written Security Awareness Training Program

**Primary CSF Mapping: Protect → Awareness and Training**

| CSF Area | Coverage | Assessment |
|----------|----------|------------|
| General Security Awareness | Moderate | A written program provides foundational knowledge, but effectiveness depends on delivery and frequency |
| Phishing Awareness | Unknown | Does your program include simulated phishing exercises? If not, this is a key gap |
| Role-Based Training | Unknown | Does the program include specialized content for IT admins, executives, or other high-risk roles? |
| Training Frequency | Unknown | Annual training meets a minimum bar but is generally considered insufficient for maintaining awareness |

**What a written training program covers:**
- Foundational cybersecurity knowledge for all staff
- Policy acknowledgment and compliance
- Documentation that training occurred (important for audits)

**What is typically missing from "written programs only":**
- Simulated phishing exercises to test and reinforce learning
- Role-based training for high-risk roles (IT administrators, finance, executives)
- Scenario-based training (what to do when you receive a suspicious email)
- Metrics on training completion and effectiveness

---

## Consolidated Coverage Map

| CSF Protect Area | Coverage | Your Controls | Gap Areas |
|-----------------|----------|---------------|-----------|
| Identity & Access Management | Good | Okta SSO + MFA | PAM for privileged accounts; access reviews |
| Awareness & Training | Partial | Written Training Program | Phishing sims; role-based training; frequency |
| Data Security | Limited | Druva (backup only) | No DLP, encryption policy, or data classification |
| Protective Technology | Good | CrowdStrike EDR | Network monitoring; cloud workload protection |
| Maintenance / Vulnerability Mgmt | Partial | Qualys | Patch management process; remediation tracking |

---

## Key Gaps in Your Protect Function Coverage

### 1. Data Security — Significant Gap
Your controls do not include data loss prevention (DLP), data classification, or encryption policies. This means you lack visibility into where sensitive data lives and whether it's adequately protected in transit and at rest.

**Recommended additions**: Data classification policy, DLP tooling (Microsoft Purview, Zscaler), encryption standards for data at rest and in transit.

### 2. Privileged Access Management — Moderate Gap
Okta handles standard user access, but privileged accounts (domain admins, database admins, root accounts) typically require additional controls: session recording, just-in-time access, and vault-based credential management.

**Recommended additions**: PAM solution (CyberArk, BeyondTrust, Delinea) or at minimum, Okta Privileged Access.

### 3. Network-Level Monitoring — Moderate Gap
CrowdStrike monitors endpoints but not network traffic. Lateral movement, command-and-control traffic, and network-level attacks may go undetected.

**Recommended additions**: Network detection and response (NDR) tool or SIEM with network log ingestion.

### 4. Patch Management Process — Moderate Gap
Qualys identifies vulnerabilities, but there must be a defined process and tooling to remediate them with defined SLAs.

**Recommended additions**: Formal patch management process with severity-based SLAs; patching tools (WSUS, Intune, Ansible).

### 5. Training Effectiveness — Moderate Gap
A written training program meets the minimum bar but is not sufficient on its own. Without simulated phishing and role-based content, human risk remains high.

**Recommended additions**: KnowBe4, Proofpoint Security Awareness, or similar platform; quarterly phishing simulations.

---

## Summary Assessment

Your five controls provide a reasonable foundation across Identity & Access, Endpoint Protection, Vulnerability Management, and Data Backup. However, there are meaningful gaps in data security controls, network monitoring, privileged access management, patch remediation, and training effectiveness that should be addressed to achieve a strong Protect function posture.
