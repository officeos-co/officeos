# CIS Controls v8: Implementation Group Scoping for a 75-Person Accounting Firm

---

## 1. Organization Profile

| Attribute | Your Organization |
|-----------|------------------|
| **Size** | 75 employees |
| **Industry** | Accounting / Professional Services |
| **IT Staffing** | 1 part-time IT generalist |
| **Data Sensitivity** | High — client financial and tax data (PII, financial records) |
| **Technology Stack** | Cloud-based accounting software, Microsoft 365 |
| **Prior Security Assessments** | None |
| **Regulatory Exposure** | IRS Safeguards Program (if handling tax data), state CPA board requirements, potentially FTC Safeguards Rule |

---

## 2. Implementation Group Determination

### Recommended IG: **IG2 (with immediate IG1 foundation)**

**Rationale:**

You sit at the upper boundary of IG1 and clearly cross into **IG2** based on the following criteria:

| IG Criterion | Your Situation |
|---|---|
| **Data sensitivity** | You process client financial and tax data — this is highly sensitive data that, if compromised, would materially harm your clients and expose your firm to liability. This is the defining IG2 signal. |
| **IT capability** | One part-time IT person is limited, but you do have *someone responsible for managing and protecting IT* — the IG2 criterion is met. IG1 is specifically for organizations with *no dedicated IT function*. |
| **Operational impact** | A breach or extended outage would significantly impact client service delivery and could trigger regulatory reporting obligations. You can withstand *some* outage, but not prolonged disruption. |
| **Commercial off-the-shelf tools** | Your stack (Microsoft 365, cloud accounting software) is commercially supported — consistent with both IG1 and IG2. |
| **Regulatory oversight** | Tax data handling brings IRS and FTC Safeguards Rule exposure. If you have more than 5,000 customer records, the FTC Safeguards Rule (16 CFR Part 314) mandates a formal written information security program — directly driving IG2 controls. |

**You are not IG3** because you do not employ dedicated security experts, you are not subject to rigorous regulatory audits (e.g., HIPAA, PCI, FEDRAMP), and a breach — while serious — would not cause "significant harm to public welfare."

**Practical guidance:** Since you have never done a formal security assessment, **begin by achieving full IG1 compliance first**, then systematically layer in IG2 safeguards. Trying to implement all 130 IG2 safeguards simultaneously will overwhelm a part-time IT resource.

---

## 3. IG1 Foundation: Where to Start (56 Safeguards Across 18 Controls)

The following is a prioritized starting sequence tailored to your environment. Work through these in order — each builds on the last.

---

### Phase 1: Know What You Have (Weeks 1–4)

**CIS Control 1 — Inventory and Control of Enterprise Assets**
- **Safeguard 1.1:** Create and maintain a detailed inventory of all devices: laptops, desktops, phones, printers, routers. Use a spreadsheet or Microsoft Intune (already available in M365) to track device name, owner, OS version, and location.
- **Safeguard 1.2:** Identify and address any unauthorized devices found on the network.
- **Why first:** You cannot protect what you do not know exists. This is the prerequisite for every other control.

**CIS Control 2 — Inventory and Control of Software Assets**
- **Safeguard 2.1:** Document all software authorized to run on firm devices, including your cloud accounting platform, M365 apps, and any installed applications.
- **Safeguard 2.2:** Confirm all software is currently vendor-supported (no end-of-life OS or applications).
- **Safeguard 2.3:** Remove or disable unauthorized software.

**CIS Control 3 — Data Protection (IG1 Safeguards)**
- **Safeguard 3.1:** Establish a data management process — define what client data you hold, where it lives, and who can access it.
- **Safeguard 3.2:** Maintain a data inventory (e.g., "client tax returns stored in [cloud platform]; client PII in M365 SharePoint").
- **Safeguard 3.3:** Configure access control lists — client data should be accessible only to staff working on that client.
- **Safeguard 3.4/3.5:** Define data retention periods and a secure disposal process for old records (shredding, secure deletion).

---

### Phase 2: Protect Accounts and Access (Weeks 3–6)

**CIS Control 5 — Account Management**
- **Safeguard 5.1:** Inventory all user accounts in Microsoft 365 and your accounting software. Remove former employee accounts immediately.
- **Safeguard 5.2:** Enforce unique, strong passwords. Use Microsoft 365's password policies and ban common passwords.
- **Safeguard 5.3:** Disable or remove accounts for departed employees and inactive accounts (no activity in 90 days).
- **Safeguard 5.4:** Restrict admin privileges — only the IT person (and a designated backup) should have global admin in M365. Partners should not use admin accounts for daily work.

**CIS Control 6 — Access Control Management (IG1 Safeguards)**
- **Safeguard 6.1:** Establish a formal process for granting access (new hire checklist).
- **Safeguard 6.2:** Establish a formal process for revoking access (offboarding checklist — same day as termination).

> **Critical note for your firm:** Multi-factor authentication (MFA) for externally-exposed applications (Safeguard 6.3) is technically an IG2 safeguard, but given that you process client financial data through cloud applications accessible over the internet, **treat this as an immediate priority regardless of IG level**. Enable MFA for all Microsoft 365 accounts and your cloud accounting platform now. This single action prevents the majority of credential-based attacks.

---

### Phase 3: Harden Systems and Prevent Malware (Weeks 5–8)

**CIS Control 4 — Secure Configuration**
- **Safeguard 4.1:** Document baseline security configurations for all devices. Use Microsoft Security Baseline templates (free from Microsoft).
- **Safeguard 4.3:** Configure automatic screen lock after 15 minutes of inactivity on all devices.
- **Safeguard 4.4/4.5:** Enable host-based firewalls on all servers and end-user devices. Windows Defender Firewall is already available.

**CIS Control 10 — Malware Defenses**
- **Safeguard 10.1:** Deploy anti-malware on all endpoints. Microsoft Defender (included in M365 Business Premium) covers this.
- **Safeguard 10.2:** Confirm automatic signature updates are enabled.
- **Safeguard 10.3:** Disable AutoRun/AutoPlay on all Windows devices.
- **Safeguard 10.4:** Configure anti-malware scanning of removable media (USB drives).

---

### Phase 4: Patching and Backups (Weeks 7–10)

**CIS Control 7 — Continuous Vulnerability Management (IG1 Safeguards)**
- **Safeguard 7.1:** Create a vulnerability management process — at minimum, a monthly patching schedule.
- **Safeguard 7.2:** Document a remediation process: critical patches within 14 days, high within 30 days.
- **Safeguard 7.3:** Enable automatic OS patch management for all Windows/Mac devices. Microsoft Intune or Windows Update for Business (included in M365) handles this.
- **Safeguard 7.4:** Enable automatic application patch management. Enable auto-updates for all third-party apps.

**CIS Control 11 — Data Recovery**
- **Safeguard 11.1:** Document your backup and recovery process.
- **Safeguard 11.2:** Implement automated backups of all critical data. Your cloud accounting platform likely has built-in backups — verify this. For M365, enable Microsoft 365 Backup or use a third-party backup (Veeam, Acronis).
- **Safeguard 11.4:** Maintain an isolated backup copy (offline or separate cloud tenant) to protect against ransomware. This is non-negotiable for a firm handling financial data.

---

### Phase 5: Email, Web, and Logging (Weeks 9–12)

**CIS Control 9 — Email and Web Browser Protections**
- **Safeguard 9.1:** Ensure all browsers and email clients are fully supported and up-to-date.
- **Safeguard 9.2:** Enable DNS filtering (Microsoft Defender for Endpoint includes this; Cisco Umbrella or Cloudflare Gateway are low-cost alternatives).
- **Safeguard 9.3:** Configure SPF, DKIM, and DMARC DNS records for your email domain to prevent spoofing. This is free and critical for an accounting firm targeted by business email compromise (BEC) attacks.

**CIS Control 8 — Audit Log Management (IG1 Safeguards)**
- **Safeguard 8.1:** Define a log management process.
- **Safeguard 8.2:** Enable audit logging in Microsoft 365 (Microsoft Purview Audit). Confirm logging is enabled in your cloud accounting platform.

---

### Phase 6: Training and Incident Response (Ongoing)

**CIS Control 14 — Security Awareness and Skills Training**
- **Safeguard 14.1:** Establish a security awareness program. Microsoft 365 includes Attack Simulator for phishing training. KnowBe4 and Proofpoint Security Awareness Training are cost-effective options.
- **Safeguard 14.2:** Train all staff to recognize phishing and social engineering — the #1 attack vector against accounting firms.
- **Safeguard 14.3:** Train on authentication best practices (why MFA matters, how to recognize MFA fatigue attacks).
- **Safeguard 14.4/14.5:** Train on data handling — what client data can be emailed, how to securely share documents.

**CIS Control 17 — Incident Response Management**
- **Safeguard 17.1:** Designate the IT person (and a partner as backup) to manage security incidents.
- **Safeguard 17.2:** Document contact information for reporting — your cyber insurance carrier, the IRS Stakeholder Liaison (for tax data breaches), state attorney general (for breach notification).
- **Safeguard 17.3/17.4:** Write a basic incident response plan. A one-page runbook covering: detect → contain → notify → recover is sufficient at this stage.

---

**CIS Control 15 — Service Provider Management**
- **Safeguard 15.1:** List all your service providers (cloud accounting vendor, M365/Microsoft, payroll processor, any IT contractors).
- **Safeguard 15.2:** Document a service provider management policy.
- **Safeguard 15.3/15.4:** Classify providers by risk level and confirm security requirements are addressed in contracts (look for data processing agreements, SOC 2 reports from your vendors).

---

## 4. IG2 Priorities to Layer In (After IG1 is Established)

Once IG1 is in place — realistically 3–6 months for a part-time IT resource — prioritize these IG2 additions given your risk profile:

| Priority | Safeguard | Reason for This Firm |
|----------|-----------|----------------------|
| **Immediate** | 6.3 — MFA for externally-exposed apps | Cloud apps accessible from internet; BEC is top threat for accountants |
| **Immediate** | 6.4 — MFA for remote network access | Remote work is common; VPN without MFA is insufficient |
| **High** | 3.11 — Encrypt sensitive data at rest | Client tax/financial data must be encrypted on laptops and file shares |
| **High** | 3.6 — Encrypt data on end-user devices | Enable BitLocker (Windows) or FileVault (Mac) on all devices |
| **High** | 11.3 — Protect recovery data | Backups must be isolated from production to survive ransomware |
| **High** | 9.5 — Implement DMARC enforcement | Move DMARC policy from `p=none` to `p=quarantine` or `p=reject` |
| **Medium** | 7.6 — Automated vulnerability scans | Use Microsoft Secure Score or Tenable Nessus Essentials |
| **Medium** | 8.5 — Collect detailed audit logs | Enable Unified Audit Log in M365 with 1-year retention |
| **Medium** | 12.7 — Remote access VPN | Require VPN for access to on-premises resources |
| **Medium** | 15.5 — Assess service providers | Request SOC 2 Type 2 reports annually from cloud accounting vendor |

---

## 5. Quick Win Summary (Do These This Week)

1. **Enable MFA** for all Microsoft 365 accounts — takes 30 minutes, prevents ~99% of credential attacks.
2. **Audit and remove** all inactive and former-employee accounts in M365 and accounting software.
3. **Verify backups** are running and test restoration from at least one backup.
4. **Configure SPF, DKIM, DMARC** for your email domain — free, takes 1–2 hours.
5. **Enable Microsoft Defender** on all endpoints and confirm it is reporting to M365 Defender portal.
6. **Check for end-of-life software** — any Windows 10 machines should be upgraded before October 2025 end-of-support.

---

## 6. Tools Available in Your Existing Stack

You already pay for Microsoft 365 — leverage it fully before purchasing additional tools:

| Capability | M365 Feature | CIS Control |
|-----------|-------------|-------------|
| Device inventory and management | Microsoft Intune | Control 1, 4 |
| Endpoint protection | Microsoft Defender for Endpoint | Control 10 |
| Identity and MFA | Microsoft Entra ID (Azure AD) | Control 5, 6 |
| Email security | Microsoft Defender for Office 365 | Control 9 |
| Audit logging | Microsoft Purview Audit | Control 8 |
| Vulnerability assessment | Microsoft Secure Score | Control 7 |
| Phishing simulation | Microsoft Attack Simulator | Control 14 |
| Data loss prevention | Microsoft Purview DLP | Control 3 (IG2) |

> **Note:** Many of these features require M365 Business Premium or higher. If you are on M365 Business Basic or Standard, upgrading to Business Premium (~$22/user/month) unlocks most of the security capabilities above and is likely your highest-ROI security investment.

---

## 7. Regulatory Context

As an accounting firm, your CIS Controls implementation supports compliance with:

- **IRS Publication 4557 / FTC Safeguards Rule (16 CFR Part 314):** Requires a written information security plan (WISP). IG1 + IG2 implementation provides the technical controls underpinning a compliant WISP.
- **State CPA board requirements:** Many states require reasonable security measures for client data.
- **AICPA SOC 2 (if applicable):** If you seek a SOC 2 report, IG2 implementation maps directly to the Security Trust Services Criteria (CC6–CC9).

---

## Summary

| Question | Answer |
|----------|--------|
| **Target IG** | IG2 (with IG1 as the mandatory foundation) |
| **Where to start** | Enable MFA immediately; then build asset inventory (Controls 1–2) |
| **Biggest risk** | Business email compromise and ransomware targeting client financial data |
| **Timeline** | IG1 complete in 3–6 months; IG2 in 12–18 months at part-time IT capacity |
| **Best leverage** | Maximize Microsoft 365 Business Premium security features before buying new tools |
| **Critical gap to address now** | No MFA on cloud applications is your highest-probability breach vector |
