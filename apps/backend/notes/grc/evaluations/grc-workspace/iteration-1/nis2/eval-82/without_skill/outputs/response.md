# NIS2 Compliance Assessment: B2B SaaS HR Software Company

## Are You Directly Subject to NIS2?

The answer is: **probably yes, though the specific classification matters.**

The EU NIS2 Directive (Directive 2022/2555), which became law in October 2024, significantly expanded the scope of the original NIS Directive. Whether your company falls under NIS2 depends on your sector classification and size thresholds.

### Size Threshold Analysis

NIS2 applies to "medium-sized enterprises" and larger organizations in covered sectors. The EU definition of a medium-sized enterprise is:

- 50 or more employees, **OR**
- Annual turnover/revenue exceeding €10 million

With **75 employees and €15M annual revenue**, your company exceeds **both** thresholds. You are not a micro or small enterprise and cannot use those exemptions.

### Sector Classification

NIS2 organizes covered entities into two tiers:

**Annex I - Essential Entities** (higher scrutiny, proactive supervision):
Includes energy, transport, banking, financial market infrastructure, health, drinking water, wastewater, digital infrastructure (IXPs, DNS, TLD registries, cloud computing providers, data centers, CDNs, trust service providers, public electronic communications networks).

**Annex II - Important Entities** (lower scrutiny, reactive supervision):
Includes postal services, waste management, chemicals, food, manufacturing (certain sectors), digital providers (online marketplaces, online search engines, social networking platforms), and **managed service providers / digital service providers**.

As a **B2B cloud-based SaaS HR software provider**, your most likely classification is under **Annex II as a Digital Service Provider** — specifically as a provider of cloud computing services or a managed service provider to other businesses. Some EU member states may also classify B2B SaaS providers under the broader "digital providers" category.

**Conclusion on applicability:** Yes, you are very likely an **Important Entity** under NIS2 Annex II, subject to the directive's requirements. Your customer's warning is correct.

> **Note:** NIS2 is an EU Directive, meaning each EU member state must transpose it into national law. The specific national implementing legislation in the countries where you operate or where your customers are based will govern your exact obligations. If you are not established in the EU but serve EU customers, you may still need to designate an EU representative.

---

## What Does Article 21 Require?

Article 21 of NIS2 sets out **cybersecurity risk-management measures** that covered entities must implement. It requires a risk-based approach with measures "appropriate to the risks posed." Here is what it mandates:

### Article 21 Requirements Breakdown

**Article 21(1) - General Obligation**
You must take "appropriate and proportionate technical, operational and organisational measures to manage the risks posed to the security of network and information systems." Measures must be based on an "all-hazards approach."

**Article 21(2) - Specific Measures Required**

The directive lists ten specific categories of measures that must be implemented:

---

#### 1. Policies on Risk Analysis and Information Security (Art. 21(2)(a))
**What it means:** Formal, documented information security policies covering risk analysis methodology, asset inventory, and risk treatment.

**Practical steps:**
- Develop and maintain a written Information Security Policy
- Conduct and document regular risk assessments (at least annually)
- Maintain an asset register (systems, data, infrastructure)
- Implement a risk treatment plan with documented decisions

---

#### 2. Incident Handling (Art. 21(2)(b))
**What it means:** Processes and procedures for detecting, reporting, and responding to security incidents.

**Practical steps:**
- Establish an Incident Response Plan (IRP)
- Define incident classification and severity levels
- Set up internal incident detection and logging
- Designate incident response roles and responsibilities
- Implement communication procedures for internal and external notification

---

#### 3. Business Continuity, Including Backup Management and Disaster Recovery (Art. 21(2)(c))
**What it means:** Ensuring your service can continue operating or recover quickly after a disruption.

**Practical steps:**
- Implement and test regular data backups (automated, off-site or cloud)
- Develop a Business Continuity Plan (BCP) and Disaster Recovery Plan (DRP)
- Define Recovery Time Objectives (RTOs) and Recovery Point Objectives (RPOs)
- Test recovery procedures at least annually
- Document procedures for maintaining service during significant incidents

---

#### 4. Supply Chain Security (Art. 21(2)(d))
**What it means:** Assessing and managing cybersecurity risks from your suppliers, subprocessors, and technology vendors.

**Practical steps:**
- Inventory all third-party vendors and cloud providers (AWS, Azure, GCP, SaaS tools, etc.)
- Conduct security due diligence on critical suppliers
- Include cybersecurity requirements in supplier contracts
- Assess your software supply chain (open-source libraries, CI/CD pipelines, etc.)
- Establish a vendor risk management process

---

#### 5. Security in Network and Information Systems Acquisition, Development, and Maintenance (Art. 21(2)(e))
**What it means:** Security-by-design in your software development lifecycle and infrastructure management.

**Practical steps:**
- Implement a Secure Software Development Lifecycle (SSDLC)
- Conduct code reviews and security testing (SAST, DAST)
- Manage vulnerabilities and apply patches promptly
- Implement change management procedures
- Conduct penetration testing periodically

---

#### 6. Policies and Procedures to Assess the Effectiveness of Cybersecurity Risk-Management Measures (Art. 21(2)(f))
**What it means:** Monitoring, auditing, and continuously improving your security controls.

**Practical steps:**
- Define and track key security metrics/KPIs
- Conduct internal security audits
- Perform or commission external audits/penetration tests
- Implement continuous monitoring (SIEM, log management)
- Review and update policies at regular intervals

---

#### 7. Basic Cyber Hygiene Practices and Cybersecurity Training (Art. 21(2)(g))
**What it means:** Ensuring all staff follow fundamental security practices and receive adequate training.

**Practical steps:**
- Implement mandatory security awareness training for all 75 employees
- Train on phishing, social engineering, password hygiene, and data handling
- Apply basic hygiene measures: MFA everywhere, patch management, endpoint protection, principle of least privilege
- Role-specific training for developers, IT staff, and management

---

#### 8. Policies and Procedures Regarding the Use of Cryptography and Encryption (Art. 21(2)(h))
**What it means:** Formal policies governing when and how encryption is used.

**Practical steps:**
- Encrypt all data at rest (databases, backups, file storage)
- Encrypt all data in transit (TLS 1.2/1.3 minimum)
- Document your cryptography standards and key management procedures
- Manage certificates and keys through a defined lifecycle
- Prohibit use of deprecated algorithms (MD5, SHA-1, DES, RC4)

---

#### 9. Human Resources Security, Access Control Policies, and Asset Management (Art. 21(2)(i))
**What it means:** Controlling who can access what, and managing the human element of security.

**Practical steps:**
- Implement Role-Based Access Control (RBAC) with least privilege
- Conduct background checks for staff with privileged access
- Implement onboarding and offboarding security procedures
- Enforce MFA for all systems, especially administrative access
- Maintain an up-to-date asset inventory
- Conduct periodic access reviews

---

#### 10. Use of Multi-Factor Authentication (MFA) and Secure Communications (Art. 21(2)(j))
**What it means:** Mandating MFA and secured communications where appropriate.

**Practical steps:**
- Enforce MFA for all remote access, admin panels, cloud consoles, and sensitive systems
- Implement MFA for your HR software's administrative interfaces
- Use secure (encrypted) communications for sensitive internal communications
- Consider privileged access management (PAM) for administrative accounts

---

## Article 21(4) - Management Accountability

A critical provision often overlooked: **management bodies are personally responsible** for approving and overseeing cybersecurity risk management measures. Your CEO/board must:

- Approve the cybersecurity policies
- Oversee their implementation
- Undergo cybersecurity training themselves
- Can be held **personally liable** for non-compliance

---

## NIS2 Incident Reporting Obligations (Article 23)

Alongside Article 21, you must also implement reporting procedures to comply with Article 23's notification timelines:

| Timeframe | Action Required |
|-----------|----------------|
| **24 hours** | Early warning to national CSIRT/competent authority if you suspect a significant incident |
| **72 hours** | Formal incident notification with initial assessment |
| **1 month** | Final report with full details, impact, and remediation measures |

A "significant incident" is one that causes or could cause severe operational disruption or financial loss to your organization or other entities.

---

## Penalties for Non-Compliance

For Important Entities (your likely classification):
- Administrative fines up to **€7 million or 1.4% of total global annual turnover**, whichever is higher
- Member states may impose additional penalties

---

## Recommended Action Plan

Given your size and resources, here is a pragmatic prioritization:

**Immediate (0-3 months):**
1. Determine your exact classification under your applicable national NIS2 implementation law
2. Identify your competent national authority and register if required
3. Conduct a gap assessment against all 10 Article 21 measures
4. Establish incident response procedures and reporting capabilities
5. Enforce MFA across all systems

**Short-term (3-6 months):**
6. Develop formal Information Security Policy and risk assessment process
7. Implement backup and disaster recovery procedures and test them
8. Launch mandatory security awareness training program
9. Inventory and assess your third-party suppliers

**Medium-term (6-12 months):**
10. Implement supply chain security controls and supplier contracts
11. Establish secure SDLC practices
12. Conduct penetration testing
13. Implement continuous monitoring
14. Achieve and document management sign-off on all policies

---

## Key Caveats

1. **National implementation varies:** NIS2 is a directive, not a regulation. Each EU member state has implemented it differently. Check the specific national law in your country of establishment.
2. **Self-assessment vs. audit:** Important Entities may be subject to reactive supervision (audited after an incident), whereas Essential Entities face proactive audits. However, you should be able to demonstrate compliance at any time.
3. **Customer contracts:** Your B2B customers may contractually require NIS2 compliance certifications or audit rights — review your customer agreements.
4. **Proportionality:** Requirements must be "appropriate and proportionate" to your risk and size. A 75-person company is not expected to implement controls at the same scale as a multinational, but the measures must be genuine and documented.

---

*Assessment based on NIS2 Directive 2022/2555 and general compliance knowledge as of April 2026. This is not legal advice — consult a qualified legal counsel in the relevant EU member state for your specific situation.*
