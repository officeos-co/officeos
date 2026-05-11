# SWIFT CSP Incident Response: Malware on SWIFT-Connected Server

## Scenario
Malware discovered on a SWIFT-connected server with suspected fraudulent transactions.

---

## 1. Immediate Incident Response Obligations Under SWIFT CSP Control 7.1

### What is Control 7.1?
SWIFT Customer Security Programme (CSP) Control 7.1 — **Cyber Incident Response Planning** — is a mandatory control under the Customer Security Controls Framework (CSCF). It requires that SWIFT users have a formal cyber incident response plan that specifically addresses SWIFT-related incidents and that they execute that plan when incidents occur. In CSCF v2025, this remains a mandatory control.

### Immediate Actions Required

**Within the first hours (containment phase):**

1. **Isolate the compromised system** — Immediately isolate the affected SWIFT-connected server from the SWIFT network (SWIFT Alliance Gateway/Access Point or equivalent) and from your internal network to prevent further propagation or unauthorized transaction submission. Do not power off the system if forensically avoidable — preserve volatile memory (RAM) if possible before isolation.

2. **Suspend SWIFT messaging activity** — Halt outbound SWIFT message transmission from the affected system until the scope of compromise is determined. Coordinate with your SWIFT service bureau or correspondent if applicable.

3. **Activate your Incident Response Plan** — Formally declare a cyber incident and activate the documented SWIFT-specific incident response plan. Assign an Incident Commander and convene the response team.

4. **Preserve evidence** (see Section 3 below).

---

## 2. Who to Notify and By When

### A. SWIFT Itself

**Obligation:** SWIFT users are required under the SWIFT CSP/CSCF to report confirmed or suspected fraud related to SWIFT transactions to SWIFT's dedicated reporting channels. SWIFT operates the **SWIFT ISAC (Information Sharing and Analysis Centre)** and has a dedicated security team.

- **How:** Report via the SWIFT Customer Security Intelligence (CSI) portal or directly through SWIFT's security reporting mechanism. As of CSCF v2025, mandatory reporting of cybersecurity incidents affecting SWIFT infrastructure to SWIFT is explicitly required.
- **When:** As soon as a cyber incident involving the SWIFT environment is confirmed or strongly suspected — typically within **24 hours** of discovery, though SWIFT guidance urges "as soon as possible." Do not wait for a full investigation before notifying SWIFT.
- **What to report:** Nature of the incident, affected systems, any suspicious or fraudulent payment messages (with SWIFT transaction references/URNs if available), and initial indicators of compromise (IOCs).

### B. Your Correspondent Banks and Counterparties

- Notify receiving banks of potentially fraudulent transactions immediately so they can place holds or reverse transactions before settlement where possible. Time is critical — SWIFT payments that have already settled may be very difficult to recover.
- Use SWIFT messaging (from a clean, uncompromised system) or out-of-band telephone/email to alert counterparties.
- **When:** Immediately upon identifying suspect transactions — hours matter for fund recovery.

### C. Your Regulators and National Authorities

- **Central Bank / Banking Regulator:** Most jurisdictions require regulated financial institutions to notify their prudential regulator (e.g., the Federal Reserve, ECB, FCA, MAS, RBI) of significant cybersecurity incidents within prescribed timeframes. Common thresholds are **36–72 hours** depending on jurisdiction, but many require notification "without undue delay."
- **Financial Intelligence Unit (FIU) / AML Authority:** If fraudulent transactions are confirmed, a Suspicious Activity Report (SAR) or equivalent may be required under AML/CFT obligations.
- **Law Enforcement:** Contact national cybercrime authorities (e.g., FBI in the US, NCSC in the UK, BKA in Germany) to support investigation and potential recovery of funds.
- **CERT/ISAC:** Notify your national CERT and relevant financial sector ISAC (e.g., FS-ISAC) to support threat intelligence sharing.

### D. Internal Stakeholders

- Senior management, Board (if material), Legal, Compliance, Communications/PR (for potential disclosure obligations), and Insurance (cyber policy notification requirements typically have tight windows, often 24–72 hours).

### E. Data Protection Authorities (if personal data is involved)

- If the breach involves personal data (e.g., customer account information), GDPR or equivalent regulations may require notification to the supervisory authority within **72 hours** of becoming aware of the breach.

---

## 3. Evidence to Preserve

Preserving forensic evidence is critical both for investigation and for meeting CSCF documentation requirements. Do **not** wipe, reimage, or power off systems before capturing:

### System and Network Evidence
- **Full disk images** of the compromised server(s) using forensically sound tools (e.g., write-blocked bit-for-bit copies)
- **Volatile memory (RAM) dumps** captured before isolation or shutdown — malware often exists only in memory
- **Network traffic captures (PCAP)** from IDS/IPS, firewalls, and network taps covering the period of suspected compromise
- **Firewall and proxy logs** showing inbound/outbound connections from the affected server
- **Authentication logs** (Active Directory, RADIUS, local OS logs) showing logon events, privilege escalation, and lateral movement
- **Endpoint Detection and Response (EDR) logs** and antivirus/antimalware scan results
- **Event logs** (Windows Event Logs, Syslog) from the affected server and surrounding infrastructure

### SWIFT-Specific Evidence
- **SWIFT Alliance logs** — all message logs from the SWIFT messaging interface (Alliance Gateway, Alliance Access, Lite2, or SWIFTNet Link) covering the incident window
- **SWIFT message archives** — copies of all outbound messages sent in the period surrounding the incident, particularly any MT 103 (Single Customer Credit Transfer), MT 202 COV, or other payment messages
- **Operator and supervisor activity logs** within the SWIFT application — who logged in, what messages were created/authorized/sent
- **Four-eyes/dual-authorization logs** — evidence of whether approval controls were bypassed
- **SWIFT Relationship Management Application (RMA)** logs showing authorized counterparties
- **Database audit logs** for the SWIFT back-office database
- **Change management records** — any recent changes to the SWIFT environment (patches, configuration changes, new users)

### Malware Artifacts
- **Malware samples** — preserve the actual malicious files in a quarantined/contained manner for forensic analysis
- **Indicators of Compromise (IOCs)** — hashes, filenames, registry keys, C2 IP addresses, domains
- **Timeline of malware execution** reconstructed from logs

### Chain of Custody
- Maintain a formal chain of custody for all forensic evidence. Document who collected what, when, using what tools, and where evidence is stored. This is essential for law enforcement and potential litigation.

---

## 4. SWIFT-Specific Incident Response Plan Requirements Under CSCF v2025

### Control 7.1 Mandatory Requirements

To be compliant with CSCF v2025, your SWIFT-specific incident response plan must contain the following elements:

#### 4.1 Scope and Purpose
- Explicit coverage of the SWIFT environment (all systems in the mandatory and advisory control scope: operator PCs, SWIFT interface processors, back-office connectors, jump servers, HSMs)
- Clear definition of what constitutes a SWIFT-related incident (malware, unauthorized access, fraudulent payment, credential compromise, etc.)

#### 4.2 Roles and Responsibilities
- Named or role-based Incident Commander with authority to make isolation and notification decisions
- Designated SWIFT Security Officer or equivalent who owns CSP compliance and SWIFT reporting
- Contact list for SWIFT, regulators, correspondent banks, law enforcement, legal counsel, and cyber forensics retainer
- Clear escalation paths and decision authority matrix

#### 4.3 Detection and Initial Assessment
- Procedures for confirming that an incident involves the SWIFT environment
- Initial triage steps to determine if fraudulent transactions were sent
- Criteria for escalating to a "SWIFT fraud incident" (elevated response)

#### 4.4 Containment Procedures
- Specific steps for isolating SWIFT components without destroying evidence
- Procedure to suspend SWIFT message transmission
- Guidance on whether/when to engage SWIFT's support team for technical assistance

#### 4.5 Communication and Notification Procedures
- **SWIFT reporting procedure:** Step-by-step instructions for reporting to SWIFT ISAC/CSI portal, including what information to include and timelines
- **Regulatory notification matrix:** Jurisdiction-specific notification requirements with applicable timeframes
- **Correspondent bank alert procedure:** Template communications for notifying counterparties of potentially fraudulent transactions
- **Internal escalation:** Board/senior management notification thresholds

#### 4.6 Evidence Preservation Procedures
- Forensic evidence collection checklist tailored to the SWIFT environment (as described in Section 3 above)
- Chain of custody procedures
- Guidance on working with external forensics firms

#### 4.7 Eradication and Recovery
- Procedures for removing malware and verifying eradication before reconnecting to SWIFT
- Secure rebuild procedures for compromised SWIFT components
- Validation testing before returning to production (including SWIFT connectivity tests)
- Conditions under which SWIFT messaging may be resumed

#### 4.8 Post-Incident Activities
- Root cause analysis (RCA) process
- Lessons learned documentation
- Update of security controls and the incident response plan based on findings
- Reporting outcomes to SWIFT if required (SWIFT may require post-incident reporting on root cause and remediation)
- Regulatory post-incident reporting (some regulators require follow-up reports)

#### 4.9 Testing and Maintenance
- CSCF v2025 requires that the incident response plan be **tested at least annually** through tabletop exercises, simulations, or drills that specifically include SWIFT fraud/malware scenarios
- The plan must be reviewed and updated at least annually and after any significant incident or change to the SWIFT environment

#### 4.10 Integration with Broader Frameworks
- The SWIFT IR plan should be integrated with (or clearly reference) the organization's broader cyber incident response plan, business continuity plan (BCP), and disaster recovery plan (DRP)
- Alignment with SWIFT's published security guidelines (e.g., SWIFT's "Security Guidance for Customers")

---

## 5. Key Timelines Summary

| Action | Timeline |
|--------|----------|
| Isolate compromised system | Immediately upon discovery |
| Preserve volatile memory/RAM | Before isolation/shutdown |
| Notify SWIFT (CSI/ISAC) | As soon as possible; within 24 hours |
| Alert correspondent banks on suspect transactions | Within hours (fund recovery window) |
| Notify banking regulator | Per jurisdiction; typically 36–72 hours |
| Notify data protection authority (if personal data involved) | Within 72 hours (GDPR and equivalents) |
| Notify cyber insurer | Per policy; typically 24–72 hours |
| File SAR with FIU (if fraud confirmed) | Per jurisdiction AML requirements |
| Complete full forensic evidence collection | Within 24–48 hours where possible |

---

## 6. Important Notes

- **Do not reimage or wipe** the compromised server until forensic imaging is complete — doing so destroys evidence and may create regulatory and legal liability.
- **Do not pay ransom** (if ransomware is involved) without consulting legal counsel and law enforcement.
- **Coordinate fund recovery immediately** — SWIFT fraud recovery depends on speed. The SWIFT Payment Controls Service (if subscribed) may assist in flagging or recalling suspect transactions.
- **SWIFT CSP attestation impact:** A confirmed breach of the SWIFT environment will likely require you to update your CSP attestation and may trigger mandatory re-assessment of your security posture.
- Under CSCF v2025, failure to report a SWIFT-related cybersecurity incident to SWIFT can result in sanctions including notification of your supervisors and counterparties by SWIFT directly.

---

*This response is based on general knowledge of SWIFT CSCF requirements, SWIFT's published security guidance, and standard incident response practices as of CSCF v2025. Always verify against the current SWIFT CSCF documentation and applicable regulatory requirements in your jurisdiction.*
