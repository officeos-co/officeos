Preparing for an IRAP Assessment of a PROTECTED-Level System

The Information Security Registered Assessors Program (IRAP) is an Australian Signals Directorate (ASD) initiative that authorises qualified assessors to evaluate the security posture of systems against the requirements of the Australian Government Information Security Manual (ISM). For a PROTECTED-level system, the stakes and documentation requirements are significant. The following covers what artefacts to prepare, what to expect during the assessment, and the post-assessment process.


ARTEFACTS TO PREPARE BEFORE THE ASSESSMENT

1. System Security Plan (SSP)
The SSP is the central document for any IRAP assessment. It must describe:
- The system's purpose, boundaries, and architecture
- All data flows, including ingress, egress, and inter-component flows
- The classification and handling requirements for the information processed (PROTECTED in your case)
- All hardware, software, and network components in scope
- How ISM controls are addressed, including any controls not applied and the justification for each exception
- Roles and responsibilities for system security

2. Statement of Applicability (SoA)
A control-by-control mapping against the ISM, identifying which controls apply to the system, which are implemented, and which are not applicable (with rationale). For a PROTECTED system, this will include a broad range of controls across topics such as access control, cryptography, patch management, logging, and personnel security.

3. Risk Management Documentation
- A formal risk assessment conducted in accordance with the agency's risk management framework (typically aligned to NIST SP 800-30 or equivalent)
- A risk register capturing identified threats, vulnerabilities, likelihood, consequence, and residual risk ratings
- A risk treatment plan or Plan of Action and Milestones (POA&M) documenting how identified risks will be addressed

4. Security Architecture and Network Diagrams
Detailed diagrams showing:
- System topology and network segmentation
- Data classification zones and boundary protections
- External connections, gateways, and any cross-domain solutions
- Cloud service usage and relevant shared responsibility boundaries

5. Standard Operating Procedures (SOPs) and Security Policies
Relevant operational documentation the assessor will review, including:
- Access control procedures (including privileged access management)
- Incident response plan
- Patch management procedures
- Change management process
- Backup and recovery procedures
- Media handling and disposal procedures
- Personnel security procedures

6. Evidence of Control Implementation
Actual evidence that stated controls are in place, such as:
- Configuration baselines and hardening guides (aligned to ASD's Hardening Guides where applicable)
- Vulnerability scan results and penetration test reports
- Audit and log management evidence
- Training records for personnel with system access
- Certificates or configurations for encryption at rest and in transit

7. Previous Assessment Reports and Remediation Evidence
If any prior security reviews, penetration tests, or internal audits have been conducted, gather these along with evidence of remediation for any findings.

8. Data Classification and Information Management Documentation
Documentation showing how PROTECTED information is classified, labelled, handled, stored, transmitted, and disposed of in accordance with the Protective Security Policy Framework (PSPF).

9. Authorising Officer (AO) Engagement Records
Evidence that the relevant Authorising Officer has been engaged in the risk acceptance process. The AO is ultimately responsible for accepting residual risk and authorising the system to operate.

10. Third-Party and Supply Chain Agreements
Any contracts, Memoranda of Understanding (MoUs), or cloud service agreements relevant to the system's security, including any ASD-certified cloud services used and their certification documentation.


WHAT TO EXPECT DURING THE IRAP ASSESSMENT

An IRAP assessment typically involves three main phases:

Phase 1: Documentation Review (Desk-Based Review)
The assessor reviews all submitted documentation against ISM requirements. This is largely a paper-based exercise. You should expect:
- Requests for additional or clarifying documentation
- Preliminary identification of potential gaps or non-conformances
- Questions about architectural decisions and risk acceptance rationale

This phase can take several weeks depending on the complexity of the system and the completeness of the documentation provided. Incomplete documentation is a common cause of delays.

Phase 2: Technical Testing and Interviews
The assessor will validate that documented controls are actually implemented. This includes:
- Interviews with system administrators, security staff, and other key personnel
- Inspection of live system configurations (firewall rules, access control lists, audit logging settings, etc.)
- Review of vulnerability scan results or conducting their own scanning
- Verification of cryptographic implementations
- Spot-checks on patch currency and hardening

For a PROTECTED system, the assessor will pay particular attention to boundary controls, access management for privileged users, handling of credentials, logging and monitoring completeness, and any connections to external or lower-classification environments.

Phase 3: Reporting and Findings Discussion
After testing, the assessor will:
- Document all findings, categorised by risk rating (typically High, Medium, Low, or Informational)
- Brief the agency on preliminary findings before the final report is issued
- Allow the agency to respond to factual inaccuracies before finalisation

Assessors are independent and cannot advise on how to remediate findings during the assessment itself, as this would compromise their independence.

Things to Expect Generally
- The assessment duration for a PROTECTED system is typically 4 to 12 weeks depending on system complexity, scope, and documentation readiness.
- Active cooperation is required — designate a point of contact and ensure relevant technical staff are available for interview.
- The assessor works to the current version of the ISM (updated monthly by ASD), so confirm you are referencing the correct version.
- IRAP assessors are individuals, not organisations. Ensure your assessor holds a current IRAP authorisation from ASD.


AFTER THE IRAP REPORT IS ISSUED

The IRAP assessor produces a formal assessment report, commonly called an IRAP Security Assessment Report (SAR). This document presents:
- An overall assessment of the system's security posture
- Findings and residual risks rated by severity
- Recommendations for remediation
- The assessor's professional opinion on whether the system is suitable for operation at the assessed classification level

Step 1: Agency Review of the Report
The agency (system owner) reviews the SAR and prepares a formal response. This typically involves:
- Acknowledging each finding
- Documenting planned remediation actions and timelines
- Seeking risk acceptance from the Authorising Officer for any residual risks that will not be immediately remediated

Step 2: Authorising Officer Risk Acceptance
The Authorising Officer reviews the SAR and the agency's response. If the AO is satisfied that residual risks are acceptable, they issue a formal Authority to Operate (ATO) or equivalent authorisation. This is not automatic — the AO may require certain High findings to be remediated before authorisation is granted.

Step 3: Remediation of Findings
For findings that are not accepted as residual risk, the agency must implement remediation and track progress through the risk register or POA&M. Some agencies agree with their AO to operate with a time-limited authorisation pending completion of remediation.

Step 4: Submission to ASD (if applicable)
For some systems, particularly those used in Commonwealth agencies or where ASD accreditation is required, the SAR and associated documentation may need to be submitted to ASD for review. ASD may provide additional feedback or requirements. Confirm with your agency's Chief Information Security Officer (CISO) whether formal ASD submission is required in your case.

Step 5: Ongoing Assurance
IRAP assessment is not a one-time event. Authorisations are typically valid for a defined period (often two to three years) or until significant changes occur. Ongoing obligations include:
- Continuous monitoring of security controls
- Reassessment if significant architectural or functional changes are made to the system
- Annual review of the risk register and SSP currency
- Maintaining an Evidence of Compliance file to support future assessments
- Reporting significant security incidents to ASD and the agency's CISO

Step 6: Plan for Reassessment
Document lessons learned from the first assessment and begin maintaining the SSP and evidence artefacts as living documents. This significantly reduces effort for future assessments.


KEY TIPS FOR FIRST-TIME IRAP ASSESSMENT

- Engage the assessor early, ideally 6 to 8 weeks before assessment commencement, to agree on scope, methodology, and documentation requirements.
- Do not wait until the last moment to compile documentation. Gaps in documentation are one of the most common causes of findings and delays.
- Conduct an internal pre-assessment or gap analysis against the ISM before the formal IRAP begins. This allows time to address obvious gaps.
- Ensure your SSP is current and accurately reflects the system as it exists — not as it was designed or as you intend it to be.
- Brief your Authorising Officer well in advance so they understand their role and the likely risk acceptance decisions they will need to make.
- The ISM is the primary benchmark. Supplementary guidance such as ASD's Essential Eight and Hardening Guides are relevant but the ISM controls are the formal assessment basis.
