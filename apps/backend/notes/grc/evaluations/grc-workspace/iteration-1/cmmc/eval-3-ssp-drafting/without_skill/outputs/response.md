# System Security Plan (SSP) Section
## CMMC Practice IA.L2-3.5.3 — Multifactor Authentication (MFA)

---

### 1. Practice Reference

| Field | Value |
|---|---|
| Practice ID | IA.L2-3.5.3 |
| Domain | Identification and Authentication (IA) |
| CMMC Level | Level 2 |
| Source Requirement | NIST SP 800-171 Rev 2, 3.5.3 |
| Requirement Statement | Use multifactor authentication for local and network access to privileged accounts and for network access to non-privileged accounts. |

---

### 2. Implementation Description

#### 2.1 Overview

The organization employs Azure Active Directory (Azure AD) as its centralized identity and access management (IAM) platform. Multifactor authentication is enforced for all user accounts — both privileged and non-privileged — accessing organizational systems, applications, and data. Microsoft Authenticator serves as the primary MFA application, delivering push notifications, one-time passcodes (OTP), and passwordless phone sign-in capabilities. MFA enforcement is achieved through Azure AD Conditional Access policies, which evaluate authentication context at the time of each sign-in and require a second factor before granting access.

#### 2.2 Scope of MFA Enforcement

MFA is required for all of the following access scenarios:

- **Local and network access to privileged accounts**: All accounts with administrative, elevated, or privileged roles (e.g., Global Administrator, Security Administrator, Privileged Role Administrator, system owners, IT administrators) must authenticate with MFA for every sign-in session, regardless of device, location, or network.
- **Network access to non-privileged accounts**: All standard user accounts accessing organizational resources over any network connection — including remote access, VPN, cloud services, and internal applications — are required to complete MFA at authentication.
- **Access to Controlled Unclassified Information (CUI) systems**: Any system, application, or data repository that stores, processes, or transmits CUI is covered under MFA Conditional Access policies.
- **Access to Microsoft 365 and Azure resources**: All SaaS and IaaS services integrated with Azure AD are governed by the same Conditional Access framework.

#### 2.3 Technical Implementation

**Azure AD Conditional Access Policies**

The following Conditional Access policies are configured and active:

1. **Policy: Require MFA — All Users, All Applications**
   - Users: All users (includes privileged and non-privileged)
   - Cloud Apps: All cloud apps
   - Conditions: Any location, any device platform
   - Grant Control: Require multifactor authentication
   - Status: Enabled (Report-only mode is NOT used for CUI-scope systems)

2. **Policy: Require MFA — Privileged Roles**
   - Users: Directory roles — Global Administrator, Privileged Role Administrator, Security Administrator, Exchange Administrator, SharePoint Administrator, and other privileged roles as defined in the Privileged Account Register
   - Cloud Apps: All cloud apps
   - Grant Control: Require multifactor authentication + Require compliant device (where applicable)
   - Status: Enabled

3. **Policy: Block Legacy Authentication**
   - Conditions: Client apps using legacy authentication protocols (Basic Auth, SMTP Auth, POP, IMAP)
   - Grant Control: Block
   - Rationale: Legacy authentication protocols cannot enforce MFA and represent a bypass vector; blocking is required to ensure MFA coverage is comprehensive.

**Microsoft Authenticator Configuration**

- Microsoft Authenticator is deployed as the organization's approved second factor.
- Approved authentication methods include:
  - Microsoft Authenticator push notification (primary method)
  - Time-based one-time passcode (TOTP) via Microsoft Authenticator
  - Passwordless phone sign-in (for eligible devices)
- SMS/voice OTP may be permitted only as a fallback for account recovery, subject to approval by the Identity and Access Management (IAM) team, and is not accepted as a primary MFA method for CUI access.
- Hardware FIDO2 security keys (e.g., YubiKey) are approved for privileged administrators and users requiring phishing-resistant MFA.

**Authentication Method Policies**

Authentication method policies in Azure AD are configured to:

- Enable Microsoft Authenticator for all users.
- Disable or restrict weaker methods (SMS, voice call) for standard authentication flows.
- Enforce number matching and additional context display in Authenticator push notifications to mitigate MFA fatigue attacks.

**Azure AD Identity Protection**

Azure AD Identity Protection is enabled to:

- Detect risky sign-ins and require step-up MFA or block access based on risk level.
- Generate alerts for anomalous authentication activity.
- Feed risk signals into Conditional Access for dynamic policy enforcement.

#### 2.4 Account Provisioning and MFA Registration

- All new users are required to register for MFA during the onboarding process, prior to being granted access to any organizational systems.
- MFA registration is enforced via a dedicated Conditional Access policy that redirects unregistered users to the MFA registration portal.
- The IAM team reviews MFA registration completeness on a monthly basis through Azure AD reporting.
- Users who lose access to their registered MFA device must follow the Identity Verification and Account Recovery Procedure (referenced in Section 6 — Related Documents) before MFA can be re-registered.

---

### 3. Responsible Roles

| Role | Responsibility |
|---|---|
| **System Owner** | Overall accountability for ensuring MFA is implemented and maintained across all systems within scope. Approves exceptions and reviews MFA compliance reports quarterly. |
| **Information System Security Officer (ISSO)** | Monitors MFA policy configurations, reviews audit logs for authentication failures, coordinates remediation of MFA gaps, and maintains SSP accuracy. |
| **Identity and Access Management (IAM) Team** | Owns Conditional Access policy configuration, manages authentication method policies in Azure AD, administers MFA registration workflows, processes account recovery requests, and conducts monthly MFA registration compliance reviews. |
| **IT Help Desk** | Provides first-line support for MFA issues (device lost/replaced, lockouts). Escalates account recovery to the IAM team following the Identity Verification Procedure. |
| **Security Operations Center (SOC)** | Monitors Azure AD Identity Protection alerts and sign-in risk events. Investigates and responds to MFA-related security incidents. |
| **All Users** | Required to register for MFA prior to system access, maintain their registered MFA device(s), and report lost or compromised devices immediately to the Help Desk. |
| **Microsoft (External Cloud Provider)** | Operates and maintains the Azure AD platform and Microsoft Authenticator service. Governed by the Microsoft Online Services Agreement and Data Processing Addendum. |

---

### 4. Evidence Artifacts

The following artifacts serve as evidence of implementation and are available for assessment:

| Artifact | Description | Location / Owner |
|---|---|---|
| Azure AD Conditional Access Policy Exports | JSON/screenshot exports of all active Conditional Access policies, demonstrating MFA enforcement scope, user and app targeting, and grant controls. | Azure AD Portal / IAM Team |
| Azure AD Sign-In Logs | Logs showing MFA challenge events, success/failure records, and authentication method used per sign-in. Retained for a minimum of 90 days (extended via Log Analytics Workspace or SIEM). | Azure AD / SOC |
| MFA Registration Status Report | Azure AD report showing percentage of users registered for MFA, broken down by authentication method. Generated monthly. | Azure AD / IAM Team |
| Authentication Methods Policy Configuration | Screenshots or exports of the Authentication Methods policy settings in Azure AD, showing enabled/disabled methods and targeting. | Azure AD / IAM Team |
| Legacy Authentication Block Evidence | Sign-in logs or Workbook reports confirming that legacy authentication sign-ins are being blocked. | Azure AD / IAM Team |
| Identity Protection Risk Detections Report | Report of risky sign-ins and users detected by Azure AD Identity Protection, with remediation actions taken. | Azure AD / SOC |
| Privileged Account Register | List of accounts assigned privileged roles in Azure AD, used to verify MFA enforcement scope for privileged access. | IAM Team |
| MFA Exception Log | Log of any approved temporary MFA exceptions, including justification, approver, duration, and compensating controls. | ISSO |
| User MFA Onboarding Procedure | Documented procedure for MFA registration during onboarding, including identity verification steps. | IAM Team / HR |
| Account Recovery Procedure | Documented procedure for MFA re-registration following device loss or compromise, including identity verification requirements. | IAM Team |
| Incident Response Records | Any tickets or records related to MFA bypass attempts, failures, or incidents. | SOC / Help Desk |

---

### 5. System Interconnections Relevant to MFA

The following system interconnections are relevant to the MFA implementation and must be considered when assessing coverage:

| Connected System / Service | Connection Type | MFA Applicability | Notes |
|---|---|---|---|
| **Microsoft 365 (Exchange Online, SharePoint, Teams)** | Azure AD federated SSO | Covered by Conditional Access MFA policies | All M365 workloads are registered as cloud apps in Azure AD and subject to MFA enforcement. Legacy protocols (Basic Auth) are blocked. |
| **Azure Portal and Azure Resource Manager** | Azure AD authentication | Covered; privileged MFA policy applied | Administrative access to Azure subscriptions requires MFA. Privileged Identity Management (PIM) used for just-in-time privileged role activation, which also requires MFA. |
| **On-Premises Active Directory (AD DS)** | Azure AD Connect hybrid identity | Partially covered — see notes | Hybrid joined users authenticate via Azure AD for cloud resources (MFA enforced). Pass-through authentication or federation must be configured so that on-premises interactive sign-ins also route through Azure AD MFA where technically feasible. On-premises RDP and local console access are addressed separately through Windows Hello for Business or smart card requirements. |
| **VPN Gateway (Azure VPN / Third-Party VPN)** | SAML/RADIUS integration with Azure AD | Covered if integrated with Azure AD | VPN authentication must be integrated with Azure AD (via RADIUS/NPS extension or SAML) to inherit Conditional Access MFA enforcement. If using a legacy RADIUS-only VPN, Azure AD MFA NPS Extension must be deployed. |
| **Third-Party SaaS Applications (CUI-scope)** | Azure AD federated SSO (SAML/OIDC) | Covered by Conditional Access | All CUI-scope SaaS applications must be registered in Azure AD as Enterprise Applications and scoped within the "All cloud apps" Conditional Access policy. Applications not integrated with Azure AD are out-of-scope and must be tracked as a gap. |
| **Developer and CI/CD Platforms (e.g., GitHub, Azure DevOps)** | Azure AD SSO | Covered if SSO is enforced | Organizations must enforce Azure AD SSO for developer tooling and disable native username/password authentication to ensure MFA coverage. Service accounts and managed identities used in pipelines must be reviewed separately. |
| **Service Accounts and Non-Interactive Identities** | Azure AD Workload Identities / Managed Identities | Not subject to IA.L2-3.5.3 per NIST 800-171 guidance | Service accounts that perform automated, non-interactive functions are not subject to MFA under 3.5.3. These accounts must be tightly scoped, use managed identities where possible, and be governed under the Least Privilege and Service Account Management policies. Human accounts must not be used for automated processes. |
| **Microsoft Authenticator App (Mobile)** | End-user mobile device (iOS/Android) | MFA delivery channel | Device must be enrolled in Intune (or registered in Azure AD) for Microsoft Authenticator push notifications. Device compliance policy may be required as an additional Conditional Access grant control for privileged access. |

---

### 6. Related Documents and Policies

- Identity and Access Management Policy
- Privileged Access Management Policy and Procedures
- Account Management Policy
- Identity Verification and Account Recovery Procedure
- Incident Response Plan (MFA Incident Scenarios)
- Azure AD Tenant Configuration Baseline
- NIST SP 800-171 Rev 2, Control 3.5.3
- NIST SP 800-63B, Authenticator Assurance Levels (AAL)
- CMMC Assessment Guide, Level 2, Domain IA

---

### 7. Control Effectiveness Summary

| Assessment Dimension | Status | Notes |
|---|---|---|
| MFA enforced for privileged network access | Implemented | Conditional Access policy in place; confirmed via sign-in logs. |
| MFA enforced for non-privileged network access | Implemented | All-users, all-apps policy active. |
| MFA enforced for local privileged access | Partially Implemented | On-premises local console access addressed via Windows Hello for Business; hybrid scenarios require verification. |
| Legacy authentication blocked | Implemented | Block policy active; confirmed via sign-in logs. |
| MFA registration compliance monitored | Implemented | Monthly review by IAM team. |
| Phishing-resistant MFA available for high-value accounts | Implemented | FIDO2 keys approved; number matching enabled in Authenticator. |

---

*Document prepared by: Information System Security Officer (ISSO)*
*Review cycle: Annual or upon significant system change*
*Last reviewed: 2026-04-25*
