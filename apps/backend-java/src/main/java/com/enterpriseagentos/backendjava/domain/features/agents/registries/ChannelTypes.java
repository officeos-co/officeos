package com.enterpriseagentos.backendjava.domain.features.agents.registries;

import java.util.List;
import java.util.Optional;

import com.enterpriseagentos.backendjava.domain.features.agents.dtos.ChannelTypeDefinition;

public final class ChannelTypes {
    public static final List<ChannelTypeDefinition> ALL = List.of(
        definition("slack", "Slack", "Connect a Slack workspace"),
        definition("telegram", "Telegram", "Connect a Telegram bot"),
        definition("discord", "Discord", "Connect a Discord bot"),
        definition("whatsapp", "WhatsApp", "Connect via QR code - like WhatsApp Web"),
        definition("teams", "Microsoft Teams", "Connect to Microsoft Teams"),
        definition("google-chat", "Google Chat", "Connect to Google Chat")
    );

    private ChannelTypes() {
    }

    public static Optional<ChannelTypeDefinition> getByType(String channelType) {
        return ALL.stream().filter(type -> type.type().equalsIgnoreCase(channelType)).findFirst();
    }

    private static ChannelTypeDefinition definition(String type, String displayName, String description) {
        return new ChannelTypeDefinition(type, displayName, description, "", List.of());
    }
}
