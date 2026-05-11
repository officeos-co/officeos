# AI-Specific Risks in ISO 42001 vs Traditional IT Risk Management — WITHOUT SKILL Response

## Why AI Needs Its Own Risk Framework

Traditional IT risk management frameworks like ISO 27001 were built to protect the confidentiality, integrity, and availability of information and information systems. They address threats like unauthorized access, data breaches, system failures, and malicious code. These remain important, but AI systems introduce an entirely different class of risks that traditional frameworks were not designed to handle.

The fundamental difference is this: **traditional IT systems are deterministic — they do what they're programmed to do. AI systems are probabilistic — they learn patterns from data and make predictions, and their behavior can be surprising, unfair, or harmful in ways that cannot be fully anticipated from code review alone.**

---

## Category 1: Model Risk — The AI Behaves Unexpectedly

### Algorithmic Bias and Discrimination
Traditional software treats all inputs according to the same programmed rules. AI models can learn and perpetuate discriminatory patterns that exist in training data, producing systematically different — and worse — outcomes for certain groups.

**Concrete example**: A recruitment AI trained on a company's historical hiring decisions learns that the company historically hired fewer women in engineering roles. It begins down-ranking female candidates' resumes, automating and scaling past discrimination without any human making an explicit discriminatory decision.

### Hallucination (Generating False Information)
Traditional software either returns data or throws an error when it doesn't have information. Generative AI can produce confident, fluent, but entirely fabricated outputs.

**Concrete example**: A customer-facing AI assistant built on a large language model confidently tells a customer that a product has safety certifications it doesn't actually have, or provides incorrect dosage information for a medication. There is no error message — just wrong information delivered with confidence.

### Model Drift
Traditional software does not change unless someone modifies it. AI models can become less accurate over time as the real world changes, without any code change.

**Concrete example**: A fraud detection AI trained on pre-2020 transaction patterns becomes progressively less accurate as consumer behavior permanently shifts post-pandemic. Fraud rates rise while the model continues reporting normal performance metrics based on outdated patterns.

### Adversarial Attacks
Traditional cyberattacks exploit code vulnerabilities. Adversarial attacks on AI exploit the statistical nature of machine learning — making small, imperceptible changes to inputs that cause the model to produce wrong outputs.

**Concrete example**: Researchers have demonstrated that adding a small sticker pattern to a stop sign causes some image recognition models (used in autonomous vehicles) to misclassify it as a speed limit sign. Humans see a normal stop sign; the AI sees something completely different.

---

## Category 2: Data Risk — The Problem Starts Before Deployment

### Training Data Quality Failures
In traditional IT, data quality affects reports and queries but can be corrected. In AI, training data quality determines what the model learns — and poor training cannot be retroactively fixed without retraining the entire model.

**Concrete example**: A medical diagnosis AI is trained primarily on data from academic medical centers, which skew toward severe, unusual, or complex cases. When deployed in primary care settings, it performs poorly because the population it was trained on doesn't represent the patients it now encounters.

### Data Poisoning
This is an AI-specific attack where malicious actors inject carefully crafted examples into a model's training data to corrupt its behavior in attacker-chosen ways.

**Concrete example**: An attacker who gains access to a financial institution's training pipeline injects transactions labeled as legitimate that share subtle characteristics with a new fraud technique the attacker plans to use. The trained model is then blind to that specific fraud pattern.

### Privacy Leakage from Training Data
Traditional data privacy focuses on access controls — preventing unauthorized people from seeing data. AI creates a new risk: models can memorize and inadvertently reproduce training data, even if those records were never meant to be accessible.

**Concrete example**: A customer service AI trained on historical support tickets can, under certain prompting conditions, reproduce verbatim text from those tickets — including customer names, addresses, or account details — to unrelated users.

---

## Category 3: Operational Risks — Scale and Automation

### Harm at Scale
Traditional software bugs cause defined, bounded errors. AI errors can silently affect large populations before they are detected.

**Concrete example**: A content moderation AI incorrectly flags posts in a minority language as violating platform policies, because the language is underrepresented in its training data. Hundreds of thousands of legitimate posts are removed before anyone notices the systematic error.

### Automation Bias and Over-Reliance
There is no equivalent to this in traditional IT. Humans tend to over-trust AI recommendations, especially when the AI presents outputs with high confidence scores.

**Concrete example**: Radiologists using an AI-assisted screening tool become less likely to challenge the AI's findings over time. When the AI misses certain early-stage tumors that fall outside its training distribution, the radiologists also miss them — not because they wouldn't have caught them without the AI, but because they defer to the AI's judgment.

### Explainability and Auditability Failures
Traditional software decisions are fully auditable through code review. Many AI models, especially deep learning systems, are not explainable even to their developers.

**Concrete example**: A credit scoring AI denies a loan. The customer asks why. The company cannot explain the specific factors — they know inputs were provided and a score came out, but the internal reasoning of the neural network is opaque. Under GDPR's right to explanation for automated decisions, this is a compliance failure.

---

## Category 4: Third-Party AI Supply Chain Risk

Traditional supplier risk management in IT focuses on whether a vendor has good security practices. AI supply chain risk adds a new dimension: **do your AI suppliers have good AI governance practices?**

**Concrete examples**:
- You embed a third-party sentiment analysis API that has documented racial bias in its training data — you inherit that bias as your own risk and reputational exposure
- A third-party AI provider updates their underlying model without notifying you — your application's behavior changes in ways you didn't test or approve
- You depend on an AI startup's specialized model for a core product feature; the startup shuts down with no notice, leaving you unable to replicate its capabilities

---

## Category 5: Societal and Systemic Risks

Traditional IT risk management is organizational — it focuses on risks to the business. AI risk management must also consider impacts on people and society, including indirect and systemic effects that no single actor intends.

**Concrete examples**:
- A predictive recidivism AI used in criminal sentencing systematically assigns higher risk scores to Black defendants, perpetuating racial disparities in incarceration — a harm to society that emerges from aggregate AI use, not from any single decision
- A social media recommendation algorithm optimized for engagement learns that outrage-inducing content keeps users on the platform longer, inadvertently contributing to political polarization and erosion of shared epistemic standards
- Multiple banks independently deploy similar AI credit-scoring models trained on similar data; the correlated nature of their decisions amplifies systemic credit risk in ways no individual bank's risk model captures

---

## Summary: What Traditional IT Risk Management Misses

| Risk Type | Covered by ISO 27001? | Unique to AI? |
|-----------|----------------------|---------------|
| Unauthorized data access | Yes | No |
| System availability failures | Yes | No |
| Algorithmic bias | No | Yes |
| Hallucination / false outputs | No | Yes |
| Model drift | No | Yes |
| Adversarial attacks on AI models | Partially (cybersecurity) | Mostly yes |
| Training data poisoning | No | Yes |
| Privacy leakage from model weights | No | Yes |
| Automation bias / over-reliance | No | Yes |
| Explainability failures | No | Yes |
| Third-party model governance risk | No | Yes |
| Societal/systemic impacts | No | Yes |

The core insight is that **AI systems fail differently than traditional software** — their failures are often silent, statistical, and societal rather than obvious, binary, and technical. Effective AI risk management requires a framework specifically designed for these characteristics.
