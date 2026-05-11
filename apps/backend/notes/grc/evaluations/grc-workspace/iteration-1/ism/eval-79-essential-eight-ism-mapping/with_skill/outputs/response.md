# Essential Eight and the ISM: Relationship, Chapter Mapping, and ML2 Requirements

## How the Essential Eight Relates to the Broader ISM

The Essential Eight is not a separate standard — it is a prioritised subset of controls drawn directly from the Australian Information Security Manual (ISM), published by the Australian Signals Directorate (ASD). ASD identified these eight mitigation strategies as the highest-value actions an organisation can take to protect against the most common cyber threats, particularly opportunistic and targeted attacks.

Key points about this relationship:

- The ISM contains 22 guideline chapters covering the full breadth of government cybersecurity — from physical security and personnel screening through to cryptography, networking, and data transfers. The Essential Eight maps primarily into Chapters 6, 9, 13, 14, and 19 of the ISM.
- Achieving Essential Eight ML2 is not the same as achieving full ISM compliance. An organisation that completes ML2 has addressed the most critical technical controls, but will still have significant gaps across the remaining ISM chapters (e.g., physical security, communications systems, media handling, software development, cryptographic key management, gateways, and data transfers).
- For government entities, both ISM control compliance AND Essential Eight targets apply. The Essential Eight is effectively the "do this first" subset; the remaining ISM chapters address the full risk posture required for system authorisation.
- Essential Eight Maturity Level 2 (ML2) is the ASD-recommended baseline for all government entities and reflects a posture designed to address sophisticated, targeted cyber threats. It is the 2026 target for Australian federal agencies.
- ASD publishes an official Essential Eight to ISM control mapping document (https://www.cyber.gov.au/business-government/asds-cyber-security-frameworks/essential-eight/essential-eight-maturity-model-and-ism-mapping) that details the specific ISM control IDs covered by each strategy and maturity level.


## Essential Eight — ISM Chapter Mapping and ML2 Requirements

The table below maps each of the eight strategies to the primary ISM chapters they draw from, and describes what Maturity Level 2 requires your organisation to implement.

---

### 1. Application Control (Allow-listing)

**Primary ISM Chapter:** Chapter 13 — System Hardening

**ISM Domain:** Prevent execution of unapproved software on workstations and servers.

**What ML2 Requires:**
- Application control is implemented on workstations and internet-facing servers to prevent execution of unapproved executables, software libraries, scripts, and installers.
- Application control rules are validated at least annually or when significant changes occur.
- Application control is applied using a vendor-supported allow-listing technology (e.g., Microsoft AppLocker or Windows Defender Application Control).
- Events where application control blocks execution are logged and reviewed.
- Application control also covers scripts (e.g., PowerShell, VBScript) and installers, not just executables.

ML2 builds on ML1 by extending coverage beyond just executables to include scripts and installers, and by requiring regular rule validation.

---

### 2. Patch Applications

**Primary ISM Chapter:** Chapter 14 — System Management

**ISM Domain:** Patch management, vulnerability remediation, software currency.

**What ML2 Requires:**
- Applications with known security vulnerabilities (critical/high severity) are patched or mitigated within 14 days of patch release.
- Internet-facing applications with critical vulnerabilities are patched within 48 hours.
- Applications that are no longer supported by vendors (end-of-life) are removed or replaced.
- Patch status of applications is tracked centrally; automated scanning is used to identify missing patches.
- Online services are updated within defined timeframes.

ML2 extends ML1 by tightening patch timeframes and requiring a more systematic approach to tracking and reporting patch status across all applications.

---

### 3. Configure Microsoft Office Macros

**Primary ISM Chapter:** Chapter 13 — System Hardening

**ISM Domain:** Application hardening, restricting macro execution to prevent malicious code delivery.

**What ML2 Requires:**
- Microsoft Office macros are disabled for users who do not have a documented business requirement.
- Only macros from trusted locations or signed by a trusted publisher are permitted to run.
- Macro settings are enforced via Group Policy or equivalent configuration management — users cannot change macro settings themselves.
- Antivirus scanning of macros is enabled.
- Microsoft Office macro security settings are validated and reviewed regularly.

ML2 moves beyond simply advising users not to run macros, requiring enforceable policy-based controls that prevent unapproved macros from executing regardless of user action.

---

### 4. User Application Hardening

**Primary ISM Chapter:** Chapter 13 — System Hardening

**ISM Domain:** Browser, Office, and PDF reader hardening to reduce attack surface.

**What ML2 Requires:**
- Web browsers are configured to block or disable: web advertisements, Flash, Java, and other high-risk browser plugins.
- Web browsers do not process Java from the internet.
- Internet Explorer 11 is disabled or removed (no longer supported).
- PDF software is hardened — PDF viewers are configured to disable JavaScript execution.
- Microsoft Office is hardened — Object Linking and Embedding (OLE) packages are blocked.
- Hardening configurations are enforced via Group Policy or equivalent and cannot be changed by standard users.
- Application hardening baselines are validated regularly.

ML2 requires enforceable, policy-driven hardening rather than relying on default vendor settings or user behaviour.

---

### 5. Restrict Administrative Privileges

**Primary ISM Chapters:** Chapter 13 — System Hardening; Chapter 6 — Personnel Security

**ISM Domains:** Least privilege, privileged access management, personnel accountability.

**What ML2 Requires:**
- Administrative privileges are granted only where there is a documented business need; all privilege assignments are recorded.
- Privileged users use separate accounts for privileged and unprivileged activities — no browsing the internet or reading email from a privileged account.
- Privileged accounts are not used to access the internet or email.
- Privileged access to operating systems and applications is validated at least annually; unnecessary privileges are revoked.
- Use of privileged accounts is logged, and logs are reviewed regularly.
- Just-in-time and just-enough-access approaches are implemented where feasible.

ML2 requires documented review cycles and active enforcement of privilege separation, rather than ad hoc controls.

---

### 6. Patch Operating Systems

**Primary ISM Chapter:** Chapter 14 — System Management

**ISM Domain:** Patch management for operating system vulnerabilities.

**What ML2 Requires:**
- Operating systems with known critical vulnerabilities are patched within 14 days of patch release.
- Internet-facing operating systems with critical vulnerabilities are patched within 48 hours.
- End-of-life/unsupported operating systems are removed from the environment or subject to formal risk acceptance and compensating controls.
- Patch status across all operating systems is tracked centrally; automated vulnerability scanning is used.
- Operating system versions in use across the environment are inventoried and kept current.

ML2 requires tighter patch timeframes and systematic tracking compared to ML1, and draws a clear line on end-of-life operating systems.

---

### 7. Multi-Factor Authentication (MFA)

**Primary ISM Chapters:** Chapter 6 — Personnel Security; Chapter 9 — Enterprise Mobility; Chapter 19 — Networking

**ISM Domains:** Identity and access management, remote access security, mobile device management.

**What ML2 Requires:**
- MFA is required for all users accessing remote access solutions (VPN, remote desktop, cloud services).
- MFA is required for all users accessing internet-facing services that process or store sensitive data.
- MFA is required for all privileged users (administrators, system accounts) regardless of access method.
- MFA is required for all users accessing cloud service providers' management interfaces.
- MFA solutions use phishing-resistant methods where possible (e.g., FIDO2/hardware tokens, certificate-based authentication) rather than SMS OTP alone.
- MFA is enforced via policy and cannot be bypassed by users or help desk staff without a formal exception process.

ML2 extends MFA coverage beyond ML1 to include a broader set of services and begins to transition away from weaker MFA factors such as SMS.

---

### 8. Regular Backups

**Primary ISM Chapter:** Chapter 14 — System Management

**ISM Domain:** Business continuity, backup management, resilience against data loss and ransomware.

**What ML2 Requires:**
- Backups of important data, software, and configuration settings are performed and retained for at least three months.
- Backups are stored in a manner that prevents them from being encrypted or deleted in a ransomware event (i.e., offline or immutable storage; backups are not accessible from the production network).
- Backup restoration is tested at least every three months to confirm recoverability.
- Unprivileged accounts cannot access or delete backups; backup system access is restricted.
- Backups cover all critical systems and data, including system configurations.

ML2 requires tested, ransomware-resilient backups with regular restoration verification — moving well beyond simply scheduling backups to confirming they actually work.

---

## Summary: ISM Chapter Coverage of the Essential Eight

| Essential Eight Strategy | Primary ISM Chapter(s) |
|--------------------------|------------------------|
| Application control | Ch. 13 System Hardening |
| Patch applications | Ch. 14 System Management |
| Configure Microsoft Office macros | Ch. 13 System Hardening |
| User application hardening | Ch. 13 System Hardening |
| Restrict administrative privileges | Ch. 13 System Hardening, Ch. 6 Personnel Security |
| Patch operating systems | Ch. 14 System Management |
| Multi-factor authentication | Ch. 6 Personnel Security, Ch. 9 Enterprise Mobility, Ch. 19 Networking |
| Regular backups | Ch. 14 System Management |

---

## What ML2 Does NOT Cover (Broader ISM Obligations)

Achieving Essential Eight ML2 addresses controls primarily within four ISM chapters (6, 9, 13, 14, and 19). Your organisation will still need to address the remaining 17+ ISM chapters for full ISM compliance and system authorisation readiness. Key areas not covered by the Essential Eight include:

- **Chapter 1** — CISO appointment and security governance roles
- **Chapter 2** — Cyber security incident response and ASD notification obligations
- **Chapter 3** — Procurement and supply chain security
- **Chapter 4** — Security documentation (System Security Plan, risk register)
- **Chapter 5** — Physical security controls
- **Chapter 7/8** — Communications infrastructure and systems
- **Chapter 10** — Evaluated/certified product requirements
- **Chapter 11/12** — IT equipment management and media sanitisation
- **Chapter 15** — System monitoring, logging, and SIEM
- **Chapter 16** — Secure software development
- **Chapter 17** — Database hardening
- **Chapter 18** — Email security (DMARC, DKIM, SPF)
- **Chapter 20** — Cryptographic standards and key management
- **Chapter 21** — Gateway and content filtering controls
- **Chapter 22** — Data transfer controls and labelling

For system authorisation (Authorisation to Operate), your organisation must address all applicable ISM controls across all relevant chapters — not just the Essential Eight. If your systems handle OFFICIAL: Sensitive information, all NC and OS-marked controls apply. An IRAP assessment is strongly recommended for OFFICIAL: Sensitive systems and mandatory for PROTECTED systems.

---

## Recommended Next Steps

1. Conduct an Essential Eight gap assessment against ML2 criteria for each of the eight strategies.
2. Prioritise remediation based on current maturity gaps; strategies touching Chapters 13 and 14 (hardening and patch management) tend to have the most breadth.
3. Run the Essential Eight alongside a broader ISM gap analysis to identify the delta between ML2 compliance and full ISM compliance for your system's classification level.
4. Document progress in your System Security Plan (SSP) with evidence of implementation for each control.
5. Reference the official ASD Essential Eight to ISM mapping: https://www.cyber.gov.au/business-government/asds-cyber-security-frameworks/essential-eight/essential-eight-maturity-model-and-ism-mapping
