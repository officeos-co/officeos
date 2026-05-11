# Third-Party LLM API Supplier Management Under ISO 42001 — WITH SKILL Response

## Your Dual Role

When you use a third-party LLM API (such as OpenAI or Anthropic) as a component in your AI product, ISO/IEC 42001:2023 classifies you in a **dual role**:
- **AI provider**: You develop and deploy an AI product to your customers
- **AI user**: You integrate a third-party AI system (the LLM) from an external provider

This dual role matters because it activates obligations from **both** the AI provider and AI user perspectives in Annex A — particularly Annex A.10 (Third-party and supplier requirements for AI).

The key principle under ISO 42001 is clear: **using a third-party LLM does not transfer your AI governance obligations to the LLM provider.** You remain accountable for the behaviour of your AI product, including the component behaviour of the third-party LLM within it.

---

## Primary Control: Annex A.10.3 — Suppliers

**A.10.3** is the core third-party AI supplier management control. It requires organisations to:

1. **Assess third-party AI providers** before integration — not just their security posture (which ISO 27001 addresses) but specifically their **AI governance practices**
2. **Establish AI-specific contractual obligations** with third-party AI providers
3. **Monitor ongoing supplier AI performance** — including model updates and changes

### What the A.10.3 Assessment Must Cover

For an LLM API provider like OpenAI or Anthropic, your supplier assessment under A.10.3 should evaluate:

| Assessment Dimension | What to Look For | Key Questions to Ask |
|---------------------|-----------------|---------------------|
| **AI governance and responsible AI framework** | Does the provider have a documented responsible AI framework? Do they publish an AI safety/ethics policy? | What principles govern their model development? How are those principles enforced? |
| **Model training data practices** | What data was used to train the model? Is it GDPR-compliant? Does it avoid rights-violating data sources? | What datasets were used? Were data subjects notified? How is copyright handled? |
| **Bias testing and evaluation** | Has the model been systematically tested for fairness and bias? Are results published? | What bias evaluations have been conducted? What populations were tested? |
| **Model update and change notification** | Will they notify you when the underlying model changes behaviour? (This is a significant risk — see model drift) | What is their policy for notifying customers of model updates? Is there versioning? Can you pin to a specific model version? |
| **Incident management and disclosure** | How do they handle and disclose AI-related incidents (e.g., harmful outputs, data exposure)? | What is their AI incident response process? How are customers notified? |
| **Transparency about capabilities and limitations** | Do they document known failure modes, limitations, and use case restrictions? | What does their system card / model card document? Are known limitations disclosed? |
| **Data processing and retention** | How is data you send via the API processed, stored, and used? Will it be used to retrain their models? | Are API inputs used for training? What is the data retention period? Is there a data processing agreement? |

---

## Contractual Requirements Under A.10.3

ISO 42001 requires that contracts with third-party AI providers include **AI-specific clauses** beyond standard data processing agreements. Key contractual provisions to include:

### 1. Model Versioning and Change Notice
- Right to pin to a specific model version (e.g., `gpt-4-turbo-2024-04-09` rather than `gpt-4-turbo`)
- Obligation for advance notice (ideally 30+ days) before significant model behaviour changes
- Rationale: Unnoticed model updates can silently change your product's behaviour — this is a form of AI-specific operational risk

### 2. Transparency About Limitations and Known Failures
- Provider must disclose known failure modes, bias characteristics, and use case restrictions applicable to your deployment
- Reference their system card / model card as a contractual deliverable

### 3. Data Processing Restrictions
- Explicit prohibition on using your API inputs to retrain the provider's models (or explicit consent if you accept this)
- Data retention period and deletion commitments
- This addresses the training data privacy risk under Annex A.7 and Clause 6.1.2

### 4. Incident Notification
- Obligation to notify you within a defined timeframe (e.g., 48–72 hours) of incidents affecting the model's behaviour, safety, or security that could impact your product

### 5. AI Governance Representations
- Warranties that the model was developed in accordance with the provider's published responsible AI framework
- Right to audit (or review third-party audit reports) of the provider's AI governance practices

### 6. Liability and Risk Allocation
- Clarity on where liability sits if the LLM produces harmful, biased, or incorrect outputs that cause loss to your customers

---

## Risk Assessment Obligations Under Clause 6.1.2

Even though the LLM is a third-party component, you must include **third-party LLM risks** in your own AI risk assessment (Clause 6.1.2). These supply chain risks are explicitly listed in ISO 42001's risk framework:

| Risk | Description | Treatment Options (Clause 6.1.3) |
|------|-------------|----------------------------------|
| **Inherited model bias** | The LLM has biases from its training data that manifest in your product | Test your use case for bias; implement output filters; document known limitations; transfer risk contractually via A.10.3 |
| **Hallucination** | The LLM produces confident but false outputs | Implement retrieval-augmented generation (RAG); add human review for high-stakes outputs; prompt engineering guardrails |
| **Model drift from provider updates** | Provider updates the model; your product behaviour changes without your action | Pin model versions; implement regression testing on model update; monitor output quality metrics |
| **Provider dependency / continuity risk** | Provider discontinues the model or goes out of business | Architect for model portability; maintain fallback options; assess provider financial stability |
| **Data exposure via API** | Sensitive data sent to the API is retained or used inappropriately | Data minimisation before API calls; review provider data processing terms; DPA in place |
| **Prompt injection / adversarial inputs** | End users manipulate your LLM integration to produce harmful outputs | Input validation; output filtering; system prompt hardening; rate limiting |

---

## AI System Impact Assessment (AISIA) — Clause 6.1.2

Your AISIA for the AI product must reflect the fact that the LLM is a component. Key considerations:
- The LLM's output type (content generation, classification, decision support) affects the impact level of your product
- If the LLM drives decisions or recommendations affecting individuals, this increases impact classification
- Human oversight requirements (Annex A.9.1) must account for the fact that LLM outputs may be unpredictable — formal human review processes are especially important

---

## Practical Implementation Checklist

Under ISO 42001, your supplier management programme for a third-party LLM API should produce the following evidence:

| Evidence | Clause/Control | Status |
|----------|---------------|--------|
| Supplier AI assessment report for LLM provider | A.10.3 | Required |
| Data Processing Agreement (DPA) with LLM provider | A.10.3, A.7 | Required |
| AI-specific contractual clauses documented | A.10.3 | Required |
| LLM risks included in AI risk register | Clause 6.1.2 | Required |
| Third-party LLM included in AISIA scope | Clause 6.1.2 | Required |
| Model version pinning or change monitoring process | A.10.3, A.6 | Required |
| Incident notification process from provider | A.8.4, A.10.3 | Required |
| Annual review of supplier AI assessment | A.10.3, Clause 9 | Required |

---

## Common Gap: What Organisations Typically Miss

The most common gap flagged in ISO 42001 audits for organisations using third-party LLMs is:

1. **No AI-specific supplier assessment** — only a general security questionnaire sent to the provider, with no questions about model governance, bias testing, or training data practices
2. **Data processing terms not reviewed** — assuming the API provider's standard terms are acceptable without reviewing whether API inputs are used for model training
3. **No model version pinning** — using a floating model version endpoint, meaning silent model updates can affect product behaviour
4. **Third-party risks missing from the AI risk register** — the risk register covers internal model risks but treats the third-party LLM as a black box outside the risk management scope

Addressing these four gaps is the minimum required to satisfy A.10.3 in an ISO 42001 audit context.

---

*Response generated using ISO/IEC 42001:2023 AIMS skill — all clauses and Annex A controls cited per the standard.*
