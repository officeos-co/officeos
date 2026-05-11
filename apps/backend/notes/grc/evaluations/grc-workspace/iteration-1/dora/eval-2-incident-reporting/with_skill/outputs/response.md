# DORA Obligations: ICT Third-Party Outage — Incident Classification and Reporting

## Scenario: AWS (Critical ICT TPSP) Suffers an Outage

Under DORA, an outage at a critical ICT third-party service provider such as AWS does not automatically trigger your incident reporting obligations — what matters is whether the outage causes an ICT-related incident **at your institution** that meets the materiality thresholds for a "major" incident. Your obligations flow from Chapter III (Art. 17–23), which is the reactive, event-driven framework, distinct from your ongoing ICT risk management obligations in Chapter II.

---

## Step 1 — Activate Your Incident Management Process (Art. 17)

Under Art. 17(1), you must have a documented ICT-related incident management process with:
- Defined roles, responsibilities, and escalation paths
- Thresholds for classifying incidents as "major"
- Senior management and board notification for major incidents (Art. 17(3))

When AWS suffers an outage affecting your services, your incident management team must immediately assess whether the event meets or exceeds your classification thresholds.

---

## Step 2 — Classify the Incident (Art. 18 + CDR (EU) 2024/1772)

Under Art. 18(1), incidents are classified using six mandatory criteria:

| Criterion | Art. 18(1) Ref | AWS Outage Considerations |
|-----------|---------------|--------------------------|
| Number of clients/counterparts affected and transaction value | (a) | How many customers cannot access banking services? What is the aggregate transaction value blocked? |
| Reputational impact | (b) | Is the outage publicly known? Is media coverage affecting your institution's reputation? |
| Duration and geographic spread | (c) | How long has the outage lasted? Which geographic regions are affected? |
| Data losses (availability, authenticity, integrity, confidentiality) | (d) | Has data availability been impaired? Any integrity or confidentiality concerns? |
| Criticality of services affected | (e) | Are critical banking functions (payments, online banking, core systems) unavailable? |
| Economic impact | (f) | What is the estimated financial loss, including foregone transactions and remediation costs? |

**Materiality thresholds** are set in **CDR (EU) 2024/1772** (RTS on classification). An incident is classified as **major** if it meets or exceeds any single threshold. The thresholds specify quantitative triggers (e.g., percentage of clients affected, transaction value, duration in hours).

**Important:** You must classify the incident at your institution, even if the root cause is AWS. DORA does not exclude third-party-caused incidents from your reporting obligations.

---

## Step 3 — Three-Stage Reporting to Your Competent Authority (Art. 19)

Once an incident is classified as **major**, you must follow the three-stage reporting timeline under Art. 19:

| Stage | Deadline | Required Content |
|-------|----------|-----------------|
| **Initial notification** | **4 hours** after classification as major | Basic facts: nature of incident, services affected, initial impact assessment, classification outcome |
| **Intermediate report** | **72 hours** after classification as major | Updated impact assessment, root cause indications (AWS outage identified as source), containment measures taken |
| **Final report** | **1 month** after initial notification | Full root cause analysis, lessons learned, recovery measures implemented, resilience improvements planned |

**Key RTS:** CDR (EU) 2025/301 — sets the content requirements and time limits for each reporting stage
**Key ITS:** CIR (EU) 2025/302 — provides standard forms and templates for submission

**Reporting channel:** Reports must be submitted to your national competent authority (e.g., ECB/SSM if you are a significant institution, or your national banking supervisor for less significant institutions).

---

## Step 4 — Voluntary Cyber Threat Reporting (Art. 19(2))

If the AWS outage is caused by a cyber attack (e.g., a DDoS or ransomware event targeting AWS infrastructure), you may also voluntarily notify your competent authority of the **significant cyber threat**, even before it has caused a classifiable incident at your institution. This is distinct from mandatory incident reporting and can be done proactively.

---

## Step 5 — Payment-Specific Obligations (Art. 23)

As a credit institution, if the AWS outage affects payment processing, Art. 23 applies specific rules for payment-related major incidents. These integrate with EBA payment security reporting obligations.

---

## Your Third-Party Risk Management Obligations in Parallel (Chapter V)

While the Chapter III incident process runs in parallel, the AWS outage also has implications for your Chapter V (third-party risk) obligations:

- **Art. 28(6) — ICT concentration risk:** An AWS outage affecting multiple critical functions demonstrates your concentration risk exposure. You must assess and document this.
- **Art. 28(7) — Exit strategy:** If the outage exposes gaps in your exit or substitution plans for AWS, these must be addressed.
- **Art. 30(2)(d)–(g) — Contractual provisions:** Your AWS contract should contain provisions on availability and security standards, incident notification by AWS to you, and audit/monitoring rights. If these are absent or inadequate, this is a DORA compliance gap.

---

## Common Errors to Avoid

| Error | Correct Approach |
|-------|-----------------|
| Waiting to report until the outage is resolved | The 4-hour clock starts at **classification as major**, not at resolution |
| Submitting only one report | DORA requires **three reports**: initial (4h), intermediate (72h), final (1 month) |
| Treating AWS's own incident communications as sufficient | DORA reporting runs from **your institution** to **your competent authority** — AWS's communications to you do not satisfy your regulatory obligations |
| Classifying as minor to avoid reporting | Misclassification is itself a regulatory breach; apply CDR 2024/1772 thresholds objectively |

---

## Practical Preparation Checklist

- [ ] Does your incident management process (Art. 17) explicitly address third-party-caused incidents?
- [ ] Do you have the CDR 2024/1772 classification thresholds embedded in your incident triage playbook?
- [ ] Have you pre-identified your competent authority contact and reporting channel?
- [ ] Do you have templates aligned with CIR 2025/302 ready for submission?
- [ ] Does your AWS contract include Art. 30(2)(d)–(g) compliant provisions (availability SLAs, incident notification obligations, audit rights)?
- [ ] Have you assessed AWS as a concentration risk under Art. 28(6)?
