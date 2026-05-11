# Adding Availability to Your SOC 2 Report

## What the Availability Criterion Covers

SOC 2's **Availability** category is one of five Trust Services Criteria. While Security is always required, Availability is optional — you add it when customers or contracts require you to demonstrate controls around system uptime and recovery.

The Availability criterion addresses whether your system is available for operation and use as you've committed to your customers. It's fundamentally about uptime, capacity, and disaster recovery.

---

## What Availability Requires (That Security Doesn't)

Security-only SOC 2 focuses on protecting your system from threats. Availability asks a different question: "Can your system stay up, and can you recover when it goes down?"

### Uptime Monitoring and Commitments

When you add Availability, you must:

- **Define your availability commitments** — this goes into your System Description (the narrative section of your SOC 2 report). You need to state something like "99.9% uptime, measured monthly, excluding scheduled maintenance."
- **Actually measure and monitor uptime** — you need a monitoring tool and documented evidence of uptime metrics over the audit period
- **Alert on availability issues** — automated alerting when systems degrade or go down

Warning: Whatever uptime targets you put in the report, auditors will check whether you actually met them. Be accurate and realistic.

### Capacity Management

- Document current infrastructure capacity and utilization
- Monitor resource usage (CPU, memory, storage, bandwidth)
- Have a process for scaling when capacity limits are approached
- Document auto-scaling or capacity expansion procedures

### Disaster Recovery

This is the biggest addition. For Availability, you need:

- **Written DR/Business Continuity Plan** — covers what happens when key systems fail
- **Defined RTO (Recovery Time Objective)** — how quickly can you restore service?
- **Defined RPO (Recovery Point Objective)** — how much data can you afford to lose?
- **Backup procedures** — what is backed up, how often, where stored, how long retained
- **Tested backups** — you must actually verify backups can be restored; document this
- **Annual DR test** — run a test of your recovery procedures and document the results

### Scheduled Maintenance

- Document your maintenance window policy
- Have a process for notifying customers of planned downtime

---

## How It Differs from Security-Only

**Security asks:** Are you protecting your systems and data from threats?
**Availability asks:** Is your system up and running as promised, and can you recover from failures?

There's some overlap — your Security criteria already requires you to have an incident response plan and some monitoring. But Availability takes this further:

- Security's monitoring is about detecting threats
- Availability's monitoring is about measuring uptime and capacity
- Security's incident response is about handling breaches
- Availability's DR/BCP is specifically about restoring service after any type of failure

The main new controls Availability adds are:
1. Uptime SLA monitoring and reporting
2. Capacity planning and monitoring
3. Formal DR/BCP documentation with tested recovery procedures
4. Backup restoration testing (just having backups isn't enough)
5. Documented RTO and RPO targets

---

## New Policies You'll Need

On top of your Security policy set, add:

- **Business Continuity and Disaster Recovery Policy** — the most important addition
- **Backup and Recovery Procedures** — detailed operational procedures
- Update your **Incident Response Policy** to cover availability incidents and customer communication

---

## Evidence Auditors Will Ask For

For a Type 2 report with Availability in scope, collect:

- Uptime/availability reports from your monitoring tool for each month of the audit period
- Capacity dashboards or reports showing utilization trends
- DR/BCP plan document with approval date
- DR test report — date, scope, participants, findings, and whether RTO/RPO were met
- Backup configuration and backup logs showing backups ran
- Backup restoration test record (what was restored, when, did it work)
- Any incidents during the audit period and how they were handled

---

## Should You Add Availability?

**Good reasons to add it:**
- Your customer specifically requested it
- You have SLAs in your contracts and want audit evidence to back them up
- You already have solid monitoring and a tested DR plan

**Reasons to defer it:**
- Your first SOC 2 is already a major project; Security alone is substantial
- Your DR and backup practices aren't yet mature enough to withstand scrutiny
- Your customer hasn't specifically required it

Many first-time SOC 2 organizations start with Security-only. Adding Availability on the second or third cycle is common. That said, if your DR and monitoring practices are already solid, adding Availability is not dramatically more work — the main lift is the DR test documentation and monthly uptime reporting.

---

## Practical Next Steps if Adding Availability

1. Define your uptime SLA targets and document them
2. Set up or confirm uptime monitoring with reportable metrics (Datadog, Pingdom, etc.)
3. Write or update your DR/BCP plan with defined RTO/RPO
4. Test your backups — restore something and document it
5. Schedule an annual DR tabletop or functional test before your audit window ends
6. Build capacity dashboards and export monthly reports for the audit period
