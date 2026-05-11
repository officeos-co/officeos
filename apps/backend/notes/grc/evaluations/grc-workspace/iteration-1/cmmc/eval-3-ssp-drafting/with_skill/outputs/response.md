# System Security Plan (SSP) — Practice Section

---

## Practice: IA.L2-3.5.3 — Multifactor Authentication

| Field | Value |
|-------|-------|
| **Practice ID** | IA.L2-3.5.3 |
| **Domain** | IA — Identification and Authentication |
| **CMMC Level** | Level 2 (Advanced) |
| **NIST SP 800-171 Rev 2 Reference** | 3.5.3 |
| **Implementation Status** | MET |

---

### 1. Requirement Statement

> Use multifactor authentication (MFA) for local and network access to privileged accounts and for network access to non-privileged accounts.

This practice requires that all user accounts — privileged and non-privileged alike — authenticate using at least two distinct factors before being granted access to organizational systems or CUI. Local access to privileged accounts and all network access (remote or otherwise) are within scope.

---

### 2. Implementation Description

The organization has fully implemented multifactor authentication across all in-scope systems using **Azure Active Directory (Azure AD)** as the centralized identity provider and **Microsoft Authenticator** as the primary MFA method. MFA enforcement is achieved through **Azure AD Conditional Access policies** that apply organization-wide.

#### 2.1 MFA Scope

| Access Type | Account Type | MFA Required | Enforcement Mechanism |
|-------------|-------------|--------------|----------------------|
| Network access (cloud and on-premises) | Privileged (Global Admins, IT Admins, Security Admins) | Yes | Azure AD Conditional Access — "Require MFA for All Admins" policy |
| Network access (cloud and on-premises) | Non-privileged (standard users, end users) | Yes | Azure AD Conditional Access — "Require MFA for All Users" policy |
| Local access to privileged accounts | Privileged | Yes | Azure AD joined device + Windows Hello for Business (PIN/biometric as second factor) |
| Remote access (VPN, RDP, remote management) | All users | Yes | Azure AD Conditional Access — MFA required as grant control for remote session initiation |

#### 2.2 Authentication Methods

Microsoft Authenticator is the primary MFA method enforced for all users. Supported second-factor methods in order of organizational preference are:

1. **Microsoft Authenticator push notification** (primary) — time-based one-time passcode (TOTP) or number-matching push approval
2. **Windows Hello for Business** — FIPS-compatible biometric or PIN tied to a hardware TPM chip, used for local device authentication
3. **FIDO2 security keys** — hardware security keys (e.g., YubiKey) permitted for privileged administrator accounts
4. **SMS/voice OTP** — explicitly **disabled** via Authentication Methods policy; not permitted as a valid second factor due to known SIM-swapping vulnerabilities and NIST SP 800-63B guidance

#### 2.3 Conditional Access Policy Configuration

MFA enforcement is managed through the following Azure AD Conditional Access policies:

| Policy Name | Users in Scope | Cloud Apps in Scope | Grant Control |
|-------------|---------------|---------------------|---------------|
| CA-001: Require MFA — All Users | All users (including guests with CUI access) | All cloud apps | Require MFA |
| CA-002: Require MFA — Privileged Roles | Global Admins, Privileged Role Admins, Security Admins, Exchange Admins | All cloud apps | Require MFA + Compliant Device |
| CA-003: Require MFA — Remote Access | All users | Microsoft 365, Azure Management, on-premises apps via App Proxy | Require MFA |
| CA-004: Block Legacy Authentication | All users | All cloud apps | Block (legacy auth protocols do not support MFA) |

All Conditional Access policies are set to **"Report-only"** mode during initial deployment and reviewed before enforcing. Production policies are in **"On"** (enforce) mode.

Legacy authentication protocols (IMAP, POP3, SMTP AUTH, Basic Auth) are blocked via CA-004 to prevent MFA bypass.

#### 2.4 Privileged Account-Specific Controls

Privileged accounts (those with Azure AD directory roles or elevated permissions to CUI systems) are subject to additional controls beyond standard user MFA:

- **Privileged Identity Management (PIM)**: All privileged roles are configured as eligible (not permanent). Administrators must activate roles on demand with MFA confirmation and business justification.
- **Compliant device requirement**: Conditional Access policy CA-002 requires privileged users to authenticate from an Intune-enrolled, compliant device in addition to completing MFA.
- **Break-glass accounts**: Two emergency access accounts are maintained per Microsoft guidance. These accounts are excluded from Conditional Access policies, secured with hardware FIDO2 keys, monitored with Azure AD audit alerts, and documented in the Break-Glass Account Procedure.

#### 2.5 On-Premises and Hybrid Systems

For on-premises systems that are Azure AD-joined or hybrid-joined:

- **Azure AD Connect** synchronizes on-premises Active Directory accounts to Azure AD, enabling Conditional Access and MFA for hybrid authentication flows.
- **Azure AD Application Proxy** is used to publish on-premises legacy applications. MFA is enforced at the Application Proxy pre-authentication layer before any session is established with the on-premises system.
- **Windows Hello for Business (Hybrid Key Trust)** provides MFA-equivalent authentication for local Windows device logon, backed by the device's TPM and tied to the user's Azure AD identity.

---

### 3. Responsible Roles

| Role | Responsibility |
|------|---------------|
| **Information System Security Officer (ISSO)** | Owns the MFA policy, reviews Conditional Access configurations quarterly, approves exceptions, and reports MFA compliance metrics to senior leadership. |
| **Identity and Access Management (IAM) Administrator** | Configures and maintains Azure AD Conditional Access policies, Authentication Methods policies, and PIM settings. Reviews MFA registration reports and remediates gaps. |
| **IT Systems Administrator** | Manages Azure AD Connect, on-premises hybrid join configurations, and device compliance policies in Microsoft Intune. |
| **Help Desk / IT Support** | Processes MFA reset requests following the MFA Self-Service Reset and Help Desk Verification Procedure. Escalates suspected account compromise to the ISSO. |
| **End Users / All Personnel** | Required to register at least two MFA methods within 14 days of account provisioning. Must not share authentication credentials or approve unexpected MFA prompts. |
| **Security Operations Center (SOC)** | Monitors Azure AD Sign-in Logs and Identity Protection alerts for MFA bypass attempts, impossible travel detections, and sign-in risk events. |

---

### 4. Evidence Artifacts

The following artifacts serve as evidence of IA.L2-3.5.3 implementation and should be collected and maintained in the organization's evidence repository:

| Artifact | Description | Location / Owner | Review Frequency |
|----------|-------------|-----------------|-----------------|
| **MFA Policy** | Written organizational policy requiring MFA for all system access, including scope, approved methods, and exception handling. | Policy Management System / ISSO | Annual |
| **Conditional Access Policy Exports** | JSON or screenshot exports of all Conditional Access policies from the Azure AD portal, showing policy scope, conditions, and grant controls. | Azure AD Admin Center / IAM Admin | Quarterly |
| **Authentication Methods Policy Screenshot** | Screenshot from Azure AD > Security > Authentication Methods showing Microsoft Authenticator enabled and SMS/voice disabled. | Azure AD Admin Center / IAM Admin | Quarterly |
| **MFA Registration Report** | Azure AD report showing percentage of users registered for MFA; any accounts not yet registered. | Azure AD > Reports > Authentication Methods / IAM Admin | Monthly |
| **Sign-in Logs (MFA Events)** | Filtered Azure AD sign-in logs demonstrating MFA challenges and completions for all user account types. | Azure Monitor / Log Analytics / SOC | On-demand / 90-day retention minimum |
| **PIM Configuration Screenshots** | Evidence that privileged roles are configured as eligible (not permanent) and require MFA activation. | Azure AD PIM / IAM Admin | Quarterly |
| **Conditional Access Named Locations / Exclusions** | Documentation of any Conditional Access exclusions (e.g., break-glass accounts), with justification and compensating controls. | ISSO / IAM Admin | Semi-annual |
| **Break-Glass Account Procedure** | Documented procedure for emergency access accounts excluded from MFA Conditional Access policy, including monitoring and periodic access review. | Policy Management System / ISSO | Annual |
| **Intune Device Compliance Policy** | Evidence that compliant-device requirements are enforced for privileged role access. | Microsoft Intune / IT Admin | Quarterly |
| **MFA User Awareness Training Records** | Training completion records showing users have been trained on MFA enrollment, recognizing MFA fatigue attacks, and reporting suspicious prompts. | LMS / AT.L2-3.2.1 evidence package | Annual |

---

### 5. System Interconnections Relevant to MFA

The following system interconnections are in scope for IA.L2-3.5.3 and rely on or interact with the Azure AD MFA enforcement infrastructure:

| Connected System | Connection Type | MFA Enforcement Method | Notes |
|-----------------|----------------|----------------------|-------|
| **Microsoft 365 (Exchange Online, SharePoint, Teams)** | Cloud SaaS — Azure AD OAuth/OIDC | Conditional Access CA-001 and CA-003 | Primary platform for CUI processing and storage; all access gated by MFA. |
| **Azure Government / Azure Commercial** | Cloud IaaS/PaaS — Azure AD | Conditional Access CA-002 (admin) and CA-001 (users) | Azure subscriptions used for CUI workloads must be in scope. If CUI is stored in Azure, confirm FedRAMP Moderate authorization status. |
| **On-Premises Active Directory Domain** | Hybrid — Azure AD Connect (password hash sync or pass-through auth) | MFA enforced via Azure AD Conditional Access at pre-authentication | Domain controllers are in scope for the CUI boundary; hybrid join ensures MFA is enforced before on-premises resource access. |
| **Corporate VPN** | Network access — RADIUS / Azure AD NPS Extension | NPS Extension triggers Azure AD MFA challenge for RADIUS authentication | VPN is the primary remote access path for on-premises CUI systems. NPS Extension must be deployed and verified. |
| **On-Premises Applications (via Azure AD App Proxy)** | Hybrid — Azure AD Application Proxy | Pre-authentication MFA enforced by App Proxy before session forwarding | Legacy web applications that cannot natively integrate with MFA are published through App Proxy to enforce MFA. |
| **Microsoft Intune (MDM/MAM)** | Cloud SaaS — Azure AD | Device enrollment requires Azure AD authentication with MFA | Device compliance status is used as a Conditional Access signal for privileged account access (CA-002). |
| **Azure Active Directory Identity Protection** | Internal — Azure AD | Risk-based Conditional Access triggers step-up MFA or blocks sign-in based on real-time risk score | Integration enhances MFA by dynamically requiring MFA when risky sign-in conditions are detected. |
| **Microsoft Sentinel / Azure Monitor** | Log aggregation — Azure AD Diagnostic Settings | N/A (monitoring, not authentication) | Sign-in logs with MFA event details are forwarded to Sentinel/Log Analytics for SOC monitoring and audit log retention (AU.L2-3.3.1). |

---

### 6. Related Practices and Dependencies

| Practice ID | Relationship |
|-------------|-------------|
| **IA.L1-3.5.1** | Foundational — all users and devices must be identified before MFA can be applied. |
| **IA.L1-3.5.2** | Foundational — identity verification is the first factor; MFA adds the second. |
| **IA.L2-3.5.4** | Replay-resistant authentication — Microsoft Authenticator number-matching and FIDO2 keys satisfy this requirement alongside MFA. |
| **AC.L2-3.1.12** | Remote access monitoring — MFA is enforced at the point of remote session initiation; Conditional Access logs provide session monitoring evidence. |
| **AC.L2-3.1.5** | Least privilege — PIM for privileged role activation complements MFA by limiting standing privileged access. |
| **MA.L2-3.7.5** | Remote maintenance sessions also require MFA; covered by the same Conditional Access policies. |
| **AU.L2-3.3.1** | Azure AD Sign-in Logs provide the audit trail for MFA events and must be retained per the organization's log retention policy. |
| **AT.L2-3.2.1** | Users must be trained to recognize and report MFA fatigue attacks (unsolicited push notifications). |

---

### 7. Exceptions and Special Circumstances

| Exception | Justification | Compensating Controls | Approval Authority | Review Date |
|-----------|--------------|----------------------|-------------------|-------------|
| Break-glass emergency accounts (2 accounts) | Required for emergency access if Azure AD becomes unavailable; excluded from CA policies per Microsoft guidance. | Hardware FIDO2 keys stored in physical safe; accounts monitored with Azure Sentinel alerts on any sign-in activity; quarterly access review. | ISSO + CIO | Semi-annual |
| Service accounts / non-interactive identities | Service accounts do not support interactive MFA flows. | Managed Identities (no credentials) or certificate-based authentication used wherever possible. Service accounts are restricted to specific IP ranges and resource scopes. Password-based service accounts are reviewed quarterly and use randomly generated 64-character passwords. | IAM Admin + ISSO | Quarterly |

---

### 8. Assessment Notes

This practice is identified as a **high-priority** control and one of the **most commonly failed practices** in CMMC Level 2 assessments. Assessors (C3PAO) will specifically verify:

1. That MFA is enforced for **all** users, not only administrators.
2. That **legacy authentication is blocked** — any Conditional Access exclusion that permits Basic Auth or legacy protocols represents a gap.
3. That MFA is required for **local privileged access**, not only network/remote access — Windows Hello for Business or equivalent satisfies this requirement.
4. That **service account exceptions are documented** with compensating controls.
5. That **MFA registration is complete** (100% or with a documented remediation plan for non-registered users).

SPRS impact if NOT MET: IA.L2-3.5.3 carries a **5-point deduction** from the SPRS score, the maximum weight for a single practice, reflecting its criticality.

---

*SSP Section prepared: 2026-04-25*
*Prepared by: Information System Security Officer (ISSO)*
*Review cycle: Annual or upon significant system change*
*Document classification: CUI — For Official Use Only*
