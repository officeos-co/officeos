# CMMC Level 2 Gap Assessment Report

**Assessment Date**: 2026-04-25  
**CMMC Level**: Level 2 — Advanced (110 Practices, NIST SP 800-171 Rev 2)  
**Applicable DFARS Clauses**: 252.204-7012, 252.204-7019, 252.204-7020, 252.204-7021  

---

## IT Environment Summary

| Component | Detail |
|-----------|--------|
| Identity Management | Active Directory, 80 users |
| MFA | NOT DEPLOYED |
| Endpoints | Windows 10/11 managed via Intune |
| Perimeter Security | Palo Alto Firewall |
| Productivity Suite | Microsoft 365 E3 (commercial — NOT GCC) |
| SIEM | NOT DEPLOYED |
| Vulnerability Scanning | Quarterly, Nessus, **unauthenticated** |
| CUI Storage | SharePoint Online (commercial M365 E3) |

---

## Critical Pre-Assessment Finding: SharePoint Online (Commercial M365) for CUI

> **BLOCKER**: CUI stored in commercial Microsoft 365 E3 (non-GCC) violates DFARS 252.204-7012, which requires cloud services handling CUI to be FedRAMP Authorized at Moderate or equivalent. Commercial M365 E3 is NOT FedRAMP Moderate authorized for CUI. This must be migrated to **Microsoft 365 Government (GCC or GCC High)** before a C3PAO assessment can proceed. This issue affects every CUI-related practice in scope.

---

## Gap Assessment Table

### IA — Identification and Authentication

| Practice ID | Domain | Practice Statement | Status | Evidence Needed | Gap Notes | SPRS Score Impact |
|-------------|--------|--------------------|--------|-----------------|-----------|------------------|
| IA.L2-3.5.3 | IA | Use MFA for local and network access to privileged accounts and for network access to non-privileged accounts. | ❌ NOT MET | MFA enrollment report showing 100% of privileged and non-privileged accounts enrolled; Conditional Access policy exports from Entra ID/Azure AD | No MFA deployed anywhere. This is a **critical practice** — cannot have a POA&M at certification. Must be remediated before C3PAO assessment. Affects all 80 users and all privileged accounts. | **-5 pts** |
| IA.L2-3.5.4 | IA | Employ replay-resistant authentication mechanisms for network access. | ❌ NOT MET | Kerberos/NTLM configuration export; authentication logs showing replay-resistant protocols in use | With no MFA and no evidence of replay-resistant auth configuration, this is likely not met. Kerberos provides some replay resistance but must be verified and documented. | **-3 pts** |
| IA.L2-3.5.5 | IA | Employ identifier management to prevent reuse of identifiers for a defined period. | 🟡 PARTIAL | AD account lifecycle policy; script/report showing identifier reuse prevention settings | AD can enforce this but requires a documented policy and verified configuration. No evidence provided. | **-1 pt** |
| IA.L2-3.5.6 | IA | Disable identifiers after a defined inactivity period. | 🟡 PARTIAL | AD inactive account review reports; Group Policy Object (GPO) exports showing inactivity lockout settings | Likely partially configured in AD but requires formal policy, documented threshold, and evidence of enforcement. | **-1 pt** |
| IA.L2-3.5.7 | IA | Enforce minimum password complexity and change requirements. | 🟡 PARTIAL | GPO export showing password policy settings; Intune compliance policy screenshots | AD likely enforces some password complexity but must meet NIST SP 800-171 requirements and be fully documented. | **-1 pt** |
| IA.L2-3.5.10 | IA | Store and transmit only cryptographically-protected passwords (FIPS 140-2/3 validated). | 🟡 PARTIAL | AD/Windows FIPS mode configuration; verification that NTLMv2 or Kerberos with FIPS cipher suites in use | Windows can operate in FIPS mode but this must be explicitly enabled and verified. Non-GCC M365 raises concerns about FIPS-validated storage of credentials. | **-3 pts** |
| MA.L2-3.7.5 | MA | Require MFA to establish remote maintenance sessions. | ❌ NOT MET | Same as IA.L2-3.5.3 — MFA enrollment/enforcement evidence | No MFA means remote maintenance sessions (RDP, remote admin tools) are not MFA-protected. | **-1 pt** |

---

### AU — Audit and Accountability (No SIEM)

| Practice ID | Domain | Practice Statement | Status | Evidence Needed | Gap Notes | SPRS Score Impact |
|-------------|--------|--------------------|--------|-----------------|-----------|------------------|
| AU.L2-3.3.1 | AU | Create and retain system audit logs and records to enable monitoring, analysis, investigation, and reporting. | ❌ NOT MET | Log retention policy; SIEM or centralized log repository configuration; evidence logs from all CUI-touching systems (AD, SharePoint, endpoints, firewall) are captured and retained for 3 years | **Critical practice** — no SIEM means logs are likely siloed on individual systems with no centralized collection, correlation, or assured retention. Windows Event logs on endpoints have limited local retention. | **-3 pts** |
| AU.L2-3.3.2 | AU | Ensure actions of individual users can be uniquely traced. | 🟡 PARTIAL | Audit log samples showing user-level attribution from AD, M365 Audit Log, Palo Alto traffic logs | Without a SIEM, correlating user actions across systems is manual and unreliable. M365 Unified Audit Log provides some coverage but must be explicitly enabled and retained. | **-2 pts** |
| AU.L2-3.3.3 | AU | Review and update logged events. | ❌ NOT MET | Documented logged event list; records of periodic review meetings; evidence of updates to log configuration | No formal process likely exists without a SIEM. Requires documented policy + evidence of regular review. | **-1 pt** |
| AU.L2-3.3.4 | AU | Alert in the event of an audit logging process failure. | ❌ NOT MET | SIEM alerting rules or equivalent monitoring configuration; evidence of test alerts | Requires automated alerting capability. No SIEM means this is not met. | **-1 pt** |
| AU.L2-3.3.5 | AU | Correlate audit record review, analysis, and reporting processes. | ❌ NOT MET | SIEM correlation rules; documented analysis procedures | Cannot be met without log aggregation/SIEM capability. | **-1 pt** |
| AU.L2-3.3.6 | AU | Provide audit record reduction and report generation. | ❌ NOT MET | Log management tool configuration; sample reports generated | No SIEM = no log reduction/reporting capability. | **-1 pt** |
| AU.L2-3.3.7 | AU | Provide system capability to compare and synchronize internal clocks with authoritative sources. | 🟡 PARTIAL | NTP configuration on all in-scope systems; AD time sync settings | AD provides NTP via domain hierarchy; must verify all systems (including non-domain joined or BYOD) sync to an authoritative source. Document the configuration. | **-1 pt** |
| AU.L2-3.3.8 | AU | Protect audit information and tools from unauthorized access, modification, and deletion. | ❌ NOT MET | Evidence of log protection (SIEM access controls, immutable storage, write-once log settings) | Without a SIEM with access controls, local logs on endpoints/servers are modifiable by admins — not adequately protected. | **-1 pt** |
| AU.L2-3.3.9 | AU | Limit management of audit logging to a subset of privileged users. | ❌ NOT MET | RBAC configuration for log management; AD/SIEM role assignments | Cannot be effectively enforced or demonstrated without centralized logging with role-based access. | **-1 pt** |

---

### RA — Risk Assessment (Unauthenticated Scanning)

| Practice ID | Domain | Practice Statement | Status | Evidence Needed | Gap Notes | SPRS Score Impact |
|-------------|--------|--------------------|--------|-----------------|-----------|------------------|
| RA.L2-3.11.1 | RA | Periodically assess the risk to organizational operations, assets, and individuals. | 🟡 PARTIAL | Formal risk assessment report with date; risk register; evidence of periodic reassessment | Quarterly Nessus scans address vulnerability risk but do not constitute a full organizational risk assessment. A formal risk assessment against all 110 CMMC practices is needed. | **-3 pts** |
| RA.L2-3.11.2 | RA | Scan for vulnerabilities in organizational systems periodically and when new vulnerabilities are identified. | 🟡 PARTIAL | Nessus scan reports (dated); scan scope documentation showing all in-scope assets covered; **credentialed scan configuration** | Quarterly scans exist — that is positive. However, **unauthenticated scans are a common assessment finding** (per DoD Assessment Methodology). Unauthenticated scans miss locally-installed software vulnerabilities, misconfigurations, and patch gaps that only credentialed scans detect. Assessors will flag this. Monthly credentialed scans are expected. | **-3 pts** |
| RA.L2-3.11.3 | RA | Remediate vulnerabilities in accordance with risk assessments. | 🟡 PARTIAL | Vulnerability remediation tracking records; evidence of timely patching tied to risk ratings; Intune patch compliance reports | No evidence provided of a formal remediation process. Intune can enforce patching but a documented vulnerability management program with SLAs (e.g., Critical: 15 days, High: 30 days) is required. | **-3 pts** |

---

### SI — System and Information Integrity (No SIEM, Monitoring Gaps)

| Practice ID | Domain | Practice Statement | Status | Evidence Needed | Gap Notes | SPRS Score Impact |
|-------------|--------|--------------------|--------|-----------------|-----------|------------------|
| SI.L2-3.14.3 | SI | Monitor system security alerts and advisories and take action in response. | 🟡 PARTIAL | Subscription to US-CERT/CISA advisories; documented process for evaluating and acting on alerts; Intune/Defender alert response records | Microsoft Defender (available via M365 E3 + Intune) provides some advisory monitoring. Must document the process and show evidence of advisory reviews and responses. | **-2 pts** |
| SI.L2-3.14.6 | SI | Monitor systems to detect attacks and indicators of potential attacks. | ❌ NOT MET | SIEM with IDS/IPS rules; EDR/XDR alert configuration and evidence; network monitoring logs from Palo Alto showing active threat detection | **Critical practice** — no SIEM means no centralized attack detection. Palo Alto firewall provides perimeter monitoring, and Microsoft Defender for Endpoint (available but unclear if deployed) can provide EDR. Without a SIEM or documented detection capability covering all CUI-touching systems, this practice is NOT MET. | **-5 pts** |
| SI.L2-3.14.7 | SI | Identify unauthorized use of systems. | ❌ NOT MET | User behavior analytics (UBA) or equivalent; DLP alerts for SharePoint; anomalous access reports from M365 Audit Log | No evidence of capability to detect unauthorized system use. M365 E3 has some Defender for Cloud Apps functionality but comprehensive coverage is unlikely without additional tooling. | **-3 pts** |

---

### SC — System and Communications Protection (CUI in Commercial Cloud)

| Practice ID | Domain | Practice Statement | Status | Evidence Needed | Gap Notes | SPRS Score Impact |
|-------------|--------|--------------------|--------|-----------------|-----------|------------------|
| SC.L2-3.13.8 | SC | Implement cryptographic mechanisms to protect confidentiality of CUI during transmission (FIPS 140-2/3). | 🟡 PARTIAL | TLS configuration for all CUI transmission paths; FIPS mode configuration on Windows endpoints; M365/SharePoint transport encryption documentation | **Critical practice**. Commercial M365 E3 uses TLS 1.2+ for transport but FIPS validation status of commercial (non-GCC) SharePoint is not equivalent to GCC/GCC High. Windows endpoints must have FIPS mode enabled. Must document and verify all CUI transmission paths. | **-5 pts** |
| SC.L2-3.13.11 | SC | Employ FIPS-validated cryptography when used to protect CUI. | ❌ NOT MET | FIPS mode enabled on all Windows 10/11 endpoints (Group Policy or Intune policy export); documentation that CUI storage/transmission uses only FIPS-validated modules | **Critical practice**. No evidence of FIPS mode deployment. Commercial M365 (non-GCC) does not provide the FedRAMP Moderate authorization needed to satisfy FIPS requirements for CUI. This is a foundational gap. | **-5 pts** |
| SC.L2-3.13.16 | SC | Protect CUI at rest. | ❌ NOT MET | BitLocker encryption status reports from Intune for all endpoints; SharePoint data-at-rest encryption documentation (FedRAMP Moderate equivalent) | Intune can enforce BitLocker on endpoints — must verify deployment. Commercial SharePoint encrypts at rest but not under a FedRAMP Moderate ATO, making this non-compliant for CUI. | **-3 pts** |
| SC.L2-3.13.6 | SC | Deny network communications by default; allow by exception. | 🟡 PARTIAL | Palo Alto firewall policy export showing default-deny ruleset; documentation of exception process | Palo Alto supports this but requires verified configuration. Must export and document the deny-by-default rule set. | **-1 pt** |
| SC.L2-3.13.7 | SC | Prevent split tunneling for remote access. | 🟡 PARTIAL | VPN configuration showing split tunneling disabled; routing tables for remote users | No mention of VPN configuration. Must verify remote worker traffic routes through managed access points — split tunneling must be disabled for CUI systems. | **-1 pt** |

---

### AC — Access Control (CUI Flow, Least Privilege)

| Practice ID | Domain | Practice Statement | Status | Evidence Needed | Gap Notes | SPRS Score Impact |
|-------------|--------|--------------------|--------|-----------------|-----------|------------------|
| AC.L2-3.1.3 | AC | Control the flow of CUI in accordance with approved authorizations. | ❌ NOT MET | SharePoint permissions audit; data classification labels applied to CUI content; DLP policies configured; documented CUI access control policy | **Critical practice**. CUI stored in commercial SharePoint Online with no documented access controls or DLP policies. Must implement and document CUI flow controls, including who can access, download, share, and forward CUI content. | **-5 pts** |
| AC.L2-3.1.5 | AC | Employ least privilege, including for privileged accounts. | 🟡 PARTIAL | AD privileged account inventory; role-based access control documentation; evidence of quarterly access reviews; Intune admin role assignments | With 80 users in AD and no described PAM/privilege management, this is likely partially implemented at best. Must document role assignments and show regular access reviews. | **-3 pts** |
| AC.L2-3.1.12 | AC | Monitor and control remote access sessions. | 🟡 PARTIAL | VPN/remote access logs; Conditional Access policy exports; evidence of session monitoring | No mention of VPN or remote access solution beyond M365. Must document how remote access to CUI systems is monitored and controlled. | **-1 pt** |
| AC.L2-3.1.13 | AC | Employ cryptographic mechanisms to protect confidentiality of remote access sessions. | 🟡 PARTIAL | VPN configuration showing encryption standards; TLS configuration for remote access portals | Tied to SC.L2-3.13.8 and FIPS concerns. Must verify remote access uses FIPS-validated encryption. | **-1 pt** |

---

### CA — Security Assessment

| Practice ID | Domain | Practice Statement | Status | Evidence Needed | Gap Notes | SPRS Score Impact |
|-------------|--------|--------------------|--------|-----------------|-----------|------------------|
| CA.L2-3.12.1 | CA | Periodically assess the security controls in organizational systems. | ❌ NOT MET | Annual security control assessment report; evidence of assessor qualifications; schedule for ongoing assessments | This gap assessment likely represents the first formal assessment. Must institute an annual security assessment process. | **-3 pts** |
| CA.L2-3.12.2 | CA | Develop and implement plans of action to correct deficiencies. | ❌ NOT MET | Formal POA&M document covering all gaps identified; milestone dates; responsible owners; remediation budgets | No POA&M exists based on the environment description. Must be created immediately following this gap assessment. | **-3 pts** |
| CA.L2-3.12.3 | CA | Monitor security controls on an ongoing basis. | ❌ NOT MET | Continuous monitoring program documentation; automated compliance dashboards (Intune compliance, Defender for Endpoint, M365 Secure Score evidence) | Without a SIEM and with limited visibility, ongoing monitoring is not in place. Intune and M365 Secure Score can partially address this with proper configuration. | **-3 pts** |
| CA.L2-3.12.4 | CA | Develop, document, and periodically update system security plans. | ❌ NOT MET | Complete SSP covering all 110 practices; signed by senior official; dated within last 12 months | No SSP mentioned. This is a foundational requirement for any CMMC assessment. | **-3 pts** |

---

### IR — Incident Response

| Practice ID | Domain | Practice Statement | Status | Evidence Needed | Gap Notes | SPRS Score Impact |
|-------------|--------|--------------------|--------|-----------------|-----------|------------------|
| IR.L2-3.6.1 | IR | Establish an operational incident-handling capability. | ❌ NOT MET | Written Incident Response Plan (IRP); evidence of DIBNET portal account registration; documented 72-hour reporting procedure for DFARS 7012 | **Critical practice**. No incident response capability described. Must include DIBNET reporting procedures per DFARS 252.204-7012. | **-1 pt** |
| IR.L2-3.6.2 | IR | Track, document, and report incidents to designated officials and authorities. | ❌ NOT MET | Incident tracking system/ticketing records; DIBNET reporting templates; evidence of test reporting | No incident tracking capability in place. | **-1 pt** |
| IR.L2-3.6.3 | IR | Test the organizational incident response capability. | ❌ NOT MET | Tabletop exercise records with dates; lessons-learned documentation; after-action reports | No evidence of IR testing. Must conduct and document at least annual tabletop exercises. | **-1 pt** |

---

### AT — Awareness and Training

| Practice ID | Domain | Practice Statement | Status | Evidence Needed | Gap Notes | SPRS Score Impact |
|-------------|--------|--------------------|--------|-----------------|-----------|------------------|
| AT.L2-3.2.1 | AT | Ensure personnel are aware of CUI security risks and related policies. | ❌ NOT MET | Training completion records for all 80 users; training content covering CUI handling, CMMC requirements; dates within last 12 months | No training program described. With CUI in SharePoint and 80 users, all must complete documented security awareness training. | **-1 pt** |
| AT.L2-3.2.2 | AT | Ensure personnel are trained to carry out assigned security responsibilities. | ❌ NOT MET | Role-based training records for system admins, IT staff, and security personnel; training content covering their specific responsibilities | IT administrators managing CUI systems must receive role-specific training. | **-1 pt** |
| AT.L2-3.2.3 | AT | Provide security awareness training on recognizing and reporting insider threats. | ❌ NOT MET | Insider threat training content; completion records for all personnel with CUI access | Must include insider threat indicators in training curriculum. | **-1 pt** |

---

### CM — Configuration Management

| Practice ID | Domain | Practice Statement | Status | Evidence Needed | Gap Notes | SPRS Score Impact |
|-------------|--------|--------------------|--------|-----------------|-----------|------------------|
| CM.L2-3.4.1 | CM | Establish and maintain baseline configurations and inventories. | 🟡 PARTIAL | Intune device inventory export; documented baseline configuration for Windows 10/11; hardware/software inventory | Intune manages endpoints and can produce inventory. Requires documented baseline configurations per OS version and documented approval. | **-3 pts** |
| CM.L2-3.4.2 | CM | Establish and enforce security configuration settings. | 🟡 PARTIAL | Intune configuration profile exports; CIS Benchmark or DISA STIG application evidence; Compliance Policy reports | Intune supports enforcing security baselines (Microsoft Security Baseline or CIS). Must verify deployment and document the baseline standard used. | **-1 pt** |
| CM.L2-3.4.6 | CM | Employ principle of least functionality. | 🟡 PARTIAL | Intune application inventory; evidence of disabling unnecessary features/services; Windows feature audit | Intune can restrict applications but must document and enforce least functionality policy. | **-1 pt** |
| CM.L2-3.4.7 | CM | Restrict/disable nonessential programs, functions, ports, protocols, and services. | 🟡 PARTIAL | Palo Alto firewall policy showing blocked ports/protocols; Windows Firewall/Intune policies restricting services; AppLocker or Intune app control policy | Palo Alto provides network-level restriction but endpoint-level service/port restriction must also be documented. | **-1 pt** |
| CM.L2-3.4.8 | CM | Apply deny-by-exception or allow-by-exception software policy. | ❌ NOT MET | AppLocker policy exports; Intune application control policy; Windows Defender Application Control (WDAC) configuration | No application whitelisting/blacklisting mentioned. This is a significant gap for CUI-handling systems. | **-1 pt** |

---

## SPRS Score Impact Summary

| Category | NOT MET Practices (Count) | Estimated Point Deduction |
|----------|--------------------------|--------------------------|
| Identification & Authentication (IA) | 2 fully NOT MET + 4 partial | -14 pts |
| Audit & Accountability (AU) | 6 NOT MET + 2 partial | -11 pts |
| Risk Assessment (RA) | 0 NOT MET + 3 partial | -9 pts |
| System & Information Integrity (SI) | 3 NOT MET + 1 partial | -10 pts |
| System & Comms Protection (SC) | 3 NOT MET + 3 partial | -15 pts |
| Access Control (AC) | 1 NOT MET + 3 partial | -10 pts |
| Security Assessment (CA) | 4 NOT MET | -12 pts |
| Incident Response (IR) | 3 NOT MET | -3 pts |
| Awareness & Training (AT) | 3 NOT MET | -3 pts |
| Configuration Management (CM) | 1 NOT MET + 4 partial | -8 pts |

> **Note**: Partial implementations receive FULL deductions per SPRS methodology (no partial credit). Estimated deductions above reflect a conservative calculation based on the DoD Assessment Methodology point weights. The exact score depends on assessor findings for all 110 practices.

### Estimated SPRS Score Range

| Scenario | Estimated Score |
|----------|----------------|
| Starting score (all 110 MET) | **+110** |
| Conservative estimate (partial = NOT MET) | **~+15 to +30** |
| Realistic assessment estimate given described gaps | **~-20 to +10** |

The estimated score is well below what contracting officers expect to see. Any score below **+70** is likely to trigger scrutiny from the DoD Contracting Officer (CO).

---

## Critical Gaps Requiring Immediate Remediation (Cannot POA&M)

The following practices are **critical** and must be fully MET before any C3PAO certification can be issued. These cannot appear in a POA&M at the time of certification:

| # | Practice ID | Gap | Priority Action |
|---|-------------|-----|----------------|
| 1 | **IA.L2-3.5.3** | No MFA deployed for any users | Deploy Microsoft Entra MFA + Conditional Access policies; enforce for all 80 users and privileged accounts immediately |
| 2 | **SC.L2-3.13.8** | CUI transmission encryption unverified; commercial M365 not FedRAMP Moderate | Migrate CUI from commercial SharePoint to M365 GCC/GCC High; enforce TLS 1.2+ with FIPS cipher suites |
| 3 | **SC.L2-3.13.11** | FIPS-validated cryptography not deployed | Enable FIPS mode on all Windows 10/11 endpoints via Intune/Group Policy; migrate to GCC environment |
| 4 | **AC.L2-3.1.3** | CUI flow controls not implemented in SharePoint | Implement Microsoft Purview sensitivity labels + DLP policies; audit and restrict SharePoint permissions |
| 5 | **SI.L2-3.14.6** | No SIEM or centralized attack detection | Deploy Microsoft Sentinel (integrates with M365 GCC) or equivalent; configure Defender for Endpoint with centralized alerting |
| 6 | **AU.L2-3.3.1** | No centralized audit logging; no SIEM | Deploy SIEM with 3-year log retention; enable M365 Unified Audit Log; collect Palo Alto and AD logs |
| 7 | **IR.L2-3.6.1** | No incident response plan or capability | Draft and approve an IRP; register with DIBNET; document 72-hour reporting procedure per DFARS 7012 |

---

## Top Remediation Priorities (Ranked by SPRS Impact + Risk)

| Priority | Action Item | Practices Addressed | Estimated Effort |
|----------|-------------|---------------------|-----------------|
| 1 | **Migrate CUI from commercial M365 to M365 GCC/GCC High** | SC.L2-3.13.8, SC.L2-3.13.11, SC.L2-3.13.16, AC.L2-3.1.3 | 4–8 weeks; licensing change + migration |
| 2 | **Deploy MFA via Microsoft Entra + Conditional Access** | IA.L2-3.5.3, MA.L2-3.7.5 | 1–2 weeks (M365 E3 includes Entra ID P1 with add-on) |
| 3 | **Deploy Microsoft Sentinel or equivalent SIEM** | AU.L2-3.3.1–3.3.9, SI.L2-3.14.6, SI.L2-3.14.7 | 4–8 weeks; requires GCC migration first |
| 4 | **Enable FIPS mode on all Windows endpoints via Intune** | SC.L2-3.13.11, IA.L2-3.5.10 | 1–2 weeks |
| 5 | **Switch to credentialed, monthly Nessus scans** | RA.L2-3.11.2 | 1 week configuration change |
| 6 | **Draft SSP and POA&M** | CA.L2-3.12.4, CA.L2-3.12.2 | 4–6 weeks |
| 7 | **Develop and deliver security awareness training** | AT.L2-3.2.1, AT.L2-3.2.2, AT.L2-3.2.3 | 2–4 weeks |
| 8 | **Draft and test Incident Response Plan** | IR.L2-3.6.1, IR.L2-3.6.2, IR.L2-3.6.3 | 2–4 weeks |
| 9 | **Implement application control (WDAC/AppLocker)** | CM.L2-3.4.8 | 4–8 weeks |
| 10 | **Document baseline configurations and enforce via Intune** | CM.L2-3.4.1, CM.L2-3.4.2 | 2–4 weeks |

---

## Recommended Remediation Timeline

| Phase | Timeframe | Focus |
|-------|-----------|-------|
| **Phase 1 — Critical Blockers** | Months 1–2 | MFA deployment; M365 GCC migration initiation; FIPS mode; IR Plan draft |
| **Phase 2 — Foundation Building** | Months 2–4 | SIEM deployment; credentialed scanning; SSP/POA&M drafting; training |
| **Phase 3 — Documentation & Hardening** | Months 3–6 | Baseline configurations; least privilege audit; application control; access reviews |
| **Phase 4 — Assessment Readiness** | Months 5–8 | Mock assessment; POA&M remediation; C3PAO engagement |

---

## Additional Regulatory Notes

1. **DFARS 252.204-7012 — Immediate Obligation**: The 72-hour DIBNET incident reporting requirement is in effect NOW, regardless of CMMC certification status. Register at dibnet.dod.mil immediately.

2. **False Claims Act Risk**: If your organization has submitted an SPRS score to date without performing a formal assessment, ensure the score accurately reflects the current state. Inflated SPRS scores expose the organization to False Claims Act liability under 31 U.S.C. § 3729.

3. **Subcontractor Flow-Down**: If you engage subcontractors who handle your CUI, DFARS 252.204-7021(c) requires you to flow CMMC requirements to them. Audit your subcontractor base.

4. **C3PAO Timeline**: Given the scope of gaps identified, a realistic timeline to C3PAO readiness is **8–12 months** from the start of remediation, assuming adequate resources and executive commitment.

---

*Assessment based on NIST SP 800-171 Rev 2, CMMC 2.0 (32 CFR Part 170), and DoD Assessment Methodology v2.2. All 110 Level 2 practices were evaluated; this report highlights practices with identified gaps based on the environment description provided. A complete assessment requires review of all 110 practices with documentary, technical, and interview evidence.*
