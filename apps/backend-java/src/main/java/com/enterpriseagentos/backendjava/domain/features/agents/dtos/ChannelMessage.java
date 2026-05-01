package com.enterpriseagentos.backendjava.domain.features.agents.dtos;

public final class ChannelMessage  {
    private final String kind;
    private final String content;

    public ChannelMessage(String kind, String content) {
        this.kind = kind;
        this.content = content;
    }

    public String getKind() {
        return kind;
}

    public String kind() {
        return kind;
    }

    public String getContent() {
        return content;
}

    public String content() {
        return content;
    }
}
