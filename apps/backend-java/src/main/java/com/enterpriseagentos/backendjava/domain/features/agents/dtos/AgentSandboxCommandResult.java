package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

public final class AgentSandboxCommandResult  {
    private final String output;
    private final int exitCode;

    public AgentSandboxCommandResult(String output, int exitCode) {
        this.output = output;
        this.exitCode = exitCode;
    }

    public String getOutput() {
        return output;
}

    public String output() {
        return output;
    }

    public int getExitCode() {
        return exitCode;
}

    public int exitCode() {
        return exitCode;
    }
}
