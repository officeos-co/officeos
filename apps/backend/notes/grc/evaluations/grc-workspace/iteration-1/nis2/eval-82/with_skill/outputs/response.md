# NIS2 Directive Compliance Assessment
## B2B Cloud-Based HR SaaS — 75 Employees, €15M Annual Revenue

---

## Part 1: Are You Directly Subject to NIS2?

**Short answer: Yes — you are very likely subject to NIS2 as an Important Entity.**

### Size Threshold Analysis (Article 3)

NIS2 applies to **medium-sized and larger entities** in scope sectors. The thresholds are:
- **≥ 50 employees**, OR
- **≥ €10 million annual turnover**

Your company meets **both** criteria (75 employees and €15M revenue), so the size threshold is satisfied.

### Sector Classification Analysis

The critical question is which NIS2 sector you fall into. As a B2B cloud-based HR software provider, there are two plausible classifications:

**Most likely: Annex II — Digital Providers**
NIS2 Annex II covers "digital providers," which includes providers of online marketplaces, online search engines, and cloud computing services. B2B SaaS providers delivering cloud-based services to business customers are generally captured under this category. You provide cloud-hosted software on a subscription/service basis to business customers — this squarely fits the "digital provider" framing under Annex II.

**Possible: Annex I — ICT Service Management (B2B)**
If your HR software integrates deeply into your customers' IT infrastructure or you manage ICT services on behalf of clients, you may fall under Annex I (ICT service management, B2B), which would classify you as an **Essential Entity** with more stringent supervisory obligations. This is the less common classification for a standalone HR SaaS, but worth reviewing based on the nature of your service delivery.

**Conclusion for Planning Purposes:**
Treat yourself as an **Important Entity (IE)** under Annex II — Digital Providers. This is the most conservative and likely correct classification for a B2B HR SaaS at your size. You should verify this with a qualified legal advisor in the EU Member State(s) where you are established, as national transposition laws (effective from 17 October 2024) may refine the classification.

### Why Your Customer's Request Makes Sense

Even if there were ambiguity about direct applicability, your customers who *are* NIS2 entities have obligations under **Article 21(3) and Article 26** to address supply chain security — which means they must flow security requirements down to suppliers like you. Your customer is effectively telling you that your security posture is part of their own NIS2 compliance.

### Supervisory Regime as an Important Entity

As an IE:
- Supervision is **reactive (ex-post)** under Article 33 — you will not face routine proactive audits, but competent authorities can investigate upon evidence of non-compliance or following a significant incident.
- **Maximum penalty exposure: €7,000,000 or 1.4% of global annual turnover** (whichever is higher). At €15M revenue, the turnover-based cap is approximately **€210,000**, but the absolute cap of €7M would apply if the percentage exceeds it — meaning real penalty exposure is up to **€7,000,000**.

---

## Part 2: Article 21 — What You Need to Do

Article 21 requires you to implement **appropriate and proportionate technical, operational, and organisational measures** across 10 defined areas. The proportionality principle means controls must be scaled to your size, risk exposure, and the cost of implementation — you are not expected to implement enterprise-scale controls identical to a major bank, but you must demonstrably address all 10 measures.

Below is a practical breakdown of what each measure requires for a B2B HR SaaS at your scale.

---

### Measure 1 — Risk Analysis & Information Security Policies

**What you must do:**
- Adopt a formal risk management methodology. ISO 27005 or NIST RMF are well-suited; a simplified approach acceptable for a company of your size could follow the ISO 27001 risk assessment framework.
- Build and maintain an **asset inventory** covering all systems, cloud infrastructure, databases, and data assets used to deliver your HR software.
- Define **risk acceptance criteria** aligned to the sensitivity of HR data (which is likely to include personal data, employment records, salary information, and potentially special category data under GDPR).
- Conduct risk assessments **at least annually** and after significant changes (new product features, infrastructure migrations, major incidents).
- Have the **management body formally approve** your information security risk management policy (this links to Art. 20 governance obligations).

**Priority:** High. This is the foundation of all other measures. Start here.

---

### Measure 2 — Incident Handling

**What you must do:**
- Produce a written **Incident Response Plan (IRP)** covering detection, analysis, containment, eradication, and recovery for all plausible incident scenarios (ransomware, data breach, DDoS, insider threat, cloud provider outage).
- Define internal thresholds for what constitutes a **"significant incident"** under NIS2 Art. 23(3): substantial service disruption, financial loss to your entity, or material damage to your B2B customers.
- Establish documented **24h/72h/1-month reporting workflows** with pre-drafted templates for notifying the national CSIRT or competent authority in your Member State:
  - **24 hours:** Early warning — was the incident (suspected to be) malicious? Could it have cross-border impact?
  - **72 hours:** Incident notification — initial severity assessment, impact scope, indicators of compromise.
  - **1 month:** Final report — detailed description, threat type, root cause, mitigations applied/ongoing, cross-border impact.
- Conduct post-incident reviews and feed lessons learned back into your risk register.
- Test the IRP **at least annually** via tabletop exercises.

**Priority:** High. HR software holds sensitive employee data; a breach affecting multiple B2B customers could trigger significant incident thresholds rapidly.

---

### Measure 3 — Business Continuity, Backup & Disaster Recovery

**What you must do:**
- Define **Recovery Time Objectives (RTO)** and **Recovery Point Objectives (RPO)** for your core HR platform. For SaaS, customers will expect short RTOs (hours, not days).
- Implement **automated, encrypted, offsite backups** and test restoration **at least quarterly** — not just backup creation, but verified restoration.
- Maintain documented **DR runbooks** and infrastructure failover playbooks for your cloud environment.
- Establish a **crisis management structure** with clear roles, decision authorities, and escalation paths for executives and the management body.
- Test BCP/DR **at least annually**, involving senior leadership in crisis exercises.

**Priority:** High. SaaS service availability is a contractual and regulatory obligation. This measure directly protects against the "substantial disruption" threshold for significant incidents.

---

### Measure 4 — Supply Chain Security

**What you must do:**
- Maintain a **register of critical ICT suppliers** (cloud infrastructure providers, SaaS sub-processors, development tool vendors, security vendors).
- Perform **pre-onboarding security assessments** — at minimum, request security questionnaires, review certifications (ISO 27001, SOC 2), and assess breach history for critical vendors.
- Include **security requirements in contracts**: right-to-audit clauses, incident notification SLAs (require vendors to notify you within 24-48 hours of incidents affecting your data or systems), and minimum security baselines.
- Incorporate **supply chain risk into your annual risk assessment**.
- Monitor ENISA and national authority advisories on high-risk ICT vendors.

**Note:** You are also your customers' supply chain. Your own NIS2 compliance (including the policies and certifications you can demonstrate) is what your customer is asking about. A SOC 2 Type II report or ISO 27001 certificate significantly simplifies evidencing your security posture to customers and competent authorities.

**Priority:** Medium-High. Critical for maintaining customer contracts and demonstrating NIS2 readiness to your enterprise customers.

---

### Measure 5 — Secure Acquisition, Development & Maintenance

**What you must do:**
- Apply a **Secure SDLC**: threat modelling for new features, mandatory code reviews, SAST/DAST tooling integrated into your CI/CD pipeline, and at least annual penetration testing of your application and infrastructure.
- Establish a **vulnerability management programme** with defined patching SLAs by severity:
  - Critical (CVSS ≥ 9.0): patch or mitigate within 7 days
  - High (CVSS 7.0–8.9): within 30 days
  - Medium: within 90 days
- Publish a **coordinated vulnerability disclosure (CVD) policy** — a simple security.txt file and disclosure process aligned to ENISA CVD guidelines.
- Track relevant CVEs against your technology stack using threat intelligence or vulnerability scanning tools.

**Priority:** Medium-High. As a software provider, this measure is core to what you deliver and directly impacts your customers' security.

---

### Measure 6 — Effectiveness Assessment

**What you must do:**
- Define **KPIs/KRIs** for each Art. 21 measure. Examples:
  - Mean Time to Detect (MTTD) and Mean Time to Respond (MTTR) for security incidents
  - Patch compliance rate (% of critical/high vulnerabilities patched within SLA)
  - Security training completion rate
  - Backup restoration test success rate
- Conduct **internal audits or management reviews** at least annually covering all 10 measures.
- Engage **independent third-party assessors** for penetration testing and, ideally, NIS2 gap assessments.
- Report effectiveness metrics to the **management body** at regular intervals (quarterly dashboards are good practice).
- Map audit findings to your risk register and track remediation to closure.

**Priority:** Medium. This is the control system for all other measures — without it, you cannot demonstrate compliance to a regulator.

---

### Measure 7 — Cyber Hygiene & Training

**What you must do:**
- Deploy **mandatory security awareness training** for all 75 employees at least annually, covering: phishing recognition, social engineering, password hygiene, incident reporting procedures, and remote/BYOD working security.
- Conduct **role-based training** for IT/infrastructure staff, developers, and incident responders at appropriate depth.
- Ensure the **management body (board/executives) completes cybersecurity training** specifically addressing their Art. 20 governance obligations — NIS2 imposes personal liability on management, so they must understand what they are approving.
- Track training completion rates and address gaps ahead of any audit cycle.

**Priority:** Medium. This is relatively low-cost to implement and highly visible to regulators and auditors. With 75 staff, annual training is straightforward to operationalise.

---

### Measure 8 — Cryptography & Encryption

**What you must do:**
- Define a **Cryptography Policy** specifying approved algorithms:
  - Data at rest: AES-256
  - Data in transit: TLS 1.2 minimum (TLS 1.3 preferred)
  - Hashing: SHA-256 or SHA-384
- Establish a **key management lifecycle**: generation, secure storage (use your cloud provider's key management service — AWS KMS, Azure Key Vault, GCP Cloud KMS), rotation schedules, and secure destruction.
- Require encryption of all **sensitive HR data** (employee records, salary data, authentication credentials) in cloud storage and in databases.
- Encrypt **portable media** and any exports of HR data.
- Document exceptions with compensating controls where encryption is technically infeasible.
- Review cryptographic standards annually.

**Priority:** Medium. As a cloud-native SaaS, much of this may already be handled by your cloud provider — but you need the documented policy and explicit controls in place, and you need to ensure end-to-end encryption coverage (not just transport layer).

---

### Measure 9 — HR Security, Access Control & Asset Management

**What you must do:**
- Enforce **least-privilege and need-to-know** for all system access — developers should not have production access unless required; customer data access should be logged and restricted.
- Implement **Role-Based Access Control (RBAC)** and conduct **quarterly access reviews** to remove unnecessary permissions.
- Conduct **background screening** for roles with access to production systems or sensitive customer data (subject to applicable employment law in your jurisdiction).
- Maintain an authoritative **asset inventory** covering hardware, software, SaaS tools, cloud resources, and data assets.
- Enforce a rigorous **joiners/movers/leavers process** — same-day access revocation for departing employees is a firm expectation under NIS2.
- Disable **dormant accounts** after 30–90 days of inactivity.
- Apply **privileged access management (PAM)** controls for administrative accounts — just-in-time access, separate admin accounts, audit logging.

**Priority:** High. HR software companies often hold employee data from tens or hundreds of customer organisations — a compromised insider account or stale privileged access is a high-impact risk.

---

### Measure 10 — MFA, Secure Communications & Emergency Systems

**What you must do:**
- Require **MFA for all remote access**, privileged accounts, cloud management consoles, code repositories, and customer-facing admin interfaces.
- Apply **phishing-resistant MFA** (FIDO2/hardware security keys) for the highest-privilege accounts (production infrastructure, identity providers, billing systems).
- Enforce MFA for **email access, VPN, and all SaaS platforms** holding sensitive data (e.g., your CRM, HR tools, financial systems).
- Use **end-to-end encrypted channels** for sensitive internal communications — particularly during incident coordination (Signal, encrypted Slack channels, or equivalent).
- Maintain a documented **emergency communication plan** with out-of-band channels (pre-agreed mobile contacts, backup communication tools) for use when primary systems are compromised.

**Priority:** High. MFA is one of the most effective single controls against credential-based attacks, and NIS2 explicitly names it. This is likely the first thing any regulator or enterprise customer will ask about.

---

## Part 3: Article 20 — Management Body Obligations

Beyond Art. 21, note that **Article 20** imposes direct obligations on your management body (board/C-suite):

1. The management body must **approve your cybersecurity risk management measures** — they cannot simply delegate this to IT without oversight.
2. The management body must **oversee implementation** of Art. 21 measures and ensure adequate resources are allocated.
3. Management body members must complete **regular cybersecurity training** to understand the risks and how to govern them.
4. **Personal liability** of management body members is possible under Member State transposition law — individual executives can be held accountable for failures of oversight.

Practical implication: schedule a board/executive session on NIS2 obligations, formally adopt your information security policy at board level, and establish a recurring cybersecurity governance agenda item.

---

## Part 4: Recommended Implementation Roadmap

Given your size and a likely Important Entity classification, here is a pragmatic sequencing:

| Phase | Timeframe | Priority Actions |
|-------|-----------|-----------------|
| **1 — Foundations** | Months 1–2 | Confirm classification with legal counsel; conduct NIS2 gap assessment; build asset inventory; draft information security policy; establish management body governance process |
| **2 — High-Priority Controls** | Months 2–4 | Enforce MFA everywhere; implement/formalise IRP with 24h/72h/1-month reporting workflows; verify backup and DR processes; deploy access control and leavers process audit |
| **3 — Process & Policy** | Months 3–5 | Complete cryptography policy; launch security awareness training programme; implement vulnerability management programme; build supplier register and security contract clauses |
| **4 — Assurance** | Months 5–6 | Conduct internal audit against all 10 measures; commission penetration test; establish effectiveness KPIs and reporting to management body; document CVD policy |
| **5 — Continuous** | Ongoing | Annual risk assessment; annual IRP test; quarterly backup restoration tests; quarterly access reviews; annual training refresh; management body annual cyber briefing |

---

## Part 5: Evidence and Documentation Checklist

To demonstrate compliance to competent authorities or enterprise customers, maintain the following:

- [ ] Written information security risk management policy (board-approved)
- [ ] Asset inventory (systems, data, cloud resources)
- [ ] Annual risk assessment report
- [ ] Incident Response Plan with NIS2 notification templates
- [ ] BCP/DR plan with RTO/RPO definitions and test records
- [ ] Supplier/vendor register with security assessments and contract clauses
- [ ] Secure SDLC documentation and penetration test reports
- [ ] Vulnerability management policy and patching SLA records
- [ ] Coordinated vulnerability disclosure policy (security.txt)
- [ ] Effectiveness KPIs/KRI dashboard and internal audit reports
- [ ] Security awareness training completion records
- [ ] Cryptography and key management policy
- [ ] Access control policy, RBAC matrix, and quarterly review records
- [ ] Joiners/movers/leavers process documentation
- [ ] MFA implementation records and privileged access management policy
- [ ] Emergency communication plan
- [ ] Management body training records and meeting minutes approving cybersecurity policies

---

## Part 6: ISO 27001 Alignment

If you pursue or already hold **ISO 27001:2022 certification**, the Annex A controls map closely to all 10 NIS2 Art. 21 measures. ISO 27001 certification provides strong evidence of NIS2 compliance and is often the most efficient path for a B2B SaaS to demonstrate security posture to both regulators and enterprise customers. It does not substitute formal NIS2 registration and incident reporting obligations, but substantially closes the compliance gap.

---

## Disclaimer

This response is an expert analysis based on the NIS2 Directive (Directive (EU) 2022/2555) and does not constitute legal advice. NIS2 has been transposed differently across EU Member States, and the precise classification and obligations applicable to your company depend on the national transposition law of the Member State(s) in which you are established. Engage qualified legal counsel in the relevant jurisdiction to confirm your classification and registration obligations.
