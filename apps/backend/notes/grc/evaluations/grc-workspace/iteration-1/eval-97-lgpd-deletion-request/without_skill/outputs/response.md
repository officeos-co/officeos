# LGPD Data Deletion Request: How to Handle a Brazilian User's Request

## Overview

Brazil's Lei Geral de Proteção de Dados Pessoais (LGPD), Law No. 13,709/2018, grants data subjects a set of enforceable rights over their personal data. When a Brazilian user requests deletion of all their personal data held across your CRM, email platform, and analytics tool, you are legally obligated to respond in a structured and timely manner. This document covers the applicable rights, the step-by-step process, mandatory timelines, and lawful exemptions.

---

## 1. Applicable LGPD Rights

### Right to Deletion (Art. 18, IV and VI)

The LGPD provides two distinct deletion rights:

- **Art. 18, IV — Deletion of unnecessary or excessive data**: The data subject may request deletion of personal data that is unnecessary, excessive, or processed in non-compliance with LGPD.
- **Art. 18, VI — Deletion of data processed with consent**: Where processing is based on the data subject's consent, they may request deletion at any time.

### Related Rights That May Also Be Invoked

- **Art. 18, I — Confirmation of processing**: The user may ask whether you hold their data at all.
- **Art. 18, II — Access**: The user may request a copy of all data held.
- **Art. 18, III — Correction**: The user may request correction of inaccurate or incomplete data.
- **Art. 18, V — Anonymization, blocking, or elimination**: Where data cannot be deleted (e.g., legal obligation), it may be anonymized or blocked instead.
- **Art. 18, VII — Information on third parties**: The user may ask which third parties received their data.
- **Art. 18, IX — Revocation of consent**: If consent was the legal basis, the user may revoke it, which triggers the obligation to stop processing and, where applicable, delete.

---

## 2. Step-by-Step Process

### Step 1: Verify the Identity of the Requester

Before acting on any deletion request, confirm the requester's identity to prevent unauthorized deletion of another person's data. This can be done by:

- Matching the email address to the account on record.
- Sending a verification link or code to the registered email.
- Requesting a government-issued ID if the scope of the request is particularly sensitive or broad.

The verification step must not be used as a delay tactic and should be completed promptly.

### Step 2: Acknowledge Receipt

Send a written acknowledgment to the requester confirming that their request has been received, and provide an expected response date. Under LGPD Art. 18, §5, the controller must respond to requests. Best practice is to acknowledge within 2–3 business days.

### Step 3: Assess the Legal Basis for Processing in Each System

Before deleting, determine the legal basis under which you hold the data in each system:

| System | Likely Legal Basis | Deletion Implication |
|---|---|---|
| CRM | Contract (Art. 7, V), Consent (Art. 7, I), or Legitimate Interest (Art. 7, IX) | Review which data elements are tied to each basis |
| Email Platform | Consent (Art. 7, I) | Consent-based data is deletable upon request; unsubscribes may also trigger obligations |
| Analytics Tool | Legitimate Interest (Art. 7, IX) or Consent (Art. 7, I) | Anonymized or aggregated analytics data may not constitute personal data and may be retained |

### Step 4: Identify Any Applicable Exemptions

Check whether any exemption applies before deleting (see Section 4 below). Document the rationale for retaining any data that falls under an exemption.

### Step 5: Execute Deletion Across All Systems

For data that must be deleted:

- **CRM**: Delete the contact record including all associated fields (name, email, phone, interaction history, deal history) unless a legal hold applies.
- **Email Platform**: Remove the subscriber record, including all campaign interaction data (opens, clicks, bounces). Unsubscribe records may need to be retained separately to prove suppression compliance.
- **Analytics Tool**: Delete or anonymize user-level identifiers (user ID, IP address, device fingerprint). Aggregate statistical data that cannot be attributed to an individual does not constitute personal data under LGPD and may be retained.

Ensure deletion cascades to:
- Backup systems (set a retention window after which backups are purged).
- Any third-party sub-processors who received the data (Art. 18, §7 — the controller must communicate the deletion request to all entities with whom data was shared).

### Step 6: Notify Third Parties

Under LGPD Art. 18, §7, the controller must communicate corrections, deletions, anonymizations, or blockings to third parties to whom data was transmitted — unless such communication is impossible or disproportionately costly. Document any notifications sent to third-party processors (e.g., the CRM vendor, email service provider, analytics vendor).

### Step 7: Respond to the Requester

Provide a written response confirming:
- Which data was deleted, from which systems.
- Which data was retained, citing the specific exemption and its expected retention period.
- The identity of any third parties notified.

### Step 8: Maintain an Internal Record

Log the request and your response in your data subject request register. This supports accountability obligations under LGPD Art. 37, which requires controllers to maintain records of processing activities.

---

## 3. Timelines

LGPD does not specify a single fixed deadline for responding to data subject requests in the same way GDPR specifies 30 days. However, the following applies:

- **ANPD guidance and best practice**: Respond within **15 business days** of receiving the verified request. Some practitioners reference the Brazilian Consumer Protection Code (CDC), which mandates a 5-business-day response for consumer complaints, but the LGPD standard is broader.
- **No undue delay**: Art. 18, §5 states that responses must be provided "immediately" or, where that is not possible, within a reasonable period consistent with the complexity of the request.
- **Practical benchmark**: Many Brazilian DPOs adopt a **15-day target** for straightforward requests and a **30-day maximum** for complex or multi-system requests, with an explanation provided to the requester if the longer period is needed.

---

## 4. Exemptions — When You May Refuse or Defer Deletion

LGPD Art. 16 explicitly permits the retention of personal data even after the end of processing or upon a deletion request under the following circumstances:

### Art. 16, I — Compliance with a Legal or Regulatory Obligation

If you are required by Brazilian law (e.g., tax law, labor law, anti-money laundering regulations) to retain certain records for a defined period, you may retain the minimum data necessary to fulfill that obligation. For example:

- Brazilian tax records (Receita Federal) may require retention of invoicing and customer data for 5 years.
- Labor-related data must be retained for periods specified under the CLT (Consolidação das Leis do Trabalho).

### Art. 16, II — Transfer to a Third Party Under ANPD Rules

Data may be retained for transfer to a third party, provided the transfer is conducted in accordance with LGPD and ANPD regulations.

### Art. 16, III — Sole Use by the Controller, Anonymized

Data may be retained in anonymized form for the controller's exclusive use, where it can no longer be attributed to an identified or identifiable individual. Aggregate analytics data that has been properly anonymized falls into this category.

### Art. 16, IV — Fraud Prevention and Data Subject Security

Data may be retained when necessary for fraud prevention or to protect the safety of the data subject, in accordance with ANPD regulations.

### Art. 18, §3 — Rights of Third Parties or the Public Interest

The right to deletion may be restricted where deletion would harm the rights and freedoms of third parties, or where retention is required in the public interest.

---

## 5. Handling Each System in Practice

### CRM

- Delete all personal identifiers: name, email, phone, address, account history.
- If the customer relationship involves outstanding contractual obligations (e.g., an active warranty, pending dispute), you may retain the minimum data necessary to fulfill or enforce that contract under Art. 7, V (contract performance) until the obligation is discharged.
- After the contractual period ends, delete residual data.

### Email Platform

- Remove the subscriber from all active lists and suppress them from future sends.
- **Unsubscribe/suppression records**: Retain a minimal suppression entry (typically the hashed email address) to prevent re-adding the user to marketing lists in error. This is a narrow, defensible retention under the fraud prevention or legitimate interest basis, but document the rationale explicitly.
- Delete all campaign engagement history linked to the individual.

### Analytics Tool

- Delete or anonymize any user-level identifiers (user ID, client ID, IP address, device ID, session tokens).
- Aggregate metrics (e.g., "1,200 unique visitors in March") that cannot be re-associated with the individual are not personal data under LGPD and may be retained.
- If the tool uses cookies or device fingerprinting, ensure those identifiers are also purged from any persistent stores.

---

## 6. Documentation and Accountability

LGPD's accountability principle (Art. 6, X and Art. 37) requires you to be able to demonstrate compliance. Maintain:

- A log of the deletion request (date received, identity verified, systems searched, data deleted, exemptions claimed, response sent).
- Evidence of third-party notifications.
- Records of any partial refusals with legal justification.

Your Data Protection Officer (Encarregado, Art. 41) should oversee and sign off on the response if one has been appointed.

---

## 7. Summary Checklist

- [ ] Verify the identity of the requester.
- [ ] Acknowledge receipt of the request promptly.
- [ ] Map all personal data held in CRM, email platform, and analytics tool.
- [ ] Identify the legal basis for processing in each system.
- [ ] Assess whether any Art. 16 exemption applies to any data category.
- [ ] Execute deletion in all applicable systems (CRM, email, analytics).
- [ ] Notify all third-party sub-processors and recipients of the deletion.
- [ ] Respond to the requester in writing, confirming deletions and documenting any retained data with justification.
- [ ] Log the full request lifecycle in your data subject request register.
- [ ] Complete the above within 15 business days (30 days maximum for complex cases).

---

## 8. Key LGPD Articles Referenced

| Article | Subject |
|---|---|
| Art. 6, X | Accountability principle |
| Art. 7 | Legal bases for processing |
| Art. 16 | Retention after end of processing |
| Art. 18 | Data subject rights |
| Art. 18, §3 | Limits on the right to deletion |
| Art. 18, §5 | Obligation to respond to requests |
| Art. 18, §7 | Notification to third parties |
| Art. 37 | Records of processing activities |
| Art. 41 | Data Protection Officer (Encarregado) |

---

*This response is based on LGPD (Law No. 13,709/2018) as amended by Law No. 13,853/2019, and reflects ANPD guidance available as of May 2026. For jurisdiction-specific legal advice, consult a qualified Brazilian data protection attorney.*
