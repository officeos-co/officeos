# GDPR Compliance Checklist for US-Based SaaS Company with EU Customers

> **Legal Advice Disclaimer**: This guidance is informational and based on the GDPR text and established regulatory guidance. It does not constitute legal advice. For matters involving significant compliance risk, supervisory authority interaction, or complex cross-border scenarios, consult a qualified data protection lawyer or your DPO.

## Overview

As a US-based SaaS company with a European customer, you are now subject to the GDPR by virtue of the **territorial scope** rules under **Article 3(2)**. Even without an EU establishment, GDPR applies because you are offering services to data subjects in the EU ("targeting criterion"). You act as both a **data controller** (for your employee data and your own analytics) and potentially a **data processor** (for your customer's end-user data, depending on contract terms) — see Art. 4(7) and Art. 4(8).

---

## Part 1: Establish Your Legal Foundations

### 1.1 Determine Roles (Art. 4(7–8))
- [ ] Confirm whether you are a **controller**, **processor**, or both for each processing activity
- [ ] For customer account data: you are likely a **processor** acting on behalf of your EU customer (the controller)
- [ ] For employee data: you are the **controller**
- [ ] For usage analytics: you are typically the **controller** — assess whether this data relates to EU individuals

### 1.2 Identify and Document All Processing Activities — Record of Processing Activities (Art. 30)
You must maintain a RoPA covering:
- [ ] Name and contact details of the controller/processor (Art. 30(1)(a))
- [ ] Purposes of each processing activity (Art. 30(1)(b))
- [ ] Categories of data subjects and personal data (Art. 30(1)(c))
- [ ] Categories of recipients (Art. 30(1)(d))
- [ ] Third-country transfers and applicable safeguards (Art. 30(1)(e))
- [ ] Retention periods (Art. 30(1)(f))
- [ ] Description of security measures (Art. 30(1)(g))

**Note**: RoPA is mandatory for organisations processing EU personal data, regardless of EU establishment, when processing is not occasional (Art. 30(5)).

### 1.3 Establish a Lawful Basis for Each Processing Activity (Art. 6(1))
| Processing Activity | Recommended Lawful Basis | Article |
|---------------------|--------------------------|---------|
| Employee data (payroll, HR) | Legal obligation / Contract | Art. 6(1)(b)(c) |
| Customer account data | Contract performance | Art. 6(1)(b) |
| Usage analytics (aggregated) | Legitimate interests (LIA required) | Art. 6(1)(f) |
| Usage analytics (individual tracking) | Consent or Legitimate interests + LIA | Art. 6(1)(a)(f) |
| Marketing to existing customers | Legitimate interests | Art. 6(1)(f) |

- [ ] Document the chosen lawful basis for each activity in your RoPA
- [ ] For legitimate interests: complete a **Legitimate Interests Assessment (LIA)** covering purpose, necessity, and balancing tests (Art. 6(1)(f); Recital 47)

---

## Part 2: Transparency Obligations

### 2.1 Privacy Notices (Art. 13 & 14)
For personal data collected directly from individuals, provide a privacy notice at the point of collection (Art. 13) containing:
- [ ] Identity and contact details of the controller (Art. 13(1)(a))
- [ ] Contact details of any Data Protection Officer if appointed (Art. 13(1)(b))
- [ ] Purposes and lawful basis for each processing activity (Art. 13(1)(c))
- [ ] Legitimate interests pursued (if relied upon) (Art. 13(1)(d))
- [ ] Recipients or categories of recipients (Art. 13(1)(e))
- [ ] Details of international transfers and safeguards (Art. 13(1)(f))
- [ ] Retention periods (Art. 13(2)(a))
- [ ] All data subject rights (Art. 13(2)(b))
- [ ] Right to withdraw consent (Art. 13(2)(c))
- [ ] Right to lodge a complaint with a supervisory authority (Art. 13(2)(d))
- [ ] Whether provision of data is statutory/contractual and consequences of not providing (Art. 13(2)(e))

For data obtained indirectly (e.g., from your EU customer about their end users), Art. 14 notice requirements apply.

- [ ] **Privacy notice must be in plain, intelligible language** accessible to a layperson (Art. 12(1))
- [ ] Create a publicly accessible privacy policy on your website covering EU users

### 2.2 EU Representative (Art. 27)
If you have no EU establishment, you **must designate an EU Representative** in a Member State where your EU data subjects are located — unless processing is occasional and does not involve special category data or large-scale criminal data.
- [ ] Appoint an EU Representative and publicise their contact details
- [ ] Your EU Representative can be contacted by supervisory authorities and data subjects

---

## Part 3: Data Subject Rights (Art. 15–22)

Establish procedures to handle the following rights within **one calendar month** (extendable by two months for complex requests) per Art. 12(3):

- [ ] **Right of access** (Art. 15): Provide a copy of personal data held and supplementary information
- [ ] **Right to rectification** (Art. 16): Correct inaccurate or incomplete data
- [ ] **Right to erasure ("right to be forgotten")** (Art. 17): Delete data when no longer necessary, consent withdrawn, or objection upheld
- [ ] **Right to restriction** (Art. 18): Restrict processing in specified circumstances
- [ ] **Right to data portability** (Art. 20): Provide data in machine-readable format where lawful basis is consent or contract
- [ ] **Right to object** (Art. 21): Honour objections to processing based on legitimate interests or for direct marketing (absolute right for marketing)
- [ ] **Rights related to automated decision-making** (Art. 22): Do not subject individuals to solely automated decisions with significant effects without safeguards

**Implementation checklist**:
- [ ] Create a data subject rights intake process (email, web form)
- [ ] Verify identity of requesters without excessive burden (Art. 12(6))
- [ ] Respond free of charge; may charge for manifestly unfounded/excessive requests (Art. 12(5))
- [ ] Document all requests and responses for accountability (Art. 5(2))

---

## Part 4: Processor and Third-Party Obligations

### 4.1 Data Processing Agreements (Art. 28)
If you receive personal data from your EU customer to process on their behalf, you **must** sign a DPA. Equally, for any sub-processors you engage:
- [ ] Execute a DPA with your EU customer covering all Art. 28(3) mandatory clauses
- [ ] Obtain prior written consent from your customer before engaging sub-processors (Art. 28(2))
- [ ] Execute DPAs with all sub-processors (cloud infrastructure, analytics tools, support platforms)
- [ ] Maintain a list of sub-processors

### 4.2 International Data Transfers (Art. 44–49)
As a US company receiving EU personal data, a transfer mechanism is **mandatory** for every data flow from the EU to the US:
- [ ] **EU-US Data Privacy Framework (DPF)**: Certify your organisation under the DPF (replaces Privacy Shield post-Schrems II) — this provides an adequacy-equivalent basis
- [ ] Alternatively, execute **Standard Contractual Clauses (SCCs)** approved by the European Commission (2021 SCCs)
- [ ] Complete a **Transfer Impact Assessment (TIA)** where SCCs are used, to assess whether US law undermines the protection
- [ ] Document the chosen transfer mechanism in your RoPA and DPAs

---

## Part 5: Security Measures (Art. 32)

Implement appropriate technical and organisational measures:
- [ ] **Encryption** of personal data at rest and in transit (Art. 32(1)(a))
- [ ] **Pseudonymisation** of data where feasible (Art. 32(1)(a))
- [ ] **Access controls**: role-based access, least privilege principle
- [ ] **Ongoing confidentiality, integrity, availability** of processing systems (Art. 32(1)(b))
- [ ] **Incident response plan** and ability to restore data after incidents (Art. 32(1)(c)(d))
- [ ] **Regular testing** of security measures — penetration testing, vulnerability scans (Art. 32(1)(d))
- [ ] **Staff training** on data protection

### 5.1 Privacy by Design and Default (Art. 25)
- [ ] Integrate data protection into system design from the outset
- [ ] Default settings should be the most privacy-protective option (e.g., analytics opt-out by default)

---

## Part 6: Breach Notification (Art. 33–34)

- [ ] Establish a **breach detection and response procedure**
- [ ] Report qualifying breaches to the **lead supervisory authority within 72 hours** of becoming aware (Art. 33(1))
- [ ] Notify affected data subjects "without undue delay" if breach is high risk to their rights and freedoms (Art. 34(1))
- [ ] Maintain an internal **breach register** for all breaches, including those not reported to authorities (Art. 33(5))

---

## Part 7: Data Protection Impact Assessment (Art. 35)

A DPIA is mandatory before commencing processing that is **likely to result in high risk**:
- [ ] Assess whether any processing activity requires a DPIA (e.g., large-scale profiling, systematic monitoring, special category data)
- [ ] Usage analytics involving individual profiling may trigger a DPIA
- [ ] Conduct DPIA with DPO involvement if appointed (Art. 35(2))

---

## Part 8: Data Retention (Art. 5(1)(e))

- [ ] Define retention periods for each data category (employee data, customer data, analytics)
- [ ] Implement automated deletion or anonymisation at end of retention period
- [ ] Apply retention policy to backups as well
- [ ] Document retention schedules in your RoPA

---

## Part 9: Data Protection Officer (Art. 37)

A DPO is mandatory if you:
- Process special category data on a large scale (Art. 9), or
- Carry out large-scale systematic monitoring of individuals

For a SaaS company at early EU stage, a DPO may not yet be mandatory — but appointing one (internal or external) is a strong best-practice signal to regulators and customers.
- [ ] Assess DPO requirement (Art. 37(1))
- [ ] If appointed, register DPO with supervisory authority (Art. 37(7))

---

## Priority Action Plan (Immediate — First 30 Days)

| Priority | Action | Article |
|----------|--------|---------|
| 1 | Appoint EU Representative | Art. 27 |
| 2 | Execute DPA with EU customer | Art. 28 |
| 3 | Implement international transfer mechanism (DPF or SCCs) | Art. 44–46 |
| 4 | Update Privacy Notice (website + product) | Art. 13 |
| 5 | Create RoPA for all three processing activities | Art. 30 |
| 6 | Establish data subject rights intake process | Art. 12–22 |
| 7 | Implement breach detection and 72-hour notification process | Art. 33 |
| 8 | Review and document retention periods | Art. 5(1)(e) |
| 9 | Conduct LIA for analytics processing | Art. 6(1)(f) |
| 10 | Assess DPIA requirement for analytics | Art. 35 |

---

## Specific Notes on Your Three Data Types

### Employee Data
- High-risk area: HR/employment data processing warrants specialist legal advice, particularly as employment law derogations vary significantly across Member States (Art. 88)
- Lawful bases: primarily contract (Art. 6(1)(b)) and legal obligation (Art. 6(1)(c))
- Do not rely on employee consent as it is rarely "freely given" due to power imbalance (Recital 43)

### Customer Account Data
- If you process this on behalf of your EU customer, you are a **processor** — execute a DPA (Art. 28)
- If you also use it for your own analytics, you become a controller for that secondary purpose — requires separate lawful basis and disclosure in your privacy notice

### Usage Analytics
- Identify whether analytics data constitutes personal data (IP addresses, device IDs, and cookies are personal data per Art. 4(1); Recital 30)
- If tracking individual behaviour: consider consent via cookie banner (Art. 7; ePrivacy Directive)
- If using aggregated/anonymised analytics only: GDPR does not apply to truly anonymised data (Recital 26)
