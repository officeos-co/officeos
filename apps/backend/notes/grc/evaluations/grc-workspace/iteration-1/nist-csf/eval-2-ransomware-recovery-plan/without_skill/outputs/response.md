# 90-Day Ransomware Recovery and Improvement Plan
## NIST CSF Framework — Respond & Recover Functions
### Mid-Size Manufacturing Company

---

## Overview

This 90-day plan uses the NIST Cybersecurity Framework (CSF) Respond and Recover functions as its backbone to guide your manufacturing organization from active incident response through operational restoration and toward improved cybersecurity resilience.

---

## Understanding the Two Key Functions

**Respond (RS)**: Activities to take action when a cybersecurity incident is detected. This includes incident management, analysis, mitigation, and communication.

**Recover (RC)**: Activities to restore operations and systems affected by the incident, and to improve resilience to prevent or minimize future incidents.

---

## Phase 1: Days 1–30 — Immediate Response

### Respond Function Activities

**Incident Management**
- Activate your Incident Response Plan (IRP) immediately. If you don't have one, establish an emergency response team with defined roles: Incident Commander, IT Lead, Legal/Compliance, Communications.
- Engage your cyber insurance carrier — they may provide IR retainer services.
- Retain an external incident response firm for forensic investigation if internal capabilities are limited.
- Isolate affected systems immediately to prevent ransomware from spreading further across your manufacturing network and OT (operational technology) systems.

**Incident Analysis**
- Identify the ransomware variant to understand capabilities (data encryption only vs. data exfiltration/double extortion).
- Determine the initial attack vector: phishing email, exposed RDP, unpatched VPN, or compromised vendor access.
- Map the full scope of compromise: which systems are affected, which data was encrypted or potentially stolen.
- Assess the status of all backups — determine if backups were also encrypted or corrupted.
- Preserve forensic evidence before any remediation activities begin.

**Communication**
- Notify executive leadership and the board within 24 hours.
- Engage legal counsel to assess notification obligations under state breach notification laws and sector-specific regulations.
- Determine whether ransom payment should be considered — consult with the FBI and your legal team before making any payment decision.
- Prepare internal communications for employees and external communications for customers if production is disrupted.
- Report to CISA and FBI as appropriate.

**Mitigation**
- Reset all credentials across the organization, starting with privileged accounts.
- Disable or block malicious indicators of compromise identified during forensics.
- Enforce multi-factor authentication on all remote access connections.

---

## Phase 2: Days 31–60 — Recovery and Restoration

### Recover Function Activities

**Recovery Planning and Execution**
- Prioritize systems for recovery based on operational criticality: manufacturing/OT systems first, then ERP and business systems, then general IT.
- Validate backup integrity before restoring — ensure backups pre-date the infection and are clean.
- Rebuild compromised systems from known-good images rather than attempting to clean infected systems.
- Test restored systems in an isolated environment before reintroducing them to production.
- Document all recovery steps for insurance claims and post-incident review.

**Protecting Manufacturing Operations**
- If production lines were disrupted, engage OT vendors for specialized recovery of industrial control systems.
- Implement emergency network segmentation between IT and OT environments to prevent cross-contamination during recovery.
- Validate that manufacturing equipment and SCADA/PLC systems are not compromised before resuming automated operations.

**Communication During Recovery**
- Provide daily recovery status briefings to leadership.
- Update customers and partners on expected production restoration timelines.
- Coordinate with your cyber insurance carrier on claim documentation.

### Root Cause Remediation
While restoring operations, begin addressing the root cause:
- Patch the vulnerability that allowed initial access.
- Implement stronger email filtering if phishing was the vector.
- Disable unnecessary remote access protocols (particularly open RDP).
- Implement network segmentation to limit lateral movement.
- Review and harden backup systems: implement air-gapped or immutable backups.

---

## Phase 3: Days 61–90 — Improvement and Resilience

### Strengthening Both Functions Long-Term

**Improve Your Respond Capability**
- Update and formalize your Incident Response Plan based on lessons learned from this incident.
- Conduct a tabletop exercise against the updated IRP with leadership and key IT staff.
- Establish relationships with external IR firms, legal counsel, and law enforcement contacts before the next incident.
- Define communication templates and escalation procedures in advance.

**Improve Your Recover Capability**
- Implement a robust backup strategy following the 3-2-1 rule: 3 copies, 2 different media types, 1 offsite.
- Consider immutable cloud backups that cannot be encrypted by ransomware.
- Define and document Recovery Time Objectives (RTOs) and Recovery Point Objectives (RPOs) for all critical systems.
- Conduct a full disaster recovery test to validate you can recover critical systems within your RTO.

**Build Detection Capabilities**
- Deploy or improve endpoint detection and response (EDR) tools across all endpoints.
- Implement centralized logging and monitoring to detect ransomware behaviors (mass file encryption, unusual network traffic) earlier.
- Consider a Security Operations Center (SOC) capability, either in-house or outsourced.

**Governance and Policy**
- Conduct a formal post-incident review (lessons learned session) with all involved parties.
- Update your cybersecurity risk register with ransomware as a key risk.
- Review cyber insurance coverage — assess whether your current policy is adequate.
- Establish regular security awareness training with emphasis on phishing recognition.

---

## Manufacturing-Specific Priorities

**OT/IT Separation**: Manufacturing environments often have IT and OT networks that should be separated. Ensure ransomware cannot pivot from corporate IT to production control systems.

**Vendor Access**: Third-party vendors with remote access to your manufacturing systems are a common attack vector. Implement strict access controls and monitoring for all vendor connections.

**Production Recovery Priority**: Coordinate your IT recovery plan with operations leadership to align with production schedules and customer commitments.

---

## 90-Day Summary

| Days | Phase | Primary Focus |
|------|-------|---------------|
| 1–7 | Respond | Contain, preserve evidence, notify stakeholders |
| 8–21 | Respond | Full scope assessment, mitigation, legal/regulatory |
| 22–30 | Respond + Recover | Begin recovery planning, restore priority systems |
| 31–60 | Recover | System restoration, root cause remediation |
| 61–90 | Improve | Update plans, harden defenses, conduct exercises |

---

By following this structured approach using the NIST CSF Respond and Recover functions, your manufacturing organization will move from crisis to resilience in a disciplined, defensible manner that also satisfies the documentation needs of your insurance carrier, legal team, and any regulatory bodies involved.
