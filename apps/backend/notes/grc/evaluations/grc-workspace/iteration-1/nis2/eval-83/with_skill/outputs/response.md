# NIS2 Incident Reporting: Ransomware Attack Response Guide

## Situation Summary

A ransomware attack occurred at 09:00 Monday morning, encrypting core operational systems. Under the EU NIS2 Directive (Directive (EU) 2022/2555), Article 23, this almost certainly constitutes a **significant incident** triggering mandatory multi-stage reporting obligations.

---

## Step 1: Confirm This Is a "Significant Incident"

Before reporting, confirm the incident meets the NIS2 threshold. An incident is **significant** if it:

- Causes or is capable of causing **substantial disruption** to the provision of your services
- Causes or could cause **significant financial loss** to your organisation
- Has caused or could cause **material or non-material damage** to other natural or legal persons

A ransomware attack that has **encrypted your core operational systems** almost certainly satisfies all three criteria. You should treat this as a significant incident immediately and begin the reporting timeline below. Do not wait for damage quantification before initiating the 24-hour early warning.

---

## The Three-Stage Reporting Timeline

The attack occurred at **09:00 Monday**. Your deadlines are:

| Stage | Deadline | Absolute Deadline |
|---|---|---|
| Early Warning | Within 24 hours of becoming aware | **09:00 Tuesday** |
| Incident Notification | Within 72 hours of becoming aware | **09:00 Thursday** |
| Final Report | Within 1 month of incident notification | **Approx. 09:00 the following month** |

---

## Stage 1: Early Warning — Due by 09:00 Tuesday

### To Whom
Report to your **national CSIRT (Computer Security Incident Response Team)** and/or your **competent national authority** under NIS2. Depending on your Member State, these may be the same body or separate. Examples:
- Germany: BSI (Bundesamt fur Sicherheit in der Informationstechnik)
- France: ANSSI (Agence nationale de la securite des systemes d'information)
- Netherlands: NCSC-NL
- Ireland: NCSC Ireland

If your Member State has designated a single point of contact (SPOC) for NIS2, route through that channel.

### What to Report at This Stage
The early warning is intentionally brief. You must include:

1. **Whether the incident was (or is suspected to be) malicious in nature** — Ransomware is inherently malicious; answer yes.
2. **Whether the incident could have cross-border impact** — If you provide services in multiple EU Member States, or if suppliers/customers in other Member States are affected, flag this. Cross-border incidents may trigger coordination with other national authorities.

You do NOT need a full technical picture at this stage. A phone call to your CSIRT hotline followed by a brief written submission is typically acceptable. Check your national authority's portal for a specific notification form.

### Practical Checklist for Stage 1
- [ ] Confirm CSIRT/authority contact details are accessible (should be in your incident response plan)
- [ ] Submit early warning via official channel (portal, email, or hotline — varies by Member State)
- [ ] Record the exact time of submission and obtain a reference number if issued
- [ ] Confirm whether cross-border impact applies and flag it if so

---

## Stage 2: Incident Notification — Due by 09:00 Thursday

### To Whom
Same body as Stage 1: your national CSIRT and/or competent authority.

### What to Report at This Stage
This is a more detailed notification providing an **initial assessment** of the incident. You must include:

1. **Severity of the incident** — Describe the scale of encryption, which systems are affected, estimated scope of operational disruption
2. **Impact assessment** — What services are degraded or unavailable? What is the estimated financial impact? Are customers or third parties affected?
3. **Indicators of Compromise (IoCs)** — Any technical indicators identified so far: ransomware family/variant (if identified), malicious file hashes, C2 IP addresses, attack vectors observed (e.g., phishing email, exploited RDP, supply chain vector)
4. **Update on cross-border impact** — Confirm or refine your earlier assessment

You are not required to have root cause analysis complete at this stage, but you should be able to describe the observed attack pattern and initial response actions taken.

### Practical Checklist for Stage 2
- [ ] Document which systems are encrypted and the operational impact
- [ ] Engage your incident response team / forensic partner to begin IoC collection
- [ ] Complete your Member State's formal notification form (72-hour submission)
- [ ] Update the CSIRT on any developments since the early warning
- [ ] Record submission time and reference

---

## Stage 3: Final Report — Due Within 1 Month of Stage 2 Submission

### To Whom
Same national CSIRT and/or competent authority.

### What to Report at This Stage
This is a comprehensive post-incident report requiring:

1. **Detailed description of the incident** — Full timeline from initial compromise through detection, containment, and recovery
2. **Type of threat or root cause** — How did the attacker gain initial access? (e.g., phishing, unpatched vulnerability, compromised credentials, supply chain compromise)
3. **Applied and ongoing mitigations** — What immediate containment actions were taken? What remediation steps are underway? What systemic improvements are being implemented to prevent recurrence?
4. **Cross-border impact** — Confirmed assessment of whether other Member States were affected and how
5. **Lessons learned** — Where relevant, authorities may expect information on how your security posture will be strengthened

### Practical Checklist for Stage 3
- [ ] Complete forensic investigation and root cause analysis
- [ ] Document full attack timeline with evidence
- [ ] Draft remediation roadmap and document completed actions
- [ ] Prepare final report using competent authority's template if one is provided
- [ ] Obtain board/management sign-off before submission (Art. 20 governance obligation)

---

## Article 20: Management Body Obligations

Under Art. 20, your **management body** (board or equivalent) bears direct responsibility for cybersecurity risk management. This has immediate implications for this incident:

- Management must be briefed on this incident and the reporting obligations without delay
- Decisions about the reporting content and remediation approach require management oversight
- Management may face **personal liability** under Member State law for failure to comply with NIS2 obligations, including missed reporting deadlines

Ensure the board is informed and actively involved, not merely copied on communications.

---

## Article 21: Measures This Incident Puts Under Scrutiny

Regulators reviewing this incident will examine whether you had adequate Art. 21 measures in place. Key measures most directly relevant to a ransomware attack:

| Art. 21 Measure | Ransomware Relevance |
|---|---|
| **Measure 2** — Incident handling | Did you have detection, response, and recovery procedures? |
| **Measure 3** — Business continuity, backup management, DR | Do you have clean, offline backups? What is your RTO/RPO? |
| **Measure 4** — Supply chain security | Was the attack vector through a supplier or third-party system? |
| **Measure 8** — Cryptography and encryption | Was sensitive data protected before encryption by attacker? |
| **Measure 9** — Access control, asset management | Were privileged credentials compromised? Was MFA in place? |
| **Measure 10** — MFA, secured communications | Could MFA have prevented the initial compromise? |

Be prepared to demonstrate that you had documented policies for these measures. Gaps in these controls may be raised by the supervisory authority during or after the incident investigation.

---

## Supervision and Penalty Exposure

Your supervisory exposure depends on your entity classification:

- **If you are an Essential Entity (EE):** You are subject to **proactive (ex-ante) supervision** under Art. 32. Expect the competent authority to conduct active follow-up, potentially including on-site inspections or security audits as part of the post-incident review. Maximum penalty: **€10,000,000 or 2% of global annual turnover**, whichever is higher.
- **If you are an Important Entity (IE):** You are subject to **reactive (ex-post) supervision** under Art. 33. The authority will likely open a compliance review triggered by this incident. Maximum penalty: **€7,000,000 or 1.4% of global annual turnover**, whichever is higher.

Failure to meet the 24-hour or 72-hour reporting deadlines, or submission of materially incomplete notifications, can itself constitute a separate compliance failure subject to penalties independent of the underlying security incident.

---

## Immediate Action Summary

Given the attack occurred at 09:00 Monday:

1. **Right now:** Brief management, activate your incident response plan, engage forensic support, and prepare the early warning submission.
2. **Before 09:00 Tuesday:** Submit the early warning to your national CSIRT/competent authority — brief, factual, confirming malicious nature and flagging cross-border risk if applicable.
3. **Before 09:00 Thursday:** Submit the full 72-hour incident notification with severity assessment, impact summary, and known IoCs.
4. **Within 1 month of Thursday's submission:** Deliver the comprehensive final report with root cause analysis, full timeline, and remediation plan.

Do not let the incident response effort crowd out the reporting obligation. These are **parallel workstreams** — designate a compliance/legal lead to own the reporting timeline while the technical team manages containment and recovery.

---

*This guidance is based on Article 23 of Directive (EU) 2022/2555 (NIS2 Directive). Member State transposition laws may impose additional or more specific requirements. Verify the exact notification portal, form, and contact details with your national competent authority. As of the transposition deadline of 17 October 2024, all EU Member States were required to have implemented NIS2 into national law.*
