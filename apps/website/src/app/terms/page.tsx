import { Navbar } from "@/components/sections/navbar";
import { FooterSection } from "@/components/sections/footer-section";

export const metadata = {
  title: "Terms of Service — OfficeOS",
  description: "Terms and conditions for using the OfficeOS platform.",
};

export default function Terms() {
  return (
    <div className="relative mx-auto max-w-7xl border-x">
      <div className="absolute top-0 left-6 z-10 block h-full w-px border-border border-l" />
      <div className="absolute top-0 right-6 z-10 block h-full w-px border-border border-r" />
      <Navbar />

      <main className="mx-auto max-w-4xl px-6 pt-20 pb-28 md:pt-28">
        <h1 className="text-4xl font-bold tracking-tight text-center md:text-5xl">
          Terms of Service
        </h1>
        <p className="mt-4 text-sm text-muted-foreground text-center">
          Last updated: April 15, 2026
        </p>

        <div className="mt-16 space-y-12 text-muted-foreground leading-relaxed">
          {/* Intro */}
          <section>
            <p>
              These Terms of Service (&quot;Terms&quot;) govern your use of the
              OfficeOS platform operated by Harro Krog (Einzelunternehmen,
              Hamburg, Germany) (&quot;we&quot;, &quot;us&quot;,
              &quot;our&quot;). By creating an account or using the platform,
              you agree to these Terms in their entirety.
            </p>
          </section>

          {/* Service Description */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              1. Service Description
            </h2>
            <p>
              OfficeOS is a platform for deploying and managing personalized AI
              agents. The platform enables you to create agents, assign skills,
              manage credentials, and execute tasks through a central dashboard.
              Agents run as isolated containers and interact with third-party
              services through skills and LLM providers you configure.
            </p>
          </section>

          {/* User Responsibility */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              2. User Responsibility
            </h2>
            <p>
              You are responsible for reviewing and validating all agent outputs
              before acting on them. Agents operate autonomously and may produce
              incorrect, incomplete, or unexpected results.{" "}
              <strong className="text-primary">
                You acknowledge that you are solely responsible for any
                decisions, actions, or consequences resulting from agent
                outputs, including any damages to third parties.
              </strong>
            </p>
            <p className="mt-3">
              You are responsible for the credentials you provide to the
              platform and for ensuring that your use of third-party services
              through agents complies with the respective service terms. You
              must not configure agents to perform actions that would violate
              any applicable law or the rights of any third party.
            </p>
          </section>

          {/* Assumption of Risk */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              3. Assumption of Risk
            </h2>
            <p>
              You acknowledge and accept that autonomous AI agents carry
              inherent risks, including but not limited to: producing inaccurate
              or misleading information, executing unintended actions through
              configured skills, interacting with third-party services in
              unexpected ways, and generating content that may be inappropriate
              or incorrect.
            </p>
            <p className="mt-3">
              <strong className="text-primary">
                By using the platform, you voluntarily assume all risks
                associated with the deployment and operation of AI agents.
              </strong>{" "}
              You agree to implement appropriate human oversight, review
              processes, and safeguards for your use case.
            </p>
          </section>

          {/* Disclaimer of Warranties */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              4. Disclaimer of Warranties
            </h2>
            <p>
              <strong className="text-primary">
                The platform, all agent outputs, and all actions performed by
                agents are provided on an &quot;as is&quot; and &quot;as
                available&quot; basis without warranties of any kind, either
                express or implied.
              </strong>{" "}
              We expressly disclaim all warranties, including but not limited to
              implied warranties of merchantability, fitness for a particular
              purpose, accuracy, reliability, non-infringement, and any
              warranties arising from course of dealing or usage of trade.
            </p>
            <p className="mt-3">
              We do not warrant that: (a) the platform will be uninterrupted,
              error-free, or secure; (b) agent outputs will be accurate,
              complete, or reliable; (c) agents will perform as intended in all
              circumstances; or (d) any defects will be corrected.
            </p>
          </section>

          {/* Limitation of Liability */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              5. Limitation of Liability
            </h2>
            <p>
              To the maximum extent permitted by applicable law, we shall not be
              liable for any indirect, incidental, special, consequential, or
              punitive damages arising from your use of the platform, including
              but not limited to damages caused by agent actions, skill
              executions, LLM outputs, data loss, or service interruptions,
              regardless of whether we were advised of the possibility of such
              damages.
            </p>
            <p className="mt-3">
              <strong className="text-primary">
                Our total aggregate liability for any and all claims arising
                from or related to these Terms or your use of the platform shall
                not exceed the total amount you paid to us in the six (6) months
                preceding the event giving rise to the claim, or one hundred
                euros (100 EUR), whichever is greater.
              </strong>
            </p>
            <p className="mt-3">
              Nothing in these Terms excludes or limits our liability for: (a)
              death or personal injury caused by our negligence; (b) fraud or
              fraudulent misrepresentation; or (c) any liability that cannot be
              excluded or limited under applicable law.
            </p>
          </section>

          {/* Indemnification */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              6. Indemnification
            </h2>
            <p>
              You agree to indemnify, defend, and hold harmless OfficeOS, its
              operator, affiliates, and service providers from and against any
              and all claims, damages, losses, liabilities, costs, and expenses
              (including reasonable legal fees) arising from or related to:
            </p>
            <ul className="mt-3 space-y-2 list-disc pl-5">
              <li>Your use of the platform or any agent outputs.</li>
              <li>
                Actions taken by agents you have configured or deployed,
                including any harm caused to third parties.
              </li>
              <li>
                Your violation of these Terms, any applicable law, or any
                third-party rights.
              </li>
              <li>
                Content, data, or credentials you provide to the platform.
              </li>
              <li>
                Any third-party claims resulting from your agents&apos;
                interactions with external services.
              </li>
            </ul>
          </section>

          {/* Third-Party Services */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              7. Third-Party Services
            </h2>
            <p>
              The platform integrates with third-party LLM providers, APIs, and
              services as configured by you. We make no representations or
              warranties regarding the availability, accuracy, or reliability of
              any third-party service.{" "}
              <strong className="text-primary">
                Your use of third-party services through the platform is at your
                own risk and subject to the terms and conditions of those third
                parties.
              </strong>
            </p>
            <p className="mt-3">
              We are not liable for any damages, data loss, or other harm
              arising from the actions of third-party services, even when
              accessed through agents on our platform.
            </p>
          </section>

          {/* Acceptable Use */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              8. Acceptable Use
            </h2>
            <p>You agree not to use OfficeOS to:</p>
            <ul className="mt-3 space-y-2 list-disc pl-5">
              <li>
                Engage in any illegal activity or violate applicable laws and
                regulations.
              </li>
              <li>
                Deploy agents that perform malicious actions, including
                unauthorized access to systems, data exfiltration, or
                distribution of malware.
              </li>
              <li>
                Generate or distribute harmful, abusive, or deceptive content.
              </li>
              <li>
                Circumvent platform security controls, rate limits, or access
                restrictions.
              </li>
              <li>
                Use the platform in a way that could harm other users or degrade
                service performance.
              </li>
              <li>
                Process personal data of third parties without a lawful basis
                under applicable data protection laws.
              </li>
            </ul>
          </section>

          {/* Account Termination */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              9. Account Termination
            </h2>
            <p>
              We reserve the right to suspend or terminate your account at any
              time if we reasonably believe you have violated these Terms or our
              Acceptable Use policy. We will make reasonable efforts to notify
              you before termination unless the violation poses an immediate
              risk. Upon termination, your agent data will be retained for 30
              days to allow for data export, then permanently deleted.
            </p>
          </section>

          {/* Enterprise SLAs */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              10. Enterprise Agreements
            </h2>
            <p>
              Enterprise customers may negotiate separate Service Level
              Agreements (SLAs) with specific uptime guarantees, support
              commitments, and custom terms. Enterprise SLAs, where applicable,
              take precedence over these public Terms of Service.
            </p>
            <p className="mt-3">
              A Data Processing Agreement (DPA) is available for enterprise
              customers upon request, covering GDPR-compliant data processing,
              sub-processor management, and data transfer safeguards.
            </p>
          </section>

          {/* Intellectual Property */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              11. Intellectual Property
            </h2>
            <p>
              You retain full ownership of your agent configurations, custom
              skills, workspace data, and any content generated through your use
              of the platform. We claim no intellectual property rights over
              your data or agent outputs.
            </p>
          </section>

          {/* Force Majeure */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              12. Force Majeure
            </h2>
            <p>
              We shall not be liable for any failure or delay in performing our
              obligations under these Terms where such failure or delay results
              from circumstances beyond our reasonable control, including but
              not limited to: natural disasters, acts of government, internet or
              infrastructure failures, third-party service outages,
              cyberattacks, or changes in law or regulation.
            </p>
          </section>

          {/* Changes to Terms */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              13. Changes to Terms
            </h2>
            <p>
              We may update these Terms from time to time. Material changes will
              be communicated via email or through the platform dashboard at
              least 30 days before they take effect. Continued use of the
              platform after the effective date constitutes acceptance of the
              updated Terms.
            </p>
          </section>

          {/* Severability */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              14. Severability and Entire Agreement
            </h2>
            <p>
              If any provision of these Terms is found to be invalid or
              unenforceable, the remaining provisions shall continue in full
              force and effect. The invalid provision shall be modified to the
              minimum extent necessary to make it valid and enforceable.
            </p>
            <p className="mt-3">
              These Terms, together with our Privacy Policy, constitute the
              entire agreement between you and OfficeOS regarding your use of
              the platform and supersede all prior agreements and
              understandings.
            </p>
          </section>

          {/* Governing Law */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              15. Governing Law
            </h2>
            <p>
              These Terms are governed by and construed in accordance with the
              laws of the Federal Republic of Germany. Any disputes arising from
              these Terms shall be subject to the exclusive jurisdiction of the
              courts of Hamburg, Germany.
            </p>
          </section>

          {/* Contact */}
          <section>
            <h2 className="text-2xl font-bold tracking-tight text-primary mb-4">
              16. Contact
            </h2>
            <p>
              For questions about these Terms, contact us at{" "}
              <a
                href="mailto:legal@officeos.co"
                className="text-primary underline underline-offset-4 hover:text-primary/80"
              >
                legal@officeos.co
              </a>
              .
            </p>
          </section>
        </div>
      </main>

      <FooterSection />
    </div>
  );
}
