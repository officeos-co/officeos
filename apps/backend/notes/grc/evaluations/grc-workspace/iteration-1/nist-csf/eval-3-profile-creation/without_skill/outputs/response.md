# Using NIST CSF Profiles to Assess Cybersecurity Posture
## Creating Current Profile and Target Profile

---

## What Is a CSF Profile?

A NIST Cybersecurity Framework (CSF) Profile is a customized alignment of the framework's functions, categories, and subcategories to your organization's business requirements, risk tolerance, and available resources. Profiles help you:

- Understand your current cybersecurity posture (Current Profile)
- Define where you want to be (Target Profile)
- Identify and prioritize gaps between the two

Think of it as a before-and-after snapshot of your cybersecurity program, with the gap between them driving your improvement roadmap.

---

## The Two Profile Types

### Current Profile
A Current Profile is an assessment of the cybersecurity outcomes your organization is achieving today. It answers the question: "What are we actually doing right now across each area of the framework?"

### Target Profile
A Target Profile represents the cybersecurity outcomes your organization wants to achieve. It answers: "Where do we need to be, based on our business goals, risk tolerance, and regulatory requirements?"

---

## Step-by-Step Process

### Step 1: Understand Your Business Context

Before you can assess anything, you need to understand what you're protecting and why. Gather information on:

**Business Mission and Critical Processes**
- What does your organization do?
- What are the most critical processes that must keep running?
- What are the consequences of a cybersecurity incident on operations, reputation, or finances?

**Assets and Crown Jewels**
- What data, systems, and capabilities are most valuable?
- Customer data? Intellectual property? Financial systems? OT/manufacturing systems?

**Legal and Regulatory Requirements**
- Which regulations apply to your organization? (HIPAA, PCI DSS, SOX, GDPR, state breach notification laws)
- Do contracts with customers or partners impose specific security requirements?
- These obligations create mandatory minimum standards that must be reflected in your Target Profile.

**Risk Appetite**
- How much cybersecurity risk is your organization willing to accept?
- Is leadership risk-averse or willing to accept higher risk in exchange for lower cost?

---

### Step 2: Select a Rating Scale

Choose a consistent way to rate each area. A common approach:

| Rating | Meaning |
|--------|---------|
| Not Implemented | No evidence of this practice |
| Partially Implemented | Some activities exist but are incomplete or inconsistent |
| Largely Implemented | Mostly in place with minor gaps |
| Fully Implemented | Consistently in place, documented, and operating effectively |
| Not Applicable | Does not apply to this organization (document rationale) |

---

### Step 3: Assess and Build the Current Profile

Work through each of the six CSF functions and their subcategories, rating each area honestly based on available evidence.

**How to gather evidence:**
- Interview IT, security, HR, legal, and operations staff
- Review existing security policies and procedures
- Review audit findings and penetration test reports
- Check security tool configurations (firewalls, endpoint protection, SIEM, etc.)
- Review incident history

**Sample Current Profile (partial):**

| CSF Function | Category | Subcategory | Current Rating | Notes |
|-------------|----------|-------------|----------------|-------|
| Govern | Risk Management | Risk tolerance documented | Not Implemented | No formal risk appetite statement |
| Identify | Asset Management | Hardware inventory | Partially Implemented | CMDB exists but incomplete |
| Identify | Asset Management | Software inventory | Not Implemented | No formal process |
| Protect | Access Control | MFA enforced | Partially Implemented | Only on email, not VPN |
| Protect | Awareness Training | Regular training | Partially Implemented | Annual only, no phishing tests |
| Detect | Monitoring | Continuous network monitoring | Partially Implemented | Basic monitoring, no SIEM |
| Respond | Incident Response | IRP documented and tested | Partially Implemented | Plan exists, not tested in 2 years |
| Recover | Recovery Plan | Formal recovery plan | Not Implemented | Ad hoc only |

**Tips for honest assessment:**
- Require evidence for any rating above "Not Implemented" — do not self-certify
- Involve multiple stakeholders to get a complete picture
- Use external assessors if internal objectivity is a concern

---

### Step 4: Define and Build the Target Profile

The Target Profile defines where you need to be. It should be informed by:

**Business Risk Priorities**: Higher-criticality areas need higher target states.

**Regulatory Requirements**: If a regulation requires it, it must be "Fully Implemented" in your target.

**Threat Landscape**: If your industry is frequently targeted by ransomware, detection and response capabilities should have high target states.

**Resources and Feasibility**: Be realistic — a target of "Fully Implemented" across all 100+ subcategories in 12 months is not achievable for most organizations.

**Sample Target Profile (partial):**

| CSF Function | Category | Subcategory | Current | Target | Driver | Priority |
|-------------|----------|-------------|---------|--------|--------|----------|
| Govern | Risk Management | Risk tolerance documented | Not Implemented | Fully Implemented | Cyber insurance | High |
| Identify | Asset Management | Hardware inventory | Partially | Fully Implemented | Security best practice | High |
| Identify | Asset Management | Software inventory | Not Implemented | Largely Implemented | Security best practice | High |
| Protect | Access Control | MFA enforced | Partially | Fully Implemented | Cyber insurance mandate | Critical |
| Protect | Awareness Training | Regular training | Partially | Largely Implemented | — | Medium |
| Detect | Monitoring | Continuous monitoring | Partially | Largely Implemented | Regulatory | High |
| Respond | Incident Response | IRP documented and tested | Partially | Fully Implemented | Regulatory | High |
| Recover | Recovery Plan | Formal recovery plan | Not Implemented | Fully Implemented | Business continuity | High |

---

### Step 5: Conduct the Gap Analysis

Compare your Current Profile to your Target Profile to identify gaps. Calculate the gap for each subcategory:

- **Critical Gap**: Not Implemented → Fully Implemented (requires significant effort)
- **Significant Gap**: Partially Implemented → Fully Implemented
- **Moderate Gap**: Largely Implemented → Fully Implemented
- **No Gap**: Current = Target (no action needed in current cycle)

**Prioritize gaps by:**
1. Regulatory requirement (mandatory compliance)
2. Risk reduction value (which gaps leave you most exposed)
3. Ease of implementation (quick wins build momentum)
4. Cost and resource constraints

---

### Step 6: Build the Improvement Roadmap

Turn the gap analysis into an action plan organized by time horizon:

**30–60 Days (Quick Wins)**
- Low-effort, high-impact items (e.g., enabling MFA universally, updating policies)
- Regulatory mandatory items that are easy to implement

**60–180 Days (Foundation)**
- Complete asset inventory
- Deploy or improve monitoring tools
- Formalize and test incident response plan

**6–12 Months (Strategic)**
- More complex controls requiring significant investment or vendor selection
- Mature your detection and response program
- Build recovery capabilities

---

### Step 7: Document and Maintain Your Profiles

Your profiles are living documents — they should be:

- **Reviewed at least annually**
- **Updated after significant incidents** (what new gaps did the incident reveal?)
- **Refreshed when regulatory requirements change**
- **Shared with leadership** to communicate cybersecurity posture in business terms

Store your profiles alongside your risk register and use them as the basis for security budget conversations with leadership.

---

## Common Mistakes to Avoid

1. **Overstating your current state**: Rating everything as "Fully Implemented" when controls are not consistently applied undermines the value of the exercise.

2. **Setting unachievable targets**: Every gap becomes a required action — be realistic about what your team can accomplish with available resources.

3. **Treating it as a one-time exercise**: Profiles are most valuable when updated regularly as your environment changes.

4. **Not involving the business**: Security profiles that don't reflect business context and priorities will not drive the right investment decisions.

---

## Summary

| Step | Activity | Output |
|------|----------|--------|
| 1 | Understand business context | Context statement |
| 2 | Define rating scale | Scoring rubric |
| 3 | Assess current state | Current Profile |
| 4 | Define target state | Target Profile |
| 5 | Gap analysis | Prioritized gap list |
| 6 | Build roadmap | Improvement action plan |
| 7 | Document and maintain | Living profile documents |

By completing this process, you will have a clear, evidence-based picture of your cybersecurity posture, a defined destination, and a prioritized path to get there — all aligned to the NIST Cybersecurity Framework.
