# Cybersecurity Incident Response Plan (CIRP) for TSA Purposes

## What Is a CIRP?

A Cybersecurity Incident Response Plan (CIRP) — sometimes called an Incident Response Plan (IRP) — is a documented set of procedures that defines how an organisation detects, responds to, contains, and recovers from cybersecurity incidents. For organisations subject to TSA Security Directives (pipeline operators, freight railroads, passenger rail and transit agencies), a CIRP is a mandatory compliance requirement.

The CIRP is part of the broader Cyber Risk Management Program (CRMP) that TSA requires covered critical infrastructure operators to establish and maintain.

---

## Why TSA Requires a CIRP

TSA's pipeline and surface transportation Security Directives, issued starting in 2021, require covered operators to have formal incident response capabilities because:

- Cyberattacks on operational technology (OT) systems — like SCADA and industrial control systems — can cause physical safety consequences and service disruptions
- Without a pre-planned response, organisations typically respond more slowly and less effectively
- TSA and CISA need to be notified promptly when incidents occur so they can provide assistance and assess sector-wide risk

---

## What a TSA-Aligned CIRP Must Include

While the precise requirements vary by the specific Security Directive applicable to your organisation, a TSA-compliant CIRP generally must address:

### Core Elements

**1. Scope and Applicability**
Defines which systems are covered — primarily "Critical Cyber Systems" (OT, ICS, SCADA, and connected IT systems) — and under what circumstances the plan is activated.

**2. Roles and Responsibilities**
- Incident Response Team structure
- Cybersecurity Coordinator role (required by TSA — this person is your primary contact with TSA and CISA)
- Leadership escalation paths
- Backup personnel to ensure 24/7 response capability

**3. Incident Detection and Classification**
- How cybersecurity events are identified
- Criteria for classifying severity levels
- Escalation triggers (what turns an "event" into a declared "incident")

**4. Containment Procedures**
Steps to stop the spread of an incident, including:
- Isolating affected systems
- Separating IT from OT networks under emergency conditions
- Disabling compromised accounts and access paths

**5. Eradication and Recovery**
- Removing malware or unauthorised access
- Restoring systems from clean backups
- Verifying system integrity before returning to operations

**6. Regulatory Notification Requirements**
- Procedures for notifying CISA within the required timeframe (generally understood to be within 24 hours for significant incidents)
- TSA notification procedures
- Internal escalation to executive leadership

**7. Post-Incident Review**
Process for conducting a lessons-learned review after each significant incident, and for updating the CIRP based on findings.

**8. Vendor and Third-Party Coordination**
How you engage OT/ICS vendors, IT vendors, and third-party incident response firms during an incident.

---

## Testing Requirements

TSA Security Directives require that covered operators periodically test their incident response plans. The purpose of testing is to verify that procedures actually work before a real incident occurs.

### How Often?
Testing is generally required at least annually. TSA's directives typically require testing of specific objectives — organisations need to test enough of the plan to demonstrate it is functional.

### What Types of Testing Count?
- **Tabletop exercises**: Discussion-based simulations where participants walk through how they would respond to a scenario
- **Functional exercises**: Actually executing specific procedures, such as isolating OT from IT or restoring from backup
- **Full-scale exercises**: Comprehensive simulations involving multiple teams

### What Should Be Tested?
Common test objectives include:
- Ability to isolate OT systems from IT systems in an emergency
- Backup data restoration capability
- Escalation and notification procedures (including ability to reach CISA and TSA)
- Vendor coordination procedures

### Documentation
Evidence of testing must be retained and may be reviewed by TSA. Documentation should include the date, scenario, participants, what was tested, what gaps were found, and what corrective actions were taken.

---

## Relationship to Other CRMP Requirements

The CIRP does not stand alone — it is one component of a larger Cyber Risk Management Program that also includes:
- A Cybersecurity Implementation Plan (CIP) — the master compliance document submitted to TSA for approval
- Network architecture reviews
- Cybersecurity assessments

The CIRP must be consistent with and integrated into the CIP.

---

## Key Takeaway on Reporting

One of the most important aspects of TSA incident response requirements is the regulatory reporting obligation. When a significant cybersecurity incident occurs affecting critical cyber systems, you must notify CISA promptly — typically within 24 hours. This is a mandatory obligation separate from your internal response process. You should not wait for your investigation to conclude before reporting; initial reports can be updated as more information becomes available.
