# DORA Third-Party Risk: Contracts and Due Diligence for 15 Cloud and SaaS Providers

## Governing Framework

Your obligations for managing 15 cloud and SaaS providers under DORA are set out in **Chapter V (Art. 28–44)**. The requirements differ depending on whether each arrangement supports a **critical or important function** — this distinction is the single most important factor in determining your contractual and due diligence obligations.

---

## Step 1 — Classify Each Arrangement (Critical/Important vs. Non-Critical)

Before applying any contractual requirements, you must assess which of your 15 providers support **critical or important functions**. This classification determines:
- The level of contractual provisions required (Art. 30(2) vs. Art. 30(3))
- The due diligence depth required
- Whether the arrangement must appear in your Register of Information with full mandatory fields

**How to classify:** Under Art. 28 and your ICT third-party risk policy (Art. 28(1)), map each provider to the business functions they support, then assess whether those functions are critical or important to your operations. The classification must be documented and reviewed at least annually.

---

## Step 2 — Contractual Provisions Required (Art. 30)

### For Critical or Important Function Providers — Art. 30(2)(a)–(i)

All contracts with providers supporting critical or important functions **must** include the following provisions per Art. 30(2):

| Art. 30(2) Ref | Required Provision | Practical Notes for Cloud/SaaS |
|---------------|-------------------|-------------------------------|
| (a) | Clear, complete description of ICT services provided | Include service scope, SLAs, service levels for each component |
| (b) | Locations where services are provided and where data is processed/stored | Critical for data sovereignty; specify regions; cover all sub-processors |
| (c) | Data protection provisions — compliance with GDPR and relevant data protection rules | Ensure DORA data protection clauses complement your DPA with the provider |
| (d) | Availability, authenticity, integrity, and confidentiality of data and services | Uptime SLAs, data integrity controls, encryption standards |
| (e) | **Audit and access rights** for the financial entity, the competent authority, and the resolution authority | This is a common gap with large cloud providers; must be substantive, not nominal |
| (f) | Termination rights and minimum exit notice periods | Exit for cause (breach, insolvency, regulatory direction) and exit for convenience |
| (g) | Reporting and monitoring obligations — provider must report incidents to you promptly | Defines your ability to monitor the provider's performance and security posture |
| (h) | Data portability and migration assistance upon termination | Ensures you can retrieve your data and migrate to an alternative provider |
| (i) | Sub-contracting arrangements — prior consent requirements and notification obligations for changes | Prevents undisclosed sub-processing chains; requires visibility of sub-processors |

**Key RTS:** CDR (EU) 2024/1773 — specifies detailed requirements for each of the Art. 30(2) provisions
**Key RTS:** CDR (EU) 2025/532 — specific requirements for subcontracting of ICT services

### For Non-Critical Arrangements — Art. 30(3)

Providers supporting non-critical functions are subject to a **lighter contractual regime** under Art. 30(3). The core provisions on service description and data protection still apply, but the full audit rights, exit provisions, and sub-contracting controls of Art. 30(2) are not mandatory.

---

## Step 3 — Pre-Contractual Due Diligence (Art. 28(4))

Before entering any new ICT service arrangement (or at renewal for existing arrangements), you must conduct **pre-contractual due diligence**. For critical or important function providers, this should include:

- Assessment of the provider's ICT security posture and certifications (e.g., ISO 27001, SOC 2)
- Assessment of the provider's financial stability and business continuity capabilities
- Review of the provider's sub-contracting chain (sub-processors)
- Assessment of data location and jurisdictional risks
- Assessment of **substitutability** — how easily could you replace this provider?
- Assessment of ICT concentration risk (Art. 28(6)) — are multiple critical functions supported by the same provider?

---

## Step 4 — Maintain the Register of Information (Art. 28(3) + CIR (EU) 2024/2956)

You must maintain a **Register of Information** covering **all** ICT service arrangements — all 15 providers must be registered, not only those supporting critical functions. The Register is submitted to your competent authority annually (or on demand).

**Mandatory fields per CIR (EU) 2024/2956 include:**

| Field | Description |
|-------|-------------|
| Arrangement reference | Unique identifier for each arrangement |
| TPSP name and LEI | Legal entity identifier of the service provider |
| Service type | Nature of ICT service (SaaS, IaaS, PaaS, etc.) |
| Critical or important function | Whether the function supported is critical/important (Y/N) |
| Data storage location | Country/region where data is stored and processed |
| Substitutability | Assessment of ease of substitution |
| Sub-processors | Chain of sub-processors |
| Contractual start/end dates | Term of the arrangement |

**Important:** A vendor list or procurement register does not satisfy the Register of Information requirement. The CIR 2024/2956 mandatory fields are specific and detailed.

---

## Step 5 — ICT Concentration Risk Assessment (Art. 28(6) + Art. 29)

With 15 providers, you must assess:

- Whether any single provider supports **multiple critical functions** (concentration at entity level — Art. 29)
- Whether there is **sector-wide concentration** in a small number of providers (e.g., if AWS or Microsoft Azure supports critical functions for many EU banks — this is relevant context for regulatory discussions but does not change your entity-level obligations)

If you identify material concentration risk (e.g., AWS supports core banking, payments, and disaster recovery), you must:
- Document the concentration risk assessment
- Consider mitigation measures (multi-cloud strategy, alternative providers)
- Reflect this in your exit strategy planning (Art. 28(7))

---

## Step 6 — Exit Strategies (Art. 28(7))

For each critical or important function supported by a third-party provider, you must develop and maintain a documented **exit strategy** covering:
- Trigger conditions for exit (regulatory direction, provider insolvency, material breach, resilience failure)
- Steps for transition to an alternative provider or in-house solution
- Data migration plan (aligned with Art. 30(2)(h) contractual rights)
- Estimated transition timescale and resource requirements

---

## Gap Assessment: Common Issues with Cloud/SaaS Contracts

| Common Gap | DORA Requirement |
|-----------|-----------------|
| Audit clause is nominal ("right to request reports") only | Art. 30(2)(e) requires substantive audit and access rights exercisable by the entity and competent authority |
| No data location specification in contract | Art. 30(2)(b) requires explicit specification of processing and storage locations |
| Sub-processor chain is opaque | Art. 30(2)(i) requires prior consent and notification of changes to sub-contracting |
| No termination/exit assistance clause | Art. 30(2)(f)–(h) require exit notice periods, termination rights, and migration assistance |
| Register of Information is an informal spreadsheet | CIR 2024/2956 requires specific mandatory fields; informal registers do not comply |
| No concentration risk documented | Art. 28(6) and Art. 29 require formal concentration risk assessment |

---

## Recommended Action Plan for Your 15 Providers

1. **Classify all 15 providers** as critical/important or non-critical against your function mapping
2. **Review all contracts** for Art. 30(2) provisions; identify gaps
3. **Negotiate contract addenda** where required (budget time — large cloud providers have standard DPAs that may need supplementation)
4. **Build the Register of Information** per CIR 2024/2956 mandatory fields
5. **Conduct concentration risk assessment** — map which providers support which critical functions
6. **Develop exit strategies** for critical providers
7. **Implement ongoing monitoring** per Art. 28(3) — annual review of all arrangements
