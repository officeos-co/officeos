package com.enterpriseagentos.backendjava.domain.common.services;

import java.time.Instant;
import java.util.Comparator;
import java.util.List;
import java.util.Objects;
import java.util.stream.Collectors;
import java.util.stream.Stream;

import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentMemoryModel;
import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentModel;
import com.enterpriseagentos.backendjava.domain.features.agents.models.AgentPersonalityModel;

public final class SystemPromptComposer {
    private SystemPromptComposer() {
    }

    public static String compose(AgentModel agent) {
        return compose(agent.getName(), agent.getPrompt(), safe(agent.getPersonalityFiles()), safe(agent.getMemories()));
    }

    public static String compose(
        String agentName,
        String userPrompt,
        List<AgentPersonalityModel> personalityFiles,
        List<AgentMemoryModel> memories
    ) {
        return Stream.of(
                identity(agentName),
                tooling(),
                safety(),
                fileWork(),
                taskWork(),
                workspace(agentName),
                projectContext(personalityFiles, userPrompt),
                memory(memories),
                currentDateTime(),
                runtime())
            .filter(Objects::nonNull)
            .collect(Collectors.joining("\n\n"));
    }

    public static String tooling() {
        return "## Tooling\n\n"
            + "- NEVER fabricate tool results. If a tool fails, report the actual error.\n"
            + "- Prefer dedicated tools over shell commands for files and search.\n"
            + "- Use tool_search when a useful built-in, MCP, or browser tool may exist.";
    }

    public static String identity(String agentName) {
        return "## Identity\n\nYou are " + agentName + ", an EnterpriseAgentOS coding agent running in a Linux Kubernetes pod.";
    }

    public static String safety() {
        return "## Safety\n\n"
            + "- Never exfiltrate credentials, API keys, or sensitive data.\n"
            + "- Never execute destructive commands without explicit user confirmation.\n"
            + "- Prefer reversible operations.";
    }

    public static String fileWork() {
        return "## File Work\n\n"
            + "- Read files before editing or overwriting them.\n"
            + "- Prefer targeted edits over full rewrites.\n"
            + "- Preserve user changes and unrelated work.\n"
            + "- Run the narrowest meaningful tests/checks after code changes.";
    }

    public static String taskWork() {
        return "## Task Tracking\n\n"
            + "- For multi-step work, create tasks and keep progress current.\n"
            + "- Keep one task in progress at a time unless work is truly parallel.";
    }

    public static String workspace(String agentName) {
        return "## Workspace\n\nAgent: " + agentName + "\nWorking directory: /home";
    }

    public static String projectContext(List<AgentPersonalityModel> files, String userPrompt) {
        List<String> sections = safe(files).stream()
            .sorted(Comparator.comparingInt(AgentPersonalityModel::compositionOrder))
            .map(AgentPersonalityModel::formatPromptSection)
            .collect(Collectors.toList());
        if (userPrompt != null && !userPrompt.isBlank()) {
            sections.add("<file path=\"PROMPT.md\">\n" + userPrompt.trim() + "\n</file>");
        }
        return sections.isEmpty() ? null : "## Project Context\n\n" + String.join("\n\n", sections);
    }

    public static String memory(List<AgentMemoryModel> memories) {
        List<AgentMemoryModel> safeMemories = safe(memories);
        if (safeMemories.isEmpty()) {
            return null;
        }
        return "## Memory\n\n" + safeMemories.stream()
            .map(AgentMemoryModel::formatPromptSection)
            .collect(Collectors.joining("\n\n"));
    }

    public static String currentDateTime() {
        return "## Current Date & Time\n\n" + Instant.now() + " UTC";
    }

    public static String runtime() {
        return "## Runtime\n\nHost: EnterpriseAgentOS | Platform: Linux (Kubernetes pod)";
    }

    private static <T> List<T> safe(List<T> list) {
        return list == null ? List.of() : list;
    }
}
