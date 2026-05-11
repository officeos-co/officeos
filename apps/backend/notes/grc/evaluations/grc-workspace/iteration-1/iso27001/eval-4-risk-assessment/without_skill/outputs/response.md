# ISO 27001 Risk Assessment — Beginner's Guide

## What Is a Risk Assessment and Why Does It Matter?

In ISO 27001, the risk assessment is the foundation of everything. The standard requires you to systematically identify and evaluate risks to your information assets, then decide what to do about them. The controls you implement (and what you include in your Statement of Applicability) should flow directly from this process.

Without a risk assessment, you cannot demonstrate to an auditor that your security controls are based on actual risks rather than random choices.

---

## Step 1: Choose and Document Your Methodology

ISO 27001 does not mandate a specific risk assessment methodology. However, you must document whichever method you choose and apply it consistently.

The most commonly used approach is **likelihood × impact scoring**, sometimes called a qualitative risk matrix. This is what we'll walk through here.

Before you start assessing risks, document:
- **Your scoring scales** (e.g., 1–5 for likelihood and 1–5 for impact)
- **What each score means** in plain terms
- **Your risk acceptance threshold** — what score requires treatment vs. what you'll accept

---

## Step 2: Define Your Scope

Your risk assessment should cover everything within your ISMS scope — typically the systems, people, and processes involved in your core business. Define this clearly before you start. You don't need to assess everything in the entire world, only what's in scope.

---

## Step 3: Identify Your Information Assets

List what you are trying to protect. Common categories include:
- Customer data and databases
- Employee data (HR records)
- Financial records
- Business applications and software
- Infrastructure (servers, networks, cloud services)
- Intellectual property (source code, product designs)
- Physical assets (laptops, office equipment)
- Third-party services you depend on

For each asset, assign an **owner** — someone accountable for its security.

---

## Step 4: Identify Threats and Vulnerabilities

For each asset, think about what could go wrong. A useful framework is to think in terms of:
- **Threats** — who or what could cause harm? (e.g., hackers, malicious insiders, accidents, equipment failure, natural events)
- **Vulnerabilities** — what weaknesses make that threat more likely to succeed? (e.g., no MFA, unpatched software, poor access controls, untrained staff)

Common threats to consider for most businesses:
- Ransomware or malware
- Phishing attacks leading to credential theft
- Unauthorised access (external or internal)
- Accidental data exposure (wrong recipient, misconfigured permissions)
- Data breach via a vendor or supplier
- System failure or outage
- Physical theft of devices

---

## Step 5: Score Each Risk

For each combination of asset + threat, assign:

**Likelihood score (1–5):**
- 1 = Very unlikely (once every several years)
- 3 = Possible (could happen once a year)
- 5 = Almost certain (happens regularly)

**Impact score (1–5):**
- 1 = Negligible (no meaningful harm)
- 3 = Significant (disruption, potential regulatory issues)
- 5 = Catastrophic (major breach, business-threatening)

**Risk Score = Likelihood × Impact** (range: 1–25)

You can also factor in your existing controls when scoring. If you already have strong controls in place, your likelihood score might be lower.

---

## Step 6: Evaluate and Prioritise

Compare each risk score against your acceptance threshold. A common approach:

- **20–25** — Critical: must be treated immediately
- **10–19** — High/Medium: should be treated
- **5–9** — Low: consider treatment or accept with documentation
- **1–4** — Very low: typically accepted

The key is consistency — apply your criteria the same way to all risks.

---

## Step 7: Decide How to Treat Each Risk

For risks above your acceptance threshold, you have four options:

1. **Mitigate** — Implement controls to reduce the likelihood or impact. This is the most common choice.
2. **Accept** — Decide the risk is tolerable. Must be documented and formally accepted by a risk owner.
3. **Transfer** — Shift the risk to someone else (cyber insurance, contractual clauses with vendors).
4. **Avoid** — Stop doing the activity that creates the risk.

---

## What Documentation Do You Need?

ISO 27001 requires you to keep records of:

**Risk Assessment Results** — Your completed risk register. This is a document (spreadsheet or tool) showing every risk you identified, your scores, and your treatment decisions.

**Risk Treatment Plan** — A document detailing the specific actions you will take to treat risks above your threshold, who is responsible, and when they will be completed.

**Statement of Applicability (SoA)** — A required document listing all ISO 27001 Annex A controls, indicating which ones apply to you and why, and their implementation status. Your SoA should be directly linked to your risk assessment — the controls you implement should address the risks you identified.

---

## The Risk Register

Your risk register should be a structured document (spreadsheet works fine) with columns like:

- Risk ID
- Asset
- Threat description
- Vulnerability
- Likelihood score (1–5)
- Impact score (1–5)
- Risk score (L × I)
- Treatment option (Mitigate/Accept/Transfer/Avoid)
- Control(s) to implement
- Risk owner (named individual)
- Target completion date
- Status

---

## Determining Which Risks Need Treatment

Simple rule: **if the risk score exceeds your documented acceptance threshold, it requires treatment**. You must document the treatment decision regardless of which option you choose — even "accept" needs to be formally recorded with the risk owner's sign-off.

Don't leave any risk undocumented. An auditor will look for evidence that you've made a conscious decision about every significant risk.

---

## After the Assessment

Once your risk assessment is complete:

1. Build your Risk Treatment Plan — list every action needed to address high/critical risks
2. Implement the controls
3. Update your Statement of Applicability
4. Set a schedule to review the risk register at least annually (or when significant changes occur)
5. Keep records — auditors will want to see the risk register, treatment plan, and evidence that treatment actions were completed

Risk assessment is not a one-time exercise. ISO 27001 expects it to be a living process that you revisit regularly as your business and threat environment changes.
