# System Boundary Description — FedRAMP Moderate SSP

*Note: This content should be inserted into the appropriate section of the official FedRAMP SSP template, available at fedramp.gov.*

---

## System Boundary Description

### Overview

The [System Name] Cloud Service Offering (CSO) is a web application hosted on AWS GovCloud (US) that provides [describe purpose] to federal agency customers. The authorization boundary encompasses all components that process, store, or transmit federal information.

The system is logically segmented within a Virtual Private Cloud (VPC) in AWS GovCloud and is designed with defense-in-depth principles. The authorization boundary includes all AWS GovCloud resources owned and managed by [CSP Name], as well as the two external SaaS integrations described below.

---

### Components Within the Authorization Boundary

#### Web Application Layer

The primary user interface and application logic are delivered through a web application hosted on AWS GovCloud compute resources (EC2 instances or containerized workloads). The application is accessible to end users over the public internet via HTTPS. An AWS Application Load Balancer (ALB) distributes incoming traffic across application instances in the private subnet.

**Key components:**
- AWS Application Load Balancer (public-facing, HTTPS only)
- Web application servers (EC2 or ECS, private subnet)
- AWS WAF for web application firewall protection

All web traffic is encrypted in transit using TLS 1.2 or higher.

#### Database Layer (PostgreSQL)

Federal data is persisted in an Amazon RDS for PostgreSQL instance located in a private subnet with no direct internet connectivity. The database is encrypted at rest using AWS Key Management Service (KMS). Automated backups are enabled and stored in encrypted S3 buckets within AWS GovCloud.

**Key components:**
- Amazon RDS for PostgreSQL (primary instance, private subnet)
- Amazon RDS for PostgreSQL (read replica or standby, for availability)
- AWS KMS for encryption key management
- S3 (GovCloud) for encrypted backup storage

#### Caching Layer (Redis)

An Amazon ElastiCache Redis cluster is deployed in the private subnet and used for application-level caching and session management. The cache does not serve as the authoritative store for federal data but may transiently hold federal information during active user sessions.

**Key components:**
- Amazon ElastiCache for Redis (private subnet)
- Encryption in transit (TLS) and at rest enabled

#### Security and Monitoring Infrastructure

| Component | Purpose |
|---|---|
| AWS CloudTrail | Audit logging for all API activity |
| Amazon GuardDuty | Threat detection |
| AWS Security Hub | Centralized security findings |
| AWS Config | Configuration compliance monitoring |
| VPC Flow Logs | Network traffic logging |

---

### External Services Outside the Authorization Boundary

Two external SaaS services integrate with the [System Name] boundary. These services are **outside the authorization boundary** and require documentation of the data exchanged, the security controls in place, and approval from the Authorizing Official (AO).

#### Okta (Authentication)

| Attribute | Details |
|---|---|
| **Purpose** | Federated single sign-on (SSO) and multi-factor authentication (MFA) |
| **Data exchanged** | User identity tokens (SAML/OIDC); no federal payload data |
| **Location** | External SaaS — outside the boundary |
| **FedRAMP status** | Check FedRAMP Marketplace for current authorization status |
| **Risk** | Low — only identity assertions exchanged, not federal content |

If Okta holds an active FedRAMP authorization at the appropriate impact level, document this as a leveraged authorization in the SSP.

#### SendGrid (Email Delivery)

| Attribute | Details |
|---|---|
| **Purpose** | Transactional email notifications |
| **Data exchanged** | Email content, which may include user-specific data or system notifications |
| **Location** | External SaaS — outside the boundary |
| **FedRAMP status** | Check FedRAMP Marketplace for current authorization status |
| **Risk** | Medium to High — depending on the content of email notifications, federal data may leave the boundary |

**Recommendation**: Minimize the inclusion of federal data in email content. If federal data must be included in emails, ensure SendGrid holds an appropriate FedRAMP authorization or obtain AO approval with documented compensating controls. Consider using AWS SES (GovCloud) as an alternative to keep email delivery within the authorization boundary.

---

### What Is Outside the Boundary

The following are explicitly excluded from the authorization boundary:
- Agency end-user devices and workstations
- Agency network infrastructure
- Okta's underlying SaaS infrastructure
- SendGrid's underlying SaaS infrastructure
- AWS commercial (non-GovCloud) services
- Any services hosted outside of AWS GovCloud

---

### Boundary Diagram

*[INSERT FIGURE: Authorization Boundary Diagram — showing all in-boundary components within a boundary line, with Okta and SendGrid depicted outside the boundary, all data flows labeled with protocols, and the AWS GovCloud region clearly indicated]*

The boundary diagram must show:
- Public subnet (ALB, WAF)
- Private subnet (web servers, RDS, Redis)
- Management/monitoring components (CloudTrail, GuardDuty)
- External connections to Okta and SendGrid with labeled data flows
- Internet ingress/egress points
- VPC boundary and AWS GovCloud region label
