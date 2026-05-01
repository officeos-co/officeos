package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

public final class BrowserToolDescriptor  {
    private final String name;
    private final String description;
    private final Object inputSchema;

    public BrowserToolDescriptor(String name, String description, Object inputSchema) {
        this.name = name;
        this.description = description;
        this.inputSchema = inputSchema;
    }

    public String getName() {
        return name;
}

    public String name() {
        return name;
    }

    public String getDescription() {
        return description;
}

    public String description() {
        return description;
    }

    public Object getInputSchema() {
        return inputSchema;
}

    public Object inputSchema() {
        return inputSchema;
    }
}
