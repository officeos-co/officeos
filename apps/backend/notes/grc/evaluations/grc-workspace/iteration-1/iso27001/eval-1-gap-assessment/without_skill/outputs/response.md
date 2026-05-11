# ISO 27001 Gap Assessment & Roadmap for SaaS Company

## Overview

For a 120-person SaaS company with no prior security certification, storing customer PII and financial data in AWS, achieving ISO 27001 certification in 18 months is challenging but achievable with the right planning and resources.

## What You Are Likely Missing

### Governance & Documentation
- **Formal Information Security Policy** — Most companies this size have informal practices but nothing signed off by executive leadership
- **ISMS Scope Definition** — You need to formally define what systems, locations, and people are in scope
- **Roles and Responsibilities** — Security ownership is usually unclear without formal documentation

### Risk Management
- **Risk Assessment Process** — You need a documented methodology for identifying and scoring risks
- **Risk Register** — A formal inventory of all identified risks with likelihood and impact scores
- **Risk Treatment Plan** — Decisions on how to handle each risk (mitigate, accept, transfer, avoid)
- **Statement of Applicability (SoA)** — A mandatory document listing which ISO 27001 controls apply to you and why

### Policy Library
You likely need 10–15 formal policies including:
- Access Control Policy
- Incident Response Policy
- Asset Management Policy
- Supplier/Third-Party Security Policy
- Acceptable Use Policy
- Business Continuity Policy
- Cryptography Policy
- Data Classification Policy
- Secure Development Policy

### Operational Controls
- **Access Management** — Formal processes for provisioning/deprovisioning, privileged access controls
- **Vulnerability Management** — Regular scanning and patching processes
- **Incident Response** — A documented and tested IR plan
- **Backup and Recovery** — Tested recovery procedures with documented RTO/RPO
- **Security Monitoring** — Log collection and review processes
- **Security Awareness Training** — Formal training programme with completion tracking

### Audit & Review
- **Internal Audit Programme** — You need qualified internal auditors and a formal audit schedule
- **Management Review Process** — Executive reviews of ISMS performance at least annually
- **Corrective Action Process** — System for tracking and closing identified gaps

## Phased 18-Month Roadmap

### Phase 1: Establish the Foundation (Months 1–5)

**Hire or appoint a security lead** — Someone needs to own this project full-time or you need an external consultant.

**Define scope** — Decide what systems and people are in scope. Given your AWS environment, this should include your production infrastructure, corporate IT, and all employees.

**Get executive buy-in documented** — CEO needs to sign an Information Security Policy and formally commit resources.

**Build your policy library** — Draft the core policies. These don't need to be perfect immediately, but they need to exist and be approved.

**Conduct initial risk assessment** — Identify your most significant risks. For a SaaS company handling PII and financial data, you should expect risks around:
- Unauthorized access to customer data
- Insider threats
- Third-party/vendor breaches
- Data loss or ransomware

**Create your Statement of Applicability** — Map which controls apply to your business.

### Phase 2: Implement Controls (Months 4–10)

Focus on closing the most critical gaps identified in the risk assessment:

- **Access control** — Implement MFA everywhere, formal IAM procedures, privileged access management
- **Incident response** — Document your IR process and run a tabletop exercise
- **Vendor management** — Assess your key vendors and update contracts with security requirements
- **Security awareness** — Launch training for all employees and track completion
- **Monitoring and logging** — Enable comprehensive logging (AWS CloudTrail, etc.) and establish review processes
- **Vulnerability management** — Regular scanning, plus a penetration test
- **Business continuity** — Document and test your disaster recovery procedures
- **Secure development** — Implement security reviews in your development process

### Phase 3: Operate and Collect Evidence (Months 9–14)

ISO 27001 requires you to demonstrate that your controls are operating effectively, not just that they exist on paper.

- Run your processes for at least 3–6 months before the Stage 2 audit
- Collect evidence: training logs, access reviews, incident records, change management records
- Hold at least two management reviews
- Run your internal audit (typically months 12–13)
- Fix any issues found in the internal audit

### Phase 4: Certification Audit (Months 15–18)

**Stage 1 Audit (Document Review)** — The certification body reviews your documentation. Typically 1 day. They will issue findings that you need to address.

**Stage 2 Audit (On-Site)** — The certification body evaluates whether your controls are actually working. Typically 2–3 days for a company your size. Interviews with staff, review of evidence.

**Close any nonconformities** — Major issues must be closed before certification is granted.

**Receive certificate** — Valid for 3 years with annual surveillance audits.

## AWS-Specific Advantages

Operating on AWS gives you several advantages:
- Physical security controls are handled by AWS (data centers, environmental controls)
- AWS provides compliance documentation (SOC 2, ISO 27001 reports) you can reference
- AWS native tools (CloudTrail, Config, Security Hub, GuardDuty) can help evidence many technical controls
- Document these as "inherited controls" in your SoA

## Realistic Assessment

For a company starting from zero with no formal security programme, expect:
- **6–9 months** to get documentation and basic controls in place
- **3–6 months** of operational evidence needed before certification
- **$100,000–$200,000** total investment including consultant, tools, pen test, and audit fees
- **Strong CEO/leadership commitment** is the single biggest predictor of success

The 18-month timeline is achievable if you start immediately, hire appropriate expertise (internal or external), and treat this as a business priority rather than a compliance checkbox.
