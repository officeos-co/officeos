# CIS Controls Implementation Group Recommendation
## 75-Person Accounting Firm Security Assessment

---

## Recommended Implementation Group: IG1

**You should target CIS Controls Implementation Group 1 (IG1).**

### Why IG1 Is the Right Fit

The CIS Controls define three Implementation Groups based on organizational risk profile, resources, and security maturity:

| Factor | Your Organization | IG Match |
|--------|------------------|----------|
| Organization size | 75 employees | IG1 |
| IT staffing | Part-time, single person | IG1 |
| Security program maturity | No formal assessments | IG1 |
| Data sensitivity | Client financial/tax data | IG1–IG2 boundary |
| Technology environment | Cloud-based SaaS + M365 | IG1 |

IG1 is designed for organizations with limited cybersecurity expertise and resources. It covers the 56 "essential cyber hygiene" safeguards that every organization should have regardless of size — the baseline below which no organization should fall. CIS describes IG1 as protecting against the most common, non-targeted attacks.

**Important nuance:** Your processing of sensitive client financial and tax data pushes your *risk* toward IG2 territory, but your *resources and capacity* clearly place you at IG1 right now. Start with IG1 fully implemented before considering IG2 expansion. Regulatory obligations (IRS Publication 4557, FTC Safeguards Rule for tax preparers) reinforce the urgency of getting IG1 right.

---

## Where to Start: Priority IG1 Safeguards

IG1 spans CIS Controls 1–16 (with specific safeguards per control). Given your profile, prioritize in this order:

### Phase 1: Immediate Actions (Month 1–2)

**CIS Control 5 — Account Management**
- Safeguard 5.3: Disable dormant accounts within 30 days
- Safeguard 5.4: Restrict administrator privileges — your IT person and no one else should have admin rights
- Safeguard 5.6: Centralize account management (M365 Active Directory/Entra ID)

*Why first:* Credential compromise is the #1 entry point for accounting firm breaches. Former employees with active accounts and over-privileged users are immediate risks.

**CIS Control 6 — Access Control Management**
- Safeguard 6.3: Require MFA for all users accessing cloud applications
- Safeguard 6.5: Require MFA for all administrative access

*Why first:* MFA alone blocks over 99% of automated credential attacks. Your M365 environment has MFA built in — this is a configuration task, not a purchase.

**CIS Control 4 — Secure Configuration of Enterprise Assets**
- Safeguard 4.1: Establish and maintain a secure configuration process
- Safeguard 4.2: Establish and maintain a secure configuration for network infrastructure
- Apply Microsoft Secure Score baselines for M365

### Phase 2: Core Hygiene (Month 2–4)

**CIS Control 1 — Inventory and Control of Enterprise Assets**
- Safeguard 1.1: Maintain a detailed inventory of all enterprise assets
- Know every device that accesses client data

**CIS Control 2 — Inventory and Control of Software Assets**
- Safeguard 2.1: Maintain an inventory of all authorized software
- Safeguard 2.5: Allowlist authorized software on endpoints

**CIS Control 3 — Data Protection**
- Safeguard 3.3: Configure data access control lists — who can access which client files?
- Safeguard 3.11: Encrypt sensitive data at rest on laptops/workstations
- Safeguard 3.12: Segment data processing and storage based on sensitivity

*Why this matters for you:* Client tax data is a high-value target. Data protection controls directly address your regulatory exposure under the FTC Safeguards Rule.

**CIS Control 11 — Data Recovery**
- Safeguard 11.1: Establish and maintain a data recovery process
- Safeguard 11.2: Perform automated backups — verify your cloud accounting software and M365 backups are actually working
- Safeguard 11.4: Test backup recovery quarterly

### Phase 3: Detection and Response Foundation (Month 4–6)

**CIS Control 8 — Audit Log Management**
- Safeguard 8.2: Collect audit logs from M365, cloud accounting software, and network devices
- Safeguard 8.5: Enable Microsoft Defender audit logging

**CIS Control 9 — Email and Web Browser Protections**
- Safeguard 9.1: Use only fully supported browsers and email clients
- Safeguard 9.2: Use DNS filtering services (Cisco Umbrella, Cloudflare Gateway — both have free/low-cost tiers)
- Safeguard 9.5: Enable anti-phishing controls in M365 (Defender for Office 365)

*Why critical for accounting firms:* Phishing and business email compromise (BEC) targeting accounting firms is rampant, especially during tax season. BEC attacks impersonate partners to redirect client payments.

**CIS Control 14 — Security Awareness and Skills Training**
- Safeguard 14.1: Establish and maintain a security awareness program
- Safeguard 14.2: Train employees to recognize social engineering attacks
- Safeguard 14.6: Train employees on how to identify and report phishing

*Practical approach:* Microsoft 365 Business Premium includes Attack Simulator for phishing training. KnowBe4 and Proofpoint offer SMB-appropriate pricing.

**CIS Control 17 — Incident Response Management**
- Safeguard 17.1: Designate personnel to manage incident handling — even if it's your part-time IT person
- Safeguard 17.2: Establish and maintain contact information for reporting security incidents (IRS, state tax agencies, clients)
- Safeguard 17.3: Create a simple written incident response plan

---

## Practical Considerations for Your Situation

### Leverage What You Already Have
Your Microsoft 365 subscription likely includes significant security tooling at no additional cost:
- **Microsoft Defender Antivirus** — ensure it is active on all endpoints
- **Microsoft Entra ID** (formerly Azure AD) — MFA, conditional access, device compliance
- **Microsoft Defender for Office 365** (Plan 1 included in M365 Business Premium) — anti-phishing, safe links, safe attachments
- **Microsoft Purview** — data loss prevention to prevent client data from leaving via email
- **Microsoft Secure Score** — provides a dashboard showing your current security posture and prioritized recommendations

Check which M365 plan you have. Upgrading from Business Basic/Standard to Business Premium (~$22/user/month) unlocks substantially more security capability and is likely cost-justified given the client data you hold.

### Regulatory Baseline You Must Meet
As an accounting firm handling tax data:
- **IRS Publication 4557** (Safeguarding Taxpayer Data) — mandatory for tax preparers; maps closely to IG1
- **FTC Safeguards Rule** — applies to financial institutions including tax preparers; requires a written information security program, risk assessment, and designated security coordinator
- **State-level data breach notification laws** — know your obligations if client data is compromised

Your IG1 implementation will substantially address these requirements.

### Resource-Constrained Implementation Strategy

Given a part-time IT resource, be realistic:

1. **Use M365 Secure Score as your roadmap** — it prioritizes actions by impact and difficulty
2. **Automate where possible** — Conditional Access policies in Entra ID enforce MFA without manual intervention
3. **Consider a one-time engagement** with an MSSP (Managed Security Service Provider) or CIS-certified assessor to baseline your current state — a 2–3 day assessment will reveal your highest risks
4. **Prioritize prevention over detection** at this stage — MFA, patching, and email filtering prevent incidents that your part-time IT person does not have capacity to investigate

### 90-Day Quick Win Checklist

- [ ] Enable MFA for all M365 users (Entra ID Conditional Access)
- [ ] Remove local administrator rights from standard user accounts
- [ ] Audit and disable inactive accounts
- [ ] Review your M365 Secure Score and address top 10 recommendations
- [ ] Confirm automated backups are running and test restoration
- [ ] Enable Microsoft Defender on all endpoints
- [ ] Enable anti-phishing policies in Defender for Office 365
- [ ] Document a one-page incident response contact list
- [ ] Conduct one phishing awareness training session
- [ ] Create an asset inventory (spreadsheet of all devices and software is sufficient to start)

---

## Summary

| Recommendation | Detail |
|---------------|--------|
| Target IG | **IG1 (all 56 safeguards)** |
| Start here | **CIS Control 5 (Accounts) and Control 6 (MFA)** |
| Highest risk | Credential compromise, phishing/BEC, unencrypted client data on endpoints |
| Quick wins | MFA in M365, admin privilege restriction, M365 Secure Score review |
| Regulatory anchor | IRS Pub 4557, FTC Safeguards Rule |
| Timeline to IG1 completion | 6–12 months with part-time IT resource |

Completing IG1 will put you ahead of the majority of firms your size and will provide a defensible security posture if a breach occurs. Once IG1 is fully implemented and documented, reassess whether your client obligations and growth trajectory warrant moving to IG2.
