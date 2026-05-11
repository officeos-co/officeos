# LGPD International Data Transfer: Brazil to United States

## Overview

Brazil's Lei Geral de Proteção de Dados (LGPD — Law No. 13,709/2018) regulates the transfer of personal data to foreign countries under Chapter V (Articles 33–36). Since the United States does not have an adequacy decision from Brazil's data protection authority (ANPD — Autoridade Nacional de Proteção de Dados), transfers to US-based parent company servers require reliance on one of the alternative legal mechanisms described below.

---

## Legal Basis for International Transfers Without an Adequacy Decision

Under Article 33 of the LGPD, international data transfers are permitted only when one of the following conditions is met:

### 1. Standard Contractual Clauses (SCCs) — Article 33, II(a)

The most commonly used mechanism. The ANPD is empowered to publish standard contractual clauses that controllers and processors may adopt to govern cross-border transfers.

**What you need to implement:**
- Execute a data transfer agreement between your Brazilian entity (data exporter) and your US parent company (data importer) incorporating the ANPD-approved standard contractual clauses.
- As of 2025, the ANPD has been developing and refining its own SCC template (distinct from EU SCCs). Monitor ANPD publications for the final approved version.
- The contract must specify: categories of data transferred, purposes of processing, data subject rights guarantees, security measures, and sub-processor obligations.

---

### 2. Specific Contractual Clauses — Article 33, II(b)

Where standard clauses do not exist or do not fit the transfer scenario, controllers may draft specific (bespoke) contractual clauses and submit them to the ANPD for review or approval.

**What you need to implement:**
- Draft a bespoke data transfer agreement tailored to your processing activities.
- Include equivalent protections to those LGPD requires domestically (purpose limitation, data minimization, security safeguards, data subject rights).
- Engage legal counsel familiar with ANPD expectations to reduce the risk of the agreement being challenged.

---

### 3. Global Corporate Policy (Binding Corporate Rules equivalent) — Article 33, III

Multinational corporate groups may adopt a global privacy/data protection policy that is submitted to and approved by the ANPD. This is the LGPD analog to the GDPR's Binding Corporate Rules (BCRs).

**What you need to implement:**
- Develop a comprehensive group-wide data protection policy covering all entities (including the US parent).
- The policy must articulate enforceable rights for data subjects, internal accountability mechanisms, audit rights, and dispute resolution processes.
- Submit the policy to the ANPD for approval.
- This mechanism is resource-intensive but provides long-term flexibility for intra-group transfers across multiple jurisdictions.

---

### 4. Specific Consent of the Data Subject — Article 33, VIII

Transfers may be based on the free, informed, specific, and unambiguous consent of the data subject, provided the data subject is informed that the destination country does not offer an equivalent level of protection.

**What you need to implement:**
- Update your privacy notices to clearly disclose: (a) that data will be transferred to the United States, (b) that the US does not have an ANPD adequacy decision, and (c) the specific purpose of the transfer.
- Obtain affirmative, granular consent (separate from other consents) before the transfer occurs.
- Implement a consent withdrawal mechanism; withdrawal invalidates the transfer basis going forward.
- **Caution:** Consent is not suitable as the primary mechanism for large-scale or operationally essential transfers because it can be withdrawn at any time, creating operational risk.

---

### 5. Legal Cooperation Between Governments — Article 33, IV

Transfers may occur to fulfill legal obligations arising from international cooperation between Brazilian and foreign government bodies. This is narrow in scope and unlikely to apply to commercial intra-group transfers.

---

### 6. Necessity for Life or Physical Safety — Article 33, V

Applies only in emergency scenarios to protect the life or physical safety of the data subject or a third party. Not applicable to routine commercial transfers.

---

### 7. Necessity for Public Policy or Legal Obligations — Articles 33, VI–VII

Covers transfers required to perform a legal obligation, exercise rights in judicial/administrative/arbitral proceedings, or fulfill public policy purposes. Not generally applicable to routine intra-group business transfers.

---

## Recommended Implementation Roadmap

Given that your transfer is for commercial/operational purposes (customer data to a US parent company), the most practical and durable mechanisms are:

| Priority | Mechanism | Rationale |
|----------|-----------|-----------|
| 1st | Standard Contractual Clauses (ANPD model) | Widely accepted, scalable, no ongoing ANPD approval needed once template is published |
| 2nd | Specific Contractual Clauses | Available now; covers gap until ANPD SCCs are finalized |
| 3rd | Global Corporate Policy | Best long-term solution for a corporate group with multiple cross-border flows |

### Step-by-Step Implementation Plan

**Step 1 — Data Mapping and Transfer Assessment**
- Identify all categories of customer personal data being transferred (name, CPF, contact details, purchase history, etc.).
- Map the data flows: which systems send data, which US systems receive it, and for what purpose.
- Classify data by sensitivity (ordinary vs. sensitive personal data under Article 5, II).

**Step 2 — Select and Implement a Transfer Mechanism**
- Execute a Data Transfer Agreement (DTA) between your Brazilian subsidiary and US parent using currently available specific contractual clauses (Article 33, II(b)) while ANPD finalizes its standard clauses.
- The DTA should include: purpose limitation, data subject rights pass-through, security obligations, breach notification timelines, sub-processor restrictions, audit rights, and jurisdiction/governing law clauses.

**Step 3 — Update Privacy Notices**
- Revise your Brazilian-facing privacy policy to disclose: the fact of international transfer, the destination country (United States), the legal basis for transfer, and the safeguards in place.
- This is required under Article 33, paragraph 1.

**Step 4 — Implement Technical and Organizational Measures**
- Encrypt data in transit (TLS 1.2+) and at rest on US servers.
- Apply access controls and least-privilege principles on the US side.
- Document security measures to demonstrate compliance if audited by the ANPD.

**Step 5 — Establish Accountability Mechanisms**
- Appoint a Data Protection Officer (DPO/Encarregado) in Brazil if not already done (Article 41).
- Maintain records of processing activities (Article 37), including the international transfer entries.
- Implement a data subject rights fulfillment process covering Brazilian customers (access, correction, deletion, portability — Articles 18–20).

**Step 6 — Monitor ANPD Regulatory Developments**
- The ANPD continues to publish guidance and finalize SCC templates. When official SCCs are published, transition your agreements to the approved model.
- Subscribe to ANPD official communications (www.gov.br/anpd).

---

## Key Risks and Considerations

| Risk | Mitigation |
|------|-----------|
| ANPD enforcement action for undocumented transfers | Execute contractual clauses before initiating transfers |
| Consent withdrawal disrupting operations | Do not rely on consent as sole transfer mechanism for operational data |
| US sub-processors receiving Brazilian data | Ensure your DTA includes flow-down obligations to US sub-processors |
| Sensitive personal data (health, financial, biometric) | Apply heightened protections; consider whether transfer is strictly necessary |
| ANPD audit request | Maintain complete transfer documentation and records of processing |

---

## Penalties for Non-Compliance

The LGPD (Article 52) provides for administrative sanctions including:
- Warnings
- Fines of up to 2% of a company's revenue in Brazil for the prior fiscal year, capped at BRL 50 million per violation
- Partial or complete suspension of personal data processing activities
- Prohibition on processing personal data

---

## Summary

Since the United States does not have an LGPD adequacy decision from the ANPD, your company must rely on one of the Article 33 alternative mechanisms. For routine intra-group customer data transfers, the most appropriate and scalable path is to:

1. Execute specific contractual clauses (Article 33, II(b)) now as an interim measure.
2. Transition to ANPD-approved standard contractual clauses once published.
3. Consider developing a Global Corporate Policy (Article 33, III) for a permanent group-wide solution.
4. Update privacy notices, implement technical safeguards, and maintain complete transfer documentation.

Engaging a Brazilian privacy law specialist to review your specific data flows and draft compliant agreements is strongly recommended before initiating transfers.

---

*This response reflects LGPD requirements as of the knowledge cutoff date (August 2025). ANPD guidance and regulations continue to evolve; always verify against current ANPD publications.*
