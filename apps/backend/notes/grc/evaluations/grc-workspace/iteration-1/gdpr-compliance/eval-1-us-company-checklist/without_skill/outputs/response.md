# GDPR Compliance Checklist for US-Based SaaS Company with EU Customers

## Overview

Congratulations on your first European customer — but this also means the General Data Protection Regulation (GDPR) now applies to your business, even though you are based in the US. The GDPR has broad territorial reach: it applies to any organisation that offers goods or services to EU residents, regardless of where the organisation is located.

Here is a practical compliance checklist covering your three data types.

---

## Step 1: Understand Your Role

First, determine whether you are a **data controller** or a **data processor** (or both):

- **Data controller**: You decide why and how personal data is processed. This applies to your employee data and your own analytics.
- **Data processor**: You process data on behalf of another controller. If your EU customer sends you their end-user data to manage within your SaaS platform, you are likely a processor for that data.

This distinction matters because controllers and processors have different obligations under GDPR.

---

## Step 2: Establish a Legal Basis for Processing

Every processing activity must have a valid legal basis. The main options are:

| Data Type | Likely Legal Basis |
|-----------|-------------------|
| Employee data | Contract performance + legal obligation |
| Customer account data | Contract performance (needed to provide the service) |
| Usage analytics | Legitimate interests (for aggregate/internal use) or Consent (for individual tracking) |

Document your chosen legal basis for each activity. If you rely on **legitimate interests**, you must conduct a balancing test showing your interests do not override individuals' rights.

---

## Step 3: Create or Update Your Privacy Notice

Your privacy policy/notice must tell users:
- Who you are and how to contact you
- What data you collect and why
- The legal basis for each processing activity
- How long you keep data
- Who you share data with (including third-party tools)
- What rights users have and how to exercise them
- How to file a complaint with a European data protection authority

The notice must be written in clear, plain language — not legal jargon.

---

## Step 4: Handle Data Subject Rights

EU residents have significant rights over their personal data. You must be able to respond to:

- **Right to access**: Provide a copy of all personal data you hold about them, typically within 30 days
- **Right to correction**: Fix inaccurate data
- **Right to deletion ("right to be forgotten")**: Delete data in certain circumstances
- **Right to restriction**: Pause processing in some situations
- **Right to data portability**: Provide their data in a usable format
- **Right to object**: Stop certain types of processing, especially direct marketing

**Action items**:
- Set up an email address or web form for privacy requests
- Create an internal procedure with clear timelines (30-day response window)
- Document all requests and your responses

---

## Step 5: Execute a Data Processing Agreement (DPA) with Your EU Customer

Since your EU customer is sharing their users' data with you (to run on your platform), you need a signed **Data Processing Agreement** with them. This is legally required and most enterprise EU customers will ask for this before signing a commercial contract. The DPA should cover:

- The scope and purpose of data processing
- The types of data and categories of data subjects
- Your security obligations
- Sub-processor rules (who else can you share data with, and under what conditions)
- Data breach notification procedures
- Data deletion/return obligations at contract end

---

## Step 6: Address International Data Transfers

Transferring EU personal data to the US requires a legal mechanism. The main options are:

- **EU-US Data Privacy Framework (DPF)**: Self-certify your company through the US Department of Commerce. This is the simplest route.
- **Standard Contractual Clauses (SCCs)**: Contractual clauses approved by the EU that can be incorporated into your agreements with EU customers.

You likely need one of these in place before your EU customer can lawfully send you their users' data.

---

## Step 7: Implement Security Measures

GDPR requires "appropriate technical and organisational measures" to protect personal data. Practically, this means:

- Encryption of data in transit (TLS) and at rest
- Access controls and role-based permissions
- Multi-factor authentication for systems handling personal data
- Regular security testing
- Staff training on data privacy and security
- A documented incident response plan

---

## Step 8: Set Up Breach Notification Procedures

If you experience a data breach involving EU personal data, you must:

- Notify the relevant EU data protection authority within **72 hours** of discovering the breach
- Notify affected individuals if the breach poses a high risk to their rights and freedoms
- Keep an internal log of all breaches

Build this into your incident response runbook now, before a breach happens.

---

## Step 9: Appoint an EU Representative

Because you have no EU office, you must designate a **representative in the EU** — an individual or company in an EU Member State who can be contacted by regulators and data subjects. Several service providers offer this service.

---

## Step 10: Review Data Retention

- Define how long you keep each type of data (employee records, customer data, analytics logs)
- Delete or anonymise data when no longer needed
- Apply retention rules to backups as well as live systems

---

## Step 11: Consider Whether You Need a Data Protection Officer (DPO)

A DPO may be required if you process large volumes of EU personal data or engage in systematic profiling. For a company at your stage, it may not be mandatory yet, but appointing an external DPO or privacy counsel is a best practice that many EU enterprise customers will expect.

---

## Checklist Summary by Data Type

### Employee Data
- [ ] Privacy notice provided to EU employees at time of hire
- [ ] Legal basis documented (contract + legal obligation)
- [ ] Retention policy set (typically aligned with employment law)
- [ ] HR data access restricted to authorised personnel

### Customer Account Data
- [ ] DPA signed with EU customer
- [ ] International transfer mechanism in place (DPF or SCCs)
- [ ] Privacy notice/terms updated to reflect data use
- [ ] Deletion procedure in place at contract end

### Usage Analytics
- [ ] Determine if analytics data is personal data (IP addresses, device IDs, user IDs are personal data)
- [ ] If tracking individuals: cookie consent banner required for EU visitors
- [ ] Legal basis documented
- [ ] Consider anonymising analytics data to reduce compliance burden

---

## Immediate Priorities (Do These First)

1. Sign DPA with your EU customer
2. Set up international transfer mechanism (DPF certification is fastest)
3. Update your privacy policy
4. Appoint EU Representative
5. Create a data subject rights request process
6. Document your data retention periods

Getting these six items done will put you in a much stronger compliance posture for your initial EU customer relationship.
