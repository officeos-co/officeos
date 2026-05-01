package com.enterpriseagentos.backendjava.domain.common.services;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.HexFormat;
import java.util.List;
import java.util.Optional;
import java.util.UUID;

import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentTemplateModel;

public final class AgentTemplateRegistry {
    public static final List<AgentTemplateModel> BUILTIN_TEMPLATES = List.of(
        template("Blank agent", "A blank starting point.", "[]", "[]", ""),
        template("Deep researcher", "Multi-step web research with citations.", "[\"browser\"]", "[]",
            "You are a research assistant. Conduct thorough web research, synthesize findings, and present them with source citations."),
        template("Support agent", "Answers questions from docs, escalates via Slack.", "[\"notion\"]", "[\"slack\"]",
            "You are a customer support agent. Answer questions using the knowledge base in Notion. Escalate to #support-escalation on Slack when needed."),
        template("Incident commander", "Triages alerts, creates tickets, runs war room.", "[\"linear\",\"browser\"]", "[\"slack\"]",
            "You are an incident commander. Triage alerts, create Linear issues, and coordinate the response in #incidents on Slack."),
        template("Code reviewer", "Reviews PRs for bugs and security.", "[\"github\"]", "[]",
            "Review pull request diffs for bugs, security vulnerabilities, and style issues. Leave constructive comments.")
    );

    private AgentTemplateRegistry() {
    }

    public static Optional<AgentTemplateModel> getBuiltin(UUID id) {
        return BUILTIN_TEMPLATES.stream().filter(template -> template.getId().equals(id)).findFirst();
    }

    private static AgentTemplateModel template(String name, String description, String integrationsJson, String channelsJson, String prompt) {
        return AgentTemplateModel.builtIn(
            deterministicGuid("agent-template:" + name),
            name,
            description,
            integrationsJson,
            channelsJson,
            prompt
        );
    }

    private static UUID deterministicGuid(String value) {
        try {
            byte[] hash = MessageDigest.getInstance("SHA-256").digest(value.getBytes(StandardCharsets.UTF_8));
            String hex = HexFormat.of().formatHex(hash, 0, 16);
            return UUID.fromString(hex.substring(0, 8) + "-" + hex.substring(8, 12) + "-"
                + hex.substring(12, 16) + "-" + hex.substring(16, 20) + "-" + hex.substring(20));
        } catch (NoSuchAlgorithmException exception) {
            throw new IllegalStateException("SHA-256 is not available.", exception);
        }
    }
}
