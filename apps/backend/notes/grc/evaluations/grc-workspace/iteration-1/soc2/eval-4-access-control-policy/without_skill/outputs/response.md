# Access Control Policy

**Company:** [Company Name]
**Version:** 1.0
**Effective Date:** [Date]
**Reviewed By:** [Name, Title]
**Approved By:** [CEO/CTO Name]
**Next Review Date:** [Date + 1 year]
**Policy Owner:** Head of Security / IT

---

## 1. Purpose

This Access Control Policy establishes requirements for managing access to [Company Name]'s systems, applications, and data. The goal is to ensure that access is granted only to authorized individuals based on their job requirements, and that access is removed promptly when it is no longer needed.

This policy applies to all employees, contractors, and third parties who access company systems.

---

## 2. Scope

This policy covers:
- All company systems, applications, and data
- Cloud infrastructure (production and non-production)
- Code repositories and development tools
- Identity and access management systems
- Corporate SaaS applications

All employees, contractors, and vendors with access to company systems are subject to this policy.

---

## 3. Core Principles

**Least Privilege:** Users are granted the minimum level of access required to perform their job duties. No user should have broader access than their role requires.

**Need-to-Know:** Access to sensitive data is limited to those who have a legitimate business need.

**Individual Accountability:** All users must have unique individual accounts. Shared accounts are not permitted unless approved as a service account with a designated owner.

**Separation of Duties:** Where possible, critical functions are divided among multiple people so that no one person can complete a high-risk action unilaterally.

---

## 4. Authentication Requirements

### Multi-Factor Authentication (MFA)
MFA is required for all user accounts. This is enforced through Okta for all company applications. Employees must complete MFA enrollment within 24 hours of account creation.

### Single Sign-On (SSO)
Okta serves as the company's identity provider. Employees must use Okta SSO to access company applications. Bypassing SSO is not permitted without documented approval from the security team.

### Passwords
Where passwords are used:
- Minimum 12 characters
- Must include letters, numbers, and special characters
- Must not be reused (last 10 passwords)
- Must not be shared with anyone
- Service account passwords must be stored in an approved password manager or secrets vault

### Session Management
- Sessions automatically time out after 60 minutes of inactivity
- Employees must lock their screens when away from their workstations

---

## 5. Access Provisioning

### New Employee Access
1. HR initiates an access request when a new employee joins
2. The employee's manager approves the appropriate access level
3. IT/Security provisions access via Okta within one business day
4. The access request and approval are documented in the ticketing system

### Additional Access Requests
Existing employees who need additional access must:
1. Submit a ticket to IT/Security through the designated system
2. Get manager approval in the ticket
3. Receive provisioning from IT/Security upon approval

**Privileged Access** (admin roles, production database access, security tools) requires approval from the Head of Security in addition to manager approval.

---

## 6. Access Reviews

A quarterly access review is conducted to ensure users have only appropriate access:

- IT/Security exports user lists from Okta, AWS, and GitHub
- Managers review and confirm which users on their team still need each access
- Any unnecessary access is removed within 5 business days of the review
- Access review results are documented and retained

Privileged accounts are reviewed monthly.

---

## 7. Access Removal and Offboarding

### Terminations
**Involuntary terminations:** HR notifies IT/Security immediately. The Okta account is disabled within 2 hours, which cascades access removal to connected applications.

**Voluntary resignations:** The Okta account is disabled on the employee's last day of work.

### Offboarding Checklist
Every offboarding is tracked with a checklist ticket confirming:
- [ ] Okta account disabled
- [ ] GitHub access removed
- [ ] AWS access reviewed and removed
- [ ] Any shared passwords the employee knew have been rotated
- [ ] Offboarding ticket closed and filed

### Role Changes
When an employee moves to a new role, their access is reviewed and updated within 5 business days to reflect the new role's requirements. Old access is removed and new access is granted through the standard provisioning process.

---

## 8. Service Accounts and API Keys

- All service accounts must have a named owner and documented purpose
- Service account credentials must be stored in approved secrets management tools (not in code)
- API keys and tokens must be rotated at least annually
- Hardcoded credentials in source code are prohibited and are detected by automated scanning
- Service accounts must be reviewed quarterly along with user accounts

---

## 9. Remote Access

- Employees working remotely must use Okta-enforced MFA for all system access
- Company data must only be accessed from company-approved or MDM-enrolled devices
- Public Wi-Fi usage for sensitive work requires VPN or equivalent secure connection

---

## 10. Physical Access

For employees working in company offices:
- Sensitive areas (server rooms, areas with customer data visible) are access-controlled
- Visitors must be escorted in areas where sensitive work occurs
- Employees must follow clean desk practices and lock screens when away from their workstations

Cloud infrastructure physical security is managed by our cloud provider (AWS) and is covered by their own SOC 2 certification, which we review annually.

---

## 11. Violations

Anyone who violates this policy may face disciplinary action, up to and including termination of employment or contract. Suspected violations should be reported to the security team immediately.

---

## 12. Exceptions

Requests for exceptions to this policy must be submitted in writing to the Head of Security. Exceptions require:
- Clear business justification
- Description of compensating controls
- Defined expiration date
- Head of Security approval

All approved exceptions are logged and reviewed quarterly.

---

## 13. Policy Review

This policy is reviewed annually by the Policy Owner and updated as needed. It is also reviewed following significant security incidents or organizational changes that affect access management.

---

## Related Policies
- Information Security Policy
- Acceptable Use Policy
- Incident Response Policy
- Vendor Management Policy

---

*Version 1.0 — [Date]*
