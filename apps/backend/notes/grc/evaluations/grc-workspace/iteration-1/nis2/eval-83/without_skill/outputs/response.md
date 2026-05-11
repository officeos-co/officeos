# NIS2 Incident Reporting: Ransomware Attack Response Guide

## Scenario Context
- **Incident:** Ransomware attack encrypting core operational systems
- **Attack detected:** 09:00 Monday morning
- **Applicable framework:** NIS2 Directive (EU) 2022/2555

---

## Step 1: Confirm NIS2 Applicability

Before acting, confirm your organisation falls under NIS2. The directive covers two tiers:

- **Essential Entities (EE):** Energy, transport, banking, financial market infrastructure, health, drinking water, wastewater, digital infrastructure, ICT service management, public administration, space
- **Important Entities (IE):** Postal/courier, waste management, chemicals, food, manufacturing, digital providers, research

A ransomware attack encrypting **core operational systems** almost certainly constitutes a **"significant incident"** triggering mandatory reporting obligations under Article 23 of NIS2.

### What Makes an Incident "Significant"?

An incident is significant if it causes or is capable of causing:
- Severe operational disruption to your services
- Financial losses to your organisation
- Significant material or immaterial damage to other persons (downstream impact)

Encryption of core operational systems clearly meets this threshold.

---

## Step 2: Who to Report To

### Primary Authority: Your National CSIRT and/or Competent Authority

You must notify **two bodies** (which may be the same in your member state):

1. **CSIRT (Computer Security Incident Response Team):** The national CSIRT designated under NIS2 for your sector/country (e.g., BSI in Germany, NCSC in the Netherlands, CERT-In equivalents across EU)
2. **Competent National Authority:** The sector-specific supervisory authority designated under NIS2 (e.g., national energy regulator, health authority, telecoms regulator)

Each EU member state has designated these bodies — check your national NIS2 implementation law for specifics.

### Secondary Notifications (Where Applicable)

- **Affected service recipients (users/customers):** If the incident affects them significantly, you must notify them without undue delay
- **Other member states' CSIRTs/authorities:** If the incident has cross-border impact
- **ENISA:** For significant cross-border incidents, ENISA may be informed via the national CSIRT network

---

## Step 3: The Three-Stage Reporting Timeline

NIS2 Article 23 mandates a structured, three-stage notification process:

### Stage 1: Early Warning — Within 24 Hours of Becoming Aware

**Deadline:** By 09:00 Tuesday (24 hours after detection at 09:00 Monday)

**Content required:**
- Indication that a significant incident has occurred
- Whether the incident is suspected to be the result of unlawful or malicious acts (ransomware = yes)
- Whether the incident is likely to have cross-border impact

**Format:** Brief, high-level alert. This is a "heads up" notification — full details are not required yet.

**Key point:** "Becoming aware" is from the moment you knew or should have known about the incident — in this case, 09:00 Monday when it was detected. Do not delay reporting while you investigate.

---

### Stage 2: Incident Notification — Within 72 Hours of Becoming Aware

**Deadline:** By 09:00 Thursday (72 hours after detection at 09:00 Monday)

**Content required:**
- Updated assessment of the incident
- Initial severity and impact assessment
- Indicators of compromise (IOCs), where available
- Attack vector or suspected cause
- Nature of the incident (ransomware attack, type if known)
- Systems/services affected
- Estimated number of affected users or entities
- Whether personal data has been affected (if so, GDPR obligations also apply — see below)
- Mitigation measures applied or underway

**Format:** More detailed than the Early Warning, but a full investigation is not expected at this stage.

---

### Stage 3: Final Report — Within 1 Month of Incident Notification Submission

**Deadline:** Approximately 1 month after submitting Stage 2 (i.e., by approximately Thursday + 1 month from the attack week)

**Content required:**
- Detailed description of the incident (full timeline, root cause analysis)
- Type and severity classification
- Threat type and attack vectors
- Impact on services and operations
- Mitigation measures taken and their effectiveness
- Cross-border impact analysis
- Lessons learned
- Recommended or implemented preventive measures

**Note:** For ongoing incidents still not resolved after 1 month, submit an **Intermediate Report** at the 1-month mark, with a Final Report within 1 month of resolution.

---

## Step 4: GDPR Overlap — Critical Consideration

If the ransomware attack has **also compromised, exfiltrated, or provided unauthorised access to personal data**, you face a **parallel obligation under GDPR Article 33**:

- **Data Breach Notification to your Data Protection Authority (DPA):** Within **72 hours** of becoming aware
- **Notification to data subjects (Article 34):** Without undue delay if the breach is likely to result in high risk to individuals

These run **concurrently** with NIS2 obligations. The 72-hour GDPR deadline and the 72-hour NIS2 deadline are independent — do not conflate them. Ransomware often involves data exfiltration prior to encryption; assume personal data may be at risk until forensics confirm otherwise.

---

## Step 5: Practical Action Checklist (Hour-by-Hour)

### Monday 09:00 (Attack Detected)
- [ ] Activate Incident Response Plan
- [ ] Isolate affected systems to prevent spread
- [ ] Preserve forensic evidence (logs, memory dumps)
- [ ] Engage external incident response/forensics team if needed
- [ ] Notify internal leadership and legal/compliance team
- [ ] Begin assessing whether personal data is involved (GDPR trigger)
- [ ] Identify your national CSIRT and competent authority contacts

### Monday — Before 09:00 Tuesday (24-Hour Deadline)
- [ ] Submit **Early Warning** to national CSIRT and competent authority
- [ ] Note: If personal data is involved, also start the GDPR 72-hour clock
- [ ] Do not wait for full forensic clarity — submit with what you know

### Tuesday to Thursday — Before 09:00 Thursday (72-Hour Deadline)
- [ ] Continue forensic investigation
- [ ] Submit **Incident Notification** (Stage 2) with fuller details
- [ ] If personal data confirmed affected: submit GDPR breach notification to DPA
- [ ] Notify affected service recipients if their services are impacted
- [ ] Document all decisions and actions taken

### Within 1 Month of Stage 2 Submission
- [ ] Submit **Final Report** with full post-incident analysis
- [ ] Document root cause, lessons learned, and remediation measures

---

## Step 6: Key Legal Obligations Summary Table

| Obligation | Recipient | Deadline | Content |
|---|---|---|---|
| Early Warning | National CSIRT + Competent Authority | 24 hours (by Tuesday 09:00) | Incident occurred, suspected malicious, cross-border? |
| Incident Notification | National CSIRT + Competent Authority | 72 hours (by Thursday 09:00) | Impact, IOCs, attack type, affected systems, mitigations |
| Final Report | National CSIRT + Competent Authority | 1 month after Stage 2 | Full analysis, root cause, lessons learned |
| GDPR Data Breach (if applicable) | National DPA | 72 hours (by Thursday 09:00) | Nature of breach, data categories, likely consequences |
| Service recipient notification | Affected customers/users | Without undue delay | If incident significantly affects their services |

---

## Step 7: Penalties for Non-Compliance

Failure to report or late reporting carries significant financial risk:

- **Essential Entities:** Fines up to **€10 million** or **2% of global annual turnover**, whichever is higher
- **Important Entities:** Fines up to **€7 million** or **1.4% of global annual turnover**, whichever is higher

National authorities also have supervisory and corrective powers, including temporary bans on management exercise of responsibilities for persistent failures.

---

## Step 8: Practical Tips

1. **Do not wait for full investigation clarity** before filing the Early Warning — NIS2 expressly permits incomplete information at Stage 1, and regulators expect prompt notification over perfect information.

2. **Document the precise time you became aware** of the incident — this is the clock-start for all deadlines.

3. **Assume personal data is at risk** from ransomware until forensics confirm otherwise, and run GDPR notifications in parallel.

4. **Check your member state's specific implementation** — NIS2 was transposed by each EU member state by October 2024; local law may add requirements or modify who the designated contacts are.

5. **Engage legal counsel** experienced in NIS2 and your sector early — mischaracterising an incident as non-significant when it is significant is a regulatory risk.

6. **Preserve all communications** with regulators as evidence of good-faith compliance efforts.

---

## Summary

Your core obligation is a **three-stage report to your national CSIRT and competent authority**: Early Warning by Tuesday 09:00, full Incident Notification by Thursday 09:00, and a Final Report within approximately one month. If personal data is involved, GDPR data breach notifications to your DPA run concurrently on the same 72-hour timeline. Prioritise speed and honesty over completeness in early filings — regulators understand investigations take time.
