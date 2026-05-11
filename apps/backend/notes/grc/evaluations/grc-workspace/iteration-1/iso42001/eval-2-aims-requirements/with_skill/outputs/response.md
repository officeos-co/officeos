# ISO 42001 Key Requirements for an AI Product Developer — WITH SKILL Response

## Your Role Classification

As an organisation that **develops AI products**, you are classified as an **AI provider** under ISO/IEC 42001:2023. This means the full weight of Annex A controls apply — including lifecycle management (A.6), data governance (A.7), transparency (A.8), and human oversight (A.9). If you also use third-party AI components (APIs, models) in your products, you are simultaneously an **AI user** for those components.

---

## Mandatory Clause Requirements (Clauses 4–10)

The following are the required elements of the AIMS, mapped to the clause structure:

### Clause 4 — Context of the Organisation

**What you need:**
- **AIMS scope document**: Defines which AI systems, organisational units, and locations are in scope. Must state what is explicitly excluded and justify exclusions.
- **Stakeholder register**: Identify all interested parties (customers, end users, regulators, employees, society) and document their requirements and expectations relevant to AI
- **AI system register**: A living inventory of all AI systems in scope — each entry should capture system name, intended purpose, deployment context, and impact classification
- **Internal/external issues analysis**: Identify factors affecting your ability to govern AI responsibly (e.g., competitive pressure to deploy AI quickly, regulatory landscape, workforce AI literacy)

**Key deliverable**: Written AIMS Scope document, signed off by leadership, with rationale for inclusions and exclusions.

---

### Clause 5 — Leadership

**What you need:**
- **AI Policy** (Clause 5.2): Top-level policy signed by the CEO or equivalent. Must state the organisation's commitment to responsible AI, define the AIMS scope, and set the tone for AI governance. This is distinct from an Acceptable Use Policy — it is the overarching commitment document.
- **Roles and responsibilities**: Documented RACI (or equivalent) for AI governance — who owns AI risk, who approves AI system deployments, who leads incident response
- **Management commitment evidence**: Records showing leadership actively supports AIMS (e.g., management review minutes, resource allocation decisions)

**Key deliverable**: AI Policy document with document control metadata (version, owner, review date, top management signature).

---

### Clause 6 — Planning

This is the most technically demanding clause for AI providers:

**AI Risk Assessment** (Clause 6.1.2):
- Must be conducted for each in-scope AI system
- Covers AI-specific risk categories: model bias, data poisoning, hallucination, model drift, adversarial inputs, privacy violations, supply chain risks
- Risk treatment decisions must be documented (modify, accept with monitoring, avoid, or transfer via contract)

**AI System Impact Assessment (AISIA)** (Clause 6.1.2):
- **Mandatory** — separate from the risk assessment
- Assesses societal and individual impacts of each AI system
- Dimensions: intended purpose, output type, impact domain, affected population, severity, reversibility, human oversight availability
- Results in an impact classification: Low / Medium / High
- Impact level drives control selection (High impact = maximum controls)

**AIMS Objectives** (Clause 6.2):
- Must be measurable (e.g., "Achieve <2% false positive rate in model output by Q4", "Complete AISIA for all new AI systems within 30 days of design phase")
- Linked to responsible AI principles
- Plans to achieve objectives must be documented

**Key deliverables**: AI Risk Assessment records, AISIA records for each AI system, AIMS Objectives document with measurement plan.

---

### Clause 7 — Support

**What you need:**
- **Competence records**: Evidence that people working on AI systems have appropriate competence (qualifications, training records, experience logs) — especially for AI development, data science, and AI ethics roles
- **Awareness programme**: All staff (not just technical teams) must be aware of AI policy, their role in AIMS, and the consequences of non-compliance
- **Communication plan**: Define what AI-related information is communicated, to whom, and how (internally and externally)
- **Documented information procedure**: How AI-related documents and records are created, controlled, retained, and disposed of

**Key deliverables**: Training completion records, awareness programme materials, documented information procedure.

---

### Clause 8 — Operation

This is where AIMS is put into practice. For an AI product developer, the key operational controls come from Annex A:

| Annex A Domain | Key Controls | What to Implement |
|----------------|-------------|-------------------|
| **A.2 — Policies for AI** | A.2.2 AI policy | Overarching AI Policy (see Clause 5.2) |
| **A.4 — Resources** | A.4.3 Data resources | Data governance for training/validation/test datasets |
| **A.5 — Impacts** | A.5.1–A.5.5 | Assess impacts on individuals, groups, communities, society, environment |
| **A.6 — AI system lifecycle** | A.6.1–A.6.2 | Lifecycle management from concept through decommission; development, testing, deployment, monitoring controls |
| **A.7 — Data for AI** | A.7.1–A.7.6 | Data sourcing, quality, bias testing, privacy in training data, data provenance records |
| **A.8 — Technical controls** | A.8.1–A.8.5 | AI system transparency documentation, security of AI systems, AI incident management |
| **A.9 — AI use** | A.9.1 | Human oversight mechanisms — formal process for humans to review, intervene, override AI outputs |
| **A.10 — Third party** | A.10.3 | Supplier AI assessments, AI-specific contractual clauses with third-party AI providers |

**Key deliverables**: Executed risk and impact assessments, lifecycle control records (design reviews, testing records, deployment approvals), supplier assessment records, incident log.

---

### Clause 9 — Performance Evaluation

**What you need:**
- **Internal audit programme** (Clause 9.2): Planned schedule of AIMS internal audits; audit reports with findings and nonconformities
- **Management review** (Clause 9.3): Periodic (at minimum annual) review by top management covering AIMS performance, audit results, incidents, objective achievement, and opportunities for improvement — documented in meeting minutes
- **AIMS metrics / KPIs**: Measurable indicators tracked over time (e.g., number of AI incidents, AISIA completion rate, training completion rate, model performance metrics)

**Key deliverables**: Internal audit programme and reports, management review minutes, performance metrics dashboard/log.

---

### Clause 10 — Improvement

**What you need:**
- **Nonconformity log**: Record of all AIMS nonconformities (deviations from requirements)
- **Corrective action records**: Root cause analysis and corrective actions for each nonconformity
- **Continual improvement register**: Broader improvement initiatives beyond corrective actions

**Key deliverables**: Nonconformity and corrective action records, improvement register.

---

## Core Policies Required

Under the AIMS, the following policies should be documented:

| Policy | Clause/Control | Purpose |
|--------|---------------|---------|
| AI Policy | Clause 5.2 | Overarching commitment and scope |
| AI Risk Management Policy | Clause 6 | Risk assessment methodology, frequency, ownership |
| AI Acceptable Use Policy | A.9.2 | Permitted/prohibited AI uses, user obligations |
| Data Governance for AI Policy | A.7 | Training data quality, sourcing, retention, bias controls |
| AI Incident and Reporting Policy | A.8.4 | Incident classification, reporting, response, post-incident review |
| AI System Lifecycle Policy | A.6 | Development, testing, deployment, monitoring standards |
| AI Third-Party and Supplier Policy | A.10.3 | Third-party AI provider due diligence and contractual requirements |

---

## Statement of Applicability (SoA)

You must produce a **Statement of Applicability** covering all 38 Annex A controls (domains A.2–A.10), stating for each:
- Whether it is applicable to your organisation
- Justification for inclusion or exclusion
- Current implementation status
- Reference to evidence

This is a key Stage 1 audit document — auditors will review it to understand your control landscape.

---

## Minimum Documentation Set for Certification

| Document | Clause/Control |
|----------|---------------|
| AIMS Scope | 4.3 |
| AI Policy | 5.2 |
| AI System Register | 4 |
| AI Risk Assessment records | 6.1.2 |
| AISIA records (per system) | 6.1.2 |
| AIMS Objectives | 6.2 |
| Statement of Applicability | Annex A |
| Competence and training records | 7.2 |
| Documented information procedure | 7.5 |
| Lifecycle control records | A.6 |
| Data governance records | A.7 |
| Supplier assessment records | A.10.3 |
| Incident log | A.8.4 |
| Internal audit programme and reports | 9.2 |
| Management review minutes | 9.3 |
| Nonconformity and corrective action records | 10.2 |

---

*Response generated using ISO/IEC 42001:2023 AIMS skill — all clauses and Annex A controls cited per the standard.*
