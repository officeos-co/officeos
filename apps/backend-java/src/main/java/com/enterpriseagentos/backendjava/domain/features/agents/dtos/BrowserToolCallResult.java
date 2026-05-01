package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

public final class BrowserToolCallResult  {
    private final boolean isError;
    private final String output;

    public BrowserToolCallResult(boolean isError, String output) {
        this.isError = isError;
        this.output = output;
    }

    public boolean getIsError() {
        return isError;
}

    public boolean isError() {
        return isError;
    }

    public String getOutput() {
        return output;
}

    public String output() {
        return output;
    }
}
