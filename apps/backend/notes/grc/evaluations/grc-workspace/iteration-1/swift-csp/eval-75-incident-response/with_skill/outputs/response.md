# SWIFT CSP Control 7.1 — Cyber Incident Response: Malware on SWIFT-Connected Server

**Scenario:** Malware discovered on a SWIFT-connected server; fraudulent transactions suspected.
**Framework:** SWIFT Customer Security Controls Framework (CSCF) v2025
**Primary Control:** 7.1 — Cyber Incident Response Planning (Mandatory — all architecture types)
**Related Controls:** 6.1 (Malware Protection), 6.4 (Log and Monitoring), 6.2 (Software Integrity), 5.1 (Logical Access Controls)

---

## Part 1: Immediate Incident Response Obligations Under Control 7.1

### Step 1 — Declare a Cyber Incident Immediately (T+0)

The moment malware is confirmed on a SWIFT-connected server, this constitutes a **notifiable cyber incident** under CSCF v2025 Control 7.1. SWIFT explicitly defines the following as qualifying incidents requiring notification:

- Malware found on SWIFT-connected systems
- Fraudulent or anomalous SWIFT transactions suspected to be cyber-related
- Unauthorised access to SWIFT systems or software
- Compromise of SWIFT credentials (operator tokens or passwords)

Both conditions in this scenario — malware presence and suspected fraudulent transactions — independently trigger mandatory incident response obligations. Do not wait for forensic confirmation before beginning notifications.

**Action at T+0:**
- Activate the SWIFT-specific Incident Response Plan (IRP)
- Convene the incident response team (security, IT operations, compliance, legal, senior management)
- Escalate immediately to senior management — this is a mandatory internal escalation per SWIFT requirements
- Begin a formal incident log capturing all actions, timestamps, and findings

---

### Step 2 — Internal Containment (T+0 to T+2 hours)

Before notifications go external, execute immediate containment to prevent further damage:

1. **Isolate the affected server** — Remove the compromised SWIFT-connected server from the SWIFT Secure Zone network immediately. Disconnect from the corporate network and any back-office interfaces. Do not power off the system — this destroys volatile memory evidence (see Evidence Preservation, below).
2. **Suspend SWIFT operator sessions** — Force-terminate all active operator sessions on the compromised system. Revoke or suspend any SWIFT operator tokens associated with the affected server (Control 5.2).
3. **Block outbound SWIFT transactions from the compromised host** — If the server initiates SWIFT messages (e.g., Alliance Access), disable or redirect the messaging queue at the gateway level to prevent further fraudulent message transmission.
4. **Preserve existing transaction queues** — Do not delete or flush pending transaction queues; capture a snapshot for forensic review before any queue management action.
5. **Alert the SIEM / security monitoring team** — Confirm that Control 6.4 log sources for the affected server are being actively collected and that no log rotation is occurring that could destroy evidence.

---

### Step 3 — SWIFT Notification (Within 24 Hours of Confirmed Incident)

**SWIFT requires initial notification within 24 hours of confirming a cyber incident affecting SWIFT infrastructure or transactions.** This is a hard deadline under CSCF v2025 Control 7.1 — it is not discretionary.

| Notification | Channel | Deadline |
|---|---|---|
| SWIFT CISO Office | security@swift.com | Within 24 hours of confirmed incident |
| SWIFT Relationship Manager | Your named SWIFT relationship manager | Within 24 hours of confirmed incident |
| KYC-SA Portal (if incident affects attestation) | SWIFT KYC-SA portal at swift.com/myswift | As applicable; coordinate with your attestation submission |
| Internal senior management | Direct escalation — verbal + written | Immediately upon detection (T+0) |
| Law enforcement | Per local jurisdiction (see below) | Per regulatory requirements |
| Prudential regulator / central bank | Per local jurisdiction (see below) | Per regulatory requirements |

**What to include in the initial 24-hour SWIFT notification:**

- Your BIC (Bank Identifier Code) / SWIFT user identifier
- Date and time the incident was detected
- Nature of the incident (malware on SWIFT-connected server; suspected fraudulent transactions)
- Systems and infrastructure components affected (which server, which SWIFT components — e.g., Alliance Access, Alliance Gateway)
- Whether fraudulent SWIFT messages are confirmed or only suspected, and if known, the approximate transaction amounts and counterparties
- Immediate containment steps taken
- Contact details of your incident response lead and CISO

Do not wait for full forensic investigation before sending the initial notification. If facts change, update SWIFT in subsequent communications.

---

### Step 4 — Regulatory and Law Enforcement Notification (Jurisdiction-Dependent)

In addition to notifying SWIFT, your organisation has regulatory obligations that vary by jurisdiction. These run in parallel to the SWIFT notification — they are not sequential:

| Jurisdiction | Regulator / Authority | Typical Obligation |
|---|---|---|
| EU | National Competent Authority (NCA) under DORA; ECB for significant institutions | DORA requires initial notification within 4 hours of major incident classification; full report within 1 month |
| UK | Bank of England / PRA; FCA | PRA/FCA Operational Resilience framework; notify within 24 hours for material incidents |
| US | FinCEN (if fraud confirmed); OCC/Federal Reserve (for regulated banks); FBI Cyber Division | File Suspicious Activity Report (SAR) within 30 days of fraud detection; notify primary regulator per FFIEC guidance |
| Singapore | MAS | MAS TRM Guidelines: notify within 1 hour of discovery for material incidents |
| Hong Kong | HKMA | HKMA Cybersecurity Fortification Initiative: notify promptly; within 1 hour for critical incidents |
| Australia | APRA | CPS 234: notify APRA within 72 hours of becoming aware of a material cyber security incident |

**Law enforcement:** Report to national law enforcement cyber units (e.g., FBI Cyber Division in the US, Action Fraud / NCSC in the UK, BKA in Germany) at the same time or immediately after regulatory notification. For suspected fraud involving wire transfers, also notify the receiving correspondent banks to initiate potential recall.

---

### Step 5 — Transaction Recall and Counterparty Notifications (T+2 to T+24 hours)

If fraudulent SWIFT messages were transmitted:

1. **Identify all suspect transactions** — Review Alliance Access / Alliance Gateway message logs for all messages transmitted from the compromised server in the period the malware was likely active. Use Control 6.4 SIEM logs to reconstruct the timeline.
2. **Send SWIFT payment cancellation / recall requests** — Use SWIFT's standard gpi (Global Payments Innovation) recall mechanism where applicable. For MT messages, send MT n92 recall requests to receiving correspondent banks immediately.
3. **Contact receiving banks directly** — Telephone and written notification to correspondent banking partners requesting recall; do not rely solely on SWIFT gpi messaging.
4. **Contact your nostro/correspondent accounts** — Alert all banks that hold accounts on your behalf to monitor for suspicious activity.
5. **Engage your cyber insurance carrier** — Notify cyber insurance as required by policy; most policies have strict notification windows (typically 24–72 hours).

---

### Step 6 — Full Incident Report to SWIFT (Within 30 Days)

Following the initial 24-hour notification, a **full incident report must be submitted to SWIFT within 30 days**. This report must document:

- Complete timeline from initial compromise to containment and recovery
- Root cause analysis findings
- All systems affected and scope of SWIFT infrastructure compromise
- List of all SWIFT transactions that were fraudulent or suspected fraudulent (BIC, amount, date, counterparty)
- Evidence of remediation steps taken
- Improvements made or planned to controls (particularly 6.1 Malware Protection, 6.4 Log and Monitoring, 7.1 IRP)
- Updated KYC-SA attestation status (if the incident impacts a mandatory control)

---

## Part 2: Evidence Preservation Requirements

Proper evidence preservation is both a legal requirement and a CSCF v2025 Control 7.1 obligation. Your SWIFT-specific IRP must define these procedures.

### Do Not Power Off the Compromised Server

Powering off destroys volatile memory (RAM) which may contain malware artifacts, encryption keys, active network connections, and running processes. Instead:

1. **Capture volatile memory (RAM image)** immediately using forensic tools (e.g., Magnet RAM Capture, WinPmem, LiME for Linux). This is the single most time-sensitive evidence preservation step.
2. **Capture running process list and network connections** — Document all active processes, open file handles, and network connections before any changes (use tools such as `ps`, `netstat`, `lsof`, or equivalent).
3. **Capture network traffic** — If network monitoring is in place, preserve packet captures for the SWIFT Secure Zone for the relevant period.

### Disk and System Evidence

4. **Create forensic disk images** of the compromised server (bit-for-bit copy using tools such as FTK Imager or dd with write-blockers) before any remediation activity alters the disk.
5. **Preserve all log files** — Extract and archive:
   - Alliance Access / Alliance Gateway application logs
   - Operating system security logs (Windows Event Logs or Linux auditd/syslog)
   - Authentication logs (MFA, Active Directory, PAM logs)
   - Database audit logs (Control 6.3)
   - Network device logs covering the SWIFT Secure Zone (firewall, switch logs)
   - Anti-malware detection logs (Control 6.1)
   - File Integrity Monitoring (FIM) logs if deployed (Control 6.2)
6. **Preserve SIEM data** — Ensure the SIEM retains all relevant log data and that log rotation is suspended for the affected sources during the investigation period. CSCF v2025 requires 1-year online retention and 3-year total retention (Control 6.4) — confirm these are intact and not at risk.

### SWIFT Transaction Evidence

7. **Export SWIFT message archives** — Capture all SWIFT messages transmitted and received in the relevant period (from the start of the suspected compromise window to the time of containment) from Alliance Access message stores.
8. **Preserve transaction reconciliation records** — Export back-office transaction records and reconcile against SWIFT message logs to identify any discrepancies or transactions that appear in SWIFT but not in the core banking system (a common indicator of fraudulent injections).
9. **Capture Alliance Access / Gateway configuration** — Take a snapshot of current application configuration (user accounts, roles, permissions, interface settings) to establish the state at time of incident.

### Chain of Custody

10. **Establish chain of custody documentation** for all evidence collected — record who collected it, when, from which system, using what tool, and where it is stored. This is essential for any subsequent legal proceedings or regulatory investigation.
11. **Store evidence in a secure, access-controlled location** separate from the production environment — ideally on write-once media or in an immutable evidence repository.
12. **Engage qualified forensic investigators** — For SWIFT incidents involving suspected fraud, engage a qualified digital forensics firm to supplement internal capabilities. Many regulators and SWIFT itself will expect professional forensic involvement.

---

## Part 3: SWIFT-Specific Incident Response Plan (IRP) — Required Content for CSCF v2025 Compliance

Control 7.1 requires a documented, tested SWIFT-specific IRP. A generic enterprise incident response plan is insufficient. The following content is mandatory for compliance.

### 1. Scope and Purpose

- Explicit statement that the IRP covers the SWIFT Secure Zone and all SWIFT-connected systems
- Reference to the CSCF v2025 Control 7.1 compliance requirement
- Identification of which SWIFT architecture type (A1/A2/A3/A4/B) the plan covers
- List of in-scope SWIFT components (Alliance Access, Alliance Gateway, HSMs, operator workstations, back-office interfaces)

### 2. Incident Classification for SWIFT Scenarios

The IRP must define which events constitute a SWIFT cyber incident requiring activation. At minimum, include:

| Incident Type | Trigger |
|---|---|
| Malware on SWIFT-connected system | Anti-malware alert from any in-scope SWIFT system |
| Suspected fraudulent SWIFT transaction | Anomalous transaction alert; back-office reconciliation discrepancy; counterparty notification |
| Compromise of SWIFT operator credentials | Suspicious authentication event; lost/stolen hardware token; phishing report from SWIFT user |
| Unauthorised change to SWIFT configuration | FIM alert; database change alert; unauthorized account creation in Alliance Access |
| Ransomware or destructive malware in SWIFT zone | Encryption of SWIFT server files; loss of Alliance Access availability |
| SWIFT software integrity failure | Hash verification failure (Control 6.2); unexpected change to SWIFT binaries |
| Physical security breach of SWIFT server room | Forced entry; tailgating; stolen hardware |

### 3. Roles and Responsibilities

Define named roles (not just job titles — name the individuals where possible):

- **Incident Commander** — Accountable for overall incident management; typically CISO or Head of IT Security
- **SWIFT Operations Lead** — Technical lead for SWIFT-specific containment and recovery
- **Legal Counsel** — Advises on regulatory notification obligations and law enforcement engagement
- **Compliance Officer** — Coordinates KYC-SA attestation updates; manages SWIFT relationship notifications
- **Communications Lead** — Manages external communications (regulators, counterparties, press)
- **Forensics Lead** — Internal or external forensic investigator
- **Executive Sponsor** — C-suite escalation (CEO/CFO for material fraud events)

### 4. SWIFT-Specific Notification Procedures

This section must be explicit and actionable — it is the most commonly missing element in non-compliant IRPs:

- **24-hour SWIFT notification procedure** — Step-by-step instructions for contacting security@swift.com and the SWIFT relationship manager, including what information to include (see Part 1, Step 3 above)
- **Contact list** — Current email addresses, phone numbers, and out-of-hours contacts for: SWIFT CISO office, your SWIFT relationship manager, your SWIFT service bureau (if Type B), and your primary regulatory supervisor
- **30-day full report procedure** — Template or outline for the comprehensive incident report to SWIFT
- **KYC-SA portal update procedure** — Process for updating your attestation if the incident reveals a compliance gap in a mandatory control
- **Internal escalation matrix** — Who notifies whom at what severity level; escalation to board level for major fraud events

### 5. Detection Triggers and Initial Triage

- List of monitoring sources that generate SWIFT incident alerts (SIEM rules, anti-malware, FIM, IDS if deployed under Control 6.5A)
- Initial triage checklist — questions to determine severity and scope within the first 30 minutes:
  - Which SWIFT components are affected?
  - Is the malware still active or has it been contained?
  - Have any SWIFT transactions been sent since the estimated compromise time?
  - Are operator credentials or hardware tokens potentially compromised?
  - Is the SWIFT Secure Zone boundary intact or has the malware spread?

### 6. Containment Procedures

SWIFT-specific containment steps (beyond generic IR containment):

- Procedure for isolating SWIFT Secure Zone components without disrupting critical financial operations
- Steps to suspend SWIFT messaging throughput (disabling message queues in Alliance Access/Gateway)
- Procedure for emergency suspension of SWIFT operator accounts and hardware tokens
- Firewall rule deployment to block SWIFT zone traffic pending investigation
- Coordination with SWIFT (and service bureau if Type B) to pause processing

### 7. Evidence Preservation Requirements

The IRP must explicitly document (aligned with Part 2 above):

- Mandatory volatile memory capture before any system changes
- Forensic disk imaging procedures
- Log extraction and preservation list (all sources per Control 6.4 scope)
- SWIFT transaction archive export procedure
- Chain of custody documentation requirements
- Evidence storage location and access controls

### 8. Investigation and Root Cause Analysis

- Forensic investigation methodology (or reference to external forensic retainer)
- Requirement to determine: initial infection vector, malware family and capabilities, persistence mechanisms, data/credential exfiltration assessment, full scope of SWIFT transactions affected
- Timeline reconstruction approach using SIEM and SWIFT logs

### 9. Recovery Procedures

- Criteria for declaring the SWIFT environment clean and safe to resume operations (e.g., forensic sign-off; fresh OS build; integrity verification per Control 6.2; SWIFT software reinstallation with hash verification)
- Sequence for restoring SWIFT services — do not resume until: affected systems rebuilt from known-good images, all operator credentials reset, hardware tokens reissued where compromised, firewall rules reviewed and tightened
- Post-recovery verification checklist (transaction reconciliation, log review, integrity check)

### 10. Post-Incident Review and Lessons Learned

- Mandatory post-incident review within 30 days of containment
- Review participants (incident team + senior management)
- Outputs: root cause report, updated IRP (if gaps found), updated KYC-SA attestation, regulatory submissions
- Metrics to capture: time to detect, time to contain, number of fraudulent transactions, financial loss, notification compliance (were 24-hour and 30-day deadlines met?)

### 11. Annual Testing Requirement

CSCF v2025 Control 7.1 requires the IRP to be **tested annually**. The plan must document:

- Annual tabletop exercise — minimum requirement; scenario should include at least one SWIFT-specific attack scenario (e.g., malware on Alliance Access server; compromised SWIFT operator credentials; fraudulent MT103 injection)
- Live drill (recommended every 2 years) — test actual containment procedures without impacting production
- Test record retention — evidence of the annual test must be retained for assessment purposes (test scenario, participants, findings, post-exercise action items)

---

## Part 4: Related Control Obligations Triggered by This Incident

This malware incident triggers compliance review obligations under several controls beyond 7.1:

| Control | Obligation Triggered |
|---|---|
| **6.1 — Malware Protection** | Malware on a SWIFT server indicates a potential Control 6.1 failure. Review: Was anti-malware deployed on the affected server? Were definitions current? Did alerts fire within 1 hour? Update attestation if a gap is found. |
| **6.2 — Software Integrity** | Verify that SWIFT software (Alliance Access/Gateway binaries) has not been tampered with. Run hash verification against SWIFT-published checksums immediately as part of containment. |
| **6.4 — Log and Monitoring** | Confirm all required log sources were active and collected throughout the incident window. Assess whether existing monitoring rules should have detected the malware or anomalous transactions earlier. Review alert configuration. |
| **1.1 — SWIFT Environment Protection** | Assess how malware reached a SWIFT Secure Zone system. Was the zone boundary intact? Were network segmentation controls (firewall rules) effective? If the malware entered from the corporate network, this is a potential Control 1.1 finding. |
| **2.2 — Security Updates** | Was the malware delivered via an unpatched vulnerability? If so, assess patch management compliance. In CSCF v2025, critical patches must be applied within 3 calendar days. |
| **5.1 — Logical Access Controls** | If fraudulent transactions were submitted, review whether they were submitted using a legitimate operator account (potential credential compromise). Audit operator access logs to identify anomalous authentication events. |
| **2.9A — Transaction Business Controls (Advisory)** | If this control is implemented, assess whether transaction value limits or time-of-day restrictions would have detected or blocked the fraudulent transactions. Use this incident to build the case for implementing 2.9A if not yet in place. |

---

## Summary Timeline

| Time | Action |
|---|---|
| T+0 — Detection | Activate SWIFT IRP; escalate to senior management; begin incident log |
| T+0 to T+2 hours | Isolate compromised server; suspend operator sessions and tokens; block outbound SWIFT transactions from affected system; begin evidence preservation (RAM image, process capture) |
| T+2 to T+6 hours | Complete forensic disk imaging; extract and preserve all logs; identify scope of affected SWIFT transactions |
| T+6 to T+24 hours | Submit initial notification to SWIFT (security@swift.com + relationship manager); notify primary regulator per jurisdiction; submit SWIFT transaction recall requests; notify correspondent banks |
| T+24 to T+72 hours | Notify cyber insurance carrier; engage external forensic firm; complete scope assessment; begin root cause analysis |
| T+30 days | Submit full incident report to SWIFT; submit regulatory reports (DORA, FinCEN SAR, etc.); complete internal post-incident review; update KYC-SA attestation if compliance gap identified |

---

## Key Contacts to Maintain in Your IRP (Required by Control 7.1)

- **SWIFT CISO / Security Incidents:** security@swift.com
- **SWIFT Relationship Manager:** [Institution-specific — must be named and number verified annually]
- **SWIFT Customer Support:** swift.com/support or your SWIFT service centre
- **External forensic retainer:** [Institution-specific — retainer contract recommended; do not attempt to source a firm during a live incident]
- **Cyber insurance carrier:** [Institution-specific — policy number and 24-hour notification line]
- **Primary regulator duty officer:** [Institution-specific — verify annually]
- **Law enforcement cyber unit:** [Jurisdiction-specific — e.g., FBI IC3: ic3.gov; UK NCSC: 0800 088 5574]

---

*This guidance is based on SWIFT CSCF v2025 (effective July 2025). Control 7.1 is mandatory for all architecture types (A1, A2, A3, A4, and B). All notification timelines cited are derived from SWIFT's published incident notification obligations. Regulatory notification timelines reflect general requirements and may vary — consult your legal and compliance team for jurisdiction-specific obligations.*
