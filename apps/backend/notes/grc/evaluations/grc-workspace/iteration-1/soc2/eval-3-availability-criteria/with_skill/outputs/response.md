# Adding Availability (A1) to Your SOC 2 Report

## Overview

The **Availability** Trust Services Criterion sits under the **A-series** (specifically **A1**) in the AICPA 2017 Trust Services Criteria. Unlike the Security Common Criteria (CC1–CC9), which are always required, Availability is an **optional, additive criterion** — but when a customer requests it, you must demonstrate controls specifically focused on system uptime, performance, and recovery.

Adding Availability to your scope does **not** replace or reduce Security requirements. It extends your report with an additional set of controls and commitments layered on top of everything you already do for CC1–CC9.

---

## What A1 (Availability) Actually Requires

The Availability criterion is organized around **A1.1, A1.2, and A1.3**:

### A1.1 — Current Processing Capacity
The entity maintains, monitors, and evaluates current processing capacity and use of system components to manage capacity demands.

**What this means in practice:**
- You must monitor resource utilization (CPU, memory, storage, network throughput) on your production systems
- You must have processes to detect when capacity is approaching limits and respond proactively
- Capacity planning is documented and reviewed (at minimum annually, or when significant changes occur)

**Specific controls needed:**
- [ ] **Infrastructure monitoring** with capacity dashboards (AWS CloudWatch, Datadog, etc.)
- [ ] **Alerting on capacity thresholds** — alerts fire before systems reach saturation
- [ ] **Capacity planning documentation** — annual or change-driven review of expected load vs. available capacity
- [ ] **Auto-scaling configurations** documented and tested (AWS Auto Scaling Groups, ECS capacity providers, etc.)
- [ ] **Performance baselines** established and documented

### A1.2 — Environmental Threats and Recovery
The entity authorizes, designs, develops or acquires, implements, operates, approves, maintains, and monitors environmental protection, software, data back-up and recovery, and other policies, procedures, and tools to prevent, detect, and recover from natural disasters, environmental hazards, and other threats.

**What this means in practice:**
- Documented Disaster Recovery (DR) and Business Continuity Plan (BCP)
- Defined RTO (Recovery Time Objective) and RPO (Recovery Point Objective) for each critical service
- Backup procedures with tested restorability
- Tested recovery procedures

**Specific controls needed:**
- [ ] **Disaster Recovery Plan (DRP)** documented and approved — covers key failure scenarios
- [ ] **RTO and RPO defined** for all in-scope services (e.g., RTO: 4 hours, RPO: 1 hour)
- [ ] **Backup procedures** documented — what is backed up, how often, where, and retention period
- [ ] **Backup restoration tested** at least annually — test results documented
- [ ] **Multi-AZ or multi-region architecture** documented (demonstrates resilience by design)
- [ ] **DR test results** documented showing the system can actually recover within stated RTO/RPO
- [ ] **Runbooks for common failure scenarios** (database failure, application server failure, dependency outage)

### A1.3 — Recovery Plan Testing
The entity tests recovery plan procedures supporting system availability to address identified threats against the availability commitments.

**What this means in practice:**
- The DR/BCP plan is not just written — it has been exercised
- Testing results are documented and gaps are remediated
- Testing frequency matches risk level (at minimum annual)

**Specific controls needed:**
- [ ] **Annual DR test** conducted — can be tabletop, functional (failover test), or full DR simulation
- [ ] **Test results documented** including: date, participants, scenarios tested, outcomes, findings
- [ ] **Remediation of test findings** tracked and resolved
- [ ] **Uptime/availability reporting** — evidence of actual uptime metrics over the audit period

---

## How Availability Differs from Security (CC)

This is a critical distinction:

| Dimension | Security (CC1–CC9) | Availability (A1) |
|---|---|---|
| **Primary focus** | Protecting the system from threats | Ensuring the system is operational when promised |
| **Key questions** | Who can access what? Are threats detected? | Is the system up? Can it recover? What's the SLA? |
| **Core controls** | Access management, incident response, change management | Capacity monitoring, DR/BCP, backup, uptime metrics |
| **Overlap** | CC7 includes incident response and some DR elements | A1 goes deeper on recovery objectives, capacity, and SLA evidence |
| **Customer concern** | "Is my data safe?" | "Will your service be there when I need it?" |
| **Evidence type** | Access logs, IR tickets, change records | Uptime reports, backup restore tests, DR test results, SLA compliance data |

**Important nuance:** CC7 (System Operations) in the Security criteria covers incident response and some DR topics. When you add Availability, auditors expect more rigor and specificity:
- CC7 asks: "Do you have an incident response process?"
- A1 asks: "What are your specific uptime commitments, how do you measure them, and can you prove you met them?"

---

## New Commitments You Must Make

When you include Availability, your audit report will describe your **System Description** — a section where you formally state your availability commitments and system requirements. This typically includes:

1. **Uptime targets** — e.g., "99.9% availability measured monthly, excluding scheduled maintenance"
2. **Scheduled maintenance windows** — documented and communicated to customers
3. **Notification procedures** — how you communicate unplanned downtime to customers
4. **Monitoring approach** — how you detect and measure availability
5. **Recovery objectives** — stated RTO and RPO

**Warning:** Whatever you commit to in the System Description, auditors will test. If you say "99.9% uptime" but your monitoring shows multiple outages exceeding that, it will be flagged. Be accurate and realistic in your commitments.

---

## Additional Policies Required for A1

Beyond the full Security policy set (CC1–CC9), Availability scope requires:

| Policy / Document | Criteria Addressed |
|---|---|
| **Business Continuity & Disaster Recovery Policy** | A1, CC7 |
| **System Availability Statement** (in System Description) | A1.1 |
| **Incident Communication Policy** (for availability incidents) | A1, CC2 |
| **Backup and Recovery Procedures** | A1.2 |

Your **DR/BCP policy** must specifically define:
- Scope of covered systems
- RTO and RPO targets
- Roles and responsibilities during an incident
- Recovery procedures
- Test requirements and cadence

---

## Evidence Required for A1 (Audit Period)

For a Type 2 audit covering Availability, you need:

| Evidence Item | Purpose |
|---|---|
| Uptime/availability dashboards or reports (e.g., monthly exports from your monitoring tool) | Proves A1.1 — capacity monitored; demonstrates you met stated availability commitments |
| Capacity utilization reports | Proves A1.1 — capacity monitoring in operation |
| Auto-scaling configurations | Proves A1.1 — capacity demand management |
| Backup configuration screenshots + backup logs | Proves A1.2 — backups running as designed |
| Backup restoration test record (date, system, result) | Proves A1.2 — restorability tested |
| DR plan document with approval signature and version date | Proves A1.2, A1.3 — plan exists and is maintained |
| DR test report (date, scenario, participants, outcome) | Proves A1.3 — plan has been tested |
| Incident records for any availability incidents during the audit period | Supports A1 review — shows how incidents were handled |
| RTO/RPO documented and signed off | Supports A1.2 — objectives are defined |

---

## Scoping Decision: Should You Add Availability?

Consider adding Availability if:
- Your customer's request specifically mentions availability or uptime SLAs
- You have an SLA in your customer contracts and the customer wants audit evidence
- Downtime would cause significant harm to your customers (financial services, healthcare, etc.)
- You already have mature monitoring and DR capabilities

Consider deferring Availability if:
- Your initial SOC 2 is already a significant lift and Availability adds material scope
- Your customer only needs Security coverage
- Your DR/backup processes are not yet mature enough to withstand scrutiny

Most first-time SOC 2 reports are **Security-only**. Adding Availability on a first audit is feasible but substantially increases the evidence collection burden, particularly around DR testing and uptime reporting over the full audit period.

---

## Summary

| | Security Only (CC1–CC9) | Security + Availability (CC1–CC9 + A1) |
|---|---|---|
| Additional controls | — | Capacity monitoring, DR/BCP, backup restore testing, DR testing |
| Additional policies | — | DR/BCP Policy, Availability Statement |
| Key new evidence | — | Uptime reports, DR test results, backup restore records, capacity dashboards |
| Additional commitments | — | Stated RTO/RPO, uptime targets, maintenance window definitions |
| Audit complexity | Baseline | Moderate increase — particularly around DR testing and uptime evidence |
