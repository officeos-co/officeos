# SWIFT CSCF v2025 — Control 4.2 MFA Finding: Software OTP Gap Analysis and Remediation Plan

**Date:** 2026-04-25
**Control in Scope:** 4.2 — Multi-Factor Authentication (Mandatory)
**Finding:** Google Authenticator (software-based TOTP) does not satisfy CSCF v2025 Control 4.2 for SWIFT operator authentication to Alliance Access
**Attestation Deadline:** July 31 (KYC-SA submission)

---

## 1. Why Software OTP (Google Authenticator) Is Insufficient

Your SWIFT assessor's finding is correct and grounded in explicit CSCF v2025 language. Control 4.2 states:

> "Acceptable MFA methods: hardware OTP tokens, smart cards with PIN, FIDO2 hardware keys. Software-based OTP (authenticator apps on shared devices) **does not satisfy** this requirement for most architecture types."

The reasons software-based TOTP fails to meet the SWIFT standard come down to security properties that hardware tokens provide and authenticator apps do not:

### 1.1 Threat: Credential Theft and Malware

A software authenticator (Google Authenticator, Microsoft Authenticator, Authy, etc.) runs on a general-purpose device — typically a smartphone or a PC. The TOTP seed is stored in software, which means:

- Malware on the device (keyloggers, screen-capture malware, RATs) can extract the TOTP seed or intercept the generated code before entry.
- If the operator's device is compromised, an attacker can silently clone the authenticator app and generate valid TOTP codes without the operator's knowledge.
- Phishing attacks combined with real-time TOTP relay (man-in-the-middle proxies) can capture and replay the 30-second codes before they expire.

### 1.2 Threat: Shared Device Risk

Software-based TOTP on a shared device — for example, an app installed on a shared computer or a phone that multiple staff members can access — provides no meaningful separation between operators. SWIFT Control 5.1 requires individual named accounts; a shared authenticator app undermines individual accountability.

### 1.3 Threat: No Physical Possession Assurance

The defining property of a second factor is physical possession. A hardware token provides cryptographic proof that the specific physical device is present. A software token on a phone does not:

- The seed can be backed up to cloud (iCloud, Google account sync) and restored to another device without physical possession.
- Phone screen-sharing or remote access tools (which are common in IT environments) allow someone to "possess" the software token remotely without holding the physical device.

### 1.4 CSCF v2025 Tightening: A Deliberate Change from v2024

This is not a grey area in v2025. The CSCF v2024 to v2025 change log explicitly records:

| Control | v2024 | v2025 |
|---------|-------|-------|
| 4.2 MFA | Hardware token strongly recommended | Hardware token explicitly required; clarified app-based OTP insufficient for most types |

SWIFT made this change in direct response to observed attack patterns against SWIFT operators — including the Bangladesh Bank and subsequent SWIFT fraud cases — where compromised operator credentials (including software-based OTPs) were a key enabler. Your assessor's finding reflects the current mandatory standard, not an aspirational best practice.

---

## 2. What CSCF v2025 Control 4.2 Exactly Requires

### 2.1 Formal Control Definition

**Control 4.2 — Multi-Factor Authentication (Mandatory)**
**Objective:** Know and Limit Access (Objective 2)
**Architecture applicability:** Mandatory for ALL architecture types — A1, A2, A3, A4, B

**Purpose:** Require MFA for all interactive operator access to the SWIFT environment.

**Full Requirements:**

1. MFA is mandatory for **all** interactive logins to SWIFT applications, including:
   - Alliance Access (your current deployment)
   - Alliance Gateway
   - SWIFT GUI and web-based management interfaces
2. MFA is mandatory for **remote administrative access** to SWIFT systems (RDP, SSH, jump-server sessions into the SWIFT Secure Zone).
3. **Acceptable MFA methods** — must be one of:
   - **Hardware OTP tokens** (e.g., RSA SecurID physical token, SafeNet/Thales hardware token, Feitian OTP token)
   - **Smart cards with PIN** (PKI certificate on a physical smart card, combined with PIN entry)
   - **FIDO2 hardware security keys** (e.g., YubiKey 5, Google Titan Key, with FIDO2 protocol)
4. **Not acceptable:**
   - Software-based OTP apps (Google Authenticator, Microsoft Authenticator, Authy, Duo Mobile app)
   - SMS-based OTP
   - Email-based OTP
   - Push notification apps without hardware backing
5. **Token lifecycle** must align with Control 5.2 (Token Management) — see Section 3.3 below.

### 2.2 Evidence the Assessor Will Require

To attest Control 4.2 as "Implemented" on your KYC-SA, you will need to provide:

| Evidence Item | Description |
|---------------|-------------|
| MFA configuration evidence | Screenshots of Alliance Access authentication settings showing hardware MFA enforcement |
| Token inventory | Complete list of all operator tokens: serial number, assigned operator, issue date, architecture (hardware) |
| Authentication logs | Sample logs showing MFA enforcement at login — each authentication event must show the second factor (hardware token code) was required and validated |
| Exemption register | If any accounts are excluded from MFA (e.g., service accounts), these must be formally documented with risk acceptance, approved by a control function, and noted in the register |

---

## 3. Remediation Options

You have three main remediation pathways. All are compliant with CSCF v2025 Control 4.2. The right choice depends on your existing infrastructure, budget, and timeline to the July 31 attestation deadline.

### 3.1 Option A: Hardware OTP Tokens (Recommended for Most A1 Deployments)

**What it is:** Dedicated physical devices (key fobs or card-sized tokens) that generate a time-based or event-based one-time password. The cryptographic seed is burned into tamper-resistant hardware and cannot be extracted.

**Compliant products:**
- RSA SecurID hardware tokens (SD700, SID700)
- Thales (formerly Gemalto) SafeNet OTP 110/300 series
- Feitian OTP c200/c300 tokens
- VASCO/OneSpan Digipass GO tokens

**Integration with Alliance Access:**
Alliance Access supports RADIUS-based authentication, which is the standard integration path for hardware OTP tokens. Your organisation would deploy a RADIUS server (e.g., RSA Authentication Manager, Thales SafeNet Authentication Service) that validates hardware token codes. Alliance Access is configured to authenticate operators via RADIUS before granting access.

**Pros:**
- Well-established, widely understood by SWIFT assessors
- Clear audit trail
- Relatively low per-unit cost (typically $30–$80 per token hardware + licensing)
- No dependency on operator smartphones

**Cons:**
- Physical token distribution and management overhead (Control 5.2 compliance required)
- Tokens have a battery life (typically 3–5 years) requiring replacement
- Lost/stolen token process must be documented

**Estimated implementation time:** 4–8 weeks for procurement, RADIUS server deployment, Alliance Access configuration, operator training, and evidence collection. This is achievable before July 31 if started immediately.

### 3.2 Option B: FIDO2 Hardware Security Keys

**What it is:** FIDO2-certified USB or NFC hardware keys that use public-key cryptography for authentication. The private key never leaves the hardware device.

**Compliant products:**
- YubiKey 5 Series (YubiKey 5 NFC, YubiKey 5C NFC)
- Google Titan Security Key
- Feitian ePass FIDO2

**Integration with Alliance Access:**
FIDO2 integration requires that Alliance Access presents a web-based login interface that supports the WebAuthn standard, or that a FIDO2-capable Identity Provider (IdP) sits in front of Alliance Access authentication. This is more complex to configure than RADIUS-based OTP and may require SWIFT to confirm support for your version of Alliance Access.

**Pros:**
- Phishing-resistant by design (origin binding prevents credential relay attacks)
- No shared secret on server side — stronger security posture
- Single key can serve multiple systems
- Resistant to real-time phishing attacks that OTP tokens are theoretically vulnerable to

**Cons:**
- More complex integration; may require IdP middleware
- SWIFT's published guidance for Alliance Access focuses primarily on RADIUS/hardware OTP; FIDO2 support should be confirmed with your SWIFT version
- Higher per-unit cost (~$50–$100 per key)
- Two keys per operator recommended (primary + backup) in case of loss

**Estimated implementation time:** 6–10 weeks including integration testing. Feasible before July 31 but tighter — begin immediately if choosing this option.

### 3.3 Option C: Smart Cards with PIN

**What it is:** PKI-based smart cards (or USB tokens with an embedded certificate) that require the operator to insert the card and enter a PIN. Authentication is based on the certificate private key, which is hardware-protected.

**Compliant products:**
- Thales/Gemalto IDPrime smart cards + card reader
- SafeNet eToken 5110 (USB form factor)
- HID Crescendo smart cards

**Integration with Alliance Access:**
Smart card authentication to Alliance Access typically goes through Windows certificate-based authentication or a PKI/SSL mutual authentication path. This requires a PKI infrastructure (Certificate Authority), smart card management system, and card readers on all operator workstations.

**Pros:**
- Familiar in environments that already have a PKI or smart card programme (e.g., for VPN or Windows login)
- PIN is separate from the card — two distinct factors
- Certificate revocation provides rapid deactivation if a card is lost

**Cons:**
- Highest infrastructure complexity if no PKI exists
- Card readers required at every operator workstation
- Certificate lifecycle management adds overhead
- PKI setup is not practical to complete before July 31 if one does not exist

**Estimated implementation time:** 10–16 weeks from scratch if no PKI exists. Not recommended for a July 31 deadline unless PKI infrastructure is already in place.

---

## 4. Recommended Remediation Path for July 31 Deadline

Given you have approximately 13 weeks from today (April 25) to the July 31 KYC-SA submission deadline, and accounting for time needed to gather and present evidence to your independent assessor, **Option A (Hardware OTP Tokens via RADIUS)** is the most realistic path.

### 4.1 Remediation Timeline

| Week | Activity |
|------|----------|
| Week 1–2 (by May 9) | Select hardware token vendor; raise procurement request; engage RADIUS server vendor (RSA, Thales, or open-source FreeRADIUS); assign project owner |
| Week 2–3 (by May 16) | Deploy RADIUS server in SWIFT Secure Zone or adjacent management zone; complete base configuration; integrate with Active Directory or local user store |
| Week 3–5 (by May 30) | Receive hardware tokens; enrol operator tokens into RADIUS server; configure Alliance Access to authenticate via RADIUS; test with pilot operators |
| Week 5–7 (by June 13) | Roll out hardware tokens to all SWIFT operators; complete operator training; disable Google Authenticator TOTP for all accounts |
| Week 7–9 (by June 27) | Collect evidence artifacts: MFA config screenshots, token inventory, authentication logs showing hardware MFA enforcement; build exemption register if needed |
| Week 9–10 (by July 4) | Present evidence to independent assessor for Control 4.2 review; remediate any assessor queries |
| Week 10–12 (by July 18) | Assessor finalises assessment; complete KYC-SA form including Control 4.2 as "Implemented" |
| By July 31 | Submit KYC-SA attestation via SWIFT KYC-SA portal |

### 4.2 Control 5.2 (Token Management) — Linked Requirement

Remediating Control 4.2 automatically triggers the token management obligations under **Control 5.2 (Token Management)**, which is also Mandatory for all architecture types. You must establish and maintain:

- **Token inventory register:** Serial number, operator name, issue date, return date (if applicable)
- **Issuance process:** Formal approval required before issuing a token to an operator
- **Lost/stolen token procedure:** Token deactivated within 1 hour of report; incident logged
- **Leaver process:** Token returned and deactivated on employee departure (same day)
- **Annual reconciliation:** Inventory cross-checked against active operator list annually

This is not optional — your assessor will check both 4.2 and 5.2 together as they are explicitly linked in the CSCF.

---

## 5. Gap Assessment: Current State vs. Required State

| Control | Control Name | Current Status | Required State | Gap | Remediation |
|---------|-------------|----------------|---------------|-----|-------------|
| **4.2** | Multi-Factor Authentication | **Not Implemented** (software TOTP in use) | Hardware OTP token, smart card, or FIDO2 key required for all operator interactive sessions | Software-based TOTP (Google Authenticator) explicitly excluded from acceptable MFA methods in CSCF v2025 | Deploy hardware OTP tokens + RADIUS; revoke software OTP access |
| **5.2** | Token Management | **Not Implemented** (no hardware token inventory exists) | Token inventory, issuance/return process, lost token procedure, annual reconciliation | No hardware token programme in place | Establish token inventory register and lifecycle procedures as part of hardware token rollout |

---

## 6. KYC-SA Attestation Implications

If your July attestation is submitted without remediation of Control 4.2:

- You must attest Control 4.2 as **"Not Implemented"** — you cannot attest "Implemented" with Google Authenticator in place, as SWIFT assessors explicitly flag software TOTP as non-compliant.
- A "Not Implemented" status on Control 4.2 is visible to all counterparties on the KYC-SA portal immediately upon submission.
- Counterparties (correspondent banks, custodians, CCPs) may restrict transactions with you, require explanations, or trigger their own internal escalations.
- Repeated or prolonged non-compliance can result in SWIFT notifying your regulator.

**The only path to a clean July attestation on Control 4.2 is to deploy hardware MFA before the attestation is submitted.** Given the 13-week window, this is achievable with Option A if procurement is initiated this week.

---

## 7. Summary of Key Facts

| Item | Detail |
|------|--------|
| Control | 4.2 — Multi-Factor Authentication (Mandatory, all architecture types) |
| Objective | Know and Limit Access (Objective 2) |
| What CSCF v2025 requires | Hardware OTP token, smart card with PIN, or FIDO2 hardware key for all interactive operator sessions to SWIFT applications |
| Why Google Authenticator fails | Software TOTP explicitly excluded; seed extractable by malware; no physical possession assurance; cloud backup risk; CSCF v2025 tightened language to make this explicit |
| Acceptable remediation options | (A) Hardware OTP tokens via RADIUS — fastest; (B) FIDO2 keys — most secure; (C) Smart cards — most complex |
| Recommended option for July deadline | Option A: Hardware OTP tokens via RADIUS |
| Linked control also requiring action | 5.2 — Token Management |
| KYC-SA deadline | July 31 |
| Risk of non-remediation | "Not Implemented" attestation visible to all counterparties; potential transaction restrictions; regulatory escalation |
| Implementation window | ~13 weeks from today to July 31 — sufficient for Option A if started immediately |
