# CIS Controls v8 IG2 Gap Assessment
## Manufacturing Company (350 Employees, IT Team of 5)

**Assessment Date:** 2026-05-06
**Framework:** CIS Controls v8, Implementation Group 2 (IG2)
**Organization Profile:** Manufacturing, ~350 employees, dedicated IT team of 5

---

## Executive Summary

Your organization has established a foundational security baseline (antivirus, monthly patching, basic firewall, annual training) but has significant gaps against CIS Controls v8 IG2 requirements. IG2 is designed for organizations with IT professionals who handle sensitive data and need to defend against more sophisticated threats — a profile that fits a mid-size manufacturer well, particularly given the risk of ransomware targeting operational technology (OT) and supply chain data.

**Overall Posture:** Partial compliance with IG1 (the most basic tier); substantially non-compliant with IG2.

**Critical Risk Areas:** No MFA on VPN, no centralized log management, no vulnerability scanning, and no asset/software inventory program represent the highest-probability attack paths an adversary would exploit today.

---

## CIS Controls v8 IG2 — Control-by-Control Gap Analysis

The table below maps your current practices to each CIS Control and its IG2 safeguards, identifies gaps, and rates residual risk.

**Risk Rating Scale:** Critical | High | Medium | Low

---

### CIS Control 1 — Inventory and Control of Enterprise Assets

**IG2 Safeguards Required:** 1.1 (establish and maintain detailed asset inventory), 1.2 (address unauthorized assets), 1.3 (utilize DHCP logging), 1.4 (use dynamic host configuration), 1.5 (use passive asset discovery)

| Current State | Gap | Risk |
|---|---|---|
| No mention of formal asset inventory process | No centralized hardware asset inventory; unknown if DHCP logs are retained; no passive network discovery tool in place | High |

**Detail:** Without a complete, current inventory of hardware assets, you cannot patch, protect, or monitor what you do not know exists. Rogue or unmanaged devices are a common ransomware entry point in manufacturing environments. IG2 requires not just a list but active processes to detect unauthorized devices.

---

### CIS Control 2 — Inventory and Control of Software Assets

**IG2 Safeguards Required:** 2.1 (establish and maintain software inventory), 2.2 (ensure authorized software is currently supported), 2.3 (address unauthorized software), 2.4 (utilize automated software inventory tools), 2.5 (allowlist authorized software), 2.6 (allowlist authorized libraries), 2.7 (allowlist authorized scripts)

| Current State | Gap | Risk |
|---|---|---|
| Antivirus present (implies some endpoint visibility); no software inventory process described | No formal software inventory; no allowlisting; no process to identify unauthorized or unsupported software | High |

**Detail:** Manufacturing environments frequently accumulate legacy, unpatched, or shadow IT software. Without an inventory, unsupported software (e.g., old HMI clients, legacy ERP components) goes undetected and unpatched.

---

### CIS Control 3 — Data Protection

**IG2 Safeguards Required:** 3.1 (establish data management process), 3.2 (establish data inventory), 3.3 (configure data access control lists), 3.4 (enforce data retention), 3.5 (securely dispose of data), 3.6 (encrypt data on end-user devices), 3.7 (establish and maintain a data classification scheme), 3.8 (document data flows), 3.9 (encrypt data on removable media), 3.10 (encrypt sensitive data in transit), 3.11 (encrypt sensitive data at rest), 3.12 (segment data processing and storage), 3.13 (deploy a data loss prevention solution)

| Current State | Gap | Risk |
|---|---|---|
| No data protection controls described | No data classification, no DLP, encryption posture unknown, no documented data flows | High |

**Detail:** Manufacturing companies hold valuable IP (designs, formulas, production processes) and supply chain data. IG2 requires a structured data protection program. The absence of encryption policy and DLP leaves sensitive data exposed to both internal and external threats.

---

### CIS Control 4 — Secure Configuration of Enterprise Assets and Software

**IG2 Safeguards Required:** 4.1 (establish and maintain a secure configuration process), 4.2 (establish and maintain a secure configuration process for network infrastructure), 4.3 (configure automatic session locking), 4.4 (implement and manage a firewall on servers), 4.5 (implement and manage a firewall on end-user devices), 4.6 (securely manage enterprise assets and software), 4.7 (manage default accounts), 4.8 (uninstall or disable unnecessary services), 4.9 (configure trusted DNS servers), 4.10 (enforce automatic device lockout), 4.11 (enforce remote wipe capability), 4.12 (separate enterprise workspaces on mobile end-user devices)

| Current State | Gap | Risk |
|---|---|---|
| Basic firewall exists; no configuration hardening program described | No documented secure baselines (CIS Benchmarks); no process to manage default accounts, disable unnecessary services, or verify hardened configurations | Medium-High |

**Detail:** A "basic firewall" with no documented ruleset review process does not meet IG2. Secure configuration requires documented baselines applied consistently to all asset types, with a process to detect and remediate drift. Default credentials on networking gear and servers are a top attack vector.

---

### CIS Control 5 — Account Management

**IG2 Safeguards Required:** 5.1 (establish and maintain an inventory of accounts), 5.2 (use unique passwords), 5.3 (disable dormant accounts), 5.4 (restrict administrator privileges to dedicated administrator accounts), 5.5 (establish and maintain an inventory of service accounts), 5.6 (centralize account management)

| Current State | Gap | Risk |
|---|---|---|
| No account management controls described | No formal account inventory, no documented policy for dormant account disablement, admin privilege usage unknown | High |

**Detail:** Privileged account abuse and credential theft are primary ransomware vectors. IG2 requires a formal, maintained account inventory and strict separation of admin from standard user accounts.

---

### CIS Control 6 — Access Control Management

**IG2 Safeguards Required:** 6.1 (establish an access granting process), 6.2 (establish an access revoking process), 6.3 (require MFA for externally-exposed applications), 6.4 (require MFA for remote network access), 6.5 (require MFA for administrative access), 6.6 (establish and maintain an inventory of authentication and authorization systems), 6.7 (centralize access control), 6.8 (define and maintain role-based access control)

| Current State | Gap | Risk |
|---|---|---|
| No MFA on VPN (explicitly stated); MFA status on other systems unknown | **Critical gap:** VPN lacks MFA (Safeguard 6.4). Admin MFA (6.5) status unknown. No formal access provisioning/revocation process described. No RBAC documentation. | **CRITICAL** |

**Detail:** This is your single highest-priority gap. VPN without MFA is the most common ransomware entry point for mid-size organizations. A stolen or phished credential provides direct network access. Safeguard 6.4 is unambiguous: MFA is required for all remote network access. This gap alone is sufficient to invalidate IG2 compliance status.

---

### CIS Control 7 — Continuous Vulnerability Management

**IG2 Safeguards Required:** 7.1 (establish and maintain a vulnerability management process), 7.2 (establish and maintain a remediation process), 7.3 (perform automated operating system patch management), 7.4 (perform automated application patch management), 7.5 (perform automated vulnerability scans of internal enterprise assets), 7.6 (perform automated vulnerability scans of externally-exposed enterprise assets), 7.7 (remediate detected vulnerabilities)

| Current State | Gap | Risk |
|---|---|---|
| Monthly Windows patching exists (partial credit for 7.3); no vulnerability scanner | No vulnerability scanner (gaps 7.5 and 7.6); no application patch management described; no formal remediation process or SLA; no external attack surface scanning | **Critical** |

**Detail:** Monthly OS patching addresses one patch category but leaves application vulnerabilities (browsers, Office, third-party tools, OT software) unaddressed. Without a scanner (e.g., Tenable Nessus, Qualys, Rapid7), you have no visibility into what vulnerabilities exist on your network. IG2 requires both internal and external automated scanning.

---

### CIS Control 8 — Audit Log Management

**IG2 Safeguards Required:** 8.1 (establish and maintain an audit log management process), 8.2 (collect audit logs), 8.3 (ensure adequate audit log storage), 8.4 (standardize time synchronization), 8.5 (collect detailed audit logs), 8.6 (collect DNS query audit logs), 8.7 (collect URL request audit logs), 8.8 (collect command-line audit logs), 8.9 (centralize audit logs), 8.10 (retain audit logs), 8.11 (conduct audit log reviews), 8.12 (collect service provider logs)

| Current State | Gap | Risk |
|---|---|---|
| Windows Event Viewer on individual servers only | No centralized log management; no SIEM; logs are siloed per server and likely overwritten; no log review process; no network device, DNS, or firewall log collection; retention policy unknown | **Critical** |

**Detail:** Decentralized Windows Event Viewer provides no actionable detection capability. You cannot correlate events across systems, you will lose logs when servers are reloaded or when storage fills, and you have no ability to detect lateral movement, privilege escalation, or data exfiltration in progress. IG2 requires centralized collection (Safeguard 8.9) and a defined retention period. This gap also means you likely cannot reconstruct what happened after a security incident.

---

### CIS Control 9 — Email and Web Browser Protections

**IG2 Safeguards Required:** 9.1 (ensure use of only fully supported browsers and email clients), 9.2 (use DNS filtering services), 9.3 (maintain and enforce network-based URL filters), 9.4 (restrict unnecessary or unauthorized browser and email client extensions), 9.5 (implement DMARC), 9.6 (block unnecessary file types), 9.7 (deploy and maintain email server anti-malware protections)

| Current State | Gap | Risk |
|---|---|---|
| No email/web protections described | DNS filtering, DMARC, URL filtering, and email anti-malware status all unknown — assumed absent | High |

**Detail:** Phishing is the primary initial access vector for manufacturing ransomware attacks. DMARC prevents domain spoofing. DNS filtering and URL filtering block malicious sites. These controls are inexpensive and highly effective.

---

### CIS Control 10 — Malware Defenses

**IG2 Safeguards Required:** 10.1 (deploy and maintain anti-malware software), 10.2 (configure automatic anti-malware signature updates), 10.3 (disable autorun and autoplay for removable media), 10.4 (configure automatic anti-malware scanning of removable media), 10.5 (enable anti-exploitation features), 10.6 (centrally manage anti-malware software), 10.7 (use behavior-based anti-malware software)

| Current State | Gap | Risk |
|---|---|---|
| Antivirus on all endpoints (partial credit for 10.1) | Central management of AV console status unknown; behavior-based/EDR capability unknown; autorun configuration unknown; anti-exploitation features (e.g., Windows Defender Exploit Guard) status unknown | Medium |

**Detail:** Traditional signature-based AV alone does not meet IG2. IG2 expects behavior-based detection (EDR-class capabilities), centralized management, and anti-exploitation features enabled. If your AV is legacy signature-only, this is a meaningful gap.

---

### CIS Control 11 — Data Recovery

**IG2 Safeguards Required:** 11.1 (establish and maintain a data recovery process), 11.2 (perform automated backups), 11.3 (protect recovery data), 11.4 (establish and maintain an isolated instance of recovery data), 11.5 (test data recovery)

| Current State | Gap | Risk |
|---|---|---|
| No backup/recovery controls described | Backup posture entirely unknown; no mention of tested, isolated, or immutable backups | High |

**Detail:** For manufacturing, ransomware that encrypts production systems and backups is an existential threat. IG2 requires automated backups, offline or immutable copies, and tested restoration. Untested backups frequently fail at the worst possible moment.

---

### CIS Control 12 — Network Infrastructure Management

**IG2 Safeguards Required:** 12.1 (ensure network infrastructure is up-to-date), 12.2 (establish and maintain a secure network architecture), 12.3 (securely manage network infrastructure), 12.4 (establish and maintain architecture diagram(s)), 12.5 (centralize network authentication, authorization, and auditing), 12.6 (use of secure network management and communication protocols), 12.7 (ensure remote devices utilize a VPN and are connecting to an infrastructure's AAA), 12.8 (establish and maintain dedicated computing resources for all administrative work)

| Current State | Gap | Risk |
|---|---|---|
| Basic firewall exists; no architecture documentation described | No network architecture diagram; no network segmentation described; no out-of-band management; no documented firewall ruleset review process | High |

**Detail:** Manufacturing environments often mix IT and OT/ICS networks. Without documented segmentation, a compromised workstation can directly reach production control systems. IG2 requires a documented, defensible network architecture.

---

### CIS Control 13 — Network Monitoring and Defense

**IG2 Safeguards Required:** 13.1 (centralize security event alerting), 13.2 (deploy a host-based intrusion detection solution), 13.3 (deploy a network intrusion detection solution), 13.4 (perform traffic filtering between network segments), 13.5 (manage access control for remote assets), 13.6 (collect network traffic flow logs), 13.7 (deploy a host-based intrusion prevention solution), 13.8 (deploy a network intrusion prevention solution), 13.9 (deploy port-level access control), 13.10 (perform application layer filtering), 13.11 (tune security event alerting thresholds)

| Current State | Gap | Risk |
|---|---|---|
| No network monitoring described | No IDS/IPS, no SIEM alerting, no network flow logging, no application layer filtering | High |

**Detail:** Without network monitoring, intrusions go undetected. The average dwell time for ransomware actors (time between initial access and detonation) is measured in days to weeks — all of which would be invisible to your current environment.

---

### CIS Control 14 — Security Awareness and Skills Training

**IG2 Safeguards Required:** 14.1 (establish and maintain a security awareness program), 14.2 (train workforce members to recognize social engineering attacks), 14.3 (train workforce members on authentication best practices), 14.4 (train workforce members on data handling best practices), 14.5 (train workforce members on causes of unintentional data exposure), 14.6 (train workforce members on recognizing and reporting security incidents), 14.7 (train workforce members on how to identify and report if their enterprise assets are missing security updates), 14.8 (train workforce members on the dangers of connecting to and transmitting enterprise data over insecure networks), 14.9 (conduct role-specific security awareness and skills training)

| Current State | Gap | Risk |
|---|---|---|
| Annual security training exists (partial credit for 14.1) | Annual frequency is insufficient for IG2; no phishing simulations described; no role-specific training; training content scope unknown | Medium |

**Detail:** Annual training satisfies the minimum IG1 bar but not IG2. IG2 expects more frequent training, phishing simulation exercises, and role-specific content (e.g., additional training for IT staff, finance staff who handle wire transfers, and executives). Phishing simulation is one of the highest-ROI controls available.

---

### CIS Control 15 — Service Provider Management

**IG2 Safeguards Required:** 15.1 (establish and maintain an inventory of service providers), 15.2 (establish and maintain a service provider management policy), 15.3 (classify service providers), 15.4 (ensure service provider contracts include security requirements), 15.5 (assess service providers), 15.6 (monitor service providers), 15.7 (securely decommission service providers)

| Current State | Gap | Risk |
|---|---|---|
| No third-party/vendor management described | No vendor inventory, no security requirements in contracts, no third-party risk assessment process | Medium |

**Detail:** Manufacturing supply chains are a major attack vector (e.g., compromised software updates, vendor remote access). IG2 requires a formal vendor risk program.

---

### CIS Control 16 — Application Software Security

**IG2 Safeguards Required:** 16.1 (establish and maintain a secure application development process), 16.2 (establish and maintain a process to accept and address software vulnerabilities), 16.3 (perform root cause analysis on security vulnerabilities), 16.4 (establish and maintain a process for receiving and addressing security vulnerability reports), 16.5 (use up-to-date and trusted third-party software components), 16.6 (establish and maintain a severity rating system and process for application vulnerabilities), 16.7 (use standard hardening configuration templates for application infrastructure), 16.8 (separate production and non-production systems), 16.9 (train developers in application security concepts and secure coding), 16.10 (apply secure design principles in application architectures), 16.11 (leverage vetted modules or services for application security components), 16.12 (implement code-level security checks), 16.13 (conduct application penetration testing), 16.14 (conduct threat modeling for sensitive applications)

| Current State | Gap | Risk |
|---|---|---|
| No application security program described | Assumed gap across all IG2 safeguards | Medium |

**Detail:** If your organization runs or customizes business applications (ERP, MES, WMS), application security practices are required. If you are purely a consumer of COTS software, the scope is reduced but vendor patch management still applies.

---

### CIS Control 17 — Incident Response Management

**IG2 Safeguards Required:** 17.1 (designate personnel to manage incident handling), 17.2 (establish and maintain contact information for reporting security incidents), 17.3 (establish and maintain an enterprise process for reporting incidents), 17.4 (establish and maintain an incident response process), 17.5 (assign key roles and responsibilities), 17.6 (define mechanisms for communicating during incident response), 17.7 (conduct routine incident response exercises), 17.8 (conduct post-incident reviews), 17.9 (establish and maintain security incident thresholds)

| Current State | Gap | Risk |
|---|---|---|
| No incident response program described | No documented IR plan, no defined roles, no exercises, no external contact relationships (e.g., law enforcement, IR retainer) | High |

**Detail:** Without a tested IR plan, your response to a ransomware event will be improvised, slow, and expensive. IG2 requires a documented plan with defined roles, communication procedures, and regular exercises. Manufacturing downtime costs during a ransomware recovery are typically measured in tens to hundreds of thousands of dollars per day.

---

### CIS Control 18 — Penetration Testing

**IG2 Safeguards Required:** 18.1 (establish and maintain a penetration testing program), 18.2 (perform periodic external penetration tests), 18.3 (remediate penetration test findings), 18.4 (validate security measures), 18.5 (perform periodic internal penetration tests)

| Current State | Gap | Risk |
|---|---|---|
| No penetration testing described | No external or internal pen testing; no validation of security controls | Medium |

**Detail:** Penetration testing provides independent validation that your controls actually work. IG2 requires periodic external and internal tests. For a company of your size, an annual external test plus periodic internal assessments is standard.

---

## Prioritized Gap Summary

### Tier 1 — Critical: Address Within 30 Days

These gaps represent the highest probability of a significant security incident causing business disruption.

| Priority | Control | Gap | Recommended Action |
|---|---|---|---|
| 1 | CIS 6.4 | No MFA on VPN | Deploy MFA immediately. Microsoft Authenticator (if using Azure AD/Entra), Duo Security, or similar. Cost is low; impact is very high. This is your single most exploitable gap. |
| 2 | CIS 8.9 | No centralized log management | Deploy a SIEM or log aggregator (e.g., Wazuh (free/OSS), Microsoft Sentinel, Graylog). At minimum, centralize Windows Event logs, firewall logs, and VPN logs. Configure 90-day online retention. |
| 3 | CIS 7.5/7.6 | No vulnerability scanner | Deploy an authenticated vulnerability scanner (Tenable Nessus Essentials is free for up to 32 IPs; Nessus Professional or Rapid7 InsightVM for full coverage). Run weekly internal scans and monthly external scans. |

---

### Tier 2 — High: Address Within 60–90 Days

| Priority | Control | Gap | Recommended Action |
|---|---|---|---|
| 4 | CIS 11 | Backup posture unknown | Implement or verify: automated daily backups, at least one offline or immutable copy (e.g., Azure Blob with immutability, Veeam with hardened repository), and quarterly tested restoration exercises. |
| 5 | CIS 17 | No incident response plan | Develop a written IR plan covering ransomware, data breach, and OT disruption scenarios. Assign roles. Conduct a tabletop exercise. Establish an IR retainer with a reputable DFIR firm. |
| 6 | CIS 1 & 2 | No asset/software inventory | Deploy a network discovery and asset inventory tool (e.g., Lansweeper, Qualys CSAM, or even a well-maintained spreadsheet fed by DHCP logs as a start). Integrate with patch management. |
| 7 | CIS 10 | AV without EDR capability | Evaluate upgrading to an EDR solution (Microsoft Defender for Endpoint (included in M365 Business Premium), CrowdStrike Falcon Go, SentinelOne). EDR provides behavioral detection that signature AV misses. |
| 8 | CIS 9.5 | DMARC not described | Implement DMARC (with SPF and DKIM) on your email domain. Start in monitoring mode (p=none), progress to p=quarantine, then p=reject. This blocks domain spoofing and phishing that uses your brand. |

---

### Tier 3 — Medium: Address Within 90–180 Days

| Priority | Control | Gap | Recommended Action |
|---|---|---|---|
| 9 | CIS 4 | No secure configuration baselines | Adopt CIS Benchmarks for Windows, your firewall/router, and any server OS. Use Group Policy or Intune to enforce baseline settings. Document and track configuration drift. |
| 10 | CIS 5 | No formal account management | Audit all accounts (disable dormant ones immediately). Enforce a policy: admin accounts separate from daily-use accounts. Document and inventory service accounts. |
| 11 | CIS 12 | No network segmentation documented | Map your network. Identify where OT/ICS systems connect to IT. Implement VLAN segmentation to isolate production systems, guest networks, and corporate systems. Document the architecture. |
| 12 | CIS 14 | Annual training only | Increase training frequency to quarterly. Add phishing simulations (KnowBe4, Proofpoint Security Awareness, or Microsoft Attack Simulator). Add role-specific training for IT, finance, and leadership. |
| 13 | CIS 9.2/9.3 | No DNS/URL filtering | Deploy DNS filtering (Cisco Umbrella, Cloudflare Gateway, or DNSFilter). This is a high-ROI, low-friction control that blocks malware callbacks and phishing sites. |
| 14 | CIS 6.5 | Admin MFA status unknown | Confirm and enforce MFA for all administrative access — servers, network devices, cloud consoles, security tools. |
| 15 | CIS 15 | No vendor risk program | Create a vendor inventory. Add security requirements to new contracts. Request SOC 2 reports or security questionnaires from critical vendors. Control and log all vendor remote access. |

---

### Tier 4 — Longer-Term: Address Within 6–12 Months

| Priority | Control | Gap |
|---|---|---|
| 16 | CIS 13 | Deploy IDS/IPS or NDR capability |
| 17 | CIS 3 | Develop formal data classification and DLP program |
| 18 | CIS 18 | Establish annual external penetration testing program |
| 19 | CIS 16 | Application security practices for any custom/configured applications |
| 20 | CIS 7.4 | Formalize application patch management (browsers, Office, third-party tools) |

---

## Resource Considerations for a 5-Person IT Team

Given your team size, prioritize controls that provide maximum coverage with minimal ongoing operational burden:

- **Free/low-cost tools to deploy first:** Wazuh (SIEM), Nessus Essentials (vulnerability scanner), Duo (MFA — free tier supports up to 10 users, then tiered pricing), DMARC/SPF/DKIM (configuration change only), Windows Defender for Endpoint (included with Microsoft 365 Business Premium if you use it)

- **Highest ROI controls for a small IT team:** MFA everywhere, centralized logging, vulnerability scanning, EDR, and DNS filtering provide the most risk reduction per hour of effort

- **Consider managed services for:** SIEM (managed SOC/MDR), IR retainer (DFIR firm), and annual pen testing — these are appropriate to outsource given team size

- **Quick wins your team can complete in a week:** Enable MFA on VPN (1–2 days), deploy DMARC monitoring mode (2–4 hours), disable dormant accounts (1 day), review and restrict firewall rules (1 day)

---

## Compliance Score Estimate

| CIS Control | IG2 Safeguards | Estimated Current Compliance |
|---|---|---|
| 1 — Asset Inventory | ~5 safeguards | ~10% |
| 2 — Software Inventory | ~7 safeguards | ~10% |
| 3 — Data Protection | ~13 safeguards | ~5% |
| 4 — Secure Configuration | ~12 safeguards | ~25% |
| 5 — Account Management | ~6 safeguards | ~15% |
| 6 — Access Control | ~8 safeguards | ~15% (MFA gap is disqualifying) |
| 7 — Vulnerability Management | ~7 safeguards | ~20% (patching only) |
| 8 — Audit Log Management | ~12 safeguards | ~10% |
| 9 — Email/Web Protections | ~7 safeguards | ~10% |
| 10 — Malware Defenses | ~7 safeguards | ~40% (AV present) |
| 11 — Data Recovery | ~5 safeguards | Unknown/~10% |
| 12 — Network Infrastructure | ~8 safeguards | ~20% |
| 13 — Network Monitoring | ~11 safeguards | ~5% |
| 14 — Security Training | ~9 safeguards | ~30% (annual training) |
| 15 — Service Provider Mgmt | ~7 safeguards | ~5% |
| 16 — Application Security | ~14 safeguards | ~10% |
| 17 — Incident Response | ~9 safeguards | ~5% |
| 18 — Penetration Testing | ~5 safeguards | ~0% |
| **Overall Estimate** | **~161 IG2 safeguards** | **~15–20% compliant** |

---

## Conclusion

Your organization has the basic hygiene controls of an IG1 organization but is substantially below the IG2 bar. The three most urgent and impactful actions are:

1. **Deploy MFA on the VPN** — this single action closes your most likely ransomware entry point
2. **Stand up centralized log management** — you currently have no ability to detect or investigate incidents
3. **Run an authenticated vulnerability scan** — you do not know what vulnerabilities exist on your network

A manufacturing company of your size and profile is actively targeted by ransomware groups. The controls above, implemented in priority order, will significantly reduce your risk exposure and bring you into reasonable alignment with CIS IG2 over a 12-month period.
