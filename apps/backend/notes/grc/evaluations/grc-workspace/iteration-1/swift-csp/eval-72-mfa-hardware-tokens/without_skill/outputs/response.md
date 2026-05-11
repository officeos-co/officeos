# SWIFT CSCF v2025 Control 4.2 — MFA Finding: Software TOTP for Alliance Access Operators

## Executive Summary

Your SWIFT assessor is correct to flag Google Authenticator (software-based TOTP) as non-compliant with CSCF v2025 Control 4.2. The control explicitly requires **multi-factor authentication using hardware-based tokens or equivalent strong authentication mechanisms** for privileged and operator access to SWIFT components. Software OTP apps installed on smartphones or general-purpose computers do not satisfy this requirement because they fail the physical possession factor as defined by SWIFT. You need to remediate before your July attestation.

---

## 1. Why Software OTP (Google Authenticator) Is Not Sufficient

### 1.1 The Core Problem: Single-Device Attack Surface

Software TOTP apps like Google Authenticator store the OTP seed secret in software on a general-purpose device (smartphone or PC). This creates several vulnerabilities that SWIFT considers unacceptable for operator access:

**Seed secret extractability.** The TOTP seed is stored in the app's data store or, in some implementations, is exportable. An attacker with device access (physical or remote via malware) can extract the seed and clone the authenticator, breaking the "something you have" factor entirely.

**Malware compromise.** A smartphone or PC running Google Authenticator can be infected with banking trojans, spyware, or RATs that silently read the OTP at the time of entry or extract the seed from app storage. This is a well-documented attack vector (e.g., Cerberus, Anubis malware families specifically target TOTP seeds).

**No tamper resistance.** Unlike hardware tokens (HSMs, dedicated OTP devices), a software app on a general-purpose device provides no tamper-resistant execution environment. The private key/seed is not protected by secure hardware.

**Backup/sync risks.** Google Authenticator (and similar apps) may allow cloud backup of seeds. If a user's Google account is compromised, all TOTP seeds can be accessed, effectively reducing authentication to a single factor (the account password).

**Screen capture and screen-sharing attacks.** A malicious application or remote session can observe the OTP on screen during entry. Hardware tokens display the OTP on a dedicated physical display with no software interface to intercept.

**No binding to the transaction.** Software TOTP provides a time-based code with no cryptographic binding to the specific transaction being authorized. Hardware-based solutions (particularly those using challenge-response or transaction signing) can bind the authentication to a specific payment instruction, preventing man-in-the-browser attacks.

### 1.2 SWIFT's Threat Model

SWIFT's CSP was designed in direct response to the 2016 Bangladesh Bank heist and subsequent attacks where threat actors gained persistent access to SWIFT environments. In many of those incidents, attackers compromised operator credentials. SWIFT concluded that software-only second factors provided insufficient protection because they operate on the same compromised endpoints the attackers controlled.

---

## 2. What CSCF v2025 Control 4.2 Actually Requires

### 2.1 Control Statement

Control 4.2 is a **Mandatory** control titled **"Multi-Factor Authentication"** (the exact title may be "Multi-Factor Authentication for SWIFT-related Accounts" in some versions).

The control objective is to prevent unauthorized access to SWIFT-related systems and data by ensuring that operator accounts are protected by multi-factor authentication where at least one factor is resistant to remote compromise.

### 2.2 Key Requirements Under CSCF v2025

**Scope:** MFA is required for all interactive operator logons to SWIFT components including:
- Alliance Access / Alliance Entry
- Alliance Web Platform (AWP)
- Alliance Gateway
- Any GUI or administrative interface to SWIFT messaging infrastructure

**Mandatory MFA factors:** CSCF v2025 requires that the authentication mechanism include at least two of the following factor types:
1. Something you know (password/PIN)
2. Something you have — but specifically a **hardware-based token or device certificate stored in hardware** (e.g., smart card, hardware OTP token, FIDO2/WebAuthn hardware key)
3. Something you are (biometric, though rare in SWIFT implementations)

**Hardware requirement specifics:** The "something you have" factor must be implemented using one of the following categories to be considered compliant:

- **Dedicated hardware OTP tokens** — Physical devices (e.g., SafeNet/Thales OTP tokens, RSA SecurID hardware tokens, Gemalto tokens) that generate TOTP or HOTP codes using a seed stored in tamper-resistant hardware. The seed cannot be extracted from the device.
- **Smart cards / PKI certificates stored in hardware** — X.509 certificates stored on a smart card or hardware security module that require physical possession of the card and a PIN. Used with Alliance Access's PKI authentication capability.
- **FIDO2/WebAuthn hardware authenticators** — Physical security keys (e.g., YubiKey, Feitian) that use public-key cryptography with the private key stored in secure hardware. SWIFT began recognizing FIDO2 hardware keys in more recent CSCF versions as they provide equivalent or stronger guarantees than dedicated OTP tokens.

**What is explicitly NOT acceptable under CSCF v2025:**
- Software TOTP applications (Google Authenticator, Microsoft Authenticator, Authy) — seed is in software
- SMS OTP — not hardware-bound and subject to SIM swap attacks
- Email OTP — not hardware-bound
- Push notification approval on smartphones without hardware-backed key storage confirmation (i.e., app-based push without a certified hardware-backed secure enclave implementation recognized by SWIFT)
- Software certificates stored on general-purpose file systems (not hardware-bound)

### 2.3 Applicability Tiers

Under the Architecture Type classifications in CSCF:
- **A1, A2, A3, B** architecture types are all in scope for Control 4.2
- Control 4.2 is **mandatory** (not advisory) across all architecture types
- There is no partial credit or compensating control option for software TOTP — SWIFT assessors must mark this non-compliant

### 2.4 Evidence Requirements

During attestation you will need to demonstrate:
1. An inventory of all operator accounts with access to SWIFT components
2. Documentation confirming hardware token assignment per operator
3. System configuration showing hardware MFA is enforced (not optional) at logon
4. Policy documentation mandating hardware MFA for SWIFT operators

---

## 3. Remediation Options Before July Attestation

Given your deadline, you have three primary paths. Each is assessed for speed of implementation, cost, and compatibility with Alliance Access.

### Option 1: Dedicated Hardware OTP Tokens (RSA SecurID, Thales SafeNet)

**How it works:** Replace Google Authenticator with physical OTP token devices. Each operator receives a physical keyfob or credit-card-format token that generates 6- or 8-digit TOTP/HOTP codes every 30–60 seconds. The seed is burned into tamper-resistant hardware at manufacture and cannot be extracted.

**Products to evaluate:**
- **Thales (formerly SafeNet) OTP tokens** — SafeNet eToken PASS, SafeNet OTP 110. Widely used in SWIFT environments. Thales also provides a companion authentication server (STA — SafeNet Trusted Access).
- **RSA SecurID hardware tokens** — The classic enterprise standard. Requires RSA Authentication Manager server infrastructure.
- **VASCO/OneSpan Digipass tokens** — Well-regarded alternative, common in financial services.

**Alliance Access integration:** Alliance Access supports RADIUS-based authentication. You can configure Alliance Access to authenticate operators via a RADIUS server that fronts your OTP token infrastructure (RSA Authentication Manager, Thales STA, FreeRADIUS with OATH module). The logon screen accepts the PIN+OTP combination.

**Timeline:** Token hardware procurement typically takes 2–4 weeks. Server infrastructure setup adds 2–4 weeks. Operator enrollment and testing: 1–2 weeks. Total: 5–10 weeks. This is feasible for a July deadline if you begin immediately.

**Cost:** Hardware tokens cost approximately $30–$80 per token (3-year lifecycle). Authentication server licensing varies; cloud-hosted options (Thales STA cloud, RSA Cloud Authentication) reduce infrastructure burden.

**Pros:** Well-understood by SWIFT assessors; clearly compliant; no smartphone dependency; dedicated device cannot run malware.

**Cons:** Physical token management overhead (lost tokens, battery replacement every 3–5 years, provisioning); higher per-user cost than software; operators must carry another device.

### Option 2: Smart Card / PKI Authentication

**How it works:** Issue each operator a smart card containing their X.509 private key in hardware. Alliance Access (and the underlying Windows/RHEL workstation) performs certificate-based authentication. The card requires physical possession plus a PIN.

**Products to evaluate:**
- Thales IDPrime smart cards
- HID Global smart cards
- Gemalto/Thales IDGo series

**Alliance Access integration:** Alliance Access supports smart card logon through the host operating system's PKI infrastructure (Windows CNG/CAPI or equivalent). You need a PKI CA (internal or outsourced), smart card readers at each operator workstation, and middleware (ActivClient, SafeNet Authentication Client).

**Timeline:** This is typically the longest path — PKI establishment, certificate issuance, workstation configuration, and testing typically take 8–16 weeks minimum. Likely too slow for July unless you already have a PKI in place.

**Cost:** Smart cards ~$15–$30 each; readers ~$30–$60 each; PKI infrastructure (cloud CA like Sectigo or DigiCert is faster than building internal CA); middleware licensing.

**Recommendation for July deadline:** Only viable if you already have an operational PKI. Otherwise, prioritize Option 1 or 3.

### Option 3: FIDO2 Hardware Security Keys

**How it works:** Physical USB/NFC security keys (YubiKey, Feitian, etc.) using FIDO2/WebAuthn. Private key is generated inside the hardware and never leaves the device. Authentication is phishing-resistant and cryptographically bound.

**Products to evaluate:**
- **YubiKey 5 series** (USB-A, USB-C, NFC variants) — most widely recognized; FIPS 140-2 Level 2 certified versions available
- **Feitian ePass FIDO** — cost-effective alternative

**Alliance Access integration:** FIDO2 native support depends on Alliance Access version. As of recent SWIFT software versions, direct FIDO2 integration may require additional middleware or an identity provider (IdP) layer. Options:
- Use a FIDO2-capable IdP (Azure AD/Entra ID with FIDO2 hardware key policy, Okta with FIDO2) that fronts Alliance Access via SAML/OIDC if your version supports it
- Use YubiKey's OTP applet (generates HOTP/TOTP) via the existing RADIUS path — this sacrifices some FIDO2 benefits but uses hardware-backed generation

**Timeline:** If using FIDO2 OTP mode over existing RADIUS infrastructure, this is fast — comparable to Option 1. If implementing a full FIDO2/IdP integration, add 4–6 weeks for IdP configuration and testing.

**Cost:** YubiKey 5 NFC is approximately $50–$55 per key; YubiKey 5 FIPS approximately $80–$90. No ongoing authentication server licensing for the FIDO2 protocol itself (though IdP licensing may apply).

**Pros:** Strong phishing resistance; no server-side shared secrets (no OTP seed database to protect); increasingly recognized by SWIFT assessors; modern standard.

**Cons:** Newer to some SWIFT assessors — ensure your assessor explicitly accepts FIDO2 hardware keys as compliant before committing. Confirm with your SWIFT-appointed assessor that your planned implementation satisfies their evidentiary requirements under Control 4.2.

---

## 4. Recommended Remediation Path for July Deadline

Given the July attestation deadline (approximately 10 weeks from today, April 25, 2026), the recommended path is:

**Primary recommendation: Thales SafeNet OTP hardware tokens with Thales STA cloud authentication service.**

Rationale:
- Fastest to implement of the three options if you have no existing MFA infrastructure
- Cloud-hosted STA eliminates on-premises server build time
- RADIUS integration with Alliance Access is well-documented and straightforward
- Thales hardware tokens are universally accepted by SWIFT assessors without ambiguity
- STA cloud can be provisioned in days; token hardware ships within 2–4 weeks

**Implementation sequence:**
1. **Week 1:** Procure Thales SafeNet OTP tokens (order immediately to account for shipping); initiate STA cloud trial/contract
2. **Week 1–2:** Configure STA cloud tenant; set up RADIUS agent on Alliance Access host or DMZ
3. **Week 2–3:** Configure Alliance Access to require RADIUS MFA at operator logon; test with pilot user
4. **Week 3–4:** Enroll all operators; conduct training on token use
5. **Week 4–5:** Run parallel operation period; validate all operators can authenticate successfully
6. **Week 5–6:** Disable Google Authenticator; enforce hardware token only
7. **Week 6–7:** Collect attestation evidence: operator enrollment list, system config screenshots, policy update, token assignment records
8. **Week 7–8:** Internal review and assessor pre-check if possible

**Secondary recommendation: FIDO2 hardware keys (YubiKey FIPS) if you have Azure AD/Entra ID or Okta already licensed.**

Only pursue this if: (a) you have an IdP that supports FIDO2 and can front Alliance Access, (b) your assessor has confirmed FIDO2 hardware keys are acceptable evidence, and (c) you can complete IdP integration and testing within the timeline.

---

## 5. Policy and Documentation Updates Required

Regardless of which hardware solution you choose, update the following before attestation:

1. **MFA Policy:** Update your SWIFT security policy to specify that hardware-based MFA is mandatory for all Alliance Access operator accounts. Remove any reference to software OTP as an acceptable method.

2. **Operator Joiner/Mover/Leaver procedure:** Document how hardware tokens are issued at onboarding, reassigned on role change, and revoked/recovered at offboarding.

3. **Token management procedure:** Document lost token process, temporary access procedure (if any — must still be MFA-compliant), and token lifecycle (replacement schedule).

4. **System configuration evidence:** Export/screenshot Alliance Access authentication configuration showing RADIUS MFA requirement is enforced, not optional.

5. **Risk register:** Close the open finding and document the remediation action, implementation date, and responsible owner.

---

## 6. Common Assessor Questions to Prepare For

- "How do you ensure the hardware token cannot be bypassed?" — Show that Alliance Access authentication configuration makes MFA mandatory at the system level, not just by policy.
- "What is your process if an operator loses their token?" — Have a documented emergency access procedure that still involves MFA (e.g., temporary token issued by security team under dual authorization).
- "Are any service accounts excluded from MFA?" — CSCF 4.2 focuses on interactive human operator sessions. Automated/service account access via APIs has different controls, but be prepared to explain scope boundaries.
- "How do you protect the authentication server?" — The RADIUS/OTP server or STA cloud service itself is part of your SWIFT security perimeter. Confirm it meets access control and availability requirements.

---

## Summary Table

| Criterion | Google Authenticator (Current) | Hardware OTP Token | Smart Card/PKI | FIDO2 Hardware Key |
|---|---|---|---|---|
| CSCF v2025 Compliant | No | Yes | Yes | Yes (confirm with assessor) |
| Seed/key in hardware | No | Yes | Yes | Yes |
| Feasible for July | N/A | Yes | Only if PKI exists | Yes (if IdP exists) |
| Approx. cost per user | Free | $50–$100 | $50–$100 + PKI | $50–$90 |
| Implementation complexity | N/A | Low–Medium | High | Medium |
| Assessor acceptance | Rejected | Universal | Universal | Confirm first |

**Bottom line:** Begin hardware token procurement this week. The finding is clear-cut and the remediation path is well-established. With focused effort, a July attestation is achievable.
