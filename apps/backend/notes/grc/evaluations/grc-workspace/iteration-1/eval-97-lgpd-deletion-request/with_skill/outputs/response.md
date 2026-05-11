# Handling a Brazilian User's Data Deletion Request Under LGPD

**Jurisdiction:** Brazil — Lei Geral de Proteção de Dados Pessoais (LGPD), Law No. 13,709/2018, as amended by Law No. 13,853/2019  
**Request type:** Data subject right — deletion of personal data  
**Relevant LGPD articles:** Art. 5, Art. 7, Art. 16, Art. 18 (IV and VI), Art. 19, Art. 37, Art. 39, Art. 41, Art. 47, Art. 52

---

## 1. Overview: The Right to Deletion Under LGPD

LGPD grants every data subject (titular) ownership of their personal data as a fundamental right (Art. 17). Within that framework, Brazilian users hold two distinct deletion rights:

| Right | LGPD Article | Trigger |
|-------|-------------|---------|
| Deletion of unnecessary, excessive, or non-compliant data | Art. 18, IV | Data that does not comply with LGPD; data no longer necessary for the original purpose |
| Deletion of consent-based data | Art. 18, VI | Data subject revokes consent; controller has no other valid legal basis to retain |

A deletion request email from a Brazilian user can invoke either or both of these rights. Your response obligations depend on which legal bases you relied on to originally collect and process the data, and whether any lawful retention exception applies under Art. 16.

---

## 2. Step-by-Step Workflow for Handling the Request

### Step 1 — Acknowledge and Log (Day 1)

Upon receiving the deletion request email:

- **Log the request immediately.** Record the date, channel (email), requestor identity, and request type. LGPD requires controllers to demonstrate accountability (Art. 6, X; Art. 47), and maintaining request logs is essential for that purpose.
- **Send an acknowledgement** within 1 business day confirming receipt and the expected response timeline.
- **Assign to your DPO (Encarregado).** Under Art. 41, your DPO must receive and coordinate data subject communications. The DPO's contact must be published on your website (Art. 41, §1º).

> The response must be provided **free of charge** (Art. 19).

---

### Step 2 — Verify the Identity of the Requestor

Before acting on any deletion request, verify that the person making the request is the actual data subject, or is duly authorised to act on their behalf.

- Request minimum necessary proof: full name, CPF (Brazilian tax ID), email address on file, or another account reference.
- Do **not** demand excessive documentation — the verification must be proportionate to the risk and consistent with the necessity principle (Art. 6, III).
- If the request is made by a third party (e.g., a legal representative), require a valid power of attorney (procuração).

Failure to properly verify identity before deletion creates both a security risk and a regulatory risk.

---

### Step 3 — Map the Data Across All Three Systems

The user has stated their data exists in:

1. **CRM** (Customer Relationship Management system)
2. **Email platform**
3. **Analytics tool**

For each system, your team must:

- Identify every **category of personal data** stored (Art. 5, I — "personal data" is broadly defined as any information relating to an identified or identifiable natural person).
- Identify any **sensitive personal data** (Art. 5, II) — e.g., health data, biometric data, ethnic origin — which carries stricter obligations.
- Identify the **legal basis** under which that data was originally collected and continues to be processed (Art. 7 for regular data; Art. 11 for sensitive data).
- Check whether any **Art. 16 retention exception** applies that overrides the deletion obligation (see Step 4 below).

This system-by-system mapping is also required for your Records of Processing Activities (RoPA) under Art. 37, which should already document these details.

---

### Step 4 — Assess Applicable Legal Bases and Retention Exceptions

This is the most critical legal analysis step. Your deletion obligation — and any lawful refusal — depends entirely on the legal basis for each processing activity.

#### 4a. If Data Was Processed Under Consent (Art. 7, I)

If you collected and processed the user's data based on their **consent** (e.g., newsletter subscriptions via email platform, opt-in analytics tracking), then:

- The user has an unqualified right to **revoke consent at any time** (Art. 18, IX; Art. 8, §5º).
- Upon revocation, you must **delete the personal data** unless a retention exception under Art. 16 applies (Art. 18, VI).
- Consent-based processing cannot continue once consent is withdrawn.

**Practical implication:** Any data in your email platform or analytics tool collected solely on the basis of consent must be deleted unless an Art. 16 exception applies.

#### 4b. If Data Was Processed Under Contract (Art. 7, V)

If you hold data necessary for the **performance of a contract** with the user (e.g., purchase records, account data in the CRM):

- A deletion request does not automatically override this legal basis while the contractual relationship is active or while obligations remain outstanding.
- However, once the contractual relationship ends and no retention exception applies, this data must also be deleted.

#### 4c. If Data Was Processed Under Legitimate Interest (Art. 7, IX)

If you relied on **legitimate interest** (LI) as the legal basis:

- The user's deletion request is a direct challenge to your LI balancing test. You must re-assess whether your interest still outweighs the data subject's fundamental rights (Art. 10).
- If the balancing test now favours the data subject (e.g., there is no compelling business necessity), the data must be deleted.
- **Important:** Legitimate interest cannot be used as a basis for processing **sensitive personal data** (Art. 11; Art. 10). Any sensitive data processed under LI was non-compliant from the outset.

#### 4d. If Data Was Processed Under Legal Obligation (Art. 7, II)

If you are **required by law or regulation** to retain certain data (e.g., tax records, anti-money laundering records, consumer transaction records under the Consumer Defence Code):

- The deletion right does **not** override a mandatory legal retention requirement.
- Art. 16, I explicitly permits retaining data for compliance with a legal or regulatory obligation even after the data subject requests deletion.

#### 4e. Art. 16 — Lawful Retention Exceptions

Art. 16 of LGPD sets out circumstances under which data **may be retained** even after a deletion request:

| Exception | Art. 16 Sub-item | Example |
|-----------|-----------------|---------|
| Compliance with legal or regulatory obligation | Art. 16, I | Tax records, financial records required by law |
| Transfer to third parties (with legal basis) | Art. 16, II | Sharing with a controller who has an independent legal basis |
| Exclusive use by controller; anonymised data | Art. 16, III | Aggregated/anonymised analytics that no longer identify the person |
| Exercise of rights in judicial, administrative, or arbitral proceedings | Art. 16, IV | Retention for active litigation involving the data subject |

Where you rely on an Art. 16 exception, you must:
- Inform the data subject that their deletion request is partially refused and on what basis.
- Limit retained data to the minimum necessary for the permitted purpose (Art. 6, III — necessity principle).
- Document the exemption in your request log.

---

### Step 5 — Execute Deletion Across All Three Platforms

For data that is not subject to an Art. 16 retention exception, you must execute deletion **without undue delay** (Art. 18, §4º). ANPD guidance indicates 15 days as the target timeframe for full responses, but deletion should proceed as quickly as technically feasible.

#### CRM

- Delete the user's contact record, interaction history, notes, tags, and any linked custom fields.
- If your CRM has backup systems, include scheduled deletion from backups within your standard backup retention cycle.
- If the CRM is operated by a third-party vendor, that vendor is acting as a **processor (operador)** under Art. 5, VII. Under Art. 39, processors must act solely per controller instructions. You must instruct the CRM vendor in writing to delete the data. Your contract with the CRM vendor should already include LGPD-compliant data processing terms (Art. 39).

#### Email Platform

- Delete the user's email address, subscription preferences, email interaction history (opens, clicks), and any segmentation or profile data.
- Suppress the email address to prevent future sends (distinguish between deletion and suppression — suppression lists typically retain the email address in a "do-not-contact" list; this may be permissible under Art. 16, I if required for compliance, or under a legitimate interest in not re-contacting someone who has requested deletion).
- As with the CRM, instruct the email platform vendor (processor) to execute deletion per Art. 39.

#### Analytics Tool

- Identify all event data, session data, user profiles, and identifiers linked to this user.
- Delete or anonymise user-level data. **True anonymisation** removes all reasonable means of re-identification (Art. 5, III; Art. 12). Anonymised aggregate data (e.g., total page views) is not personal data under Art. 12 and need not be deleted.
- If the analytics tool uses cookies or device identifiers (advertising IDs, fingerprinting), ensure those are also removed or reset.
- Instruct the analytics vendor to execute deletion per Art. 39, including any data retained in the vendor's own systems.

#### Processor Accountability (Art. 39 and Art. 42)

Controllers remain responsible for the actions of their processors. If a processor fails to delete data when instructed:
- The controller remains liable (Art. 42).
- Processors are jointly liable where they violate LGPD or fail to follow controller instructions (Art. 42, §1º).

Ensure your vendor contracts explicitly require processors to:
- Delete data upon controller instruction.
- Confirm deletion in writing within a defined timeframe.
- Not retain data beyond the agreed period.

---

### Step 6 — Respond to the Data Subject

Under Art. 18 and Art. 19, your response must be:

- **Free of charge** (Art. 19).
- **In clear, accessible language.**
- Provided within **15 days** for a full written response (Art. 19, §3º), or immediately for a simplified response.
- Delivered via a format accessible to the data subject.

Your response letter should include:

1. Confirmation that you have received and processed their deletion request.
2. A summary of the data deleted from each system (CRM, email platform, analytics tool).
3. Any data **not deleted**, identifying the specific Art. 16 exception and the legal basis for retention (e.g., "We have retained your purchase transaction records for 5 years as required by [applicable tax law] — Art. 16, I LGPD").
4. Notice of their right to file a complaint with the ANPD if they are dissatisfied with the handling of their request (Art. 18; Art. 22).
5. Your DPO's contact details (Art. 41, §1º).

---

### Step 7 — Update Records and Suppress Future Processing

After executing the deletion:

- **Update your RoPA** (Art. 37) to reflect that this data subject's data has been deleted and the request has been fulfilled.
- **Implement a suppression list** or equivalent technical control to prevent accidental re-collection (e.g., if the user re-visits your website or a third party attempts to upload their data to your CRM).
- **Revoke any active consents** in your consent management system.
- **Notify any downstream third parties** with whom you have shared the data. Art. 18, VII gives data subjects the right to know with whom their data has been shared, and Art. 18, IV implies that deletion should cascade to shared recipients where those recipients have no independent legal basis.

---

## 3. Timeline Summary

| Action | Responsible Party | Deadline |
|--------|------------------|----------|
| Acknowledge request | DPO / Privacy team | Day 1 |
| Verify identity | Privacy team | Days 1–3 |
| Map data across CRM, email, analytics | Technical / Privacy team | Days 2–5 |
| Assess legal bases and Art. 16 exceptions | Legal / DPO | Days 3–7 |
| Instruct processors to delete | Privacy team | Days 5–10 |
| Execute deletion across all systems | Technical team + processors | Days 5–13 |
| Respond in writing to data subject | DPO | Within 15 days (Art. 19, §3º) |
| Update RoPA and log | Privacy team | Within 15 days |

---

## 4. Potential Grounds for Refusing (or Partially Refusing) the Request

LGPD does not grant an absolute right to erasure in all circumstances. Under Art. 18, §3º and Art. 16, you may lawfully refuse or limit the deletion where:

| Grounds | Legal Basis | Action Required |
|---------|-------------|----------------|
| Legal or regulatory retention requirement | Art. 16, I | Retain minimum necessary data; notify data subject |
| Active litigation involving the data subject | Art. 16, IV | Retain data for duration of proceedings; notify data subject |
| National security or criminal investigation | Art. 18, §3º | Notify data subject of exemption; right to complain to ANPD |
| Data already anonymised | Art. 12 | Confirm anonymisation; no deletion required as data is not personal data |

Any refusal must be **communicated to the data subject** with the specific reason, and they must be informed of their right to file a complaint with the ANPD (Art. 18, §4º).

---

## 5. Enforcement Risk for Non-Compliance

Failure to comply with a valid deletion request is a priority enforcement area for the ANPD. The authority has specifically flagged **failure to respond to data subject rights — especially access and deletion — as a key enforcement priority**.

Under Art. 52, sanctions for non-compliance include:

| Sanction | Details |
|----------|---------|
| Warning | With deadline to remedy |
| Simple fine | Up to 2% of Brazilian revenue (prior FY); maximum R$50 million per violation |
| Daily fine | To compel compliance; same R$50M cap |
| Public disclosure | Publication of the violation |
| Blocking | Temporary blocking of the relevant personal data |
| Deletion | ANPD-ordered deletion of the data |
| Processing suspension | Partial suspension for up to 6 months |
| Processing prohibition | Complete ban on data processing activities |

The ANPD's complaint process allows the data subject to escalate directly to the ANPD (at https://www.gov.br/anpd/pt-br/assuntos/peticoes) if their request is not properly handled. The ANPD first gives the controller the opportunity to resolve the complaint before escalating to a formal investigation (Art. 54).

Aggravating factors under Art. 52, §1º that would increase any fine include: recidivism, lack of internal controls, failure to cooperate with the ANPD investigation, and degree of harm to the data subject.

---

## 6. Key LGPD Compliance Checklist for This Request

- [ ] Request received and logged with date and requestor details
- [ ] Identity of data subject verified proportionately
- [ ] DPO notified and assigned (Art. 41)
- [ ] Data mapped across CRM, email platform, and analytics tool
- [ ] Legal basis for each dataset identified (Art. 7 / Art. 11)
- [ ] Art. 16 retention exceptions assessed and documented
- [ ] Processors (CRM vendor, email platform, analytics vendor) instructed to delete per Art. 39
- [ ] Deletion executed across all systems without undue delay (Art. 18, IV / Art. 18, VI)
- [ ] Any downstream data-sharing recipients notified
- [ ] Data subject notified in writing within 15 days (Art. 19)
- [ ] Grounds for any partial refusal communicated to data subject
- [ ] ANPD complaint rights communicated to data subject
- [ ] RoPA updated to reflect deletion (Art. 37)
- [ ] Suppression controls implemented to prevent re-collection
- [ ] Request and response retained in compliance log

---

## 7. Note on LGPD vs. GDPR

For organisations also subject to GDPR, this situation maps closely to the GDPR "right to erasure" under Art. 17 GDPR. Key differences to note:

| Topic | LGPD | GDPR |
|-------|------|------|
| Deletion right scope | Art. 18, IV and Art. 18, VI | Art. 17 GDPR |
| Retention exceptions | Art. 16 (4 categories) | Art. 17(3) GDPR (6 categories) |
| Response timeframe | 15 days (Art. 19) | 1 month (extendable to 3) — Art. 12(3) GDPR |
| Supervisory authority | ANPD | Relevant EU/EEA DPA |
| Right to complain | Art. 18, §4º; Art. 22 | Art. 77 GDPR |

LGPD's 15-day response window is significantly shorter than GDPR's one-month period. Organisations operating under both regimes should calibrate their DSR processes to the more demanding LGPD timeline.

---

## Summary

A Brazilian user's deletion request triggers obligations under Art. 18, IV and Art. 18, VI of LGPD across all systems holding their personal data — your CRM, email platform, and analytics tool. The process requires: (1) identity verification; (2) data mapping and legal basis analysis for each system; (3) assessment of Art. 16 retention exceptions; (4) issuing deletion instructions to each third-party processor per Art. 39; (5) executing deletion without undue delay; and (6) a written response to the data subject within 15 days confirming what was deleted and explaining any lawful retention.

Non-compliance with a valid deletion request is an ANPD enforcement priority carrying penalties of up to 2% of Brazilian revenue or R$50 million per violation (Art. 52).

---

*This analysis is based on LGPD — Law No. 13,709/2018 as amended by Law No. 13,853/2019 — and ANPD guidance and resolutions current as of May 2026. Legal advice specific to your organisation's circumstances should be obtained from qualified Brazilian data protection counsel.*
