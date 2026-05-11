# LGPD International Data Transfer to the United States: Available Mechanisms and Implementation Guide

**Prepared under:** Lei Geral de Proteção de Dados Pessoais (LGPD) — Law No. 13,709/2018, as amended by Law No. 13,853/2019  
**Regulatory authority:** Autoridade Nacional de Proteção de Dados (ANPD)  
**Scenario:** Transfer of Brazilian customer personal data to parent company servers in the United States (no ANPD adequacy decision)

---

## 1. Legal Framework Overview

International data transfers from Brazil are governed by **Arts. 33–36 LGPD**. The law establishes that personal data may only be transferred internationally if one of the approved mechanisms is in place. An **adequacy decision** — where ANPD formally recognises that the destination country provides a level of protection equivalent to LGPD — is the simplest route, but the United States does not currently benefit from any such decision. As of 2025–2026, ANPD has not published a list of adequate countries.

This means your organisation must rely on one or more of the alternative mechanisms set out in Art. 33. Each is analysed below with practical implementation steps.

**Important:** Art. 36 further provides that ANPD may impose **additional requirements** for sensitive data or large-scale transfers even where a base mechanism is satisfied. This is particularly relevant where your customer data includes health, financial, or biometric information.

---

## 2. Available Transfer Mechanisms (Art. 33 LGPD)

### Mechanism 1 — Standard or Specific Contractual Clauses (Art. 33 combined with Art. 35)

**What it is:** A binding written agreement between your Brazilian entity (the data exporter / controller) and your US parent company (the data importer) that contractually guarantees the same level of protection that LGPD affords to data subjects in Brazil.

**Current ANPD status:** ANPD has not yet published standard contractual clauses (SCCs) equivalent to the EU's SCCs. Organisations must therefore use **specific contractual clauses** — bespoke contract provisions drafted to satisfy Art. 35 requirements.

**Implementation steps:**

1. **Draft the data transfer agreement (DTA).** The agreement must cover at minimum:
   - Identity of the exporting Brazilian controller and the importing US entity
   - Categories of personal data being transferred and the purposes
   - Legal basis for the underlying processing (Art. 7 or Art. 11)
   - Obligations of the importer to process data only for agreed purposes
   - Data subject rights — commitment by the importer to cooperate in fulfilling all Art. 18 rights
   - Security measures the importer will implement (equivalent to Art. 46 standards)
   - Breach notification — importer must notify the Brazilian controller promptly to enable the 3-working-day ANPD preliminary notification (Resolution CD/ANPD No. 15/2024)
   - Audit rights — the Brazilian controller's right to audit the importer or commission third-party audits
   - Sub-processing restrictions — requirement to obtain prior authorisation before engaging sub-processors
   - Governing law and dispute resolution clauses that preserve data subject remedies
   - Provisions for deletion or return of data on contract termination

2. **Conduct a Transfer Impact Assessment (TIA).** Although LGPD does not use this precise term, the accountability principle (Art. 6, X) and ANPD's expectation of documented compliance require you to assess whether the legal environment in the US (surveillance laws, government access rights) undermines the protections guaranteed in the contractual clauses. Document this assessment.

3. **Update your RoPA** (Records of Processing Activities, Art. 37) to record the transfer, its purpose, legal basis, and the contractual mechanism used.

4. **Update privacy notices** (Art. 9) to inform Brazilian data subjects that their data is transferred internationally, to what country, and on what basis.

**Recommended as:** Primary mechanism for intragroup transfers to the US parent.

---

### Mechanism 2 — Binding Corporate Rules (BCRs) (Art. 33 combined with Art. 35, I)

**What it is:** A set of binding internal privacy rules that apply across the entire corporate group (your Brazilian entity and the US parent, and any other group entities). BCRs create a framework where the group collectively commits to LGPD-equivalent protection for all intragroup transfers.

**Current ANPD status:** ANPD has not yet published a formal BCR approval procedure or standard framework. BCR authorisation by ANPD is referenced in Art. 35 but the implementing regulation remains outstanding. Organisations may still develop BCRs in anticipation of ANPD guidance, and some leverage GDPR-approved BCRs as a model.

**Implementation steps:**

1. **Assess group structure.** Map all entities in the group that will be bound — Brazilian operations, US parent, and any other subsidiaries that will handle Brazilian customer data.

2. **Draft the BCR document.** A LGPD BCR should address:
   - Scope (entities bound, categories of data, types of transfers)
   - Data protection obligations binding on each entity
   - Data subject rights mechanisms — how Brazilian data subjects exercise Art. 18 rights against any group entity globally
   - Enforcement mechanism — internal binding effect, liability allocation
   - DPO coordination across the group
   - Breach response and ANPD notification chain
   - Training requirements for group employees
   - Cooperation with ANPD

3. **Seek ANPD authorisation** once ANPD publishes its BCR procedure. Until then, document your BCR alongside contractual clauses (Mechanism 1) to ensure you have an immediately operative mechanism.

4. **Leverage GDPR BCR approval** if your group has GDPR BCRs approved by an EU supervisory authority. These demonstrate a high standard of data protection that ANPD is likely to consider positively, though they do not automatically satisfy LGPD requirements.

**Recommended as:** Medium-term strategic mechanism — begin development now and formalise when ANPD issues BCR guidance. Use alongside contractual clauses in the interim.

---

### Mechanism 3 — Specific Consent of the Data Subject (Art. 33, VIII)

**What it is:** The data subject explicitly consents to the international transfer to the United States, having been specifically informed that the US does not have an ANPD adequacy decision.

**Consent requirements under LGPD (Art. 8):**
- Must be **free** (not coerced; cannot be a condition of service unless strictly necessary)
- Must be **informed** — the data subject must be told: (a) that data will be transferred internationally; (b) to which country (United States); (c) that the US does not have an ANPD adequacy decision; (d) the risks this implies
- Must be **unambiguous** — affirmative opt-in action; pre-ticked boxes are invalid
- Must be **specific** — a general consent to "share your data" is insufficient; the international transfer consent must be separately identified
- Must be **documented** — the controller bears the burden of proving consent was validly obtained
- Must be **revocable** — at no cost, at any time, without prejudice

**Limitations of consent as the sole mechanism:**
- Consent can be withdrawn at any time. If a customer withdraws consent, you must cease transferring that customer's data and potentially repatriate it — operationally complex at scale.
- LGPD prohibits conditioning service on consent (Art. 8, §5º) unless the processing is genuinely necessary for service delivery. Relying solely on consent for a bulk transfer to a parent company server carries regulatory risk.
- ANPD may scrutinise whether consent was freely given where there is a power imbalance (e.g., customer needs the service).

**Implementation steps if using consent:**

1. Design a **specific, layered consent** flow:
   - At or before data collection, include a clearly distinguished disclosure about international transfers
   - Describe: data types transferred, destination country (US), absence of adequacy decision, risks
   - Provide an affirmative opt-in (checkbox, not pre-ticked)

2. **Record consent:** Capture the consent timestamp, IP address, version of the consent form, and exact language presented.

3. **Implement a withdrawal mechanism:** A data subject request portal or email address where data subjects can withdraw transfer consent, with a documented process for actioning withdrawals.

4. **Do not rely solely on consent** for operational transfers of large customer datasets. Pair with contractual clauses (Mechanism 1).

**Recommended as:** Supplementary mechanism only, particularly for transfers that are genuinely optional from the customer's perspective. Not recommended as sole mechanism for bulk customer data transfers.

---

### Mechanism 4 — ANPD Case-by-Case Authorisation (Art. 33, IX)

**What it is:** A specific authorisation granted by ANPD for a particular transfer or category of transfers. ANPD evaluates the transfer and may impose conditions.

**Current ANPD status:** ANPD has not yet published a formal procedure for individual transfer authorisations. This mechanism exists in the law but is not yet practically accessible through a structured ANPD process.

**When it may be relevant:**
- Where no other mechanism clearly applies
- For sensitive data transfers where ANPD may wish to exercise oversight
- As a long-term option if ANPD develops a structured procedure

**Practical guidance:** Do not rely on this mechanism as your primary transfer basis for ongoing operational transfers. It is inherently unpredictable, slow, and ANPD has not yet established the process. Use it only if no other mechanism is available and the transfer is genuinely necessary.

---

### Mechanism 5 — Vital Interests (Art. 33, VII)

**What it is:** Transfers necessary to protect the life or physical safety of the data subject or a third party.

**Applicability:** This is a narrowly defined emergency exception. It does not apply to routine commercial transfers of customer data. It is unlikely to be relevant to your Brazilian operations expansion scenario.

---

### Mechanism 6 — Legal Cooperation and International Treaties (Art. 33, VI)

**What it is:** Transfers between public authorities under international legal cooperation obligations or treaties.

**Applicability:** Applies to transfers between government bodies under treaty frameworks. Not applicable to private sector commercial transfers to a parent company.

---

## 3. Recommended Transfer Strategy for Your Scenario

Given that you are transferring Brazilian **customer personal data** to a US parent company for operational purposes, and the US has no ANPD adequacy decision, the recommended approach is:

| Priority | Mechanism | Rationale |
|----------|-----------|-----------|
| **Primary** | Specific Contractual Clauses (Art. 35) | Immediately operable; no ANPD approval required; directly applicable to intragroup transfers |
| **Medium-term** | Binding Corporate Rules (Art. 35, I) | Provides a comprehensive group-wide framework; begin development now |
| **Supplementary** | Specific Consent (Art. 33, VIII) | For transfers that are genuinely optional and customer-initiated |

---

## 4. Implementation Checklist

### Immediate Actions (within 30 days)

- [ ] **Legal basis audit:** Confirm the legal basis (Art. 7) for the underlying processing of Brazilian customer data — this must be valid independently of the transfer mechanism
- [ ] **Draft Data Transfer Agreement** between Brazilian entity (exporter) and US parent (importer) — engage Brazilian data protection counsel
- [ ] **Conduct Transfer Impact Assessment:** Assess US legal environment (including surveillance laws such as FISA/CLOUD Act) against contractual protections
- [ ] **Update RoPA** (Art. 37): Add international transfer fields — destination country, mechanism, purpose, categories of data
- [ ] **Update privacy notice** (Art. 9): Disclose international transfers to the US, the mechanism relied upon, and the absence of an ANPD adequacy decision
- [ ] **Appoint or confirm DPO (Encarregado)** (Art. 41): Publish DPO identity and contact on your Brazilian website

### Within 60–90 Days

- [ ] **Security standards alignment:** Ensure the US parent's security measures (Art. 46 equivalent) are documented and meet Brazilian standards — consider ISO 27001 certification or SOC 2 reports as evidence
- [ ] **Data minimisation review:** Confirm only the minimum data necessary (Art. 6, III — necessity principle) is being transferred; avoid transferring more data than the US parent requires for the stated purpose
- [ ] **Sub-processor mapping:** Identify all US-side processors and sub-processors that will handle Brazilian customer data; ensure each is covered by the data transfer agreement or equivalent contractual protections
- [ ] **Conduct RIPD (DPIA)** (Art. 38): If the transfer involves large-scale data or sensitive data, complete a Data Protection Impact Assessment before the transfer commences
- [ ] **Breach response integration:** Ensure the US parent's incident response procedure includes a contractual obligation to notify the Brazilian controller within a timeframe that allows the 3-working-day ANPD preliminary notification (Resolution CD/ANPD No. 15/2024)

### Ongoing Compliance

- [ ] **Monitor ANPD regulatory developments:** ANPD is expected to publish standard contractual clauses and BCR guidance; update your mechanisms when available
- [ ] **Annual transfer review:** Reassess the transfer annually — legal environment in the US, volume/categories of data, ongoing necessity
- [ ] **Data subject rights fulfilment:** Ensure your Brazilian entity can fulfil Art. 18 rights requests even when the data is held on US servers — this requires contractual cooperation obligations on the US parent and a practical mechanism for retrieval
- [ ] **Employee training:** Train Brazilian operations staff on LGPD transfer requirements and data subject rights obligations

---

## 5. Sensitive Data Considerations (Art. 11 and Art. 36)

If your customer data includes **sensitive personal data** (health data, biometric data, racial/ethnic origin, financial data classified as sensitive, etc.), additional requirements apply:

- The legal basis for the underlying processing must be one of the Art. 11 bases (not merely Art. 7)
- Art. 36 empowers ANPD to impose **additional requirements** for sensitive data transfers
- Contractual clauses for sensitive data should include heightened protections: encryption in transit and at rest, access controls, purpose limitation, and prohibition on onward transfers without prior consent
- A RIPD (DPIA) under Art. 38 is strongly recommended and may be required by ANPD for high-risk sensitive data transfers

---

## 6. Penalty Exposure for Non-Compliance (Art. 52)

Conducting international transfers without a valid mechanism exposes your organisation to:

| Sanction | Details |
|----------|---------|
| Warning | With compulsory remediation period |
| Simple fine | Up to **2% of revenue in Brazil** (prior financial year, group basis); maximum **R$50 million per violation** |
| Daily fine | Same cap; applied to compel compliance |
| Public disclosure | Publication of violation after ANPD investigation |
| Data blocking | Temporary suspension of all processing related to the violation |
| Data deletion | ANPD may order erasure of improperly transferred data |
| Processing suspension | Partial suspension of operations up to 6 months |
| Processing prohibition | Total ban on data processing activities in Brazil |

The statute of limitations for ANPD enforcement is **5 years** from the date ANPD becomes aware of the violation (Art. 54).

---

## 7. LGPD vs. GDPR: Key Differences Relevant to Transfers

For organisations familiar with GDPR, note the following differences:

| Topic | LGPD (Brazil) | GDPR (EU) |
|-------|--------------|-----------|
| Standard contractual clauses | Not yet published by ANPD — bespoke clauses required | EC has published SCCs; parties must use approved versions |
| Adequacy list | Not yet published; US has no adequacy decision | EC adequacy decisions exist; US has EU-US Data Privacy Framework |
| BCR procedure | ANPD procedure not yet published | Formal BCR approval process with lead SA |
| Transfer impact assessment | Implied by accountability principle | Explicitly required by EDPB guidelines post-Schrems II |
| Supervisory authority | ANPD (federal, single authority) | Lead supervisory authority mechanism (one-stop-shop) |

---

## 8. Summary

Because the United States has no ANPD adequacy decision, your transfer of Brazilian customer data to your US parent company must be grounded in one of the Art. 33 mechanisms. **Specific contractual clauses** (Art. 35) are the immediately available and most practical primary mechanism. You should:

1. Execute a comprehensive Data Transfer Agreement between your Brazilian entity and the US parent before transfers commence.
2. Conduct a Transfer Impact Assessment to document that the contractual protections are effective given the US legal environment.
3. Update all required documentation — RoPA, privacy notices, RIPD.
4. Begin developing Binding Corporate Rules as a medium-term group-wide solution.
5. Monitor ANPD for standard contractual clause publication and BCR guidance.

Transfers made without a valid mechanism risk ANPD enforcement action, fines of up to 2% of Brazilian revenue (capped at R$50 million per violation), and reputational harm in the Brazilian market.

---

*This guidance reflects LGPD (Law No. 13,709/2018 as amended), ANPD resolutions current as of 2025–2026 (including Resolution CD/ANPD No. 15/2024), and should be reviewed against any new ANPD guidance or resolutions issued after the date of preparation. Engage qualified Brazilian data protection counsel before implementing transfer mechanisms.*
