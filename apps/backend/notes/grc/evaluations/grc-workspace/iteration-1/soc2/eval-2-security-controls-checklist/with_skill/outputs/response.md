# SOC 2 Security (Common Criteria) Control Checklist
## For a Cloud-Native Company Using AWS, GitHub, and Okta

This checklist maps practical controls to the AICPA 2017 Trust Services Criteria CC1–CC9 (Security/Common Criteria). Every control listed is required for SOC 2 Security. Use this as a gap assessment tool: mark each item as Met, Partial, or Gap.

---

## CC1 — Control Environment (Governance & Integrity)

Controls that establish accountability, integrity, and oversight at the organizational level.

- [ ] Written **Information Security Policy** exists, is approved by leadership, reviewed annually, and communicated to all employees
- [ ] **Acceptable Use Policy** is in place and employees have signed acknowledgment
- [ ] **Organizational chart** with defined security responsibilities (CISO or security owner identified)
- [ ] **Background checks** performed for new hires (especially those with privileged access)
- [ ] **Security awareness training** conducted at onboarding and annually; completion tracked
- [ ] **Code of conduct / ethics policy** documented and acknowledged by employees
- [ ] Leadership demonstrates commitment to security (e.g., security is a standing agenda item in leadership meetings)

*Evidence for auditors: Signed policy acknowledgments, training completion records, org chart, board/leadership meeting minutes referencing security.*

---

## CC2 — Communication and Information

Controls ensuring relevant security information flows internally and externally.

- [ ] Security policies and procedures are **accessible to all employees** (e.g., in a wiki, intranet, or policy management tool)
- [ ] **Incident reporting channels** are documented and communicated (employees know how to report a security incident)
- [ ] **External communication policy** for disclosing security incidents to customers and regulators exists
- [ ] **Vulnerability disclosure or responsible disclosure program** is documented (even a basic policy page)
- [ ] Security updates and alerts are communicated to relevant teams (e.g., AWS security bulletins reviewed)

*Evidence: Policy repository access logs, incident reporting runbook, external disclosure policy.*

---

## CC3 — Risk Assessment

Controls for identifying, analyzing, and responding to security risks.

- [ ] **Annual risk assessment** is documented, signed off by leadership, and covers the in-scope system
- [ ] **Risk register** maintained with identified risks, likelihood/impact ratings, and assigned owners
- [ ] **Risk treatment decisions** documented (accept, mitigate, transfer, avoid) for each identified risk
- [ ] **Threat modeling** or risk review performed for significant new features or infrastructure changes
- [ ] Risk assessment methodology is documented and consistently applied

*Evidence: Dated risk assessment document with approvals, risk register with status, change-linked risk reviews.*

---

## CC4 — Monitoring Controls

Controls for ongoing monitoring of the control environment.

- [ ] **Internal audit or compliance review** scheduled at least annually to assess control effectiveness
- [ ] **Exception tracking** — identified control exceptions are logged and remediated
- [ ] Results of monitoring activities are reported to management
- [ ] **Penetration testing** or third-party security assessment performed annually
- [ ] Security metrics and KPIs tracked and reviewed by leadership (e.g., patch compliance rate, open vulnerabilities)

*Evidence: Audit reports, pen test reports, management review meeting notes, exception log.*

---

## CC5 — Control Activities (Policies & Procedures)

Controls specifying that policies are implemented as intended.

- [ ] All policies in the core policy set are **implemented** (not just written)
- [ ] **Separation of duties** enforced for critical functions (e.g., no single engineer can push to production AND approve the change)
- [ ] **Least privilege** principle is documented and enforced in AWS IAM, GitHub, and Okta
- [ ] **Documented procedures** exist for key operational processes (onboarding, offboarding, incident response, change management, etc.)
- [ ] Control activities are **tested** periodically to verify they operate as designed

*Evidence: Procedure documents, IAM policy exports showing least privilege, change records showing approvals.*

---

## CC6 — Logical & Physical Access Controls

This is typically the highest-scrutiny criterion. Controls restricting who can access systems and data.

### Identity and Authentication (Okta-specific)
- [ ] **Okta SSO** is the primary authentication mechanism for all major applications (AWS console, GitHub, internal tools)
- [ ] **MFA enforced** for all users in Okta — no exceptions for production access
- [ ] **Phishing-resistant MFA** (FIDO2/WebAuthn) enforced for privileged accounts where possible
- [ ] **Password policy** enforced via Okta: minimum length, complexity, rotation for service accounts
- [ ] **Session timeout** configured in Okta (idle session limits enforced)

### Access Provisioning
- [ ] **Formal provisioning process** — access requires a request, approval, and ticket (Jira, ServiceNow, etc.)
- [ ] **Role-based access control (RBAC)** defined in AWS (IAM roles), GitHub (teams/permissions), and Okta (groups)
- [ ] **Least privilege** enforced — no admin-by-default; production access is restricted and justified
- [ ] **Privileged access** (AWS root, admin roles) requires separate approval and is documented

### Access Reviews
- [ ] **Quarterly access reviews** conducted for all production systems — results documented and acted upon
- [ ] Access review sign-offs retained as evidence
- [ ] Stale/unused accounts identified and removed

### Offboarding
- [ ] **Offboarding procedure** ensures Okta account disabled within defined SLA (same day for terminations)
- [ ] Offboarding checklist covers: Okta disable, AWS key revocation, GitHub removal, shared credential rotation
- [ ] Offboarding completion is verified and documented

### AWS-Specific
- [ ] **AWS root account** is not used for day-to-day operations; MFA enabled on root
- [ ] **IAM roles** used for EC2/Lambda instead of long-lived access keys
- [ ] **AWS Organizations / SCPs** (Service Control Policies) used to enforce guardrails
- [ ] **AWS CloudTrail** enabled in all regions for API activity logging
- [ ] **AWS Config** enabled for configuration compliance monitoring
- [ ] **No hardcoded credentials** in code — verified via GitHub secret scanning

### GitHub-Specific
- [ ] **MFA required** for all GitHub organization members (enforced at org level)
- [ ] **Branch protection rules** on main/production branches (require PR review, status checks)
- [ ] **GitHub secret scanning** enabled to detect accidental credential commits
- [ ] External collaborator access reviewed quarterly

### Physical Access
- [ ] If you have an office: server rooms / network equipment locked; access logged
- [ ] AWS handles physical datacenter security (covered by AWS's SOC 2 report — document this as a Complementary User Entity Control)

*Evidence: Okta admin reports (user list, MFA enrollment), AWS IAM exports, GitHub org member list, access review completion records, offboarding tickets.*

---

## CC7 — System Operations (Monitoring, Incident Response, DR)

Controls for detecting and responding to threats and operational issues.

### Logging and Monitoring
- [ ] **Centralized logging** — AWS CloudTrail, VPC Flow Logs, and application logs aggregated (CloudWatch, Datadog, Splunk, etc.)
- [ ] **Security alerting** — alerts configured for anomalous activity (failed logins, privilege escalation, unusual API calls)
- [ ] **Vulnerability scanning** conducted regularly (at minimum quarterly; weekly preferred) using a tool like AWS Inspector, Tenable, or Snyk
- [ ] **SIEM or log management** in place with alert rules reviewed periodically

### Incident Response
- [ ] **Incident Response Plan (IRP)** documented and approved
- [ ] IRP covers: detection, containment, eradication, recovery, post-incident review
- [ ] **Incident severity classification** defined
- [ ] IRP has been **tested** (tabletop exercise at minimum annually)
- [ ] **Incident log** maintained (even a simple ticket/Jira record) for all security events
- [ ] Post-incident reviews documented for significant events

### Patch and Vulnerability Management
- [ ] **Vulnerability management policy** defines SLAs by severity (e.g., critical: 7 days, high: 30 days)
- [ ] Patch compliance tracked and reported
- [ ] **Dependency scanning** in GitHub Actions/CI pipeline (Dependabot, Snyk, etc.)

### Business Continuity / Disaster Recovery
- [ ] **DR/BCP plan** documented covering key system failure scenarios
- [ ] **RTO and RPO** defined for critical services
- [ ] **Backup procedures** documented; backups tested for restorability
- [ ] DR plan tested at least annually (tabletop or functional test)

*Evidence: SIEM dashboards, vulnerability scan reports with remediation tracking, incident tickets, IRP document, DR test results, backup restore records.*

---

## CC8 — Change Management

Controls ensuring changes to systems are authorized, tested, and reviewed.

- [ ] **Change management policy** documented with categories (standard, normal, emergency)
- [ ] **All code changes** go through GitHub pull requests with at least one reviewer (no direct pushes to main)
- [ ] **CI/CD pipeline** includes automated testing (unit tests, security scans) before deployment
- [ ] **Production deployments** require approval (branch protection + required PR approvals enforced in GitHub)
- [ ] **Emergency change process** defined for urgent fixes (with post-hoc approval documented)
- [ ] **Infrastructure as Code (IaC)** changes (Terraform, CloudFormation) treated as code changes and reviewed
- [ ] Change records are retained (GitHub PR history, deployment logs in CI/CD tool)
- [ ] **Rollback procedures** documented and tested

*Evidence: GitHub PR history showing approvals, CI/CD pipeline run logs, deployment records, change policy document.*

---

## CC9 — Risk Mitigation (Vendor and Third-Party Risk)

Controls for managing risks from vendors and business partners.

- [ ] **Vendor inventory** maintained listing all critical vendors (AWS, GitHub, Okta, plus any others processing customer data)
- [ ] **Vendor risk tiering** in place (critical, high, medium, low based on data access and operational dependency)
- [ ] **AWS, GitHub, Okta SOC 2 reports** reviewed annually — results documented
- [ ] **Complementary User Entity Controls (CUECs)** from vendor SOC 2 reports identified and implemented
- [ ] **Security requirements in contracts** — vendor agreements include security, data handling, and breach notification clauses
- [ ] **Pre-onboarding due diligence** for new critical vendors (security questionnaire or SOC 2 review before signing)
- [ ] **Vendor management policy** documented

*Evidence: Vendor inventory spreadsheet, downloaded SOC 2 reports from AWS/GitHub/Okta, vendor contract security clauses, CUEC implementation log.*

---

## Summary: Quick Priority Stack Rank

If you are just starting, prioritize in this order:

1. **CC6 — Access controls** (MFA, provisioning, offboarding, access reviews) — most heavily tested
2. **CC7 — Incident response + monitoring** — auditors will ask for evidence of real incidents handled
3. **CC1 — Policies** — everything needs a policy backing it
4. **CC8 — Change management** — GitHub PR process is straightforward for a dev team
5. **CC3 — Risk assessment** — do this once annually with documented sign-off
6. **CC9 — Vendor risk** — pull AWS/GitHub/Okta SOC 2 reports and log the reviews
7. **CC2, CC4, CC5** — support and monitor the above; often satisfied by documenting what you already do

---

*Note: This checklist covers the Security Common Criteria only. If your scope includes Availability (A1), Confidentiality (C1), or other criteria, additional controls are required. All controls must be tailored to your specific system — SOC 2 prescribes criteria, not specific technical implementations.*
