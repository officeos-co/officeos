# Third-Party LLM API Supplier Management Under ISO 42001 — WITHOUT SKILL Response

## Overview

Using a third-party LLM API (like those from OpenAI or Anthropic) as a component in your AI product creates a layered responsibility model. ISO 42001 recognizes that organizations integrating AI from external providers retain accountability for how that AI behaves in their products — you cannot simply outsource AI governance responsibility to the API provider.

This means you have obligations both as an AI product developer (provider) and as an AI user integrating external AI. Both roles carry distinct but complementary requirements under ISO 42001's supplier management framework.

---

## Core Principle: You Remain Accountable

The foundational principle in ISO 42001's approach to third-party AI is this: **your customers experience your product, not your supplier's API.** If your product produces biased, harmful, or inaccurate outputs because of the underlying LLM's behavior, that is your risk and your responsibility to manage.

This parallels how other management system standards treat outsourcing — ISO 9001, for instance, requires that outsourced processes remain under the organization's quality management system. ISO 42001 applies the same logic to AI: embedding a third-party AI component does not remove the embedding organization's governance obligations.

---

## What ISO 42001 Requires for Third-Party AI Suppliers

### 1. Supplier Assessment Before Integration

Before integrating a third-party LLM API into your product, you should conduct a structured assessment of the AI provider. This goes well beyond a standard security vendor questionnaire. Key areas to assess include:

**AI Governance and Responsible AI Framework**
- Does the provider have a documented responsible AI framework or ethics policy?
- How do they govern what the model can and cannot do?
- What safety testing and red-teaming do they conduct?

**Model Training Data Practices**
- What data was used to train the underlying model?
- Is that training data compliant with applicable data protection laws (GDPR, etc.)?
- Does the provider document their training data sources and quality controls?

**Bias and Fairness Evaluation**
- Has the model been systematically tested for demographic biases?
- Are evaluation results published or available on request?
- What populations were included in bias testing?

**Transparency and Explainability**
- Does the provider publish a system card or model card documenting capabilities, limitations, and known failure modes?
- Are use case restrictions clearly documented?

**Data Handling Practices**
- Are inputs you send via the API used to retrain the provider's models?
- What is the data retention period for API inputs?
- Is there a formal Data Processing Agreement available?

**Incident Response and Communication**
- How does the provider handle and disclose AI-related incidents?
- Do they notify API customers of significant model behavior changes?

### 2. Contractual Provisions

Simply having a supplier assessment is not enough — ISO 42001 requires that your third-party AI obligations are reflected in your contracts with AI providers. Key contractual provisions to seek include:

**Model Versioning and Change Notification**
One of the most significant risks with third-party LLM APIs is that the underlying model can change without notice — what worked and tested well today may behave differently tomorrow if the provider updates the model. Seek contractual commitments for:
- Advance notice of significant model behavior changes (ideally 30+ days)
- The ability to pin to a specific model version while you evaluate updates
- Defined processes for communicating breaking changes

**Data Use Restrictions**
Explicitly address whether your API inputs will be used for the provider's model training. Many providers offer opt-out options or enterprise agreements that prohibit this. Without explicit contractual terms, you may be inadvertently allowing customer data to enter the provider's training pipeline.

**Liability and Risk Allocation**
Define where responsibility lies if the LLM produces harmful, biased, or incorrect outputs that cause harm to your customers. This is an evolving area of law, but having clear contractual language is important for both governance and legal risk management.

**Incident Notification**
Require the provider to notify you within a defined timeframe of incidents that could affect your product — including safety incidents, significant capability regressions, or discovered vulnerabilities in the model.

### 3. Ongoing Monitoring

The supplier relationship doesn't end at contract signature. ISO 42001 expects ongoing monitoring of third-party AI performance, which for an LLM API means:

- **Regression testing**: Periodically test your use case against the API to detect behavioral drift when providers update their models
- **Output monitoring**: Monitor production outputs for quality, bias, and anomalies on an ongoing basis
- **News and disclosure monitoring**: Track provider safety disclosures, incident reports, and policy changes
- **Annual reassessment**: Revisit your supplier assessment periodically, especially if the provider makes significant model changes

---

## Risks You Need to Include in Your Risk Register

When you use a third-party LLM, these AI-specific supply chain risks must appear in your own AI risk assessment:

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Inherited model bias | Your product exhibits discriminatory behavior from the LLM's training data | Test your specific use cases for bias; implement output filtering; document known limitations for users |
| LLM hallucination | Your product confidently provides false information to users | Implement retrieval-augmented generation; add human review for high-stakes outputs; set user expectations clearly |
| Silent model updates | Provider updates the model; your product behavior changes unexpectedly | Pin model versions; implement automated regression testing; monitor production outputs |
| Provider discontinuity | Provider shuts down the model or company; your product loses a critical dependency | Design for model portability; test with alternative providers; avoid single-point API dependency |
| Data exposure via API | Sensitive user data sent to the API is retained or used in ways you didn't intend | Data minimization before API calls; review data terms carefully; implement DPA |
| Prompt injection | End users manipulate your LLM integration to bypass controls or produce harmful outputs | Input validation; output filtering; system prompt hardening; adversarial testing |

---

## What You Cannot Delegate to the LLM Provider

Regardless of your contractual arrangements, certain obligations under ISO 42001 remain yours:

1. **Conducting your own impact assessment** of your AI product — the LLM provider's safety assessments of their base model do not substitute for your assessment of your specific deployment
2. **Implementing human oversight** appropriate for your use case — this is your design choice, not the provider's
3. **Providing transparency to your users** about AI involvement in your product
4. **Incident management for your product** — even if a root cause is a provider model failure, your users experience it as your product's failure
5. **Compliance with applicable regulations** in your sector and jurisdiction

---

## Practical Starting Points

For organizations just beginning to address third-party LLM governance:

1. **Review the provider's published documentation** — system cards, usage policies, data processing terms — and document your review
2. **Assess data handling terms** specifically — determine whether your API inputs are used for training and whether this is acceptable given your customers' data
3. **Move to versioned API endpoints** where the provider offers them, rather than using floating "latest" endpoints
4. **Add AI-specific provisions** to your next contract renewal or data processing agreement
5. **Include third-party LLM risks** in your AI risk register with documented treatment decisions
6. **Implement output monitoring** so you can detect behavioral changes and potential harms before customers escalate them

The regulatory environment around third-party AI liability is evolving quickly, and proactive supplier management positions you well both for ISO 42001 compliance and for the emerging expectations under frameworks like the EU AI Act.
