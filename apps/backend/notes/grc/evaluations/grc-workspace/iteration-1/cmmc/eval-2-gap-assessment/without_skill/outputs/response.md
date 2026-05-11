# CMMC Level 2 Gap Assessment

**Assessment Date:** 2026-04-25
**Assessed Environment:** Active Directory (80 users), Windows 10/11 via Intune, Palo Alto Firewall, Microsoft 365 E3 (commercial), SharePoint Online for CUI, Nessus quarterly unauthenticated scanning, no MFA, no SIEM

---

## Background: CMMC Level 2 and SPRS Scoring

CMMC Level 2 maps directly to NIST SP 800-171 Rev 2, which contains 110 security requirements across 14 domains. The Supplier Performance Risk System (SPRS) score starts at 110 points. Each failed practice has a defined point value (1, 3, or 5 points). A perfect score is 110; organizations must document all deficiencies in a Plan of Action and Milestones (POA&M) and report their self-assessed score to SPRS.

Point values per practice:
- 5-point practices: highest-risk controls (typically access control, identification & authentication, incident response, configuration management)
- 3-point practices: moderate-risk controls
- 1-point practices: lower-risk controls

The average defense contractor self-assessment score is approximately 50-70 out of 110.

---

## Critical Gap Analysis by Domain

### 1. Access Control (AC) — DOMAIN SCORE IMPACT: HIGH

**Gap AC.1.001 / AC.1.002 — No MFA Deployed**
- NIST 800-171 Practice: 3.5.3 — Use multifactor authentication for local and network access to privileged accounts and for network access to non-privileged accounts
- Status: NON-COMPLIANT
- Risk: With 80 AD users and no MFA, all accounts — including privileged admin accounts — are protected only by passwords. This is one of the highest-risk gaps in the environment.
- SPRS Impact: -5 points (3.5.3 is a 5-point practice)
- Remediation: Deploy Azure AD MFA (available with M365 E3 / Entra ID P1 included). Enable Conditional Access policies requiring MFA for all users, enforce for privileged accounts as a priority. For on-premises AD, consider integrating with Azure AD via Entra ID hybrid join.

**Gap AC — Privileged Account Separation**
- NIST 800-171 Practice: 3.1.6 — Use non-privileged accounts or roles when accessing non-security functions
- Status: LIKELY NON-COMPLIANT (no evidence of enforced least privilege)
- SPRS Impact: -3 points
- Remediation: Audit AD groups; enforce separate admin accounts for privileged tasks; apply role-based access control via Intune and Group Policy.

**Gap AC — External Access Controls (CUI on SharePoint)**
- NIST 800-171 Practice: 3.1.3 — Control the flow of CUI in accordance with approved authorizations
- Status: NON-COMPLIANT — Microsoft 365 E3 (commercial, non-GCC) is not FedRAMP-authorized at the required impact level for CUI storage. SharePoint Online in commercial M365 does not meet the FedRAMP Moderate or High authorization required under DFARS 252.204-7012 and CMMC.
- SPRS Impact: -5 points (systemic impact across multiple CUI-related controls)
- Remediation: Migrate CUI workloads to Microsoft 365 GCC or GCC High. This is a critical compliance blocker — storing CUI on commercial M365 may render the entire CMMC assessment invalid.

---

### 2. Identification and Authentication (IA) — DOMAIN SCORE IMPACT: HIGH

**Gap IA — No MFA (see AC above)**
- NIST 800-171 Practice: 3.5.3
- SPRS Impact: -5 points (same practice as above, counted once)

**Gap IA — Password Policy Enforcement**
- NIST 800-171 Practice: 3.5.7 — Enforce a minimum password complexity and change when compromised
- Status: POTENTIALLY NON-COMPLIANT — AD password policies must be verified; default AD policies may not meet NIST guidelines (e.g., checking against compromised password lists)
- SPRS Impact: -3 points if not configured correctly
- Remediation: Implement Azure AD Password Protection (available in Entra ID P1, included with M365 E3) to enforce banned-password lists and complexity requirements.

**Gap IA — Authenticator Management**
- NIST 800-171 Practice: 3.5.10 — Store and transmit only cryptographically-protected passwords
- Status: REQUIRES VERIFICATION — Confirm AD is using Kerberos/NTLM hardening; confirm no legacy protocols (NTLM v1, LM hash) are enabled
- SPRS Impact: -3 points if legacy protocols are active
- Remediation: Disable LM hash storage, enforce NTLMv2 minimum, prefer Kerberos; enable smart lockout in Entra ID.

---

### 3. Audit and Accountability (AU) — DOMAIN SCORE IMPACT: HIGH

**Gap AU — No SIEM / Centralized Log Management**
- NIST 800-171 Practices: 3.3.1, 3.3.2 — Create, protect, and retain system audit logs; ensure actions of individual users can be traced
- Status: NON-COMPLIANT — Without a SIEM or centralized logging solution, audit logs from AD, endpoints, firewall, and SharePoint are siloed or unavailable for review, correlation, or forensic investigation.
- SPRS Impact: -5 points (3.3.1) + -3 points (3.3.2) = -8 points combined
- Remediation: Deploy Microsoft Sentinel (integrates natively with M365 E3, AD, Intune, and Palo Alto via connectors). Cost-effective entry point given existing M365 investment. Alternatively, deploy a SIEM such as Splunk, Elastic SIEM, or IBM QRadar. Minimum viable: centralize Windows Event Logs and Azure AD sign-in logs.

**Gap AU — Log Review Process**
- NIST 800-171 Practice: 3.3.2 — Review and update logged events
- Status: NON-COMPLIANT — No SIEM means no regular log review cadence or alerting
- SPRS Impact: Included in above (-3 points)
- Remediation: Establish formal log review procedures and alerts for critical events (failed logins, privilege escalation, CUI access).

---

### 4. Risk Assessment (RA) — DOMAIN SCORE IMPACT: MEDIUM-HIGH

**Gap RA — Unauthenticated Vulnerability Scanning**
- NIST 800-171 Practice: 3.11.2 — Scan for vulnerabilities in organizational systems and applications periodically and when new vulnerabilities affecting those systems are identified
- Status: PARTIALLY NON-COMPLIANT — Quarterly unauthenticated Nessus scans do not meet the intent of 3.11.2. Unauthenticated scans miss 60-70% of vulnerabilities (missing local privilege escalation, misconfigured services, unpatched software visible only with credentials). CMMC assessors expect authenticated scanning.
- SPRS Impact: -5 points (3.11.2 is a 5-point practice; partial credit may apply in self-assessment but assessors are likely to flag this)
- Remediation: Convert Nessus scans to authenticated mode using a dedicated service account with read-only local admin rights. Increase scan frequency to monthly at minimum. Consider continuous scanning for critical assets.

**Gap RA — Scan Frequency**
- Status: Quarterly is insufficient for a dynamic environment. NIST guidance and CMMC assessors expect more frequent scanning (monthly minimum, continuous preferred for high-value systems).
- SPRS Impact: Included in 3.11.2 above
- Remediation: Move to monthly authenticated scans; implement Intune compliance policies to enforce patch status.

---

### 5. Configuration Management (CM) — DOMAIN SCORE IMPACT: MEDIUM

**Gap CM — Baseline Configuration Documentation**
- NIST 800-171 Practice: 3.4.1 — Establish and maintain baseline configurations and inventories of organizational systems
- Status: REQUIRES VERIFICATION — Intune provides device management but a formal documented baseline configuration standard must exist
- SPRS Impact: -3 points if undocumented
- Remediation: Document baseline configuration standards (CIS Benchmarks for Windows 10/11 recommended). Enforce via Intune Configuration Profiles and Compliance Policies.

**Gap CM — Security Configuration Enforcement**
- NIST 800-171 Practice: 3.4.2 — Establish and enforce security configuration settings
- Status: POTENTIALLY NON-COMPLIANT — Intune is deployed but configuration of security settings (BitLocker, Windows Defender, firewall settings, AppLocker/WDAC) must be verified against documented baselines
- SPRS Impact: -3 points if enforcement gaps exist
- Remediation: Deploy CIS Level 1 or DISA STIG baselines via Intune; enforce BitLocker encryption; enable Windows Defender ATP (available with M365 E3 via Defender for Endpoint P1).

---

### 6. System and Communications Protection (SC) — DOMAIN SCORE IMPACT: MEDIUM

**Gap SC — CUI Encryption at Rest and In Transit**
- NIST 800-171 Practice: 3.13.10, 3.13.8 — Implement cryptographic mechanisms to prevent unauthorized disclosure of CUI
- Status: PARTIALLY NON-COMPLIANT — Commercial M365 SharePoint Online encrypts data at rest, but without GCC/GCC High authorization, the encryption key management and data residency do not meet DoD requirements. Additionally, BitLocker enforcement on endpoints must be verified.
- SPRS Impact: -3 to -5 points depending on assessor interpretation
- Remediation: Migrate to M365 GCC/GCC High (primary remediation). Enforce BitLocker via Intune for all Windows endpoints.

**Gap SC — Network Segmentation**
- NIST 800-171 Practice: 3.13.3 — Separate user functionality from system management functionality
- Status: REQUIRES VERIFICATION — Palo Alto firewall presence is positive, but network segmentation of CUI systems, management networks, and user segments must be verified and documented.
- SPRS Impact: -3 points if not implemented
- Remediation: Review Palo Alto zone configurations; document network segmentation architecture; ensure CUI systems are in a dedicated zone with restricted access.

---

### 7. Incident Response (IR) — DOMAIN SCORE IMPACT: MEDIUM

**Gap IR — Incident Response Capability**
- NIST 800-171 Practice: 3.6.1, 3.6.2 — Establish an operational incident-handling capability; track, document, and report incidents
- Status: LIKELY NON-COMPLIANT — No SIEM means no automated alerting or incident detection capability. Incident response plan likely exists on paper but may lack operational tooling.
- SPRS Impact: -3 points (3.6.1) + -3 points (3.6.2) = -6 points if IR capability is paper-only
- Remediation: Implement Microsoft Sentinel or equivalent for detection; develop and test IR playbooks; establish a DFARS 252.204-7012-compliant cyber incident reporting process (report to DoD within 72 hours).

---

### 8. Media Protection (MP) — DOMAIN SCORE IMPACT: MEDIUM

**Gap MP — CUI on Commercial Cloud**
- NIST 800-171 Practice: 3.8.1, 3.8.2 — Protect system media containing CUI; limit access to CUI on digital media
- Status: NON-COMPLIANT — Commercial M365 (non-GCC) does not provide the required government cloud boundary for CUI. This compounds the SharePoint Online gap.
- SPRS Impact: -3 points (partially overlaps with AC and SC gaps)

---

## Summary: SPRS Score Impact Estimate

| Domain | Practice | Gap Description | Point Value | Status |
|--------|----------|-----------------|-------------|--------|
| IA/AC | 3.5.3 | No MFA deployed | -5 | Non-Compliant |
| AU | 3.3.1 | No centralized logging/SIEM | -5 | Non-Compliant |
| AU | 3.3.2 | No log review process | -3 | Non-Compliant |
| RA | 3.11.2 | Unauthenticated/quarterly scanning only | -5 | Partial |
| AC | 3.1.3 | CUI on commercial M365 (not GCC) | -5 | Non-Compliant |
| IR | 3.6.1 | Insufficient IR operational capability | -3 | Likely Non-Compliant |
| IR | 3.6.2 | Incident tracking/reporting gaps | -3 | Likely Non-Compliant |
| AC | 3.1.6 | Insufficient least privilege enforcement | -3 | Likely Non-Compliant |
| CM | 3.4.1 | No documented baseline configurations | -3 | Likely Non-Compliant |
| CM | 3.4.2 | Unverified security config enforcement | -3 | Partial |
| IA | 3.5.7 | Password policy compliance uncertain | -3 | Requires Verification |
| SC | 3.13.8 | CUI encryption / GCC boundary issue | -3 | Non-Compliant |
| SC | 3.13.3 | Network segmentation unverified | -3 | Requires Verification |

**Estimated SPRS Score Deductions from Identified Gaps: -47 points**

**Estimated SPRS Score: 110 - 47 = ~63 out of 110**

Note: This estimate covers only the explicitly identified gaps. A full assessment would likely identify additional deficiencies, particularly in:
- System and Information Integrity (SI) — patch management cadence, malware protection
- Personnel Security (PS) — screening, termination procedures
- Physical Protection (PE) — physical access controls to systems storing CUI
- Awareness and Training (AT) — security awareness training documentation

A realistic SPRS score for this environment is likely in the range of **40-65**, depending on undocumented controls and additional gaps discovered in a full assessment.

---

## Priority Remediation Roadmap

### Immediate (0-30 days) — Critical Risk Reduction
1. **Enable MFA for all users** via Entra ID Conditional Access (M365 E3 includes Entra ID P1 which provides MFA and Conditional Access at no additional cost). Start with privileged accounts.
2. **Initiate M365 GCC migration planning** — storing CUI on commercial M365 is a fundamental compliance blocker. Begin procurement/licensing discussions immediately.
3. **Enable authenticated Nessus scanning** — convert scans to credentialed mode and remediate critical/high findings within 30 days.

### Short-term (30-90 days) — SIEM and Visibility
4. **Deploy Microsoft Sentinel** or equivalent SIEM. Connect data sources: Azure AD, M365 Defender, Windows Security Events, Palo Alto (via Syslog/CEF connector). Enable out-of-box analytics rules.
5. **Document and enforce endpoint baselines** via Intune — CIS Level 1 for Windows 10/11, BitLocker enforcement, Defender configuration.
6. **Develop and test Incident Response Plan** with specific playbooks for CUI incidents and DFARS 72-hour reporting.

### Medium-term (90-180 days) — Architecture and Process
7. **Complete M365 GCC migration** for all CUI workloads.
8. **Implement privileged access management** — separate admin accounts, Privileged Identity Management (PIM) in Entra ID P2 if budget allows.
9. **Conduct formal NIST 800-171 self-assessment** against all 110 practices, update SPRS score in supplier portal.
10. **Increase vulnerability scan frequency** to monthly; implement Intune compliance policies for patch management.

### Long-term (180+ days) — CMMC Level 2 Assessment Readiness
11. **Engage a C3PAO** (Certified Third-Party Assessment Organization) for pre-assessment review.
12. **Complete System Security Plan (SSP)** documenting all 110 practices.
13. **Address all POA&M items** or accept risk formally.
14. **Submit for CMMC Level 2 Third-Party Assessment** (required for contracts with sensitive CUI per DFARS 252.204-7021).

---

## Key Compliance Blocker: M365 Commercial vs. GCC

This deserves special emphasis. DFARS 252.204-7012 requires cloud service providers handling covered defense information (CDI/CUI) to meet FedRAMP Moderate authorization at minimum. Microsoft 365 commercial (E3) is NOT authorized for CUI — only M365 GCC and GCC High meet this requirement. M365 GCC High is required for some controlled unclassified information categories (ITAR, Export Controlled).

Continuing to store CUI on commercial SharePoint Online exposes the organization to:
- Contract termination for DFARS non-compliance
- False Claims Act liability
- Automatic CMMC assessment failure
- SPRS score invalidation

This single gap affects multiple NIST 800-171 practices simultaneously (3.1.3, 3.13.8, 3.8.1, 3.8.2, and potentially 3.12.1 through systemic risk).

---

*Assessment prepared based on NIST SP 800-171 Rev 2, CMMC Model v2.0, and DFARS 252.204-7012/7021 requirements. Point values reference the DoD CMMC/SPRS scoring methodology. This is a self-assessment gap analysis and does not constitute a formal C3PAO assessment.*
