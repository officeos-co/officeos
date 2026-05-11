# 90-Day Ransomware Recovery and Improvement Plan
## NIST CSF 2.0 — Respond & Recover Functions
### Context: Mid-Size Manufacturing Company

---

## Executive Summary

This 90-day plan applies the NIST CSF 2.0 **Respond (RS)** and **Recover (RC)** functions to guide your organization from active incident response through operational restoration and into a sustained improvement posture. Given your manufacturing context, this plan also accounts for OT/IT convergence risks and supply chain exposure per **GV.SC**.

The plan is structured in three 30-day phases:
- **Days 1–30**: Crisis response, containment, and stabilization
- **Days 31–60**: Operational restoration and gap closure
- **Days 61–90**: Resilience hardening and process improvement

---

## Phase 1: Days 1–30 — Respond: Contain, Analyze, Communicate

### Respond Function (RS) Actions

#### RS.MA — Incident Management
| # | Action | CSF Subcategory | Owner | Priority |
|---|--------|-----------------|-------|----------|
| 1 | Activate the Incident Response Plan (IRP); if none exists, stand up an emergency IR working group | RS.MA-01 | CISO / IT Director | Critical |
| 2 | Establish incident tracking in a ticketing system or IR platform; assign Incident Commander | RS.MA-02 | CISO | Critical |
| 3 | Isolate infected systems from production network — segment OT/SCADA from IT immediately | RS.MA-03 | IT Ops / OT Team | Critical |
| 4 | Engage external IR retainer / forensics firm if not done already | RS.MA-04 | CISO | Critical |
| 5 | Preserve all forensic evidence: memory dumps, log files, disk images before any remediation | RS.MA-05 | IR Team / Forensics | High |

#### RS.AN — Incident Analysis
| # | Action | CSF Subcategory | Owner | Priority |
|---|--------|-----------------|-------|----------|
| 6 | Determine ransomware variant, initial access vector (phishing, RDP, VPN vulnerability, supply chain) | RS.AN-03 | Forensics / IR Team | Critical |
| 7 | Map scope of compromise: which systems, data types, OT assets affected | RS.AN-06 | IR Team | Critical |
| 8 | Assess whether data was exfiltrated (double-extortion ransomware) | RS.AN-07 | IR Team / Legal | High |
| 9 | Identify all affected backup systems and determine backup integrity | RS.AN-08 | IT Ops | Critical |
| 10 | Review logs from perimeter, endpoint (EDR), and identity systems to establish timeline | RS.AN-02 | SOC / Forensics | High |

#### RS.CO — Incident Response Reporting and Communication
| # | Action | CSF Subcategory | Owner | Priority |
|---|--------|-----------------|-------|----------|
| 11 | Notify executive leadership and board within 24 hours | RS.CO-01 | CISO / CEO | Critical |
| 12 | Engage legal counsel and cyber insurance carrier immediately | RS.CO-02 | Legal / CFO | Critical |
| 13 | Assess regulatory notification obligations (state breach notification, CISA reporting, sector requirements) | RS.CO-03 | Legal / Compliance | Critical |
| 14 | Prepare customer/partner communications if production impact affects supply chain | RS.CO-04 | Communications / CEO | High |
| 15 | Report to CISA (if applicable) and FBI — coordinate with law enforcement on ransom payment decisions | RS.CO-05 | CISO / Legal | High |

#### RS.MI — Incident Mitigation
| # | Action | CSF Subcategory | Owner | Priority |
|---|--------|-----------------|-------|----------|
| 16 | Reset all privileged account credentials across IT and OT domains | RS.MI-01 | IT Ops | Critical |
| 17 | Force MFA enrollment on all remote access (VPN, RDP, cloud services) | RS.MI-02 | IT Ops | Critical |
| 18 | Disable or block identified malicious IOCs at perimeter and endpoint | RS.MI-03 | SOC / IT Ops | Critical |
| 19 | Determine ransom payment decision — engage FBI, insurance, legal before any payment | RS.MI-04 | Executive / Legal | Critical |

---

## Phase 2: Days 31–60 — Recover: Restore Operations and Address Root Causes

### Recover Function (RC) Actions

#### RC.RP — Incident Recovery Plan Execution
| # | Action | CSF Subcategory | Owner | Priority |
|---|--------|-----------------|-------|----------|
| 20 | Establish recovery prioritization: OT/manufacturing systems first, then ERP, then corporate IT | RC.RP-01 | CISO / OT Manager | Critical |
| 21 | Validate backup integrity before restoration; restore from known-good pre-infection backups | RC.RP-03 | IT Ops | Critical |
| 22 | Rebuild compromised systems from golden images rather than attempting in-place recovery | RC.RP-04 | IT Ops | High |
| 23 | Test restored systems in isolated environment before reconnecting to production | RC.RP-05 | IT Ops / OT Team | High |
| 24 | Document all recovery actions and validate against Business Continuity Plan (BCP) | RC.RP-06 | CISO / IT Director | Medium |

#### RC.CO — Incident Recovery Communication
| # | Action | CSF Subcategory | Owner | Priority |
|---|--------|-----------------|-------|----------|
| 25 | Provide regular recovery status updates to leadership (daily briefings) | RC.CO-03 | CISO | High |
| 26 | Notify customers, partners, and regulators of recovery timeline and expected service restoration | RC.CO-04 | Communications / Legal | High |
| 27 | Update cyber insurance carrier with recovery cost tracking | RC.CO-05 | CFO / Legal | Medium |

### Concurrent Protect Function Hardening (Phase 2)

While restoring operations, close identified attack vectors:

| # | Action | CSF Subcategory | Owner | Timeline |
|---|--------|-----------------|-------|----------|
| 28 | Implement privileged access workstations (PAWs) and tiered admin model | PR.AA-05 | IT Ops | Day 31–45 |
| 29 | Enforce network segmentation between OT and IT environments | PR.IR-01 | OT/IT Teams | Day 31–60 |
| 30 | Validate and test backup strategy — implement 3-2-1-1 backup rule | PR.IR-04 | IT Ops | Day 31–45 |
| 31 | Deploy or tune EDR across all endpoints including OT where applicable | DE.CM-01 | SecOps | Day 31–60 |
| 32 | Patch all critical and high vulnerabilities identified during forensics | PR.PS-02 | IT Ops | Day 31–60 |

---

## Phase 3: Days 61–90 — Improve: Build Resilience and Prevent Recurrence

### Govern Function — Post-Incident Governance Actions

| # | Action | CSF Subcategory | Owner | Priority |
|---|--------|-----------------|-------|----------|
| 33 | Conduct formal post-incident review (lessons learned); update IRP with findings | GV.OV-03 | CISO / Executive | High |
| 34 | Update organizational risk tolerance statement based on incident outcomes | GV.RM-04 | CISO / Board | High |
| 35 | Review and update cybersecurity policy to reflect new controls and procedures | GV.PO-02 | CISO / Legal | Medium |
| 36 | Assess supply chain / vendor exposure — did a supplier contribute to the compromise? | GV.SC-04 | Procurement / CISO | High |
| 37 | Conduct vendor security reviews for critical suppliers | GV.SC-06 | Procurement / CISO | Medium |

### Identify Function — Risk Assessment Update

| # | Action | CSF Subcategory | Owner | Priority |
|---|--------|-----------------|-------|----------|
| 38 | Update asset inventory with any newly discovered systems or shadow IT | ID.AM-01 | IT Ops | High |
| 39 | Conduct updated risk assessment incorporating ransomware attack vectors | ID.RA-01 | CISO / Risk Team | High |
| 40 | Build or update improvement plan with prioritized controls from gap analysis | ID.IM-01 | CISO | High |

### Detect & Respond Improvements

| # | Action | CSF Subcategory | Owner | Priority |
|---|--------|-----------------|-------|----------|
| 41 | Deploy or tune SIEM with ransomware detection use cases | DE.CM-09 | SOC | High |
| 42 | Implement behavioral analytics / UEBA to detect lateral movement | DE.AE-02 | SOC | Medium |
| 43 | Conduct tabletop ransomware exercise against updated IRP | RS.MA-05 | CISO / IR Team | High |
| 44 | Test backup recovery procedure under simulated ransomware scenario | RC.RP-05 | IT Ops / CISO | High |

### Awareness and Training

| # | Action | CSF Subcategory | Owner | Priority |
|---|--------|-----------------|-------|----------|
| 45 | Conduct mandatory phishing awareness training for all staff | PR.AT-01 | HR / Security | High |
| 46 | Deliver targeted OT security training for plant operators | PR.AT-02 | OT Manager / Security | Medium |

---

## Manufacturing-Specific Considerations

Given your manufacturing context, apply these sector-specific priorities:

1. **OT/IT Convergence (PR.IR, DE.CM)**: Ransomware in manufacturing often spreads to OT networks, disrupting production lines. Ensure physical process isolation controls exist independent of IT systems.

2. **Supply Chain Risk (GV.SC)**: Manufacturing relies heavily on suppliers — assess whether the ransomware entry point was via a vendor connection, EDI system, or third-party remote access.

3. **Production Continuity (RC.RP)**: Recovery prioritization should align with production schedules and customer delivery commitments. Coordinate with operations leadership on acceptable downtime windows.

4. **ICS/SCADA Recovery**: OT system recovery requires specialized expertise — engage vendors for proprietary systems and reference ICS-CERT guidance for industrial control system restoration.

---

## 90-Day Summary Timeline

| Week | Phase | Focus |
|------|-------|-------|
| 1 | Respond | Containment, forensics, executive notification |
| 2 | Respond | Scope determination, legal/regulatory notifications |
| 3 | Respond + Recover | Mitigation, begin recovery prioritization |
| 4 | Recover | Begin phased system restoration |
| 5–6 | Recover | OT restoration, ERP recovery |
| 7–8 | Recover + Improve | Full production restoration, vulnerability patching |
| 9–10 | Improve | Governance updates, risk reassessment |
| 11–12 | Improve | Tabletop exercise, training, updated IRP |

---

> **Note**: This plan applies NIST CSF 2.0 (February 2024). Manufacturing organizations should additionally consult ICS-CERT guidance and NIST SP 800-82 for OT-specific recovery considerations. This does not constitute legal or professional compliance advice.
