# FedRAMP Authorization Process: Start to Finish

## Overview

FedRAMP (Federal Risk and Authorization Management Program) is a US government-wide program that provides a standardized approach to security assessment, authorization, and continuous monitoring for cloud products and services used by federal agencies. As a cloud software vendor, you will need to go through FedRAMP authorization to sell cloud services to most federal agencies.

---

## The FedRAMP Authorization Process

### Phase 1: Preparation

1. **Determine your impact level** — Based on the sensitivity of the data your system handles, you'll be categorized as Low, Moderate, or High impact. This determines how many security controls you must implement.
2. **Define your system boundary** — Clearly document what systems, components, and services are in scope for the authorization.
3. **Select your authorization path** — Choose between Agency ATO or JAB P-ATO (see below).
4. **Conduct a readiness assessment** — Evaluate your current security posture against FedRAMP requirements and identify gaps.
5. **Engage a 3PAO** — Select an accredited Third Party Assessment Organization from the FedRAMP marketplace to conduct your security assessment.

### Phase 2: Documentation

6. **Develop your System Security Plan (SSP)** — This is the primary document describing how you implement security controls. It typically runs hundreds of pages.
7. **Complete supporting documentation** — Policies, procedures, incident response plans, contingency plans, and other required artifacts.

### Phase 3: Security Assessment

8. **3PAO Assessment** — Your chosen 3PAO conducts an independent security assessment, developing a Security Assessment Plan (SAP) and executing it.
9. **Vulnerability scanning and penetration testing** — Required as part of the assessment process.
10. **Security Assessment Report (SAR)** — The 3PAO documents findings in the SAR.

### Phase 4: Authorization

11. **Remediation** — Address any findings from the assessment, or document them in a Plan of Action & Milestones (POA&M).
12. **Package submission** — Submit your complete authorization package for review.
13. **Authorization decision** — Receive your Authority to Operate (ATO) or Provisional ATO (P-ATO).

### Phase 5: Continuous Monitoring

14. **Ongoing compliance** — Maintain your authorization through regular vulnerability scanning, monthly reporting, annual reassessments, and POA&M management.

---

## Agency ATO vs. JAB P-ATO

### Agency Authorization (Agency ATO)

- **Sponsor**: A specific federal agency sponsors your authorization and acts as the Authorizing Official (AO)
- **Scope**: Initially covers just that agency; other agencies may accept it via reciprocity
- **Process**: You work directly with one agency through the authorization process
- **Timeline**: Typically 12–18 months
- **Best for**: CSPs that have an existing relationship with a specific agency customer

### JAB P-ATO (Joint Authorization Board Provisional ATO)

- **Review board**: Conducted by the Joint Authorization Board, consisting of representatives from DoD, DHS, and GSA
- **Scope**: Government-wide recognition — any agency can use your service based on the P-ATO
- **Process**: More rigorous and competitive; JAB selects which CSPs to work with based on government-wide demand
- **Timeline**: Can take 12–24+ months
- **Best for**: Large platforms with demonstrated or expected demand across many federal agencies

---

## Which Path Should You Choose for a First Authorization?

**Recommendation: Agency Authorization**

For a first-time FedRAMP authorization, Agency Authorization is generally the better starting point:

1. **More accessible** — Agency path doesn't require JAB selection; you just need one willing agency sponsor.
2. **Existing customer relationship** — If you're already talking to a federal agency, they can be your sponsor.
3. **Faster path** — Working with one agency is typically more straightforward than the JAB process.
4. **Reusability** — Once you have an Agency ATO and are listed in the FedRAMP Marketplace, other agencies can leverage your authorization.

**To get started:**
- Identify one federal agency prospect willing to sponsor your authorization
- Determine your target impact level (Moderate is most common for commercial cloud services)
- Conduct a gap assessment against the applicable NIST SP 800-53 control baseline
- Engage an accredited 3PAO early in the process
- Begin developing your System Security Plan
