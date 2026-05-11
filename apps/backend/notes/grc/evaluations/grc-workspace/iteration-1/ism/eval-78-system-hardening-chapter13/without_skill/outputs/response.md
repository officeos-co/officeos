ISM Chapter 13 System Hardening: Requirements and Evidence for Windows Server 2022

Overview

The Australian Government Information Security Manual (ISM), published by the Australian Signals Directorate (ASD), dedicates Chapter 13 to System Hardening. This chapter establishes baseline security controls that reduce the attack surface of operating systems and applications. For a Windows Server 2022 deployment in a state government environment, Chapter 13 is the primary technical hardening reference. It aligns closely with ASD's Hardening guidance and the Essential Eight maturity model.

The controls in Chapter 13 are grouped across several domains. Below is a structured walkthrough of each major domain, what the ISM requires, and what evidence an assessor would expect to see.

---

1. Operating System Hardening

ISM Requirements

- Use the latest stable, supported version of the operating system. Windows Server 2022 satisfies this at present, but patching must be kept current.
- Apply vendor-supplied patches within defined timeframes. For internet-facing systems, critical patches must be applied within 48 hours; for other systems, within one month (aligned with Essential Eight Patch OS control).
- Remove or disable all unnecessary operating system components, features, and services. This includes disabling legacy protocols such as SMBv1, NetBIOS over TCP/IP, and LLMNR where not required.
- Configure the OS in accordance with a hardening standard. ASD's Windows Server 2022 hardening guide and the corresponding ACSC guidance provide the baseline.
- Disable or restrict AutoRun and AutoPlay.
- Ensure the OS boots from internal storage only, and configure BIOS/UEFI settings to prevent booting from removable media.
- Enable Secure Boot and, where supported, Trusted Platform Module (TPM) 2.0 integration.

Evidence to Collect

- Patch management records showing patch dates, patch levels, and compliance against the 48-hour and one-month thresholds.
- System build documentation or baseline configuration record referencing the applicable hardening standard.
- Screenshots or exported Group Policy results (rsop.msc or gpresult /h) showing disabled legacy features and services.
- Windows Features list (Get-WindowsFeature) showing only required roles and features are installed.
- BIOS/UEFI configuration screenshots or vendor attestation confirming Secure Boot enabled and boot order restrictions.
- Results of a CIS Benchmark or ACSC hardening assessment tool run against the server.

---

2. Application Hardening

ISM Requirements

- Only approved and required applications are installed. Maintain a documented software register.
- Application whitelisting (now called application control) must be implemented. ASD mandates this as part of the Essential Eight. On Windows Server 2022, this is achieved via Windows Defender Application Control (WDAC) or AppLocker.
- Web browsers and PDF viewers are hardened. While less critical on a server, any browser-accessible management interfaces must use hardened configurations.
- Disable or remove web-based administrative interfaces if not required.
- Office productivity software macros must be disabled or restricted; only trusted, signed macros from approved locations should be permitted if needed.
- Uninstall or disable software development tools (compilers, interpreters) unless the server's function requires them.
- Ensure all installed applications are patched. Application patches, particularly for internet-facing components (IIS, .NET Framework, SQL Server), must be applied within 48 hours for critical patches affecting internet-facing services.

Evidence to Collect

- Installed software inventory (Get-WmiObject Win32_Product or similar) compared against the approved software register.
- Application control policy export (WDAC XML policy file or AppLocker GPO export) and evidence it is in enforce mode (not audit mode).
- Event logs showing application control blocks, demonstrating the policy is active.
- Patch records for all installed applications.
- Group Policy or registry settings showing macro restrictions.

---

3. Authentication Hardening

ISM Requirements

- Enforce strong password policies: minimum length of at least 14 characters for privileged accounts, complexity requirements, and password history.
- Multi-factor authentication (MFA) must be used for all remote access and for all privileged account logons. This is an Essential Eight requirement and is reflected in ISM controls.
- Privileged accounts must not be used for standard tasks such as web browsing or email.
- Local administrator accounts must be managed. The built-in Administrator account should be renamed or disabled; use LAPS (Local Administrator Password Solution) for unique, rotating local admin passwords across servers.
- Service accounts must use unique, strong passwords and be granted only the minimum privileges required.
- Implement account lockout policies to mitigate brute-force attacks.
- Disable or restrict the use of NTLM authentication where possible; prefer Kerberos.
- Restrict which accounts can log on to the server interactively or via Remote Desktop.

Evidence to Collect

- Password policy export (Group Policy or fine-grained password policy) showing minimum length, complexity, history, and lockout thresholds.
- Documentation of MFA solution deployed and scope (which accounts and access paths are covered).
- LAPS deployment confirmation (registry keys, GPO, or Intune policy showing LAPS is enabled and password rotation is configured).
- Privileged Access Workstation (PAW) policy or documentation showing privileged accounts are separated.
- User Rights Assignment export (secedit or Group Policy) showing logon rights are restricted.
- Evidence that NTLM restrictions are in place where applicable (e.g., Group Policy: Network Security: Restrict NTLM settings).

---

4. Network Hardening

ISM Requirements

- The Windows Firewall must be enabled on all profiles (Domain, Private, Public) and configured to block inbound connections unless explicitly permitted.
- Only required network ports and protocols are open. Unnecessary services must be stopped and their associated firewall rules removed.
- Remote management must use encrypted channels only. RDP must require Network Level Authentication (NLA). Prefer Windows Admin Center, SSH (where applicable), or other hardened management interfaces over older protocols.
- Disable server message block (SMBv1). Use SMBv3 with encryption where file sharing is required.
- Disable LLMNR and NetBIOS name resolution via Group Policy to prevent name poisoning attacks.
- Restrict Remote Desktop Protocol access to specific IP ranges or jump servers.
- Implement IPsec or equivalent where lateral movement between servers must be restricted.

Evidence to Collect

- Windows Firewall policy export or Group Policy showing rules, default deny posture, and all profiles enabled.
- Port scan results (e.g., Nmap output) confirming only required ports are exposed.
- NLA requirement setting confirmed in Group Policy (Computer Configuration > Administrative Templates > Windows Components > Remote Desktop Services).
- SMBv1 disabled confirmed via Get-SmbServerConfiguration or Group Policy.
- LLMNR and NetBIOS disabled confirmed via Group Policy settings and registry values.
- Network diagram showing where the server sits and what firewall or segmentation controls exist at the network perimeter.

---

5. Logging and Auditing

ISM Requirements

- Enable Windows Security auditing for the following event categories at minimum: Account Logon, Account Management, Logon/Logoff, Object Access (for sensitive resources), Policy Change, Privilege Use, and System events.
- Forward logs to a centralised Security Information and Event Management (SIEM) system or a protected, centralised log repository.
- Log retention must meet the minimum period specified in the ISM — typically at least 18 months for government systems, with 90 days available online.
- Protect audit logs from unauthorised modification or deletion.
- Enable PowerShell script block logging, module logging, and transcription to detect and investigate malicious PowerShell use.

Evidence to Collect

- Audit policy output (auditpol /get /category:*) confirming all required categories are enabled.
- Screenshot or configuration export from the SIEM showing the server is an active log source.
- Log retention policy documentation and storage configuration showing 18-month retention.
- PowerShell logging Group Policy settings (Computer Configuration > Administrative Templates > Windows Components > Windows PowerShell).
- Sample log entries demonstrating log forwarding is operational (e.g., syslog or Windows Event Forwarding confirmation).

---

6. Restricting Administrative Privileges

ISM Requirements

- Apply the principle of least privilege. Users and services are granted only the minimum access required.
- Privileged accounts must be dedicated accounts, separate from standard user accounts.
- Time-based or just-in-time (JIT) privileged access should be considered for administrative access.
- Privileged access must be reviewed regularly; dormant or unnecessary privileged accounts must be removed.
- Domain Administrator rights must not be used for day-to-day server administration where server-local administrator rights are sufficient.
- Restrict the ability to install software to administrators only.

Evidence to Collect

- Export of local Administrators group membership on the server.
- Active Directory privileged group membership reports (Domain Admins, Enterprise Admins, Server Operators, etc.).
- Evidence of periodic access reviews (meeting minutes, sign-off records, or automated review reports).
- Documentation of JIT or PAM (Privileged Access Management) solution if deployed.
- Group Policy showing software installation restricted to administrators.

---

7. Virtualisation Hardening (where applicable)

If Windows Server 2022 is deployed as a virtual machine or as a Hyper-V host, additional ISM controls apply.

ISM Requirements

- The hypervisor and virtualisation platform must be kept patched and hardened.
- Virtual machine escape mitigations must be in place. Enable Virtualisation Based Security (VBS) and Hypervisor-Protected Code Integrity (HVCI) where supported.
- Restrict access to the hypervisor management plane; do not use hypervisor management from general-purpose workstations.
- Separate guest VMs of different sensitivity classifications if they process data at different classification levels.
- Ensure virtual networks are segmented equivalently to physical networks.

Evidence to Collect

- Hypervisor patch level and version documentation.
- VBS and HVCI enabled confirmed via msinfo32 or Get-CimInstance -ClassName Win32_DeviceGuard.
- Network segmentation diagram for virtual switch configuration.
- Hypervisor management access control policy.

---

8. Firmware and Supply Chain

ISM Requirements

- Ensure server firmware (BIOS/UEFI, BMC/iDRAC/iLO) is kept current and sourced from trusted vendor channels.
- Disable unused firmware interfaces (e.g., disable unused USB ports, disable legacy PXE boot if not needed).
- Where hardware supports it, enable firmware integrity verification.
- Document the supply chain for hardware procurement to ensure equipment is from reputable sources.

Evidence to Collect

- Firmware version records and evidence of firmware update process.
- BIOS/UEFI configuration screenshots showing disabled unused interfaces.
- Hardware procurement documentation confirming authorised supply channels.

---

Summary: Evidence Package for an ISM Chapter 13 Assessment

The following is a consolidated list of evidence artefacts an assessor will typically request for a Windows Server 2022 system hardening assessment:

1. System baseline and build documentation referencing the applicable hardening standard (ACSC Windows Server 2022 hardening guide or CIS Benchmark).
2. Group Policy results report (gpresult /h) or exported GPO settings for the server.
3. Installed roles and features list (Get-WindowsFeature).
4. Installed software inventory compared against approved software register.
5. Patch management records (OS and application patches) with dates and approval records.
6. Application control policy export (WDAC or AppLocker) confirming enforce mode.
7. Application control event logs showing the policy is active.
8. Password and lockout policy configuration export.
9. MFA deployment documentation and scope.
10. LAPS configuration confirmation.
11. Local Administrators group membership.
12. Privileged group membership reports and access review records.
13. Windows Firewall policy export.
14. Port scan results confirming only required ports are open.
15. SMBv1 disabled confirmation.
16. LLMNR and NetBIOS disabled confirmation.
17. Audit policy output (auditpol /get /category:*).
18. PowerShell logging Group Policy settings.
19. SIEM log forwarding confirmation and log retention policy.
20. Secure Boot and TPM configuration evidence.
21. VBS and HVCI enabled (if virtualisation hardening applies).
22. Firmware version and update records.

---

Practical Recommendations for Your Team

Prior to assessment, run the ASD-provided hardening scripts or a CIS-CAT scan against the server and remediate all findings. The ACSC publishes Windows Server 2022 hardening guidance with accompanying Group Policy templates that can be applied directly. Ensure your Group Policy Objects are documented and that each policy setting maps back to a specific ISM control number; this simplifies the assessment conversation significantly.

Assessors will typically check configuration both through documentation and through live technical inspection. Be prepared to demonstrate settings interactively on the server, not just via screenshots, as screenshots can be pre-staged. Ensure logging is operational and demonstrably forwarding to a centralised store before the assessment date.

Maintain a controls register or compliance matrix that maps each ISM Chapter 13 control identifier to your implementation approach, the responsible team member, and the specific evidence artefact that demonstrates compliance. This register becomes your primary assessment artefact and significantly reduces the time spent during an on-site review.
