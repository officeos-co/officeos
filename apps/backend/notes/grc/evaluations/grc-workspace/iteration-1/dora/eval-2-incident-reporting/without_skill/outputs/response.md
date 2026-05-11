# DORA Obligations: Critical ICT Third-Party Outage (AWS)

## Overview

Under DORA, an outage at a critical ICT third-party service provider like AWS triggers a set of obligations for your institution. The key point is that your reporting obligations relate to the impact on **your institution and your clients**, not merely to AWS's own incident. Your obligations span incident classification, internal management, and regulatory reporting.

---

## Incident Classification

DORA requires financial entities to classify ICT-related incidents based on defined criteria. When an AWS outage occurs, you must assess whether it constitutes a "major" ICT-related incident at your institution. Classification criteria under DORA typically include:

- **Number of clients affected**: How many customers are unable to access services?
- **Duration of the disruption**: How long are services unavailable?
- **Geographic scope**: Is the impact limited to one region or widespread?
- **Services affected**: Are critical banking functions (payments, online banking, core systems) impacted?
- **Data impact**: Is there any risk to data availability, integrity, or confidentiality?
- **Economic/financial impact**: What is the estimated financial loss?
- **Reputational impact**: Is there significant media or public attention?

DORA's implementing technical standards (published by EBA and the ESAs) provide specific quantitative thresholds that trigger "major incident" classification.

---

## Reporting Obligations

If the AWS outage causes a **major ICT-related incident** at your institution, DORA requires multi-stage reporting to your national competent authority (your banking supervisor):

### Reporting Timeline

DORA establishes a three-stage reporting process:

1. **Initial Notification** — Must be submitted quickly after the incident is classified as major (generally understood to be within a few hours, with some sources suggesting around 4 hours). This covers basic facts and initial impact.

2. **Intermediate Report** — A follow-up report providing more detail on the incident's impact, root cause analysis progress, and containment measures. This is typically required within a few days (approximately 72 hours in some framings).

3. **Final Report** — A comprehensive report including full root cause analysis, lessons learned, and remediation actions. This is typically due within about one month of the initial report.

The exact timelines are defined in DORA's Regulatory Technical Standards (RTS) adopted by the ESAs.

### What to Report

- Nature and description of the incident
- Services and systems affected
- Number of clients and counterparts affected
- Business impact (financial, operational, reputational)
- Root cause (including AWS as the third-party source)
- Remediation actions taken and planned

---

## Third-Party Specific Obligations

Because AWS is an ICT third-party service provider, the DORA incident also has implications for your third-party risk management obligations:

- **Contractual provisions**: Your contract with AWS should include incident notification obligations — AWS should be required to notify you promptly of incidents affecting your services. DORA requires that contracts with critical providers include specific provisions on availability, security, and incident reporting.
- **Concentration risk**: If AWS supports multiple critical functions, this outage highlights concentration risk that you must formally assess and manage under DORA.
- **Exit strategy**: DORA requires exit strategies for critical third-party arrangements, so this incident is an opportunity to test whether your exit plan for AWS is viable.

---

## Voluntary Reporting

DORA also allows (but does not require) financial entities to voluntarily report significant cyber threats to their competent authority, even before they have resulted in a major incident. If the AWS outage is cyber-related, you may consider doing this proactively.

---

## Key Practical Steps

1. Activate your incident management process immediately
2. Apply DORA's classification criteria to determine if the incident is "major"
3. If major, begin the multi-stage regulatory reporting process
4. Document all decisions and evidence throughout
5. Conduct a post-incident review once the outage is resolved
6. Review your AWS contract for DORA-compliant provisions
7. Reassess your ICT concentration risk profile
