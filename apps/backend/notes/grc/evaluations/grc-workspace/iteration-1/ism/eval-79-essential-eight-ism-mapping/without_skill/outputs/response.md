# Essential Eight and the ISM: ML2 Requirements and Control Mappings

## How the Essential Eight Relates to the Broader ISM

The Australian Signals Directorate (ASD) Information Security Manual (ISM) is the comprehensive cybersecurity framework that Australian government agencies and many regulated organisations must follow. It contains hundreds of controls organised across numerous chapters covering everything from governance and personnel security through to physical security, communications, cryptography, and system-specific guidance.

The Essential Eight is a prioritised subset of mitigation strategies drawn from the ISM. It was developed by ASD as a baseline set of eight strategies that, when implemented together, make it significantly harder for adversaries to compromise systems. The Essential Eight is not separate from the ISM — it is derived from and cross-referenced within the ISM. Specifically, the Essential Eight strategies correspond to controls distributed across several ISM chapters, primarily:

- Chapter 2: Guidelines for Cybersecurity Roles
- Chapter 6: Guidelines for System Hardening
- Chapter 7: Guidelines for Identity and Access Management
- Chapter 10: Guidelines for Software Development
- Chapter 11: Guidelines for Database Systems (partial overlap)
- Chapter 12: Guidelines for Email
- Chapter 13: Guidelines for Networking
- Chapter 14: Guidelines for Cryptography (partial overlap)
- Chapter 15: Guidelines for Sensitive and Classified Information (partial overlap)

The ISM provides the full control catalogue with applicability ratings (should/must), while the Essential Eight provides a pragmatic baseline prioritisation. ASD's Maturity Model for the Essential Eight defines three maturity levels (ML1, ML2, ML3) that progressively implement the controls underlying each strategy. Organisations achieving ML2 have implemented controls that are generally aligned with mitigating targeted intrusions and more capable adversaries than those addressed at ML1.

---

## The Eight Strategies: ISM Mappings and ML2 Requirements

---

### 1. Application Control

**What it is:** Preventing execution of unapproved or malicious programs, including executables, software libraries, scripts, and installers.

**Relevant ISM Chapter and Controls:**
- Primary chapter: Guidelines for System Hardening (ISM-0843, ISM-1490, ISM-1657, ISM-1658, ISM-1659, and related controls)
- Also touches Guidelines for Software Development for approved software baselines

**ML2 Requirements:**
- Application control is implemented on all workstations and internet-facing servers (not just a subset).
- Application control prevents execution of executables, software libraries (DLLs), scripts (including PowerShell, batch files, WSF, HTA, and similar), installers, compiled HTML, HTML applications, and control panel applets from the full file system — not just specific directories.
- Microsoft's recommended block rules (or equivalent) are applied to prevent known bypasses.
- Application control rulesets are validated at least annually or when significant changes occur.
- Allowed and blocked execution events are logged.
- Application control is implemented using a rules-based approach (e.g., publisher rules, hash rules, path rules ordered by preference of publisher over hash over path).

---

### 2. Patch Applications

**What it is:** Applying security patches and updates to applications to close known vulnerabilities.

**Relevant ISM Chapter and Controls:**
- Primary chapter: Guidelines for System Hardening (ISM-1690, ISM-1691, ISM-1692, ISM-1695, and related patch management controls)
- Also touches Guidelines for Software Development and Guidelines for Procurement

**ML2 Requirements:**
- A vulnerability scanner is used at least fortnightly to identify missing patches or updates for applications.
- Patches, updates, or vendor mitigations for security vulnerabilities in internet-facing services are applied within two weeks of release, or within 48 hours if an exploit exists.
- Patches, updates, or vendor mitigations for security vulnerabilities in office productivity suites, web browsers, browser extensions, email clients, PDF viewers, and security products are applied within two weeks of release, or within 48 hours if an exploit exists.
- Patches for other applications are applied within one month of release.
- Applications that are no longer supported by vendors (end-of-life) are removed or replaced.
- An automated mechanism is used to confirm and record patch application.

---

### 3. Configure Microsoft Office Macro Settings

**What it is:** Restricting or blocking Microsoft Office macros to prevent malicious macro-based malware from executing.

**Relevant ISM Chapter and Controls:**
- Primary chapter: Guidelines for System Hardening (ISM-1672, ISM-1673, ISM-1674, ISM-1675, ISM-1676, and related macro controls)
- Also touches email security guidance

**ML2 Requirements:**
- Only macros that are digitally signed by trusted publishers, or are part of a managed environment where macros are controlled, are permitted to run.
- Macros in files originating from the internet are blocked.
- Macro antivirus scanning is enabled (where supported).
- Macro security settings are centrally managed and users cannot change them.
- Macros are blocked from making Win32 API calls (where the capability exists, e.g., via Attack Surface Reduction rules in Microsoft Defender).
- Logging of blocked macro execution events is implemented.

---

### 4. User Application Hardening

**What it is:** Hardening user-facing applications — particularly web browsers, PDF readers, and Microsoft Office — to reduce the attack surface.

**Relevant ISM Chapter and Controls:**
- Primary chapter: Guidelines for System Hardening (ISM-1486, ISM-1485, ISM-1679, ISM-1680, ISM-1681, ISM-1682, and related controls)
- Also touches Guidelines for Networking (web content filtering)

**ML2 Requirements:**
- Web browsers do not process Java from the internet (Java disabled or removed from browsers).
- Web browsers do not process web advertisements from the internet (ad blocking or content filtering applied).
- Internet Explorer 11 is disabled or removed (where applicable, given its end-of-life status).
- Web browsers are hardened using vendor or ASD hardening guides.
- PDF viewers are hardened using vendor or ASD hardening guides.
- Microsoft Office is hardened (e.g., OLE package activation blocked, DDE disabled where applicable).
- .NET Framework 3.5 (which includes .NET 2.0 and 3.0) is disabled where not required.
- Windows Script Host (WSH) is disabled where not required.
- PowerShell is configured to use Constrained Language Mode where possible.
- Hardening configurations are centrally managed.

---

### 5. Restrict Administrative Privileges

**What it is:** Limiting the number of users with administrative privileges and tightly controlling how those accounts are used.

**Relevant ISM Chapter and Controls:**
- Primary chapter: Guidelines for Identity and Access Management (ISM-1507, ISM-1508, ISM-1509, ISM-1513, ISM-1524, ISM-1549, and related privileged access controls)
- Also touches Guidelines for Cybersecurity Roles (defining roles and responsibilities for privileged users)

**ML2 Requirements:**
- Privileged access to systems is validated when first requested and revalidated at least annually.
- Privileged users are assigned a separate privileged account that is used only for privileged tasks; they use a separate unprivileged account for standard tasks and email/web browsing.
- Privileged accounts (excluding privileged service accounts) are prevented from accessing the internet, email, and web services.
- Privileged accounts are not permitted to create accounts or assign privileges to accounts beyond what is required for their administrative role.
- Requests for privileged access are documented and approved.
- Just-in-time administration is implemented where feasible (i.e., privileges are granted for specific tasks and then removed).
- The number of privileged accounts is minimised.
- Privileged account activity is logged and protected from tampering.

---

### 6. Patch Operating Systems

**What it is:** Applying security patches and updates to operating systems to close known vulnerabilities.

**Relevant ISM Chapter and Controls:**
- Primary chapter: Guidelines for System Hardening (ISM-1693, ISM-1694, ISM-1696, ISM-1697, and related OS patch controls)
- Also touches Guidelines for Procurement (use of supported operating systems)

**ML2 Requirements:**
- A vulnerability scanner is used at least fortnightly to identify missing patches or updates for operating systems.
- Patches, updates, or vendor mitigations for security vulnerabilities in internet-facing servers and network devices are applied within two weeks of release, or within 48 hours if an exploit exists.
- Patches, updates, or vendor mitigations for security vulnerabilities in workstations, non-internet-facing servers, and network devices are applied within one month of release, or within 48 hours if an exploit exists.
- Operating systems that are no longer supported by vendors (end-of-life) are removed or replaced.
- Only the latest or previous release of an operating system (where supported) is used.
- An automated mechanism is used to confirm and record patch application.

---

### 7. Multi-Factor Authentication (MFA)

**What it is:** Requiring users to authenticate using more than one factor, to reduce the risk of credential-based compromise.

**Relevant ISM Chapter and Controls:**
- Primary chapter: Guidelines for Identity and Access Management (ISM-1173, ISM-1504, ISM-1505, ISM-1506, ISM-1679, and related MFA controls)
- Also touches Guidelines for Networking (remote access and VPN authentication) and Guidelines for Cryptography (for token-based authentication)

**ML2 Requirements:**
- MFA is used for all users accessing internet-facing services (e.g., remote access solutions, cloud services, webmail).
- MFA is used for all privileged users accessing systems and applications.
- MFA is used when users perform privileged actions or access important data repositories (e.g., databases, code repositories).
- MFA that is phishing-resistant (i.e., not SMS or voice call based) is strongly preferred; at ML2, phishing-resistant MFA is required for at least internet-facing services and privileged accounts.
- MFA event logs are retained.
- Failed MFA attempts are alerted on.

Note: The ASD Essential Eight Maturity Model (updated versions from 2023 onwards) specifies that from ML2, MFA must be phishing-resistant for internet-facing services and privileged access — excluding weaker forms such as SMS OTP.

---

### 8. Regular Backups

**What it is:** Backing up important data, software, and configuration settings regularly and testing restoration to ensure availability and recovery capability.

**Relevant ISM Chapter and Controls:**
- Primary chapter: Guidelines for Data Transfers and Content Filtering / Guidelines for System Management (ISM-1511, ISM-1512, ISM-1515, ISM-1516, ISM-0519, and related backup controls)
- Also touches Guidelines for Sensitive and Classified Information and Guidelines for Physical Security (offsite backup storage)

**ML2 Requirements:**
- Backups of important data, software, and configuration settings are performed and retained for at least three months.
- Backups are synchronised to enable restoration to a common point in time.
- Backups are tested at least once when initially implemented and then at least annually (full restoration test).
- Unprivileged accounts cannot access backups belonging to other accounts.
- Unprivileged accounts cannot delete or modify backups.
- Privileged accounts (other than backup administrator accounts) are prevented from accessing, modifying, or deleting backups.
- Backups are stored offline or in a manner that prevents them from being encrypted or deleted by ransomware (e.g., immutable storage, air-gapped systems, or sufficiently isolated cloud backup repositories).

---

## Summary: ISM Chapter Mapping Table

| Essential Eight Strategy         | Primary ISM Chapter(s)                                   |
|----------------------------------|----------------------------------------------------------|
| Application Control              | Guidelines for System Hardening                          |
| Patch Applications               | Guidelines for System Hardening                          |
| Configure Office Macro Settings  | Guidelines for System Hardening                          |
| User Application Hardening       | Guidelines for System Hardening; Guidelines for Networking|
| Restrict Administrative Privileges | Guidelines for Identity and Access Management; Guidelines for Cybersecurity Roles |
| Patch Operating Systems          | Guidelines for System Hardening                          |
| Multi-Factor Authentication      | Guidelines for Identity and Access Management; Guidelines for Networking |
| Regular Backups                  | Guidelines for System Management / Data Transfers        |

---

## Practical Considerations for Achieving ML2

Reaching ML2 by the end of the financial year requires a structured implementation programme. The strategies that typically require the most lead time are:

1. **Application Control** — Deploying and tuning rules across all workstations and servers without breaking legitimate applications takes time and testing.
2. **Restrict Administrative Privileges** — Restructuring privileged access patterns and implementing just-in-time administration often requires significant change management.
3. **Multi-Factor Authentication (phishing-resistant)** — Hardware key rollout or integration of FIDO2/certificate-based authentication across all internet-facing systems can be technically complex.

Patching and backup controls are generally easier to evidence if existing tooling is already in place but may require policy uplift and logging improvements.

Organisations should consult the current ASD Essential Eight Maturity Model document (available on the Australian Cyber Security Centre website) and the ISM (updated monthly) for the precise, current control identifiers, as both documents are living publications subject to revision.
