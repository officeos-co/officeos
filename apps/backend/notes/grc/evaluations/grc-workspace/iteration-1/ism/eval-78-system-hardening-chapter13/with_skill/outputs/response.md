# ISM Chapter 13 — System Hardening: Requirements and Evidence for Windows Server 2022

Source: Australian Information Security Manual (ASD), March 2026 edition
Assumed classification: OFFICIAL: Sensitive (OS) — the default baseline for state government systems handling sensitive information. If your deployment handles PROTECTED information, all NC + OS + P controls apply and additional controls are required beyond what is listed here.

---

## Overview of Chapter 13 — System Hardening

Chapter 13 sits within the Protect function of the ISM framework. Its purpose is to ensure that operating systems, applications, and firmware are configured securely before deployment and maintained in a hardened state throughout the system lifecycle. For Windows Server 2022, this translates into a set of concrete technical actions covering the OS baseline, application controls, firmware, and privileged access management.

Chapter 13 also underpins four of the eight Essential Eight mitigation strategies: application control (allow-listing), configuring Microsoft Office macros, user application hardening, and restricting administrative privileges. This means that Chapter 13 compliance is closely tied to your Essential Eight Maturity Level reporting obligations.

---

## What Chapter 13 Requires

### 1. Operating System Hardening

The ISM requires that operating systems be hardened before deployment. This means removing or disabling everything that is not needed for the system's defined purpose.

For Windows Server 2022 specifically, the following actions are required:

- Remove all Windows roles, role services, and features that are not required for the server's purpose. Every unnecessary component is an attack surface.
- Disable all default and built-in accounts that are not needed. The built-in local Administrator account must be renamed and its use restricted; the Guest account must be disabled.
- Apply the ASD hardening guidance for Windows Server. ASD publishes a dedicated hardening guide (Hardening Microsoft Windows Server) that specifies required Group Policy settings. Your configuration must align with that guide.
- Enforce screen lock and session timeout policies.
- Configure Windows Defender and ensure real-time protection is active unless a compensating security product is formally documented.
- Disable unnecessary network services and protocols (for example, SMBv1, NetBIOS over TCP/IP where not required, LLMNR, mDNS).
- Apply a host-based firewall policy that blocks all inbound traffic not explicitly required.

### 2. Application Hardening

Applications installed on the server must also be hardened:

- Disable or remove features within applications that are not required for the server's function.
- Apply vendor-supplied hardening guides for all installed server software (IIS, SQL Server, .NET, etc.).
- Uninstall or disable software that is not required — this includes browser software unless operationally justified.
- Where Microsoft Office is installed, macro execution must be restricted. At a minimum, macros must be blocked from the internet and only macros from trusted publishers or trusted locations may run. This directly maps to the Essential Eight "Configure Microsoft Office macros" strategy.

### 3. Application Control (Allow-Listing)

This is one of the highest-priority Chapter 13 controls and maps directly to the Essential Eight "Application control" strategy.

- An application allow-list must be implemented to prevent execution of unapproved or unknown executables, software libraries, scripts, and installers.
- On Windows Server 2022, this is implemented via Windows Defender Application Control (WDAC) or AppLocker, with WDAC being ASD's preferred solution.
- The allow-list must cover all user-accessible execution paths. At Essential Eight ML2 (the ASD-recommended baseline for government), the allow-list must prevent execution of malicious code in standard user profiles and temporary directories.
- At ML3, the allow-list must be enforced using a combination of publisher certificate rules and file hash rules and must cover all execution paths including scripts and interpreted code.

### 4. Firmware Security

The ISM requires that firmware-level security settings be configured before deployment:

- Secure Boot must be enabled. Windows Server 2022 supports Secure Boot; it must be active and the firmware must be configured to prevent disabling it from the OS.
- A strong BIOS/UEFI password must be set to prevent unauthorised firmware modifications.
- Boot order must be restricted so that the server can only boot from the authorised system drive.
- Where available, TPM 2.0 should be enabled and used to support measured boot and attestation.
- Firmware must be kept current — out-of-date firmware with known vulnerabilities is treated as a hardening gap.

### 5. Least Privilege and Privileged Access Management

This is a core ISM requirement that runs throughout Chapter 13 and connects to Chapter 6 (Personnel Security).

- No standing administrative access. Standard user accounts must be used for day-to-day operations; administrative credentials must only be used when performing administrative tasks.
- Separate administrative accounts must be created for each administrator — shared administrator accounts are not permitted.
- Domain Administrator and Enterprise Administrator accounts must be tightly controlled; their use must be logged and reviewed.
- Local administrator accounts on servers must be managed. Microsoft LAPS (Local Administrator Password Solution) or equivalent must be used to ensure unique, rotated local admin passwords across all servers.
- Privileged Access Workstations (PAWs) are strongly recommended for performing administrative tasks on servers handling OS-classified information.
- Just-In-Time (JIT) access is the preferred model — administrative rights should be granted only when needed and revoked after use.
- Remote Desktop Protocol (RDP) access must be restricted to authorised IP ranges, use Network Level Authentication (NLA), and require MFA for privileged accounts.

### 6. User Application Hardening

Where end-user applications are present on the server (this is less common for server deployments but applies to jump servers or terminal server deployments):

- Web browsers must be hardened: Flash, Java plugins, and unsupported extensions must be disabled or removed; automatic execution of web-delivered content must be blocked.
- PDF viewers must be configured to block internet connections and disable JavaScript execution within documents.
- Microsoft Office must be configured per ASD's hardening guide, including disabling OLE package activation.

---

## Control Applicability for Your Deployment

Your Windows Server 2022 deployment is assumed to be an OFFICIAL: Sensitive (OS) system. This means you must implement all NC-marked and OS-marked controls from Chapter 13. The table below maps the key Chapter 13 control areas to their applicability:

| Control Area | Applicability | Notes |
|---|---|---|
| OS hardening (remove unnecessary services, disable default accounts) | NC | Applies to all government systems |
| Application allow-listing | NC / OS | Essential Eight strategy; required at ML2 for OS systems |
| Firmware security (Secure Boot, BIOS password) | NC | Applies to all government systems |
| Least privilege enforcement | NC / OS | No standing admin access; separate admin accounts |
| Local admin password management (LAPS) | NC / OS | Required for all managed endpoints and servers |
| Application hardening (disable unused features) | NC | Applies to all installed software |
| Microsoft Office macro restrictions | NC / OS | Essential Eight strategy |
| Browser and PDF hardening | NC / OS | Essential Eight "User application hardening" |

If your system is reclassified to PROTECTED, additional P-marked controls apply, including stricter controls on privileged access, evaluated cryptographic products, and mandatory IRAP assessment.

---

## Evidence Required for Compliance Assessment

An assessor evaluating your Chapter 13 compliance — whether during an internal review, IRAP assessment, or government audit — will look for documented evidence that each control is implemented. Below is a structured evidence checklist.

### OS Hardening Evidence

| Evidence Item | Description |
|---|---|
| Group Policy export or baseline configuration report | Demonstrates applied hardening settings, aligned with ASD's Windows Server 2022 hardening guide |
| Roles and features inventory | Shows only required Windows roles and features are installed (e.g., output of `Get-WindowsFeature` filtered to installed items) |
| Disabled accounts listing | Shows Guest and default accounts are disabled; local Administrator is renamed |
| Host-based firewall rule set | Documents all allowed inbound/outbound rules and confirms default-deny posture |
| Network protocol configuration | Evidence that SMBv1, LLMNR, NetBIOS over TCP/IP are disabled where not required |
| ASD Windows Server Hardening Guide gap comparison | A completed checklist showing alignment with ASD's published hardening benchmark |

### Application Control Evidence

| Evidence Item | Description |
|---|---|
| WDAC or AppLocker policy export | The implemented application control policy in XML or readable format |
| Block event log samples | Windows event log entries (Event ID 3076/3077 for WDAC in audit mode; 8003/8004 for AppLocker) demonstrating the policy is active |
| Policy scope documentation | Confirms the allow-list covers all execution paths (executables, scripts, libraries, installers) |
| User acceptance testing (UAT) record | Confirms the allow-list does not block legitimate business applications |

### Firmware Security Evidence

| Evidence Item | Description |
|---|---|
| BIOS/UEFI configuration screenshots | Shows Secure Boot enabled, boot order locked, BIOS password set |
| System Information report | `msinfo32` or `Get-ComputerInfo` output confirming Secure Boot state and TPM presence |
| Firmware version record | Current firmware version against vendor's latest release, confirming firmware is up to date |

### Least Privilege and Privileged Access Management Evidence

| Evidence Item | Description |
|---|---|
| Active Directory group membership reports | Lists of users in privileged groups (Domain Admins, Server Operators, Backup Operators, local Administrators) |
| Separate admin account documentation | Evidence that all admins have a separate privileged account distinct from their standard account |
| LAPS deployment confirmation | Evidence LAPS is deployed, local admin accounts are managed, and password rotation is occurring |
| Privileged Access Workstation (PAW) documentation | Architecture diagram or policy describing how administrative tasks are performed |
| RDP access policy | Firewall rules, NLA configuration, and MFA configuration for RDP access |
| Privileged account review records | Evidence of regular review of privileged account membership (at least annually) |

### Application Hardening Evidence

| Evidence Item | Description |
|---|---|
| Installed software inventory | Current list of all installed software on each server |
| Application hardening checklists | Completed hardening checklists for IIS, SQL Server, .NET runtime, and any other installed server software |
| Microsoft Office macro policy (if Office is installed) | Group Policy setting or registry export showing macro restrictions |
| Browser hardening configuration (if browsers are present) | Policy export or configuration screenshots showing plugin and execution restrictions |

### Essential Eight Maturity Assessment Evidence

Because four Essential Eight strategies are covered by Chapter 13, your assessor will also want:

| Evidence Item | Description |
|---|---|
| Essential Eight self-assessment | Completed self-assessment against all eight strategies, scored to ML0–ML3 for each |
| Application control strategy document | Describes what is allowed, how exceptions are handled, and how the allow-list is maintained |
| Macro configuration policy document | Describes the approved macro execution policy and how it is enforced |

---

## Practical Steps: Preparing for Assessment

1. Conduct a baseline configuration review before the assessment. Use the ASD Windows Server 2022 hardening guide as your checklist and document every setting against the requirement. Note gaps and remediate before assessment.

2. Export your Group Policy Objects (GPOs). Assessors will review these in detail. Organise them by function (security baseline, application control, audit policy, etc.).

3. Validate your application allow-list is in enforced mode, not audit mode. An allow-list in audit-only mode is not considered compliant. If you are transitioning, document the timeline and risk acceptance.

4. Verify LAPS is functioning. Run a report showing last password rotation times across all servers. Stale entries indicate a gap.

5. Run a privileged account review. Identify any accounts with administrative rights that do not need them and remove them before assessment.

6. Document every exclusion formally. If a control cannot be implemented (for example, a specific protocol must remain enabled for a legacy integration), document the risk acceptance in your System Security Plan (SSP) with the Authorising Official's signature.

7. Collate all evidence into an evidence folder mapped to each control. An assessor will work through controls sequentially; having pre-organised evidence by control area significantly reduces assessment duration.

---

## Connection to System Authorisation

Chapter 13 evidence does not exist in isolation. It feeds directly into your System Security Plan (SSP), which is the primary artefact for Authorisation to Operate (ATO). Your SSP must document:

- All Chapter 13 controls selected as applicable
- Implementation status of each control
- Evidence references (pointers to your evidence folder)
- Any exclusions with documented risk justification
- The Authorising Official's acceptance of residual risk

For an OFFICIAL: Sensitive system, an IRAP assessment is strongly recommended (and may be required by your agency's security policy) before authorisation. The IRAP assessor will independently verify the Chapter 13 evidence listed above.

---

## Key References

- ASD Information Security Manual (March 2026): https://www.cyber.gov.au/sites/default/files/2026-03/Information%20security%20manual%20(March%202026).pdf
- ASD Hardening Microsoft Windows Server: https://www.cyber.gov.au/resources-business-and-government/maintaining-devices-and-systems/system-hardening/hardening-microsoft-windows-server
- Essential Eight Maturity Model: https://www.cyber.gov.au/business-government/asds-cyber-security-frameworks/essential-eight
- Essential Eight to ISM Mapping: https://www.cyber.gov.au/business-government/asds-cyber-security-frameworks/essential-eight/essential-eight-maturity-model-and-ism-mapping
- IRAP Assessors Register: https://www.cyber.gov.au/resources-business-and-government/assessment-and-evaluation-programs/irap/irap-assessors
