# CIS Controls v8 IG2 Gap Assessment
## Manufacturing Company — 350 Employees, IT Team of 5

---

## Step 1: Implementation Group Determination

**Profile:**

| Factor | Detail |
|--------|--------|
| Size | 350 employees |
| IT Capability | 5-person dedicated IT team; responsible for managing and protecting IT infrastructure |
| Data Sensitivity | Manufacturing environment — likely contains proprietary process data, supplier/customer records, OT/IT boundary considerations |
| Risk Tolerance | Operational disruptions would impact production and revenue; some outages tolerable but sustained downtime costly |
| Regulatory Exposure | Possible CMMC/DFARS exposure if defense supplier; EHS data; financial records |

**Determination: IG2**

This organization clearly meets IG2 criteria: it has dedicated IT personnel responsible for managing and protecting IT, stores data that could affect operations if compromised, and can withstand some outage. IG3 is not required as there is no evidence of a dedicated security team (separate from IT) or significant regulatory oversight of sensitive data. All IG1 and IG2 safeguards apply.

---

## Step 2: Current State Inventory

**Known Implemented Controls:**
- Antivirus on all endpoints
- Monthly Windows patching
- Basic perimeter firewall
- Annual security awareness training

**Known Gaps:**
- No MFA on VPN
- No vulnerability scanner
- No centralized log management (only Windows Event Viewer per server)

---

## Step 3: Gap Assessment Table

The table below covers all 18 CIS Controls, assessing current state against IG2 requirements. Status uses RAG scoring: **Green** = implemented, **Amber** = partial, **Red** = not implemented.

| CIS Control | Safeguard(s) | IG Level | Current State | Gap | Priority |
|-------------|-------------|----------|---------------|-----|----------|
| **1: Inventory & Control of Enterprise Assets** | 1.1 Maintain enterprise asset inventory | IG1 | Unknown — no evidence of formal inventory | No formal hardware asset inventory confirmed | HIGH |
| | 1.2 Address unauthorized assets | IG1 | Unknown | No process documented to detect/quarantine rogue devices | HIGH |
| | 1.3 Use DHCP logging | IG1 | Unknown | Likely not formally implemented | MEDIUM |
| | 1.4 Use DHCP logging to update inventory | IG1 | Unknown | No evidence of automated inventory updates | MEDIUM |
| | 1.5 Use passive asset discovery tool | IG2 | Not implemented | No asset discovery tooling in place | HIGH |
| **2: Inventory & Control of Software Assets** | 2.1 Establish software inventory | IG1 | Unknown | No formal authorized software list confirmed | HIGH |
| | 2.2 Ensure software is currently supported | IG1 | Partial — Windows patched monthly | Non-Windows and third-party apps not addressed | MEDIUM |
| | 2.3 Address unauthorized software | IG1 | Unknown | No process for detecting/blocking unauthorized software | HIGH |
| | 2.5 Allowlist authorized software | IG2 | Not implemented | No application allowlisting in place | HIGH |
| | 2.6 Allowlist authorized libraries | IG2 | Not implemented | No library allowlisting | MEDIUM |
| | 2.7 Allowlist authorized scripts | IG2 | Not implemented | No script allowlisting | MEDIUM |
| **3: Data Protection** | 3.1 Establish data management process | IG1 | Unknown | No evidence of formal data classification or management policy | HIGH |
| | 3.2 Establish data inventory | IG1 | Unknown | No data inventory confirmed | HIGH |
| | 3.3 Configure data access control lists | IG1 | Unknown | No evidence of formal ACL governance | MEDIUM |
| | 3.6 Encrypt data on end-user devices | IG2 | Unknown | Full-disk encryption status on endpoints unknown | HIGH |
| | 3.11 Encrypt sensitive data at rest | IG2 | Unknown | Encryption at rest for servers/databases not confirmed | HIGH |
| **4: Secure Configuration** | 4.1 Secure configuration process for assets | IG1 | Unknown | No evidence of configuration baselines (e.g., CIS Benchmarks) | HIGH |
| | 4.2 Secure configuration for network infra | IG1 | Partial — basic firewall exists | Firewall rules not confirmed as hardened; no configuration standard | MEDIUM |
| | 4.3 Configure automatic session locking | IG1 | Unknown | Session lock policy not confirmed | LOW |
| | 4.4 Implement firewall on servers | IG1 | Unknown | Host-based firewall status on servers not confirmed | MEDIUM |
| | 4.5 Implement firewall on end-user devices | IG1 | Unknown | Windows Firewall status not confirmed as enforced | MEDIUM |
| | 4.8 Disable unnecessary services | IG2 | Unknown | No service hardening process in place | MEDIUM |
| **5: Account Management** | 5.1 Inventory of accounts | IG1 | Unknown | No formal account inventory confirmed | MEDIUM |
| | 5.2 Use unique passwords | IG1 | Unknown | Password policy not confirmed | MEDIUM |
| | 5.3 Disable dormant accounts | IG1 | Unknown | No dormant account review process confirmed | MEDIUM |
| | 5.4 Restrict admin privileges | IG1 | Unknown | Least-privilege enforcement not confirmed | HIGH |
| | 5.5 Inventory of service accounts | IG2 | Unknown | No service account inventory confirmed | MEDIUM |
| | 5.6 Centralize account management | IG2 | Unknown | Central directory (Active Directory) assumed but not confirmed | MEDIUM |
| **6: Access Control Management** | 6.1 Access granting process | IG1 | Unknown | Formal access provisioning process not documented | MEDIUM |
| | 6.2 Access revoking process | IG1 | Unknown | Offboarding access revocation process not confirmed | MEDIUM |
| | **6.3 Require MFA for external applications** | **IG2** | **Not implemented** | **No MFA on externally exposed services** | **CRITICAL** |
| | **6.4 Require MFA for remote network access (VPN)** | **IG2** | **Not implemented** | **VPN has no MFA — confirmed gap** | **CRITICAL** |
| | 6.5 Require MFA for admin access | IG2 | Unknown | MFA for administrative accounts not confirmed | CRITICAL |
| | 6.8 Define role-based access control | IG2 | Unknown | No formal RBAC model documented | HIGH |
| **7: Continuous Vulnerability Management** | 7.1 Establish vulnerability management process | IG1 | Partial — monthly Windows patching only | No formal vulnerability management policy or process | HIGH |
| | 7.2 Establish remediation process | IG1 | Partial — patches applied monthly | No risk-based remediation SLAs defined | HIGH |
| | 7.3 Automated OS patch management | IG1 | Partial — Windows patched; manual process assumed | Non-Windows OS and third-party app patching not confirmed as automated | MEDIUM |
| | 7.4 Automated application patch management | IG1 | Not implemented | Application-level patching (third-party software) not addressed | HIGH |
| | **7.6 Automated vulnerability scans — internal assets** | **IG2** | **Not implemented** | **No vulnerability scanner — confirmed gap** | **CRITICAL** |
| | **7.7 Remediate detected vulnerabilities** | **IG2** | **Not implemented** | **Cannot remediate what is not scanned** | **CRITICAL** |
| **8: Audit Log Management** | 8.1 Establish audit log management process | IG1 | Not implemented | No formal log management policy | HIGH |
| | 8.2 Collect audit logs | IG1 | Partial — Windows Event Viewer on individual servers | Logs exist but not aggregated, no retention, no alerting | HIGH |
| | **8.3 Ensure adequate audit log storage** | **IG2** | **Not implemented** | **No centralized storage; logs at risk of overwrite** | **HIGH** |
| | **8.5 Collect detailed audit logs** | **IG2** | **Not implemented** | **Event Viewer alone does not meet IG2 detail requirements** | **HIGH** |
| | **8.11 Conduct audit log reviews** | **IG2** | **Not implemented** | **No regular log review process** | **HIGH** |
| | **8.12 Collect service provider logs** | **IG2** | **Not implemented** | **Cloud/SaaS service logs not collected** | **MEDIUM** |
| **9: Email and Web Browser Protections** | 9.1 Use fully supported browsers/email clients | IG1 | Unknown | Browser/email client version management not confirmed | LOW |
| | 9.2 Use DNS filtering | IG1 | Not confirmed | No DNS filtering service confirmed | MEDIUM |
| | 9.3 Maintain anti-spoofing (SPF/DKIM) | IG1 | Unknown | Email authentication records not confirmed | MEDIUM |
| | 9.5 Implement DMARC | IG2 | Unknown | DMARC policy not confirmed | MEDIUM |
| | 9.6 Block unnecessary file types | IG2 | Unknown | Email file type filtering not confirmed | MEDIUM |
| | 9.7 Deploy email server anti-malware | IG2 | Unknown | Centralized email anti-malware not confirmed | MEDIUM |
| **10: Malware Defenses** | 10.1 Deploy anti-malware software | IG1 | Implemented — antivirus on all endpoints | Met | LOW |
| | 10.2 Automatic signature updates | IG1 | Assumed — standard AV behavior | Confirm auto-update is enforced via policy | LOW |
| | 10.3 Disable autorun/autoplay | IG1 | Unknown | Autorun policy not confirmed | LOW |
| | 10.5 Enable anti-exploitation features | IG2 | Unknown | Windows Exploit Guard / DEP / ASLR enforcement not confirmed | MEDIUM |
| | **10.6 Centrally manage anti-malware** | **IG2** | **Unknown** | **Central AV management console/policy enforcement not confirmed** | **HIGH** |
| | 10.7 Use behavior-based anti-malware | IG2 | Unknown | Whether current AV includes EDR/behavioral detection not confirmed | MEDIUM |
| **11: Data Recovery** | 11.1 Establish data recovery process | IG1 | Unknown | No backup/recovery policy confirmed | HIGH |
| | 11.2 Perform automated backups | IG1 | Unknown | Automated backup status not confirmed | HIGH |
| | 11.3 Protect recovery data | IG2 | Unknown | Backup encryption and offsite storage not confirmed | HIGH |
| | 11.4 Establish isolated recovery data | IG1 | Unknown | Immutable/isolated backups not confirmed | HIGH |
| | 11.5 Test data recovery | IG2 | Unknown | Recovery testing not confirmed | HIGH |
| **12: Network Infrastructure Management** | 12.1 Keep network infrastructure up-to-date | IG1 | Unknown | Firewall/switch firmware patching not confirmed | MEDIUM |
| | 12.2 Establish secure network architecture | IG2 | Unknown | Network segmentation (IT/OT, guest, production) not confirmed | HIGH |
| | 12.3 Securely manage network infrastructure | IG2 | Unknown | Out-of-band management and access controls for network devices not confirmed | MEDIUM |
| | 12.4 Maintain architecture diagram | IG2 | Unknown | Formal network diagram not confirmed | MEDIUM |
| | 12.6 Use secure management protocols | IG2 | Unknown | Use of SSH/HTTPS vs Telnet/HTTP for device management not confirmed | MEDIUM |
| | **12.7 Ensure remote devices use VPN** | **IG2** | **Partial — VPN exists but no MFA** | **VPN without MFA is a critical control failure** | **CRITICAL** |
| **13: Network Monitoring and Defense** | **13.1 Centralize security event alerting** | **IG2** | **Not implemented** | **No SIEM or centralized alerting** | **HIGH** |
| | 13.2 Deploy host-based intrusion detection | IG2 | Unknown | HIDS/EDR not confirmed beyond basic AV | HIGH |
| | **13.3 Deploy network intrusion detection (NIDS)** | **IG2** | **Not implemented** | **No network-level detection** | **HIGH** |
| | 13.6 Collect network traffic flow logs | IG2 | Not implemented | No NetFlow or traffic logging | HIGH |
| **14: Security Awareness Training** | 14.1 Establish security awareness program | IG1 | Partial — annual training only | Annual cadence insufficient; no metrics or role-based training | MEDIUM |
| | 14.2 Train on social engineering | IG1 | Partial — included in annual training | Phishing simulation not confirmed | MEDIUM |
| | 14.3 Train on authentication best practices | IG1 | Partial | MFA and password hygiene likely not reinforced given no MFA deployment | MEDIUM |
| | 14.7 Train on insider threats | IG2 | Unknown | Insider threat awareness not confirmed as a training topic | MEDIUM |
| **15: Service Provider Management** | 15.1 Inventory of service providers | IG1 | Unknown | No vendor/service provider inventory confirmed | MEDIUM |
| | 15.2 Service provider management policy | IG1 | Unknown | No vendor management policy confirmed | MEDIUM |
| | 15.5 Assess service providers | IG2 | Not implemented | No third-party security assessments | HIGH |
| | 15.6 Monitor service providers | IG2 | Not implemented | No ongoing vendor monitoring | HIGH |
| **16: Application Software Security** | 16.1 Secure development process | IG2 | Unknown — manufacturing context | Likely not applicable if no in-house dev; confirm and document | LOW |
| | 16.2 Process for software vulnerability reports | IG2 | Unknown | No vulnerability disclosure intake process | MEDIUM |
| | 16.7 Use hardening configuration templates | IG2 | Unknown | CIS Benchmark hardening for deployed applications not confirmed | MEDIUM |
| **17: Incident Response Management** | 17.1 Designate incident response personnel | IG1 | Unknown | No formal IR team or designated roles confirmed | HIGH |
| | 17.3 Enterprise incident reporting process | IG1 | Unknown | No documented incident reporting process | HIGH |
| | 17.4 Establish incident response process | IG1 | Unknown | No IR plan or playbooks confirmed | HIGH |
| | 17.7 Conduct IR exercises | IG2 | Not implemented | No tabletop or IR drills | HIGH |
| | 17.9 Establish incident thresholds | IG2 | Not implemented | No defined thresholds for what constitutes a security incident | MEDIUM |
| **18: Penetration Testing** | 18.1 Establish penetration testing program | IG2 | Not implemented | No pen testing program | MEDIUM |
| | 18.2 Perform periodic external pen tests | IG2 | Not implemented | No external pen tests conducted | HIGH |
| | 18.3 Remediate pen test findings | IG2 | N/A — no tests conducted | Dependent on 18.2 | MEDIUM |

---

## Step 4: Priority Actions — Remediation Roadmap

### CRITICAL (Address Within 30 Days)

These gaps represent the highest probability of a successful breach and should be treated as immediate risks.

---

**Priority 1: Deploy MFA on VPN and All Remote Access**
- **Safeguards:** CIS Control 6, Safeguards 6.3, 6.4, 6.5
- **Why:** VPN without MFA is the single most exploited attack vector for initial access in manufacturing ransomware incidents. Credential stuffing and phishing-obtained passwords provide direct network entry.
- **Action:** Enable MFA on the VPN using an authenticator app (Microsoft Authenticator, Duo, Google Authenticator). Extend MFA to RDP, Microsoft 365/cloud portals, and local admin accounts.
- **Tools:** Duo Security, Microsoft Entra ID MFA, Cisco Duo for network devices.
- **Effort:** Low-Medium (1–3 weeks with existing infrastructure).
- **MITRE ATT&CK Addressed:** T1078 (Valid Accounts), T1110 (Brute Force), T1133 (External Remote Services).

---

**Priority 2: Deploy a Vulnerability Scanner**
- **Safeguards:** CIS Control 7, Safeguards 7.6, 7.7
- **Why:** Monthly Windows patching alone does not identify unpatched third-party software, misconfigurations, or newly discovered CVEs between patch cycles. You cannot remediate what you cannot see.
- **Action:** Deploy an authenticated vulnerability scanner. Scan all internal assets weekly. Establish remediation SLAs: Critical CVEs within 15 days, High within 30 days.
- **Tools:** Tenable Nessus Essentials (free for 16 IPs, affordable for full network), Qualys VMDR, Rapid7 InsightVM.
- **Effort:** Low (days to deploy; ongoing process to build).
- **MITRE ATT&CK Addressed:** T1190 (Exploit Public-Facing Application), T1203 (Exploitation for Client Execution).

---

**Priority 3: Implement Centralized Log Management (SIEM)**
- **Safeguards:** CIS Control 8, Safeguards 8.2, 8.3, 8.5, 8.11; CIS Control 13, Safeguard 13.1
- **Why:** Windows Event Viewer on individual servers provides no ability to detect lateral movement, correlate events across systems, or meet any retention requirements. This is a detection blind spot.
- **Action:** Deploy a SIEM or centralized log aggregation solution. Collect Windows Security Event Logs, firewall logs, VPN logs, and authentication logs at minimum. Set up alerting for failed logins, privilege escalation, and account lockouts.
- **Tools:** Microsoft Sentinel (cloud-native, low-cost for SMB), Wazuh (open-source SIEM + HIDS), Elastic Security (SIEM), Graylog.
- **Effort:** Medium (2–6 weeks for initial deployment; ongoing tuning).
- **MITRE ATT&CK Addressed:** T1078, T1021 (Remote Services), T1059 (Command and Scripting Interpreter).

---

### HIGH (Address Within 60–90 Days)

**Priority 4: Establish Hardware and Software Asset Inventories**
- **Safeguards:** CIS Control 1, Safeguards 1.1, 1.2, 1.5; CIS Control 2, Safeguards 2.1, 2.3, 2.5
- **Why:** You cannot protect assets you don't know you have. Unauthorized devices and software are entry points for attackers.
- **Action:** Deploy a network asset discovery tool (active and passive). Build and maintain an authorized hardware inventory. Create an authorized software list and begin enforcing application allowlisting on critical servers.
- **Tools:** Lansweeper (free tier available), Spiceworks Network Scanner, Microsoft Intune (if using M365), Cisco DNA Center.
- **Effort:** Medium (4–8 weeks to establish baseline inventory).

---

**Priority 5: Formalize Data Protection — Encryption at Rest and on Endpoints**
- **Safeguards:** CIS Control 3, Safeguards 3.6, 3.11
- **Why:** Manufacturing environments store valuable IP. Laptop loss or a ransomware event without encryption means data exposure.
- **Action:** Enable BitLocker (Windows) or FileVault (Mac) on all end-user devices via Group Policy. Enable encryption on file servers and databases storing sensitive data.
- **Tools:** Microsoft BitLocker (built into Windows Pro/Enterprise), Group Policy for enforcement, Intune for mobile devices.
- **Effort:** Low-Medium (can be deployed via GPO in 1–2 weeks).

---

**Priority 6: Establish and Test a Data Recovery and Backup Program**
- **Safeguards:** CIS Control 11, Safeguards 11.1, 11.2, 11.3, 11.4, 11.5
- **Why:** Ransomware recovery without clean, tested backups is catastrophic for manufacturing operations. Backup existence is not enough — recoverability must be tested.
- **Action:** Implement automated daily backups for all critical systems. Store at least one copy offsite or in immutable cloud storage (following 3-2-1 rule). Test recovery quarterly.
- **Tools:** Veeam Backup (SMB-friendly), Azure Backup, AWS Backup, Acronis.
- **Effort:** Medium (4–6 weeks to implement; quarterly for testing).

---

**Priority 7: Deploy Centralized Antivirus Management and Upgrade to EDR**
- **Safeguards:** CIS Control 10, Safeguards 10.6, 10.7
- **Why:** Antivirus on endpoints is a start, but without central management you cannot confirm coverage, push updates, or detect behavioral threats (e.g., fileless malware, ransomware precursors).
- **Action:** Centralize AV management via a management console. Evaluate upgrading to an EDR solution that provides behavioral detection and response capabilities.
- **Tools:** Microsoft Defender for Endpoint (integrates with Intune/M365), CrowdStrike Falcon Go, SentinelOne.
- **Effort:** Low-Medium (1–3 weeks if using existing Microsoft tooling).

---

**Priority 8: Establish an Incident Response Plan**
- **Safeguards:** CIS Control 17, Safeguards 17.1, 17.3, 17.4, 17.7
- **Why:** Without a documented IR plan, a ransomware or breach incident will cause maximum operational disruption and extend recovery time.
- **Action:** Document an Incident Response Plan covering: roles and responsibilities, incident classification criteria, communication chains (internal and external — CISA, FBI, legal counsel, cyber insurer), containment and eradication steps, and recovery procedures. Conduct a tabletop exercise annually.
- **Tools:** CISA's free IR planning resources; NIST SP 800-61r2 as a template.
- **Effort:** Medium (3–4 weeks to draft; tabletop exercise within 90 days).

---

**Priority 9: Implement Network Segmentation and Architecture Documentation**
- **Safeguards:** CIS Control 12, Safeguards 12.2, 12.4; CIS Control 13, Safeguard 13.3
- **Why:** Flat networks allow ransomware and attackers to move laterally without constraint. In manufacturing, OT/IT convergence creates significant risk if not segmented.
- **Action:** Document current network architecture. Implement VLANs to separate: corporate users, servers, OT/manufacturing systems, guest Wi-Fi, and IoT devices. Deploy a network intrusion detection system (NIDS) on the perimeter and key internal segments.
- **Tools:** Managed switches with VLAN support (Cisco, Ubiquiti), pfSense/Fortinet for NIDS, Zeek/Suricata for open-source NIDS.
- **Effort:** Medium-High (6–12 weeks; phased by segment).

---

### MEDIUM (Address Within 6 Months)

**Priority 10: Enhance Patching to Cover Third-Party Applications**
- **Safeguards:** CIS Control 7, Safeguards 7.3, 7.4
- **Action:** Extend patch management to third-party software (Adobe, Java, browsers, Office). Use a patch management tool that covers non-Microsoft software.
- **Tools:** PDQ Deploy + Inventory (SMB-friendly), ManageEngine Patch Manager Plus, Chocolatey (open-source).

**Priority 11: Implement Email Security (SPF, DKIM, DMARC, Anti-Malware)**
- **Safeguards:** CIS Control 9, Safeguards 9.3, 9.5, 9.6, 9.7
- **Action:** Publish and enforce SPF and DKIM records. Implement a DMARC policy (start with p=none for monitoring, progress to p=quarantine/reject). Enable email anti-malware filtering.
- **Tools:** Proofpoint Essentials, Microsoft Defender for Office 365, MXToolbox for record verification.

**Priority 12: Increase Security Training Frequency and Add Phishing Simulations**
- **Safeguards:** CIS Control 14, Safeguards 14.1, 14.2, 14.7
- **Action:** Move from annual to quarterly security awareness training. Add monthly phishing simulation campaigns. Include insider threat awareness and manufacturing-specific scenarios (e.g., USB drops, vendor phishing).
- **Tools:** KnowBe4, Proofpoint Security Awareness Training, Cofense (free tiers available).

**Priority 13: Establish Service Provider (Vendor) Management**
- **Safeguards:** CIS Control 15, Safeguards 15.1, 15.2, 15.5, 15.6
- **Action:** Create an inventory of all third-party service providers with access to systems or data. Establish minimum security requirements in contracts. Conduct annual security assessments of high-risk vendors.

**Priority 14: Establish Secure Configuration Baselines**
- **Safeguards:** CIS Control 4, Safeguards 4.1, 4.2, 4.8
- **Action:** Adopt CIS Benchmarks for Windows Server, Windows 10/11, and network devices. Apply hardened configurations. Disable unnecessary services on all servers. Use Group Policy to enforce configuration standards.
- **Tools:** CIS-CAT Lite (free), Microsoft Security Baseline (free GPO templates).

**Priority 15: Conduct External Penetration Test**
- **Safeguards:** CIS Control 18, Safeguards 18.1, 18.2, 18.3
- **Action:** Engage a qualified third-party penetration testing firm for an annual external pen test. After completing MFA and vulnerability management improvements (Priorities 1–2), an internal pen test will also be valuable.

---

## Step 5: Summary Scorecard

| CIS Control | IG Level Required | Current Maturity | Gap Severity |
|-------------|------------------|-----------------|--------------|
| 1: Asset Inventory | IG2 | Red — Not Implemented | HIGH |
| 2: Software Inventory | IG2 | Red — Not Implemented | HIGH |
| 3: Data Protection | IG2 | Red — Not Implemented | HIGH |
| 4: Secure Configuration | IG2 | Amber — Partial | MEDIUM |
| 5: Account Management | IG2 | Amber — Partial | MEDIUM |
| 6: Access Control / MFA | IG2 | Red — Confirmed Gap | CRITICAL |
| 7: Vulnerability Management | IG2 | Red — No Scanner | CRITICAL |
| 8: Audit Log Management | IG2 | Red — Confirmed Gap | CRITICAL |
| 9: Email/Web Protections | IG2 | Unknown/Partial | MEDIUM |
| 10: Malware Defenses | IG2 | Amber — AV exists, not centralized | MEDIUM |
| 11: Data Recovery | IG2 | Unknown | HIGH |
| 12: Network Infrastructure | IG2 | Amber — Firewall only | HIGH |
| 13: Network Monitoring | IG2 | Red — Not Implemented | HIGH |
| 14: Security Training | IG2 | Amber — Annual only | MEDIUM |
| 15: Service Provider Mgmt | IG2 | Red — Not Implemented | HIGH |
| 16: Application Security | IG2 | Unknown | LOW-MEDIUM |
| 17: Incident Response | IG2 | Red — Not Implemented | HIGH |
| 18: Penetration Testing | IG2 | Red — Not Implemented | MEDIUM |

**Overall IG2 Compliance Estimate: ~15–20% implemented**

---

## Step 6: Quick Win Summary (Ranked by Impact vs. Effort)

| Action | Effort | Impact | Timeframe |
|--------|--------|--------|-----------|
| Enable MFA on VPN and admin accounts | Low | CRITICAL | Week 1–2 |
| Deploy vulnerability scanner (Nessus Essentials) | Low | CRITICAL | Week 1–2 |
| Enable BitLocker on all endpoints via GPO | Low | HIGH | Week 2–3 |
| Deploy SIEM (Wazuh or Microsoft Sentinel) | Medium | CRITICAL | Month 1–2 |
| Establish hardware/software asset inventory | Medium | HIGH | Month 1–2 |
| Centralize AV management / upgrade to EDR | Low-Medium | HIGH | Month 1 |
| Implement and test backup/recovery | Medium | HIGH | Month 2–3 |
| Draft Incident Response Plan + tabletop | Medium | HIGH | Month 2–3 |
| Publish DMARC/SPF/DKIM records | Low | MEDIUM | Week 2–3 |
| Move to quarterly security training + phishing sim | Low | MEDIUM | Month 2 |

---

## Notes on Manufacturing-Specific Risks

1. **OT/IT Convergence:** If manufacturing equipment (PLCs, SCADA, HMI systems) is network-connected, CIS Control 12 (network segmentation) becomes even more critical. OT systems often run legacy OS versions that cannot be patched — isolation is the primary control.

2. **VPN Risk Profile:** Without MFA, the VPN is effectively a single-factor authenticated path into the entire network. Given that manufacturing IP is a high-value target for nation-state actors and ransomware groups, this is the single highest-priority remediation.

3. **Supply Chain Risk:** Manufacturing suppliers and partners with network access (CIS Control 15) are a common attack vector. Vendor access should be reviewed and MFA-enforced for all third-party remote connections.

4. **Ransomware Readiness:** The combination of no MFA, no vulnerability scanner, no centralized logging, and unconfirmed backups creates near-ideal conditions for a successful ransomware attack. Addressing Priorities 1–3 and 6 directly reduces ransomware risk.

---

*Assessment based on CIS Controls v8, published May 2021 by the Center for Internet Security. All safeguard references are to CIS Controls v8 Implementation Group assignments. Next assessment recommended after completing Priority 1–3 actions (approximately 90 days).*
