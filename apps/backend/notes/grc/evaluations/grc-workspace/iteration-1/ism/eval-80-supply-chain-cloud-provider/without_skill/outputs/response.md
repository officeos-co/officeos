## ISM Compliance Obligations for a Private Sector Cloud Service Provider Contracting with an Australian Federal Government Agency

### Overview

The Australian Government Information Security Manual (ISM), published and maintained by the Australian Signals Directorate (ASD), establishes the cybersecurity framework that Australian government agencies must apply when managing their information and systems. When a private sector company wins a contract to deliver cloud services to a federal government agency, the ISM's requirements flow down through that contractual relationship. You are not directly subject to the ISM as a non-government entity, but your customer — the agency — is, and it cannot outsource its compliance obligations. This means the agency will contractually require you to meet ISM controls as a condition of providing services.

---

### 1. Your Legal and Contractual Obligations

**The ISM applies indirectly but binding-ly through contract**

The agency is bound by the ISM and by the Protective Security Policy Framework (PSPF). Neither framework has direct legislative force over private companies, but the agency cannot legally procure services that place its information at unacceptable risk. Consequently, your contract will almost certainly contain clauses that:

- Require you to implement and maintain specific ISM controls for the systems and services in scope.
- Require you to support or undergo an Information Security Registered Assessors Program (IRAP) assessment.
- Require you to operate under a System Security Plan (SSP) reviewed and approved by the agency's Chief Information Security Officer (CISO) or equivalent.
- Require breach notification within timeframes aligned with ASD guidance (often 24–72 hours for significant cyber incidents).
- Require annual attestation or evidence of continued compliance.
- Require compliance with the PSPF for any handling of classified information (if applicable), including PROTECTED or higher data.

**Classification of information matters**

The data classification of the information you will process or store determines the intensity of controls required:

- OFFICIAL: Baseline controls; most commercial cloud arrangements sit here.
- OFFICIAL: Sensitive: Additional controls, restricted access, elevated data handling requirements.
- PROTECTED: Significantly elevated controls; requires IRAP-assessed cloud services; generally must use services listed on the ASD Certified Cloud Services List (CCSL) or its successor, the Cloud Services Certification Program (CSCP).
- SECRET / TOP SECRET: Rarely applicable to commercial cloud; requires highly specialised government-approved solutions.

---

### 2. Most Critical ISM Controls for a Cloud Environment

The ISM is published as a living document and contains hundreds of controls across many domains. For a cloud services provider, the following control families are most critical and most scrutinised during any assessment or audit:

**a. Identity and Access Management (IAM)**

- Multi-factor authentication (MFA) is mandatory for all privileged access and for any remote access to systems handling government data.
- Least-privilege principles must be enforced; service accounts and administrative roles must be tightly scoped and regularly reviewed.
- Privileged access workstations (PAWs) or equivalent segregation for administrative functions.
- Regular access reviews and prompt deprovisioning upon role change or departure.

**b. Data Protection and Cryptography**

- Data at rest must be encrypted using ASD-approved cryptographic algorithms (AES-256 is standard; refer to ISM cryptographic guidance).
- Data in transit must use TLS 1.2 or higher; TLS 1.3 is preferred.
- Key management must be robust: customer-managed keys or government-controlled keys are required for PROTECTED data; key storage must be segregated from the data it protects.
- For PROTECTED data, only Australian-hosted infrastructure is generally acceptable unless specific approval is obtained.

**c. Vulnerability and Patch Management**

- Critical and high-severity vulnerabilities must be patched within tight timeframes: ISM guidance typically requires critical patches within 48 hours for internet-facing systems and 2 weeks for others.
- Automated vulnerability scanning of infrastructure, containers, and application code must be in place and evidence retained.
- Software Bill of Materials (SBOM) and supply chain risk management controls are increasingly expected.

**d. System Hardening**

- Operating systems, hypervisors, container platforms, and application runtimes must be hardened against ASD's hardening guides (which align with and reference CIS Benchmarks).
- Unnecessary services, ports, and features must be disabled.
- Configuration management must ensure hardened baseline configurations are enforced and drift is detected.

**e. Network Security and Segmentation**

- Government workloads must be logically (and often physically) isolated from other tenants.
- Firewalls, security groups, and network access controls must enforce least-privilege network flows.
- Ingress and egress filtering must be in place; north-south and east-west traffic should be inspected.
- DDoS mitigation must be available for internet-exposed services.

**f. Logging, Monitoring, and Incident Response**

- Comprehensive audit logging of all privileged actions, authentication events, configuration changes, and data access is mandatory.
- Logs must be tamper-evident, retained for a minimum period (typically 7 years for some government records, with security logs often required for at least 18 months to 7 years depending on sensitivity), and protected from deletion by administrators.
- A Security Operations Centre (SOC) capability or equivalent 24x7 monitoring must be in place for PROTECTED environments.
- An incident response plan aligned with ASD's guidelines must be documented, tested, and executable; the agency must be notified of relevant incidents promptly.
- Integration with or ability to provide log feeds to the agency's SIEM may be required.

**g. Physical Security**

- Data centres hosting government data must meet physical security standards. For PROTECTED data, this typically requires data centres located in Australia that can demonstrate compliance with relevant physical security standards (e.g., AS/NZS ISO/IEC 27001 with physical security controls, or equivalent).

**h. Personnel Security**

- Staff with access to government data or systems must hold appropriate baseline security clearances or undergo background checks. For PROTECTED data, Baseline Vetting (formerly called NV1 or similar) is typically required for personnel with privileged access.
- Security awareness training must be current and documented.

**i. Backup and Recovery**

- Regular, tested backups of government data must be maintained, with recovery time objectives (RTOs) and recovery point objectives (RPOs) agreed in the contract.
- Backups must be protected from ransomware (e.g., immutable or offline copies).

**j. Essential Eight**

The ASD's Essential Eight Maturity Model is not part of the ISM per se, but the ISM references ASD mitigation strategies and the Essential Eight has become a baseline expectation in Australian government contracts. You should plan to demonstrate compliance with the Essential Eight at the maturity level required by the agency (typically Maturity Level 2 or 3 for government work):

1. Application control (whitelisting)
2. Patch applications
3. Configure Microsoft Office macro settings
4. User application hardening
5. Restrict administrative privileges
6. Patch operating systems
7. Multi-factor authentication
8. Regular backups

---

### 3. Do You Need an IRAP Assessment Even Though You Are Not a Government Entity?

**Short answer: Almost certainly yes, if the agency handles PROTECTED or higher classified information on your platform, and likely yes even for OFFICIAL: Sensitive in practice.**

**What is IRAP?**

The Information Security Registered Assessors Program (IRAP) is an ASD initiative that certifies independent cybersecurity professionals (IRAP assessors) to conduct assessments of information systems against ISM controls. An IRAP assessment produces a Security Assessment Report (SAR) that the agency uses to make its own risk-based accreditation decision.

**IRAP applies to systems, not just government entities**

A common misconception is that IRAP is only for government agencies. In reality, IRAP assessments are conducted on systems and platforms regardless of who owns or operates them. If a government agency is going to use your cloud platform to process, store, or transmit government information, the agency needs assurance that your platform meets ISM requirements. The primary mechanism for obtaining that assurance is an IRAP assessment of your platform.

**Contractual requirement**

For PROTECTED data and above: The agency will almost certainly contractually require that your platform undergo an IRAP assessment and that the resulting SAR be reviewed before the agency grants an Authority to Operate (ATO) or Authority to Connect (ATC). The agency's CISO or Chief Security Officer is ultimately responsible for authorising the use of your service, and they will need an IRAP assessment to support that decision.

For OFFICIAL: Sensitive: Practice varies. Some agencies accept lower-assurance mechanisms (e.g., self-attestation against ISM controls, ISO 27001 certification, SOC 2 Type II reports) for lower-classification workloads. However, many agencies now require or strongly prefer an IRAP assessment even for OFFICIAL: Sensitive. You should check your specific contract requirements and negotiate this early.

For OFFICIAL (unclassified): An IRAP assessment may not be strictly required, but demonstrating ISM alignment through ISO 27001, SOC 2, or an IRAP assessment will significantly strengthen your position and reduce the agency's procurement risk evaluation concerns.

**What an IRAP assessment involves**

1. Scoping: Define the system boundary — which infrastructure, services, and data flows are in scope.
2. Documentation review: The assessor reviews your System Security Plan (SSP), security policies, network diagrams, configuration documentation, and evidence of control implementation.
3. Technical testing: The assessor performs interviews, configuration reviews, and may conduct vulnerability scanning or penetration testing.
4. Report: The assessor produces a SAR that identifies compliant controls, deficiencies, and residual risks.
5. Remediation: You address findings; the agency then makes an accreditation decision.

IRAP assessors are listed on the ASD website. You must engage a certified IRAP assessor — you cannot self-assess for IRAP purposes.

**ASD Cloud Services Certification**

If your services are likely to be used by multiple government agencies, you should consider pursuing listing on the ASD Cloud Services Certification Program (CSCP), which replaced the earlier Certified Cloud Services List (CCSL). Listed services have undergone formal IRAP assessment and provide a reusable assurance artefact that agencies can rely upon rather than commissioning their own assessment.

---

### 4. Practical Steps to Take Now

1. **Identify the information classification** of the data your platform will process. This drives every other decision.

2. **Review your contract carefully** for specific ISM control requirements, IRAP obligations, data sovereignty requirements, and incident notification timelines.

3. **Conduct an internal gap assessment** against the ISM controls relevant to your classification level. The current ISM is available freely on the ASD website (cyber.gov.au) and is updated frequently — use the latest version.

4. **Implement the Essential Eight** at the maturity level required, as this is likely to be a baseline expectation.

5. **Engage an IRAP assessor early** — IRAP assessors are in demand and scoping/preparation takes time. Do not leave this until the contract is already live.

6. **Prepare your System Security Plan (SSP)** — this is the foundational document for any IRAP assessment and for the agency's accreditation decision. It should describe your system architecture, data flows, control environment, and residual risks.

7. **Ensure data sovereignty** — confirm that government data will be stored and processed in Australia, and that offshore access (including by vendors or support staff) is either prohibited or subject to specific controls.

8. **Establish incident response procedures** aligned with ASD's guidelines and your contractual notification obligations.

9. **Engage your agency contact's CISO or information security team early** — they will be your primary point of contact for accreditation and will have specific requirements based on their own risk appetite and internal policies.

---

### 5. Key References

- Australian Signals Directorate (ASD) — Information Security Manual: https://www.cyber.gov.au/resources-business-and-government/essential-cyber-security/ism
- ASD Essential Eight Maturity Model: https://www.cyber.gov.au/resources-business-and-government/essential-cyber-security/essential-eight
- Protective Security Policy Framework (PSPF): https://www.protectivesecurity.gov.au
- IRAP Program information: https://www.cyber.gov.au/resources-business-and-government/assessments-and-certifications/irap
- Cloud Services Certification Program (CSCP): https://www.cyber.gov.au/resources-business-and-government/assessments-and-certifications/cloud-services-certification-programme

---

### Summary

As a private sector cloud provider contracted to serve an Australian federal government agency, your ISM obligations flow through your contract rather than directly through law, but they are binding and enforceable. The most critical controls focus on identity and access management, encryption, patch management, system hardening, network segmentation, logging and incident response, and the Essential Eight. An IRAP assessment is a practical necessity — not just a recommendation — for any system handling PROTECTED or OFFICIAL: Sensitive government data, and you should engage a certified IRAP assessor as early as possible in your program. The agency's CISO will ultimately make the accreditation decision, but they will rely on your IRAP assessment to do so.
