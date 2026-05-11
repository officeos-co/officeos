# KYC-SA Attestation Guide for A1 Architecture Banks
## SWIFT CSCF v2025 — Annual Attestation by July 31

---

## Your Situation at a Glance

You are an **A1 architecture bank** (customer connector, customer-managed, software-based — typically Alliance Access/Gateway deployed on-premises). Your annual KYC-SA attestation must be submitted by **July 31**. Today's date is April 25, 2026, leaving you approximately 97 days.

As an A1 user, all 23 mandatory controls apply to you in full, plus 8 advisory controls you may optionally attest. This guide walks you through every step: what to prepare, who can assess you, how to complete the form, and what happens after you submit.

---

## Section 1: What Controls Apply to You (A1 Architecture)

For an A1 bank, the following controls are **mandatory** — you must attest a compliance status for each:

| Control | Name | Objective |
|---------|------|-----------|
| **1.1** | SWIFT Environment Protection | 1 — Secure Your Environment |
| **1.2** | OS Privileged Account Control | 1 — Secure Your Environment |
| **1.4** | Restriction of Internet Access | 1 — Secure Your Environment |
| **2.1** | Internal Data Flow Security | 1 — Secure Your Environment |
| **2.2** | Security Updates | 1 — Secure Your Environment |
| **2.3** | System Hardening | 1 — Secure Your Environment |
| **2.6** | Operator Session Confidentiality and Integrity | 1 — Secure Your Environment |
| **2.7** | Vulnerability Scanning | 1 — Secure Your Environment |
| **2.8** | Critical Activity Outsourcing | 1 — Secure Your Environment |
| **2.10** | Application Hardening | 1 — Secure Your Environment |
| **3.1** | Physical Security | 1 — Secure Your Environment |
| **4.1** | Password Policy | 2 — Know and Limit Access |
| **4.2** | Multi-Factor Authentication | 2 — Know and Limit Access |
| **5.1** | Logical Access Controls | 2 — Know and Limit Access |
| **5.2** | Token Management | 2 — Know and Limit Access |
| **5.4** | Physical and Logical Password Storage | 2 — Know and Limit Access |
| **6.1** | Malware Protection | 3 — Detect and Respond |
| **6.2** | Software Integrity | 3 — Detect and Respond |
| **6.3** | Database Integrity | 3 — Detect and Respond |
| **6.4** | Log and Monitoring | 3 — Detect and Respond |
| **7.1** | Cyber Incident Response Planning | 3 — Detect and Respond |
| **7.2** | Security Training and Awareness | 3 — Detect and Respond |

The following 8 controls are **advisory** — you may optionally attest these, but they are not required:

1.3A Virtualisation Platform Security, 1.5A Customer Environment Protection, 2.4A Back-Office Data Flow Security, 2.5A External Transmission Data Protection, 2.9A Transaction Business Controls, 2.11A RMA Business Controls, 5.3A Staffing, 6.5A Intrusion Detection, 7.3A Penetration Testing, 7.4A Scenario Risk Assessment

---

## Section 2: What You Need to Prepare — Evidence Checklist by Control

Gather the following evidence before your independent assessor review. Organise evidence by control number.

### Objective 1 — Secure Your Environment

**Control 1.1 — SWIFT Environment Protection**
- Network architecture diagram clearly showing the SWIFT Secure Zone boundary
- Firewall ruleset and change records (deny-by-default rules documented)
- System inventory of all components within the Secure Zone
- Configuration evidence that no server is dual-homed (connected to both SWIFT zone and general corporate network)
- Evidence SWIFT servers are not used for non-SWIFT activities (no email clients, browsers, etc.)

**Control 1.2 — OS Privileged Account Control**
- Privileged account inventory for all SWIFT servers (root/local admin accounts)
- Evidence of MFA for privileged sessions (PAM tool screenshots or authentication logs)
- Privileged account usage policy
- OS audit logs showing privileged account activity and that privileged accounts are not used for routine operations
- Evidence default OS accounts are renamed or disabled

**Control 1.4 — Restriction of Internet Access**
- Firewall rules showing internet access is blocked for all SWIFT Secure Zone IP addresses
- Network flow test results confirming no direct internet path from SWIFT servers
- Evidence jump servers/proxies used for administration are not internet-facing

**Control 2.1 — Internal Data Flow Security**
- Data flow diagram showing all internal SWIFT connections within the Secure Zone
- TLS 1.2+ configuration evidence for each connection carrying SWIFT data
- Certificate inventory with expiry dates

**Control 2.2 — Security Updates**
- Patch management reports showing SWIFT components (OS, middleware, Alliance Access/Gateway)
- Evidence of SWIFT advisory subscription and action log
- Documentation showing critical patches applied within 3 calendar days, high within 90 days
- Exception register with risk acceptance and compensating controls for any overdue patches

**Control 2.3 — System Hardening**
- Hardening baseline document per system type (CIS Benchmark or equivalent)
- Configuration scan results vs. baseline (CIS-CAT output or equivalent)
- Evidence unnecessary services are disabled (e.g., netstat/ss output)

**Control 2.6 — Operator Session Confidentiality and Integrity**
- TLS configuration for Alliance Access/Gateway web interface
- Session timeout configuration (maximum 30 minutes inactivity)
- Evidence clipboard, screen-share, and remote control tools are restricted on SWIFT workstations

**Control 2.7 — Vulnerability Scanning**
- Credentialed vulnerability scan reports for all in-scope SWIFT systems for the last 4 quarters
- Scanner configuration or credential records proving scans are authenticated
- Remediation tracking for identified vulnerabilities

**Control 2.8 — Critical Activity Outsourcing**
- Contracts with any outsourced providers (managed SOC, cloud services) including SWIFT security obligations
- Provider KYC-SA attestations or equivalent audit reports (reviewed annually)
- Annual vendor review records

**Control 2.10 — Application Hardening**
- Completed SWIFT Alliance Access/Alliance Gateway Security Hardening Guide checklist
- Application configuration screenshots showing disabled unused modules and interfaces
- Application account audit report confirming least-privilege configuration and no default passwords

**Control 3.1 — Physical Security**
- Physical access control system logs for the data centre/SWIFT server room
- Authorised access list with documented approvals
- Evidence of electronic access logging (badge reader, CCTV)
- Visitor access records and escorted access policy

### Objective 2 — Know and Limit Access

**Control 4.1 — Password Policy**
- Password policy document
- Group Policy or Active Directory configuration screenshots confirming: 14+ character minimum, complexity, 90-day max age for privileged accounts, 180-day for standard, 12-generation no-reuse, 5-attempt lockout
- No shared or generic accounts evidence

**Control 4.2 — Multi-Factor Authentication**
- MFA configuration evidence for each SWIFT interface (Alliance Access, Alliance Gateway, SWIFT GUI)
- Hardware token inventory showing tokens assigned to every SWIFT operator
- Authentication logs confirming MFA is enforced for all interactive sessions and remote administrative access
- Exemption register if any accounts are excluded (must have documented approval)
- Note: Software-based OTP authenticator apps do NOT satisfy this requirement for A1 architecture — hardware OTP tokens, smart cards with PIN, or FIDO2 hardware keys are required

**Control 5.1 — Logical Access Controls**
- User access list with roles and approval evidence (individual named accounts, no shared accounts)
- Access review records for the last four quarters
- Evidence of dual-authorisation for high-risk operations (e.g., creating new BIC connections)
- Leaver process records showing access removed within 24 hours of departure

**Control 5.2 — Token Management**
- Token inventory register (all hardware tokens, assigned to named operators)
- Token issuance and return records
- Lost/stolen token incident records (if any), showing deactivation within 1 hour
- Annual token inventory reconciliation evidence

**Control 5.4 — Physical and Logical Password Storage**
- Password manager or PAM vault evidence showing SWIFT credentials are stored securely
- No plaintext passwords in files, spreadsheets, or unencrypted documents (evidence of search/audit)
- Break-glass/emergency credential procedure and access log
- Evidence default credentials were changed on installation and after maintenance

### Objective 3 — Detect and Respond

**Control 6.1 — Malware Protection**
- Anti-malware deployment scope screenshots (all SWIFT servers and operator workstations)
- Definition update log confirming daily automated updates (last 30 days)
- Alert configuration showing detections are sent to the security team within 1 hour
- Scheduled full scan history reports

**Control 6.2 — Software Integrity**
- Hash verification records for SWIFT software installations and updates
- FIM (File Integrity Monitoring) configuration covering SWIFT binary directories (if deployed)
- Integrity check procedure document

**Control 6.3 — Database Integrity**
- Database access control configuration (restricted to authorised SWIFT application service accounts)
- Database audit log samples
- Backup and restoration test records

**Control 6.4 — Log and Monitoring**
- SIEM configuration showing SWIFT log sources ingested (Alliance Access/Gateway application logs, OS security logs, authentication logs, network device logs for SWIFT zone, database audit logs)
- Log retention policy and technical evidence (1 year online/hot; 3 years total)
- Sample alert rules for SWIFT anomalies (failed authentications, after-hours logins, large/unusual transactions, privilege escalation, config changes)
- Log review records for the last 30 days (daily review evidence for transaction anomalies and authentication failures)

**Control 7.1 — Cyber Incident Response Planning**
- SWIFT-specific Incident Response Plan document (dated, approved by senior management)
- IRP content must include: detection triggers, triage, containment, SWIFT notification (24-hour obligation), investigation, recovery, lessons learned
- Last annual test record (tabletop exercise or live drill report)
- SWIFT notification contact list (security@swift.com and SWIFT relationship manager)

**Control 7.2 — Security Training and Awareness**
- Training completion records for all staff with SWIFT access (last 12 months)
- Training content overview showing SWIFT-specific topics: phishing, social engineering, SWIFT fraud scenarios, incident reporting
- Role-specific training materials for SWIFT operators

---

## Section 3: Who Can Act as Your Independent Assessor

Since CSCF v2020, an **independent assessment is required for all SWIFT users** — self-attestation is no longer permitted.

### Eligible Assessors

The following parties are eligible to serve as your independent assessor:

1. **Internal Audit Team** — Your own internal audit function can serve as the assessor, provided:
   - The internal audit team is sufficiently independent from the SWIFT operations team
   - Auditors conducting the assessment have no operational responsibility for the SWIFT environment
   - The team has appropriate SWIFT CSP assessment competency

2. **External Audit Firm** — An external audit or consulting firm with demonstrated SWIFT CSP assessment competency. They must be independent of your SWIFT operations.

3. **SWIFT-Certified Assessors** — Assessors listed on SWIFT's KYC Registry who are specifically certified for SWIFT CSP assessments.

### Key Independence Requirement

The assessor must have **no operational responsibility** for the SWIFT environment being assessed. For example, a team member who manages your Alliance Access/Gateway system cannot serve as their own assessor.

### Assessment Standard

As an A1 bank subject to the standard community programme, you will undergo a **Community Standard Assessment (CSA)**. Your assessment covers:
- All 23 mandatory controls applicable to your A1 architecture
- Advisory controls are optionally included (recommended to demonstrate maturity)

### Practical Recommendation

Engage your assessor now (late April) to allow sufficient time to:
- Conduct the assessment
- Identify and close any gaps
- Finalise the assessment report
- Complete and submit the KYC-SA form by July 31

---

## Section 4: How to Complete the KYC-SA Form

### Step-by-Step KYC-SA Completion Process

**Step 1: Access the KYC-SA Portal**
- Log in to swift.com/myswift
- Navigate to the KYC Security Attestation (KYC-SA) section
- Confirm you are completing attestation for the correct SWIFT BIC and architecture type (A1)

**Step 2: Confirm Your Architecture Type**
- Select A1 as your architecture type
- The portal will present the applicable mandatory controls for A1 — all 23 mandatory controls

**Step 3: Complete the Assessment Status for Each Mandatory Control**
For each of the 23 mandatory controls, you must select one of three statuses:

| Status | When to Use |
|--------|-------------|
| **Implemented** | The control is fully implemented in line with CSCF requirements; evidence is available |
| **Partially Implemented** | The control is partially implemented; some requirements are met but gaps exist |
| **Not Implemented** | The control has not been implemented |

Do not inflate your attestation. Counterparties and SWIFT can query your responses, and regulators may review attestations. Honest partial or non-implementation responses are preferable to false "implemented" claims.

**Step 4: Attest Advisory Controls (Optional)**
You may also attest advisory controls (1.3A, 1.5A, 2.4A, 2.5A, 2.9A, 2.11A, 5.3A, 6.5A, 7.3A, 7.4A). Attesting advisory controls as implemented signals security maturity to counterparties.

**Step 5: Provide Assessor Information**
- Enter details of the independent assessor who conducted the assessment
- Confirm the assessment type (Community Standard Assessment)
- Include the assessment date

**Step 6: Senior Management Sign-Off**
- The attestation must be approved by a senior officer of the institution (typically the CISO or equivalent)
- This constitutes a formal declaration that the information provided is accurate

**Step 7: Submit Before July 31**
- Submit by July 31 to meet the annual deadline
- The portal will confirm submission and provide a submission reference

---

## Section 5: High-Priority Controls to Check Before Submission

The following controls are most commonly cited in assessments and should receive extra scrutiny before you attest:

| Control | Common Problem | What to Verify |
|---------|---------------|----------------|
| **4.2 MFA** | Software OTP apps used instead of hardware tokens | Confirm hardware tokens (or FIDO2/smart cards) are deployed for every SWIFT operator; no exceptions |
| **1.1 SWIFT Environment Protection** | Servers on shared segments; no documented Secure Zone | Confirm dedicated VLAN/segment, firewall rules in place, no dual-homed systems, up-to-date network diagram |
| **6.4 Log and Monitoring** | SIEM not ingesting SWIFT-specific sources; retention under 1 year | Confirm Alliance Access/Gateway logs are in SIEM; confirm 1-year online + 3-year total retention |
| **2.2 Security Updates** | Critical patches >3 days overdue; no documented exceptions | Confirm patch SLAs met: critical = 3 days, high = 90 days; exceptions documented with risk acceptance |
| **5.1 Logical Access Controls** | Shared operator accounts | Confirm every SWIFT operator has an individual named account; stale accounts removed |
| **2.7 Vulnerability Scanning** | Scans not credentialed; SWIFT components not in scope | Confirm quarterly credentialed scans include all SWIFT system IPs/hostnames |
| **7.1 Incident Response Planning** | No SWIFT-specific IRP; SWIFT notification obligation not documented | Confirm IRP covers 24-hour SWIFT notification, fraud scenarios, annual test completed |
| **3.1 Physical Security** | Server room access not logged | Confirm electronic badge access with audit trail restricted to named individuals |

---

## Section 6: What Happens After Submission

### Immediate Visibility
- Once submitted, your attestation is **immediately visible** to your counterparties via the KYC-SA portal
- Counterparties can view your compliance status for each control when assessing correspondent banking or settlement relationships

### Counterparty Queries
- Counterparties may contact you directly to request clarification or additional details about specific controls
- You should be prepared to share supporting evidence or explain any partially-implemented or not-implemented controls and your remediation timeline

### Consequences of Non-Attestation
- If you fail to submit by July 31, SWIFT will **flag your institution to counterparties** as non-attesting
- This can trigger counterparty due diligence escalations, suspension of correspondent relationships, or regulatory escalation
- In some jurisdictions (EU, UK, Singapore, HK, Australia), regulators explicitly reference SWIFT CSP compliance in their supervisory expectations

### Consequences of Non-Compliance
- Controls attested as "Partially Implemented" or "Not Implemented" will be visible to counterparties
- Counterparties may restrict payment flows or impose additional conditions
- Potential regulatory escalation by supervisors who monitor SWIFT CSP compliance
- SWIFT itself may notify your regulator in significant non-compliance cases

### Ongoing Obligations
- Maintain your CSCF controls throughout the year — attestation is a point-in-time declaration but controls must be continuously operative
- Monitor SWIFT for CSCF v2025 or any updated guidance (CSCF v2025 effective July 2025; ensure you are attesting against the correct version)
- If a cyber incident occurs after submission: notify SWIFT within 24 hours of confirmation (security@swift.com), submit a full incident report within 30 days

---

## Section 7: Recommended Timeline for A1 Bank Targeting July 31 Deadline

| Target Date | Activity |
|-------------|----------|
| **Now (April 25)** | Assign internal project owner; confirm architecture type A1; compile initial evidence inventory |
| **By May 9** | Complete evidence collection for all 23 mandatory controls; identify gaps |
| **By May 16** | Engage independent assessor; conduct gap walkthrough |
| **May 16 – June 13** | Independent assessor conducts formal review of all 23 mandatory controls |
| **By June 20** | Receive preliminary assessor findings; close any critical gaps (especially 4.2, 1.1, 6.4) |
| **By June 30** | Final assessment report issued by independent assessor |
| **By July 15** | Complete KYC-SA form in portal; senior management review |
| **By July 25** | Senior management sign-off and submission (6 days buffer before deadline) |
| **July 31** | HARD DEADLINE — KYC-SA submitted |

---

## Section 8: Critical Compliance Note on CSCF v2025 Changes

If you are attesting under CSCF v2025 (effective July 2025), be aware of these tightened requirements vs. v2024:

- **Control 2.2 Patching SLA:** Critical patches must now be applied within **3 calendar days** (previously 7 days). Verify your patch management SLAs are updated.
- **Control 4.2 MFA:** Hardware tokens are **explicitly required** for A1 architecture; the previous language "strongly recommended" has been replaced with a definitive requirement. App-based OTP is clearly insufficient.
- **Control 6.4 Log retention:** Clarified as 1 year online/hot and 3 years total (online + archived). Confirm both retention tiers are in place.

---

## Summary

As an A1 architecture bank, your KYC-SA attestation involves:

1. **Preparing evidence** for all 23 mandatory CSCF v2025 controls — starting with the highest-risk areas (4.2, 1.1, 6.4, 2.2)
2. **Engaging an independent assessor** — internal audit (if independent from SWIFT ops) or an external firm/SWIFT-certified assessor — who completes a Community Standard Assessment
3. **Completing the KYC-SA form** at swift.com/myswift, attesting Implemented/Partially Implemented/Not Implemented for each mandatory control, with senior management sign-off
4. **Submitting by July 31** — counterparties see your attestation immediately upon submission; non-submission triggers counterparty flagging

With ~97 days remaining as of today, there is sufficient time to complete this properly if evidence collection and assessor engagement begin immediately.
