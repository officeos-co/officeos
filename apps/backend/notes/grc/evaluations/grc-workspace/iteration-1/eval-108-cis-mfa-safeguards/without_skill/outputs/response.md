# CIS Controls v8 — Multi-Factor Authentication (MFA) Safeguards

## 1. Safeguards That Explicitly Require MFA

CIS Controls v8 addresses MFA primarily within **Control 6: Access Control Management**. The two safeguards that directly mandate MFA are:

---

### Safeguard 6.3 — Require MFA for Externally-Exposed Applications

| Attribute | Detail |
|-----------|--------|
| **Safeguard ID** | 6.3 |
| **Full Title** | Require MFA for Externally-Exposed Applications |
| **Implementation Group** | IG1 (foundational — applies to ALL organizations) |
| **Asset Type** | Users |
| **Security Function** | Protect |

**Description:** Require all externally-exposed enterprise or third-party applications to enforce MFA, where supported. Externally-exposed applications include those reachable from the internet, including web-based services, remote access portals, cloud services, SaaS applications, and any internet-facing systems. Because this is an IG1 control, it is considered a basic cyber hygiene requirement that every organization — regardless of size or resources — should implement.

---

### Safeguard 6.4 — Require MFA for Remote Network Access

| Attribute | Detail |
|-----------|--------|
| **Safeguard ID** | 6.4 |
| **Full Title** | Require MFA for Remote Network Access |
| **Implementation Group** | IG1 (foundational — applies to ALL organizations) |
| **Asset Type** | Users |
| **Security Function** | Protect |

**Description:** Require MFA for all remote network access (e.g., VPN, dial-up, and other forms of network access from outside the enterprise perimeter). This safeguard recognizes that remote access is a high-risk entry point and that credential theft alone should not be sufficient for an attacker to gain network-level access. Like 6.3, this is an IG1 requirement.

---

### Safeguard 6.5 — Require MFA for Administrative Access

| Attribute | Detail |
|-----------|--------|
| **Safeguard ID** | 6.5 |
| **Full Title** | Require MFA for Administrative Access |
| **Implementation Group** | IG2 (applies to organizations with moderate IT resources and risk) |
| **Asset Type** | Users |
| **Security Function** | Protect |

**Description:** Require MFA for all administrative access accounts, where supported, on all enterprise assets, whether managed on-site or through a third-party provider. Administrative accounts have elevated privileges, making them a prime target for attackers. This safeguard extends MFA to privileged users and is an IG2 requirement, meaning it is expected of organizations with dedicated security personnel and a higher risk profile.

---

## 2. Summary Table of MFA Safeguards

| Safeguard ID | Title | Implementation Group | Scope |
|---|---|---|---|
| 6.3 | Require MFA for Externally-Exposed Applications | **IG1** | Internet-facing apps, SaaS, cloud portals |
| 6.4 | Require MFA for Remote Network Access | **IG1** | VPN, remote access, dial-up |
| 6.5 | Require MFA for Administrative Access | **IG2** | Admin/privileged accounts on all assets |

---

## 3. Implementation Group Levels Explained

CIS Controls v8 introduced three Implementation Groups (IGs) to help organizations prioritize controls based on their risk profile and resources:

- **IG1 (Basic Cyber Hygiene):** Applies to all organizations. These are the minimum controls every enterprise should have. Safeguards 6.3 and 6.4 fall here, reflecting that MFA for external apps and remote access is non-negotiable regardless of organization size.
- **IG2 (Foundational):** Applies to organizations with dedicated IT/security staff handling sensitive data or providing critical services. Safeguard 6.5 falls here because privileged account MFA requires more mature identity management capabilities.
- **IG3 (Organizational):** Applies to large enterprises or those in critical infrastructure. IG3 organizations implement all safeguards from IG1 and IG2 plus additional advanced controls.

---

## 4. Types of MFA Recommended by CIS Controls

CIS Controls v8 does not prescribe a single MFA method but distinguishes between methods based on their security strength. CIS guidance (including the companion CIS Benchmarks and the CIS Controls Implementation Guide) recommends **phishing-resistant MFA** wherever feasible, and acknowledges that weaker MFA methods like SMS OTP provide some protection but are insufficient against sophisticated threats.

### MFA Methods Referenced and Recommended

**a) Phishing-Resistant MFA (Strongly Recommended)**

These methods are bound to the specific relying party (website/application) and cannot be intercepted or replayed via phishing:

- **FIDO2/WebAuthn hardware security keys** (e.g., YubiKey, Google Titan Key): Uses public-key cryptography. The private key never leaves the device. Authentication is domain-bound, so a phishing site cannot capture a usable credential.
- **Platform authenticators using FIDO2** (e.g., Windows Hello, Apple Face ID/Touch ID with passkeys): Built into the device, cryptographically bound to the origin. No shared secret to steal.
- **PIV/CAC smart cards**: Certificate-based authentication used widely in government contexts (DoD, federal agencies). Cryptographically strong and phishing-resistant.
- **Passkeys**: A modern implementation of FIDO2 credentials, increasingly adopted by major platforms. Phishing-resistant by design.

**b) Time-Based One-Time Passwords (TOTP) / Authenticator Apps (Acceptable, Not Phishing-Resistant)**

- **Examples:** Google Authenticator, Microsoft Authenticator, Authy
- **Mechanism:** Generates a 6–8 digit code that rotates every 30 seconds using a shared secret (TOTP per RFC 6238).
- **Security level:** Stronger than static passwords and SMS, but **not phishing-resistant** — an attacker can use a real-time phishing proxy to capture the TOTP code during login and replay it immediately.

**c) SMS One-Time Passwords (Weakest MFA — Discouraged)**

- **Mechanism:** A numeric code is sent via SMS to the user's registered phone number.
- **Vulnerabilities:** Subject to SIM swapping, SS7 protocol attacks, and real-time phishing interception. NIST SP 800-63B deprecated SMS OTP as a primary authenticator in 2017. CIS guidance aligns with NIST and advises against relying on SMS OTP where better options exist.
- **Use case:** CIS acknowledges SMS OTP may be used where no better option exists (e.g., legacy systems), but it is the least preferred method.

**d) Push Notifications (Acceptable, but Vulnerable to MFA Fatigue)**

- **Examples:** Microsoft Authenticator push, Duo Security push
- **Mechanism:** App sends a push notification; user approves on their phone.
- **Vulnerability:** Susceptible to **MFA fatigue attacks** (adversary-in-the-middle sends repeated push requests until user accidentally approves). Number matching and additional context are mitigations.

---

## 5. SMS OTP vs. Phishing-Resistant MFA in the CIS Controls Context

### SMS OTP (One-Time Password via Text Message)

**How it works:** When a user logs in, the server generates a short numeric code and transmits it via the public SMS network to the user's mobile phone number. The user then types this code into the authentication prompt.

**Weaknesses in the CIS Controls context:**

1. **SIM Swapping:** An attacker socially engineers a mobile carrier into transferring the victim's phone number to a SIM card the attacker controls. All subsequent SMS messages — including MFA codes — are delivered to the attacker.
2. **SS7 Protocol Attacks:** The Signaling System No. 7 (SS7) protocol, which underpins the global telephone network, has well-documented vulnerabilities that allow interception and redirection of SMS messages by well-resourced attackers.
3. **Real-Time Phishing (Adversary-in-the-Middle / AiTM):** A phishing site proxies the login to the real site in real time. The victim enters their password on the phishing site; the attacker simultaneously submits it to the real site, triggering an SMS OTP to the victim. The victim enters the OTP on the phishing site; the attacker relays it to the real site. The attacker achieves full session access.
4. **Malware:** On a compromised device, malware can read incoming SMS messages and exfiltrate the OTP silently.

**CIS Stance:** SMS OTP satisfies the letter of safeguards 6.3, 6.4, and 6.5 (technically it is "MFA") but CIS explicitly recommends stronger alternatives. Organizations relying solely on SMS OTP should treat it as a transitional measure while migrating to stronger methods.

---

### Phishing-Resistant MFA

**How it works:** Phishing-resistant MFA uses **cryptographic binding between the authenticator and the specific origin (domain)**. The most prominent example is FIDO2/WebAuthn:

1. During registration, the authenticator (hardware key or platform) generates a **unique public/private key pair** for the specific relying party (RP) origin.
2. The private key **never leaves the authenticator**.
3. During authentication, the server sends a **challenge**. The authenticator signs it using the private key. Critically, this signing operation is **origin-bound** — the authenticator checks that the origin (domain) in the request matches the registered origin before signing.
4. If a user is phished to `bank-login-secure.evil.com` instead of `bank.com`, the authenticator **refuses to sign** because the origin does not match.

**Why this defeats phishing:**

- Even if an attacker tricks a user into visiting a phishing site, **there is no code to intercept** — the credential is cryptographic and domain-bound.
- There is no shared secret (like an OTP) that can be captured and replayed.
- Real-time AiTM attacks fail because the cryptographic assertion from the authenticator is tied to the legitimate origin, not the attacker's proxy.

**CIS Controls alignment:**

- CIS Controls v8 guidance documents, the CIS Controls Implementation Guide, and the associated CIS Benchmarks increasingly reference FIDO2 and phishing-resistant MFA as the gold standard.
- For IG3 organizations and those in high-risk sectors (financial services, healthcare, critical infrastructure), phishing-resistant MFA is effectively expected.
- CISA's "Phishing-Resistant MFA" guidance (which CIS aligns with) explicitly recommends FIDO2/WebAuthn or PKI-based methods and deprecates SMS OTP.

---

## 6. Practical Recommendations by Implementation Group

| Implementation Group | Recommended MFA Approach |
|---|---|
| **IG1** | At minimum, deploy TOTP authenticator apps for 6.3 and 6.4. Avoid SMS OTP where possible. |
| **IG2** | Deploy TOTP or push MFA with number matching. Begin piloting FIDO2 keys or passkeys for admin accounts (6.5). |
| **IG3** | Require phishing-resistant MFA (FIDO2/WebAuthn, smart cards) for all privileged and high-risk accounts. Migrate user-facing systems to passkeys/FIDO2. |

---

## 7. Conclusion

CIS Controls v8 mandates MFA through three safeguards: **6.3** (external apps, IG1), **6.4** (remote access, IG1), and **6.5** (administrative access, IG2). The controls are deliberately flexible on MFA method, but CIS guidance clearly favors phishing-resistant methods — particularly FIDO2/WebAuthn — over legacy approaches like SMS OTP.

The critical distinction is that SMS OTP introduces an interceptable shared secret into the authentication flow, making it vulnerable to SIM swapping, SS7 attacks, and real-time phishing proxies. Phishing-resistant MFA eliminates the interceptable factor entirely by using origin-bound cryptographic authentication, ensuring that even a fully compromised phishing site cannot capture usable credentials.

Organizations should treat SMS OTP as a deprecated fallback, implement TOTP authenticator apps as a baseline, and progressively move toward FIDO2/WebAuthn passkeys and hardware security keys as their primary authentication mechanism — especially for privileged accounts and externally-exposed systems.

---

*Reference: CIS Controls v8, Center for Internet Security (2021). Safeguard details from Control 6: Access Control Management.*
