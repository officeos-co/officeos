# GDPR Data Breach Response: 72-Hour Notification Protocol

> **Legal Advice Disclaimer**: This guidance is informational and based on the GDPR text and EDPB Guidelines 01/2021 on Examples of Personal Data Breach Notification. It does not constitute legal advice. A data breach involving 500 individuals and financial data (account numbers) carries significant regulatory and reputational risk — consult a qualified data protection lawyer or your DPO immediately.

---

## Situation Assessment

**Data involved**: Names, email addresses, and **account numbers** of 500 customers  
**Incident**: Accidental disclosure to wrong email recipient (external)  
**Classification**: This is a **Personal Data Breach** under **Art. 4(12) GDPR** — specifically an accidental disclosure to an unauthorised recipient, constituting a **confidentiality breach**

**Initial risk assessment**: This is a **medium-to-high risk** breach. Account numbers combined with names and emails create risk of financial fraud, phishing, and identity theft. The EDPB has categorised financial data disclosures as typically requiring supervisory authority notification and potentially data subject notification.

---

## The Legal Framework

### Art. 4(12) — Definition of Personal Data Breach
A "personal data breach" means a breach of security leading to accidental or unlawful destruction, loss, alteration, **unauthorised disclosure of**, or **access to**, personal data transmitted, stored or otherwise processed.

**This incident qualifies**: Accidental email to wrong recipient = unauthorised disclosure.

### Art. 33 — Notification to Supervisory Authority
### Art. 34 — Communication to Data Subjects
### Art. 33(5) — Internal Breach Register

---

## Hour-by-Hour Action Plan for the Next 72 Hours

### IMMEDIATELY (Hour 0–2): Contain and Assess

**Step 1: Contain the breach**
- [ ] Attempt to **recall or delete** the email if your mail system allows it (many enterprise email systems support recall)
- [ ] Contact the **recipient immediately** — call them if possible. Explain the error and request:
  - Confirmation that they have deleted the email and attachments
  - Written confirmation (email reply) of deletion
  - Not to forward, share, or use the data
- [ ] **Document** the time you became aware of the breach (this starts the 72-hour clock under Art. 33(1))
- [ ] Preserve evidence: forward the original sent email, screenshot the recipient address, retain logs

**Step 2: Activate your breach response team**
- [ ] Notify your Data Protection Officer (if appointed — Art. 37) or privacy lead
- [ ] Notify your legal counsel
- [ ] Notify senior management / CEO
- [ ] Open a breach incident record in your breach register (Art. 33(5))

**Step 3: Secure the source**
- [ ] Identify how the spreadsheet was created and stored
- [ ] Confirm no other copies were sent or are at risk
- [ ] Audit email logs to confirm extent of the disclosure (one email, one recipient, or more?)

---

### Hours 2–12: Risk Assessment

Conduct a formal risk assessment using the EDPB's four-factor framework (EDPB Guidelines 01/2021):

| Factor | Assessment for This Incident |
|--------|------------------------------|
| **Type of breach** | Confidentiality breach — disclosure to wrong recipient |
| **Nature, sensitivity, and volume of data** | Names + emails + **account numbers** × 500 individuals — financial data is sensitive; high risk of phishing/fraud |
| **Ease of identification** | Data is directly identifiable (names, emails) |
| **Severity of consequences** | Account numbers → risk of financial harm; emails → risk of targeted phishing |
| **Special characteristics of data subjects** | General consumer population — no additional vulnerability factors known |
| **Number of data subjects** | 500 — significant volume |
| **Special characteristics of controller** | SaaS company — handling customer financial data |

**Likely risk classification**: **High risk** to data subjects' rights and freedoms, given:
- Account numbers can facilitate financial fraud
- Emails enable targeted phishing using the associated names
- You do not yet know whether the recipient has seen/used the data

**Decision matrix**:
- Notify supervisory authority: **Yes** — breach involves personal data (Art. 33(1))
- Notify data subjects: **Likely yes** — financial data disclosure meets the "high risk" threshold (Art. 34(1))

---

### Hours 12–48: Prepare Notifications

#### A. Supervisory Authority Notification (Art. 33) — Deadline: 72 Hours from Awareness

**Which authority to notify?**
- If you have an EU establishment: your **lead supervisory authority** (the authority in the Member State of your main establishment — Art. 55–56)
- If you are a US company without EU establishment: the authority in the Member State(s) where affected data subjects are located, or where the controller's EU Representative is based

**What the notification must contain (Art. 33(3))**:

| Element | What to Include |
|---------|----------------|
| **(a) Nature of the breach** | Describe the incident: employee sent spreadsheet to wrong email address on [DATE] at [TIME]. One external recipient. Data included names, emails, and account numbers of 500 customers |
| **(b) DPO/contact details** | Name and contact details of your DPO or privacy contact |
| **(c) Likely consequences** | Risk of financial fraud using account numbers; risk of targeted phishing; risk of reputational harm to data subjects |
| **(d) Measures taken** | Immediate recall attempt; contact with recipient requesting deletion; deletion confirmation obtained/pending; breach investigation underway; data subjects to be notified |

**Important procedural rules**:
- If you cannot provide all information within 72 hours, you may **notify in phases** (Art. 33(4)): submit what you know now, and follow up with additional information as your investigation progresses
- The 72-hour clock runs from when **you** (your organisation) became aware — not from when the employee noticed. If IT or a manager was informed and did not escalate, consider whether the clock has already been running
- **Missing the 72-hour window without good reason is itself a violation** and will be considered an aggravating factor by the supervisory authority

**Where to submit**: Most EU supervisory authorities have online breach notification portals. Examples:
- Ireland (DPC): Online reporting form at dataprotection.ie
- Germany (multiple state DPAs): depends on your EU Representative's location
- France (CNIL): notifications.cnil.fr
- UK (ICO): ico.org.uk/make-a-complaint (if UK data subjects involved)

---

#### B. Internal Breach Register Entry (Art. 33(5))

Regardless of whether you notify the supervisory authority (you must notify all breaches that are not "unlikely to result in a risk to the rights and freedoms of natural persons"), you must maintain an **internal breach register** containing:
- [ ] Nature of the breach
- [ ] Data categories and approximate number of records and individuals affected
- [ ] Effects and consequences of the breach
- [ ] Remedial action taken
- [ ] Assessment of risk level
- [ ] Notification decisions and their justification

This register must be maintained **even for minor breaches** that do not require supervisory authority notification.

---

### Hours 48–72: Data Subject Notification (Art. 34)

#### When Must You Notify Data Subjects?
You must notify affected individuals "**without undue delay**" when the breach is **likely to result in a high risk to the rights and freedoms of natural persons** (Art. 34(1)).

Given account numbers + names + emails for 500 individuals → **notification is very likely required**.

**Exceptions to individual notification (Art. 34(3))**:
- You have implemented technical measures that make data unintelligible (e.g., encryption) — **not applicable here**; the spreadsheet was in plain text
- You have subsequently taken measures that ensure high risk is no longer likely to materialise — applicable **only** if you obtain written confirmation of deletion from the recipient AND have high confidence the data was not viewed/copied
- Notification would involve disproportionate effort → use a public communication instead (Art. 34(3)(c)) — not applicable for 500 individuals where you have their email addresses

#### What to Tell Data Subjects (Art. 34(2))
The communication must include (in clear, plain language per Art. 12(1)):
- [ ] **Nature of the breach**: An employee accidentally sent a spreadsheet containing your data to the wrong email recipient
- [ ] **Contact details** of your DPO or privacy contact (Art. 34(2)(a))
- [ ] **Likely consequences** of the breach: potential risk of phishing, fraud, or misuse of account information (Art. 34(2)(b))
- [ ] **Measures taken** to address the breach and steps you recommend they take (Art. 34(2)(c)):
  - Monitor their accounts for unusual activity
  - Be alert to phishing emails using their name
  - Contact you if they notice anything suspicious
  - Consider whether account number changes are warranted

**How to notify**: Given you have their email addresses, direct email notification is appropriate.

**Tone**: Clear, honest, non-legalese. Do not minimise the incident. Data subjects are entitled to accurate information to protect themselves.

---

## Post-72-Hour Actions

### Week 1–2
- [ ] Complete your investigation: obtain written deletion confirmation from the recipient
- [ ] If confirmation not obtained: escalate risk assessment (assume data was retained)
- [ ] Follow up with supervisory authority if you filed a partial notification
- [ ] Review and update your email handling procedures
- [ ] Consider whether a **Disciplinary Process** for the employee is appropriate (note: employee data handling errors do not automatically constitute gross misconduct — assess proportionately)

### Within 30 Days
- [ ] Root cause analysis: why did this happen? (wrong recipient auto-complete? lack of double-check procedure?)
- [ ] Implement corrective controls:
  - Mandatory DLP (Data Loss Prevention) scanning for outbound emails containing personal data
  - Confirmation step for bulk/sensitive email sends
  - Staff training reinforcement
  - Consider whether bulk customer data should ever be in a spreadsheet format accessible by email
- [ ] Update your breach register with final details
- [ ] Document lessons learned

---

## Risk of Not Notifying

Failure to notify the supervisory authority within 72 hours without good reason:
- Violation of Art. 33(1) → fines under **Art. 83(4)**: up to **€10 million or 2% of global annual turnover**
- Supervisory authorities treat late/non-notification as an aggravating factor in fine calculations
- Cooperation and timely notification are both treated as **mitigating factors** (Art. 83(2)(f))

Failure to notify data subjects when required:
- Violation of Art. 34(1) → same fine tier under Art. 83(4)
- Additional risk: if data subjects suffer financial harm due to the breach and were not warned, individual compensation claims under Art. 82 GDPR become more likely

---

## Practical Summary — The 72-Hour Checklist

| Time | Action | Article |
|------|--------|---------|
| Hour 0 | Log time of awareness; open breach record | Art. 33(5) |
| Hour 0–2 | Attempt email recall; contact recipient; escalate internally | Art. 33(1) |
| Hour 2–12 | Conduct risk assessment using EDPB 4-factor framework | EDPB Guidelines 01/2021 |
| Hour 12–48 | Prepare supervisory authority notification | Art. 33(3) |
| Hour 48–72 | File supervisory authority notification | Art. 33(1) |
| Hour 48–72 | Prepare and send data subject notifications | Art. 34(1)(2) |
| Post-72h | Follow up with authority; obtain deletion confirmation; root cause | Art. 33(4) |
| Within 30 days | Implement controls; complete breach register; staff training | Art. 5(2); Art. 32 |
