# SOC 2 Security Controls Checklist
## Cloud-Native Company on AWS, GitHub, and Okta

SOC 2's Security category is the foundation of every SOC 2 report — it's always required. Here's a practical checklist of controls you need, organized by area, with notes on how AWS, GitHub, and Okta apply.

---

## Identity and Access Management

The most scrutinized area in any SOC 2 audit.

- [ ] **MFA enforced for all users** — use Okta to enforce MFA across all applications; no exceptions for production access
- [ ] **Single Sign-On (SSO)** via Okta for all major applications including AWS console and GitHub
- [ ] **Least privilege access** — users should have only the permissions they need; no admin-by-default
- [ ] **Role-based access control** — define roles in AWS IAM, GitHub teams, and Okta groups
- [ ] **Access provisioning process** — documented procedure for requesting and approving access
- [ ] **Offboarding procedure** — Okta account disabled on the day of termination; checklist covers all systems
- [ ] **Quarterly access reviews** — review who has access to production systems; document results
- [ ] **Privileged access controls** — AWS root account secured with MFA and not used for daily operations
- [ ] **No long-lived access keys** — use IAM roles rather than static access keys in AWS
- [ ] **No secrets in code** — GitHub secret scanning enabled; pre-commit hooks to catch credentials

---

## System Monitoring and Logging

- [ ] **Centralized logging** — CloudTrail for AWS API activity, application logs aggregated to a central system
- [ ] **Security alerting** — automated alerts for suspicious activity (failed logins, privilege escalation attempts)
- [ ] **Log retention** — logs retained for at least one year (common auditor requirement)
- [ ] **Vulnerability scanning** — regular scanning of infrastructure and dependencies (AWS Inspector, Snyk, etc.)
- [ ] **Dependency scanning in CI/CD** — Dependabot or similar tool scanning GitHub repos for vulnerable dependencies

---

## Incident Response

- [ ] **Incident Response Plan (IRP)** — written document covering detection, containment, recovery, and post-incident review
- [ ] **Incident severity levels** — defined (e.g., critical, high, medium, low) with different response timelines
- [ ] **Incident log** — all security incidents tracked in a ticketing system
- [ ] **Annual tabletop exercise** — documented test of the IRP; results recorded
- [ ] **Breach notification procedure** — documented process for notifying customers if their data is affected

---

## Change Management

- [ ] **Pull request reviews required** — GitHub branch protection enforcing PR approvals before merging
- [ ] **No direct pushes to main** — branch protection rules in place
- [ ] **CI/CD pipeline with automated tests** — security scans and tests run before deployment
- [ ] **Change approvals documented** — PR history serves as the change log; ensure approvals are visible
- [ ] **Infrastructure changes tracked** — Terraform/CloudFormation changes go through the same review process
- [ ] **Emergency change process** — documented procedure for urgent fixes with post-hoc review

---

## Risk Assessment

- [ ] **Annual risk assessment** — formal documented assessment covering your system and data; approved by leadership
- [ ] **Risk register** — list of identified risks with severity ratings and assigned owners
- [ ] **Risk treatment** — each risk has a documented decision (mitigate, accept, transfer, avoid)

---

## Policies and Documentation

- [ ] **Information Security Policy** — top-level policy covering overall security commitment
- [ ] **Access Control Policy** — documents how access is granted, reviewed, and revoked
- [ ] **Incident Response Policy** — references or includes the IRP
- [ ] **Acceptable Use Policy** — governs how employees use company systems
- [ ] **Change Management Policy** — documents the change control process
- [ ] **Vendor Management Policy** — covers how you assess and monitor third parties
- [ ] **Password/Authentication Policy** — documents MFA requirements, session management
- [ ] All policies reviewed and approved annually, with signatures or approval records

---

## Vendor and Third-Party Risk

- [ ] **Vendor inventory** — list of all vendors with access to your systems or customer data
- [ ] **Review vendor SOC 2 reports** — AWS, GitHub, and Okta all publish SOC 2 reports; download and review them annually
- [ ] **Security requirements in contracts** — vendor agreements include security and data handling terms
- [ ] **Pre-onboarding review** — basic security check before adding a new critical vendor

---

## Physical and Environmental

For a cloud-native company, your physical security story is mostly inherited from AWS:
- [ ] **AWS shared responsibility model** documented — you rely on AWS for physical datacenter security
- [ ] **Office physical security** — if you have an office with servers or sensitive equipment, access controls in place
- [ ] **Clean desk / screen lock policy** — documented and communicated to employees

---

## Employee Security

- [ ] **Security awareness training** — all employees trained at hire and annually; completion tracked
- [ ] **Background checks** for employees with access to production systems or sensitive data
- [ ] **Security responsibilities in job descriptions** for roles with elevated access

---

## Evidence You Need to Collect

Auditors will sample across your audit period. For each area above, you need:

- **Access controls:** User lists from Okta, AWS IAM exports, GitHub org reports, access review sign-offs, offboarding tickets
- **Change management:** PR history with approvals, deployment logs
- **Monitoring:** Alert configuration screenshots, sample alerts that fired, vulnerability scan reports
- **Incident response:** IRP document, tabletop exercise notes, sample incident tickets
- **Risk assessment:** Risk register with approval signature and date
- **Vendor management:** Vendor inventory, copies of vendor SOC 2 reports with review notes
- **Policies:** Policy documents with version history, approval, and employee acknowledgment records

---

## Quick Wins for a Cloud-Native Team

If you're just getting started, these give you the most coverage for the effort:

1. Enforce MFA for everything in Okta — immediate impact on CC6
2. Enable GitHub branch protection with required reviews — covers change management
3. Enable CloudTrail in all AWS regions — foundational for monitoring
4. Schedule and run a quarterly access review — document it
5. Write your five core policies (InfoSec, Access Control, IR, Acceptable Use, Change Management)
6. Download AWS, GitHub, and Okta SOC 2 reports and log the review

These six actions alone close a large portion of the gaps auditors commonly flag.
