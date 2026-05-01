package com.enterpriseagentos.backendjava.domain.common.valueobjects;

import java.util.Locale;

public enum ChannelType {
    Slack,
    Telegram,
    Discord,
    WhatsApp,
    Teams,
    GoogleChat;

    public String toWire() {
        return switch (this) {
            case Slack -> "slack";
            case Telegram -> "telegram";
            case Discord -> "discord";
            case WhatsApp -> "whatsapp";
            case Teams -> "teams";
            case GoogleChat -> "google-chat";
        };
    }

    public static ChannelType fromWire(String value) {
        return switch (value.trim().toLowerCase(Locale.ROOT)) {
            case "slack" -> Slack;
            case "telegram" -> Telegram;
            case "discord" -> Discord;
            case "whatsapp" -> WhatsApp;
            case "teams" -> Teams;
            case "google-chat" -> GoogleChat;
            default -> throw new IllegalArgumentException("Unknown channel type: " + value);
        };
    }
}
