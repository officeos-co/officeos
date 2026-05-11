# Access Control Policy

| | |
|---|---|
| **Document Title** | Access Control Policy |
| **Policy ID** | POL-AC-001 |
| **Version** | 1.0 |
| **Effective Date** | [DATE] |
| **Last Reviewed** | [DATE] |
| **Next Review Date** | [DATE + 1 year] |
| **Policy Owner** | Chief Information Security Officer (CISO) / Head of Security |
| **Approved By** | [CEO / CTO Name, Title] |
| **TSC Criteria Addressed** | CC6.1, CC6.2, CC6.3, CC6.6, CC6.7, CC6.8 |

---

## 1. Purpose

This policy establishes requirements for managing logical access to [Company Name]'s information systems, applications, cloud infrastructure, and data. The objective is to ensure that access to systems and data is granted only to authorized individuals, based on business need, and is revoked promptly when no longer required — thereby protecting the confidentiality, integrity, and availability of company and customer information.

This policy supports compliance with the AICPA Trust Services Criteria for Security, specifically Common Criteria CC6 (Logical and Physical Access Controls).

---

## 2. Scope

This policy applies to:

- **All employees, contractors, consultants, and temporary workers** of [Company Name]
- **All information systems, applications, and cloud infrastructure** owned, operated, or managed by [Company Name]
- **Third-party vendors** with access to [Company Name] systems or customer data (subject to Vendor Management Policy)
- **All environments:** production, staging, development, and corporate systems

This policy specifically covers access to:
- Identity provider: Okta (SSO and MFA)
- Cloud infrastructure: AWS accounts and services
- Source code: GitHub organization
- Internal applications, SaaS tools, and databases integrated with Okta

---

## 3. Roles and Responsibilities

| Role | Responsibility |
|---|---|
| **CISO / Head of Security** | Policy owner; approves exceptions; oversees access review process |
| **IT/Security Team** | Administers Okta, IAM, and GitHub access; executes provisioning and deprovisioning |
| **People/HR Team** | Initiates onboarding and offboarding requests; communicates termination events |
| **Managers / Team Leads** | Approve access requests for their team members; participate in access reviews |
| **All Employees** | Adhere to this policy; report unauthorized access or suspected compromise |

---

## 4. Access Management Principles

### 4.1 Least Privilege

Access to systems, applications, and data is granted based on the **principle of least privilege** — users receive only the minimum access necessary to perform their job function. Broad or administrative permissions are not granted by default and require additional justification and approval.

*This satisfies CC6.3.*

### 4.2 Need-to-Know

Access to sensitive or confidential data is limited to individuals who have a documented business need. Access to customer data is restricted to personnel whose job functions require it (e.g., customer support, engineering on-call).

### 4.3 Separation of Duties

Where feasible, duties are separated to prevent any single individual from having unchecked control over critical functions. Examples include:
- Engineers cannot both write and approve their own production code changes
- Finance approvals and payment execution are separated

### 4.4 Unique Accounts

All users must have a unique, individual account. Shared accounts are prohibited except for documented service accounts, which must be approved by the Security team and have a designated owner.

---

## 5. Identity and Authentication

### 5.1 Single Sign-On (SSO)

Okta is the authoritative identity provider for [Company Name]. All employees must authenticate to company applications through Okta SSO. Direct (non-SSO) credentials to applications are prohibited unless technically unavoidable and documented as an exception.

*This satisfies CC6.1, CC6.6.*

### 5.2 Multi-Factor Authentication (MFA)

MFA is **mandatory** for all users. MFA is enforced at the Okta level and applies to all Okta-integrated applications, including AWS and GitHub.

- Employees must enroll in MFA within 24 hours of account creation
- Phishing-resistant MFA (FIDO2/WebAuthn, hardware keys) is required for privileged accounts (Okta admin, AWS admin, GitHub org admin)
- SMS-based MFA is not approved for privileged access
- MFA cannot be bypassed or self-removed without Security team approval

*This satisfies CC6.1, CC6.6.*

### 5.3 Password Requirements

Where passwords are used:
- Minimum length: 14 characters
- Complexity: Must include uppercase, lowercase, number, and special character
- Passwords must not be reused (minimum 12-password history)
- Passwords must not be shared with others
- Service account passwords must be stored in the company-approved secrets manager (e.g., AWS Secrets Manager, 1Password)

### 5.4 Session Management

- Idle session timeout: 60 minutes for standard sessions; 15 minutes for sessions with access to sensitive data
- Session timeouts are enforced via Okta policy
- Users must lock their screen when leaving a workstation unattended

---

## 6. Access Provisioning

### 6.1 Onboarding and New Access Requests

Access is provisioned through a formal request and approval process:

1. **Request submission:** New hire access is initiated by the HR/People team via [ticketing system, e.g., Jira] as part of the onboarding workflow. Additional access for existing employees requires a request ticket submitted to the IT/Security team.
2. **Manager approval:** The employee's direct manager must approve the access request in the ticketing system before provisioning begins.
3. **Security team review:** Requests for privileged access (admin rights, production database access, security tool access) require additional approval from the CISO or Security team lead.
4. **Provisioning:** The IT/Security team provisions access via Okta (groups and application assignments), GitHub (team membership), and AWS (IAM role assignment).
5. **Documentation:** All provisioning actions are recorded in the ticketing system and retained for audit purposes.

Standard provisioning SLA: within 1 business day of approval.

*This satisfies CC6.1, CC6.2.*

### 6.2 Privileged Access

Privileged access (administrative access to Okta, AWS root/admin roles, GitHub organization admin) is granted only when operationally necessary and must be:

- Justified and approved by the CISO
- Limited in duration (time-boxed) where possible
- Subject to enhanced MFA requirements (FIDO2/hardware key)
- Reviewed monthly as part of privileged access reviews

AWS root credentials are secured with MFA and are not used for day-to-day operations. Root credentials are stored in a sealed envelope or equivalent and accessed only in documented break-glass scenarios.

### 6.3 Service Accounts and API Keys

- All service accounts must have a documented owner and business purpose
- Service account credentials (API keys, tokens, certificates) must be stored in approved secrets management tools (AWS Secrets Manager, Secrets management in CI/CD pipeline)
- Hardcoded credentials in source code are prohibited; GitHub secret scanning is enabled to detect violations
- Service account credentials must be rotated at least annually, or immediately upon suspected compromise

*This satisfies CC6.6.*

---

## 7. Access Reviews

### 7.1 Quarterly User Access Reviews

The Security team conducts **quarterly access reviews** for all production and sensitive systems. Reviews cover:

- All user accounts with access to AWS production accounts
- All user accounts in the GitHub organization with write or admin access
- All Okta application assignments with access to sensitive applications

**Review process:**
1. Security team exports user access lists from Okta, AWS, and GitHub
2. Reports are distributed to relevant managers for review
3. Managers confirm or deny that each user still requires the access listed
4. Unnecessary access is removed by the Security team within 5 business days of identification
5. Review results (including the signed-off access list and any changes made) are retained as audit evidence

*This satisfies CC6.3.*

### 7.2 Privileged Access Reviews

Privileged accounts (Okta admin, AWS admin, GitHub org admin) are reviewed **monthly**. Any accounts with unnecessary elevated permissions are downgraded immediately.

### 7.3 Annual Access Review

A comprehensive access review of all systems (including systems not covered by the quarterly review) is conducted annually.

---

## 8. Access Revocation and Offboarding

### 8.1 Involuntary Terminations

For involuntary terminations (layoffs, terminations for cause):

- HR must notify the IT/Security team **immediately** — before or concurrent with the employee notification
- Okta account must be **disabled within 2 hours** of notification
- Disabling the Okta account cascades to all Okta-integrated applications (AWS SSO, GitHub, internal tools)
- Any non-SSO accounts, API keys, or credentials held by the employee are revoked on the same day

### 8.2 Voluntary Resignations

For voluntary resignations, the offboarding procedure is initiated on the employee's last day:

- Okta account disabled at the end of the last business day
- GitHub organization membership removed
- AWS IAM user (if any) deleted or disabled
- Shared credentials or passwords the employee may have known are rotated
- Offboarding completion confirmed and documented in an offboarding checklist ticket

### 8.3 Role Changes (Transfers)

When an employee changes roles, their access is reviewed within 5 business days of the role change. Access no longer appropriate for the new role is removed. Access rights are not automatically accumulated — a transfer triggers a fresh access grant based on the new role's requirements.

### 8.4 Offboarding Checklist

Every offboarding generates a ticket that tracks:
- [ ] Okta account disabled
- [ ] GitHub organization membership removed
- [ ] AWS IAM user/role assignments reviewed
- [ ] Shared credentials rotated (list applicable)
- [ ] Equipment return confirmed (IT)
- [ ] Offboarding ticket closed and retained

*This satisfies CC6.2, CC6.3.*

---

## 9. Remote Access

- Remote access to company systems is secured through Okta-enforced MFA
- VPN (if applicable) or zero-trust network access (ZTNA) is required for access to internal systems not exposed via public endpoints
- Remote work does not change any access control requirements in this policy
- Employees must not access company systems from devices that are not enrolled in or compliant with the Mobile Device Management (MDM) policy

---

## 10. Physical Access

While [Company Name] is a cloud-native company and does not operate its own data centers, physical access to company offices must be controlled:

- Office access is managed through [access control system, e.g., key fobs, Kisi]
- Visitor access to areas where sensitive work occurs requires an employee escort
- Clear desk and screen lock practices are required for employees working with customer or sensitive data

AWS manages all physical access controls for cloud infrastructure — this is covered by AWS's own SOC 2 report, which [Company Name] reviews annually as a Complementary User Entity Control (CUEC) responsibility.

---

## 11. Exceptions

Any deviation from this policy requires:

1. A written exception request submitted to the CISO
2. Documentation of the business justification, compensating controls, and duration
3. Approval by the CISO
4. Logging of the exception in the exception register with scheduled review date

Exceptions are reviewed at least quarterly. Exceptions that are not renewed expire automatically.

---

## 12. Violations

Violations of this policy may result in disciplinary action, up to and including termination, and may expose [Company Name] to legal and regulatory risk. Suspected violations must be reported to the Security team or through the company's incident reporting channel.

---

## 13. Policy Review

This policy is reviewed and updated:
- **Annually** by the Policy Owner
- **Upon significant organizational or technology changes** (e.g., new identity provider, major access model changes)
- **Following any access-related security incidents**

---

## 14. References and Related Documents

| Document | Location |
|---|---|
| Information Security Policy | [Link] |
| Acceptable Use Policy | [Link] |
| Vendor Management Policy | [Link] |
| Incident Response Policy | [Link] |
| Change Management Policy | [Link] |
| Okta Access Administration Procedure | [Link] |
| AWS IAM Standards | [Link] |
| Offboarding Checklist Template | [Link] |

---

## 15. Version History

| Version | Date | Author | Change Description |
|---|---|---|---|
| 1.0 | [DATE] | [Author] | Initial version |

---

*This policy was written to satisfy AICPA Trust Services Criteria CC6.1 (logical access security software, infrastructure, and architectures), CC6.2 (prior to issuing system credentials), CC6.3 (role-based access control), CC6.6 (logical access security measures to protect against threats from sources outside its system boundaries), CC6.7 (transmission of data), and CC6.8 (unauthorized or malicious software).*
