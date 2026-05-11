# Closing the "Insufficient Evidence of Management Review" Major Nonconformity

## Understanding the Nonconformity

**Clause:** ISO 27001:2022 Clause 9.3 — Management Review
**Classification:** Major Nonconformity

A major nonconformity (NC) means your organisation has failed to satisfy a mandatory requirement of the standard. A major NC typically blocks certification or requires resolution within a defined period (usually 30–90 days) before a re-audit. This is one of the most commonly raised major NCs, and one of the most straightforward to close — if you take the right approach.

---

## What Clause 9.3 Actually Requires

ISO 27001:2022 splits management review into three sub-clauses:

### Clause 9.3.1 — General
Top management shall review the organisation's ISMS at **planned intervals** to ensure its continuing suitability, adequacy, and effectiveness.

**Key requirements:**
- Reviews must be conducted by **top management** (CEO, CTO, board — not just the CISO alone)
- They must occur at **planned intervals** (typically at least annually; many organisations do them twice a year)
- The review must assess **suitability** (is the ISMS still right for our context?), **adequacy** (do we have enough resources?), and **effectiveness** (is it working?)

### Clause 9.3.2 — Management Review Inputs
The review must consider a defined list of inputs. All of the following must be addressed:

| Input | Description |
|-------|-------------|
| **(a)** Status of actions from previous reviews | Were action items from the last review completed? |
| **(b)** Changes in external/internal issues relevant to ISMS | New regulations, business changes, new threats |
| **(c)** Changes in needs/expectations of interested parties | Customer requirements, regulatory changes |
| **(d)** Feedback on IS performance | See breakdown below |
| **(e)** Feedback from interested parties | Customer complaints, supplier issues |
| **(f)** Results of risk assessment and risk treatment | Risk register updates, treatment status |
| **(g)** Performance of IS objectives | Are we hitting our security metrics? |
| **(h)** Opportunities for continual improvement | What can we do better? |

**Input (d) — Feedback on IS performance — must include:**
- Nonconformities and corrective actions
- Monitoring and measurement results
- Audit results (internal + external)
- Fulfillment of IS objectives
- Feedback from interested parties (customers, regulators)

### Clause 9.3.3 — Management Review Results/Outputs
The review must produce documented outputs including:
- **Decisions and actions** related to continual improvement opportunities
- **Resource needs** — any changes needed to resources supporting the ISMS
- **Changes to the ISMS** if required (scope, policies, controls)

---

## What "Insufficient Evidence" Typically Means

When an auditor raises this NC, they have found one or more of the following:

| Finding Type | What the Auditor Saw |
|-------------|---------------------|
| No meeting minutes | Management review was claimed to happen verbally but no documentation exists |
| Incomplete inputs | Minutes exist but do not cover all required inputs (e.g., no discussion of risk assessment results, or no IS objectives review) |
| Minutes are too vague | e.g., "security was discussed" — no specifics, no evidence of actual analysis |
| Wrong attendees | Minutes show only the CISO attended — no top management participation |
| No outputs documented | No action items, decisions, or resource commitments recorded |
| Review never happened | ISMS has been running for over a year with no management review at all |
| Only one review in a multi-year period | Standard requires planned intervals; one review in 3 years is insufficient |

---

## What Evidence You Should Have — Complete Checklist

For each management review, you must have:

### Meeting Evidence
- [ ] **Meeting invitation/calendar record** — shows the meeting was scheduled in advance (demonstrates "planned intervals")
- [ ] **Attendance list** — must include top management (named individuals, their roles)
- [ ] **Meeting agenda** — covering all Clause 9.3.2 input items
- [ ] **Meeting minutes** — formally documented, signed off or approved by the chair (top management representative)
- [ ] **Action items log** — specific actions, named owners, deadlines

### Content Coverage (Minutes Must Address All of the Following)
- [ ] Status of actions from previous review (or note this is the first review)
- [ ] Changes to internal/external context (business changes, regulatory changes, new threats)
- [ ] Changes to stakeholder requirements
- [ ] Nonconformities raised since last review and corrective action status
- [ ] Results of internal audits (summary of findings)
- [ ] Results of monitoring and measurement (security metrics/KPIs)
- [ ] Status of information security objectives — are targets being met?
- [ ] Risk assessment results summary — any new critical risks?
- [ ] Risk treatment status — are treatment actions on track?
- [ ] Feedback from interested parties (customers, regulators, etc.)
- [ ] Opportunities for continual improvement identified
- [ ] Decisions made on resources for the ISMS
- [ ] Decisions on any ISMS changes required
- [ ] Any other relevant security matters

### Supporting Evidence (Referenced in Minutes)
- [ ] Metrics/KPI dashboard or report presented at the meeting
- [ ] Internal audit report(s) summary
- [ ] Updated risk register or risk summary
- [ ] IS objectives tracking sheet
- [ ] Corrective action log/nonconformity register

---

## How to Write the Corrective Action Plan (CAP)

A corrective action plan for a major NC must follow the standard **root cause → correction → corrective action → verification** structure. Here is a complete template:

---

### Corrective Action Plan — Major Nonconformity

**Reference:** NC-[YEAR]-[NUMBER]
**Nonconformity:** Insufficient evidence of management review
**Clause:** ISO 27001:2022 Clause 9.3
**Classification:** Major
**Date Raised:** [DATE]
**Audit Finding (verbatim):** "[Copy exact wording from audit report]"
**CAP Owner:** [CISO / Compliance Manager name]
**Target Closure Date:** [DATE — within CB's stipulated period]

---

#### Section 1: Immediate Correction (Fix the Symptom)

*What have you done right now to address the immediate deficiency?*

| Action | Responsible | Completion Date | Evidence |
|--------|-------------|-----------------|---------|
| Scheduled an emergency management review meeting for [DATE] | CISO | [DATE] | Meeting invite / calendar record |
| Prepared a comprehensive management review agenda covering all Clause 9.3.2 inputs | CISO | [DATE] | Agenda document |
| Conducted management review meeting with [CEO], [CTO], [CISO] in attendance | CEO | [DATE] | Signed meeting minutes |
| Documented meeting minutes covering all required inputs and outputs | CISO | [DATE] | Management review minutes v1.0 |
| Created action item tracker for management review outputs | CISO | [DATE] | Action log |

---

#### Section 2: Root Cause Analysis

*Why did this nonconformity occur?*

**Identified Root Causes:**

1. **Process gap:** No formal management review procedure was documented. There was no defined schedule, agenda template, or minute template to ensure consistency and coverage.

2. **Awareness gap:** Top management was not aware that their active participation and formal documented review of the ISMS was a mandatory ISO 27001 requirement. The CISO was managing the ISMS without adequate executive engagement.

3. **Document management gap:** Even where discussions occurred informally (e.g., during board meetings), they were not captured in a format that constitutes documented evidence for the purposes of Clause 9.3.

---

#### Section 3: Corrective Actions (Fix the Root Cause)

*Systemic changes to prevent recurrence:*

| Action | Responsible | Target Date | Evidence |
|--------|-------------|-------------|---------|
| Draft and approve a formal Management Review Procedure documenting: schedule (twice yearly), agenda template covering all 9.3.2 inputs, minute template, attendee requirements, action tracking process | CISO | [DATE] | Approved procedure document |
| Create a standard Management Review Pack template (agenda + minute template with all 9.3.2 inputs pre-populated as agenda items) | CISO | [DATE] | Template document |
| Schedule management reviews on standing executive calendar for next 12 months (at minimum: [DATE] and [DATE]) | EA to CEO | [DATE] | Calendar records |
| Brief executive team on Clause 9.3 requirements and their mandatory participation | CISO | [DATE] | Briefing deck / attendance record |
| Add management review schedule and completion status to ISMS KPI dashboard | CISO | [DATE] | Updated dashboard |
| Conduct second management review per schedule to demonstrate planned intervals | CEO | [DATE] | Meeting minutes |

---

#### Section 4: Effectiveness Verification

*How will you verify the corrective actions worked?*

| Verification Activity | Responsible | Date |
|----------------------|-------------|------|
| Internal audit of Clause 9.3 compliance — verify procedure exists, two reviews conducted, evidence complete | Internal Auditor | [DATE — before closure audit] |
| Review management review minutes for completeness against 9.3.2 checklist | CISO | After each review |
| Confirm management review schedule integrated into ISMS governance calendar | CISO | [DATE] |
| Present evidence package to certification body at closure audit | CISO | [DATE] |

---

#### Section 5: Evidence Package for Closure Audit

Compile the following as your evidence package for the CB:

1. Management Review Procedure (approved, version controlled)
2. Management Review Pack template (agenda + minute template)
3. Evidence of management review conducted immediately (minutes, attendance, action log)
4. Evidence of second review (if time permits before closure audit) OR scheduled date confirmed
5. Executive briefing records showing top management awareness
6. Calendar records showing future reviews scheduled
7. Internal audit report confirming Clause 9.3 compliance

---

**Approved by:** [CISO Name]
**Executive Sponsor:** [CEO Name]
**Date:** [DATE]
**Next Review:** [CLOSURE AUDIT DATE]

---

## Quick Reference — Management Review Meeting Agenda Template

```
MANAGEMENT REVIEW AGENDA — ISO 27001:2022 Clause 9.3
Date: [DATE] | Attendees: [CEO, CTO, CISO, ...]

1. Status of actions from previous management review (9.3.2a)
2. Changes to internal/external context relevant to ISMS (9.3.2b)
3. Changes to interested party requirements (9.3.2c)
4. IS performance feedback (9.3.2d):
   a. Nonconformities and corrective actions
   b. Monitoring and measurement results / KPIs
   c. Internal and external audit results
   d. Achievement of IS objectives
   e. Feedback from customers/stakeholders
5. Risk assessment results and treatment status (9.3.2f)
6. IS objectives performance (9.3.2g)
7. Opportunities for continual improvement (9.3.2h)
8. Decisions and actions required (9.3.3):
   a. Improvement opportunities
   b. Resource needs
   c. ISMS changes required
9. Any other business
```

---

## Timeline to Closure

| Week | Action |
|------|--------|
| Week 1 | Conduct emergency management review; produce minutes |
| Week 1–2 | Draft and approve Management Review Procedure and templates |
| Week 2 | Brief executive team; schedule future reviews |
| Week 3 | Conduct internal audit of Clause 9.3 compliance |
| Week 4 | Compile evidence package; submit to CB for closure review |

---

*Reference: ISO/IEC 27001:2022 Clause 9.3 | Nonconformity Management: Clause 10.2 | Date: 2026-04-18*
