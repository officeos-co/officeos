# AI-Specific Risks in ISO 42001 vs Traditional IT Risk Management — WITH SKILL Response

## Why AI Risk Is Different

ISO/IEC 42001:2023 explicitly recognises that AI systems introduce a fundamentally different risk profile from traditional IT systems, and this is why the standard mandates a **separate AI risk assessment** (Clause 6.1.2) in addition to whatever general organisational risk management an organisation already performs.

The core difference is that traditional IT risk management (as found in ISO 27001) addresses **deterministic systems** — software that behaves predictably given defined inputs. AI systems are **probabilistic, adaptive, and emergent** — their behaviour cannot be fully specified in advance and can change over time, even without deliberate modification. This creates risk categories that simply do not exist in conventional IT.

ISO 42001 also introduces the **AI System Impact Assessment (AISIA)** (Clause 6.1.2) — an impact-focused evaluation that goes beyond likelihood × severity to assess societal and individual consequences. There is no equivalent in ISO 27001.

---

## ISO 42001 AI-Specific Risk Categories (vs ISO 27001 Comparison)

### 1. Model Risks

These are risks arising from the AI model itself — how it was built, trained, and what it produces.

| AI-Specific Risk | How It Differs from Traditional IT | Concrete Example |
|-----------------|-----------------------------------|-----------------|
| **Algorithmic bias and unfairness** | Traditional IT systems apply rules equally; AI models can learn and perpetuate discriminatory patterns from training data | A credit scoring AI trained on historical loan data learns that applicants from certain postcodes are higher risk, reflecting historical discrimination — denying loans to creditworthy individuals in those areas |
| **Hallucination / confabulation** | Traditional software returns errors on unknown inputs; generative AI produces confident but false outputs | An LLM-based legal research tool confidently cites non-existent case law, leading a lawyer to submit fictitious precedents in court |
| **Model drift** | Traditional software does not change unless modified; AI models can become less accurate as real-world data distributions change without any code change | A fraud detection model trained on pre-pandemic spending patterns becomes less accurate post-pandemic as consumer behaviour permanently shifts, missing new fraud patterns |
| **Adversarial attacks** | SQL injection or buffer overflows require exploiting code vulnerabilities; adversarial AI attacks manipulate inputs in ways imperceptible to humans | An image recognition AI in a self-driving car misclassifies a stop sign with subtle pixel-level perturbations added to the sign — the human eye sees a normal stop sign, the model sees something else |
| **Output unpredictability / scope creep** | Traditional software has defined output ranges; AI systems can produce unexpected outputs outside anticipated parameters | A customer service chatbot drifts from answering product questions to expressing opinions on unrelated topics, creating reputational or legal risk |

**ISO 42001 treatment under Clause 6.1.3**: Model risks are treated by modifying the AI system (retraining, guardrails), accepting with continuous monitoring and defined performance thresholds, or avoiding deployment for specific use cases.

---

### 2. Data Risks

ISO 42001 Annex A.7 (Data for AI) specifically addresses these risks — there is no equivalent domain in ISO 27001 Annex A because traditional IT systems do not learn from data.

| AI-Specific Risk | How It Differs from Traditional IT | Concrete Example |
|-----------------|-----------------------------------|-----------------|
| **Training data quality failures** | Traditional IT data quality affects query results; training data quality determines what an AI model learns and cannot be retroactively corrected without retraining | A medical AI trained on patient records from a single hospital learns demographic-specific patterns that don't generalise to the broader population, producing systematically worse diagnoses for patients not represented in training data |
| **Data poisoning** | Traditional IT data integrity is about accuracy; data poisoning is an AI-specific attack where malicious data is injected into training pipelines to corrupt model behaviour | An attacker gains write access to a model provider's training data pipeline and injects examples that cause the model to misclassify specific inputs in attacker-chosen ways |
| **Privacy violations in training data** | Traditional IT privacy focuses on data access controls; AI training data can cause models to memorise and later reproduce personal information | An LLM trained on internet data can be prompted to reproduce verbatim excerpts of training documents containing personal information — a data protection violation that emerges from the model rather than a data breach |
| **Underrepresentation / sampling bias** | Traditional data quality focuses on accuracy; AI training data must also be representative of all groups the model will make decisions about | A facial recognition model trained mostly on lighter-skinned faces has significantly higher error rates for darker-skinned individuals — a bias that emerges from training data composition, not a code error |

**ISO 42001 Annex A.7 controls address**: Data sourcing governance, training data quality standards, bias evaluation, data provenance documentation, and data privacy in training pipelines.

---

### 3. Operational Risks

| AI-Specific Risk | How It Differs from Traditional IT | Concrete Example |
|-----------------|-----------------------------------|-----------------|
| **Automation of harm at scale** | A bug in traditional IT typically causes a defined error; an AI flaw can systematically harm large populations before detection | A hiring AI with a gender bias automatically rejects all applications from women across thousands of job postings — the scale of harm is orders of magnitude larger than a single discriminatory human decision |
| **Unexplainability of decisions** | Traditional IT decisions are auditable through code review; AI decisions (especially deep learning) may be opaque even to developers | A loan denial from a traditional rules-based system can be fully explained by pointing to the specific rule triggered. A neural network denial cannot always be explained — creating regulatory compliance risks under GDPR Art. 22 (automated decision-making) |
| **Over-reliance / automation bias** | Not applicable in traditional IT; humans may defer inappropriately to AI recommendations | Radiologists become over-reliant on an AI-assisted cancer screening tool and stop exercising independent clinical judgement, meaning they miss cancers the AI misclassifies |

**ISO 42001 Annex A.9.1** (Human oversight) specifically controls for automation bias by requiring formal mechanisms for human review and the ability to override AI outputs.

---

### 4. Supply Chain / Third-Party AI Risks

ISO 27001 addresses supplier risk (A.5.19–A.5.22) but focuses on information security of supplier systems. ISO 42001 Annex A.10.3 addresses a fundamentally different risk: **the AI governance quality of your AI providers**.

| AI-Specific Risk | Concrete Example |
|-----------------|-----------------|
| **Inherited model bias from third-party providers** | You embed a third-party sentiment analysis API into your HR platform without knowing it has documented racial bias — you inherit that bias as your own risk |
| **Third-party model drift** | A third-party LLM API you rely on updates its underlying model with different behaviour — your application's outputs change without any action on your part |
| **Provider lock-in / continuity risk** | A small AI startup providing a critical NLP component shuts down, leaving you without the model you depended on and unable to replicate its performance with alternatives |
| **Opaque training practices** | Your third-party AI provider cannot demonstrate what data their model was trained on or whether it complies with GDPR data usage restrictions |

**ISO 42001 Annex A.10.3** requires: third-party AI provider due diligence, AI-specific contractual clauses (covering model governance, bias testing, change notification, data practices), and ongoing supplier monitoring.

---

### 5. Societal Risks

ISO 27001 does not address societal impacts — its scope is organisational information security. ISO 42001 Annex A.5 (Impacts on interests of individuals and society) is unique:

| AI-Specific Societal Risk | Concrete Example |
|--------------------------|-----------------|
| **Discriminatory outcomes at population scale** | A predictive policing AI systematically recommends increased patrols in minority neighbourhoods, reinforcing over-policing and creating feedback loops that amplify existing disparities |
| **Erosion of human autonomy** | A recommendation algorithm optimised for engagement subtly shapes user beliefs and behaviours over time without users' awareness |
| **Misinformation amplification** | A content moderation AI incorrectly flags legitimate speech as harmful at scale, or fails to flag genuine misinformation — with societal consequences |
| **Environmental impact** | Large AI model training consumes significant energy and contributes to carbon emissions — an emerging area of societal risk |

---

## Key Structural Difference: AISIA vs. Traditional Risk Assessment

The most significant structural difference ISO 42001 introduces is the mandatory **AI System Impact Assessment (AISIA)** (Clause 6.1.2), which has no equivalent in ISO 27001:

| | ISO 27001 Risk Assessment | ISO 42001 AI Risk Assessment | ISO 42001 AISIA |
|--|--------------------------|-----------------------------|--------------------|
| **Focus** | Threats to CIA of information | Likelihood × severity of AI-specific risks | Societal and individual impact of AI system existence and outputs |
| **Output** | Risk register with treatment decisions | AI risk register with AI-specific treatment options | Impact classification (Low/Medium/High) driving control selection |
| **Scope** | Organisational assets | AI system behaviour and failure modes | Effect on individuals, groups, communities, society |
| **When done** | Periodic, at least annually | Per AI system, before deployment | Per AI system, before deployment |
| **Result** | Security controls selected | Risk treatment plan | Transparency obligations, human oversight requirements |

---

## Summary

ISO 42001's AI-specific risk management goes well beyond ISO 27001 in these key ways:
- **Separate AI risk assessment** covering model, data, operational, supply chain, and societal risk categories
- **Mandatory AISIA** assessing impact on people and society — not just organisational risk
- **Annex A.7** specifically for training data quality and bias — not present in any traditional IT standard
- **Annex A.9.1** for human oversight — a control category unique to AI governance
- **Annex A.10.3** for AI-specific supplier due diligence beyond information security
- **Annex A.5** for societal impact assessment — a category entirely absent from IT risk management frameworks

*Response generated using ISO/IEC 42001:2023 AIMS skill — all clauses and Annex A controls cited per the standard.*
