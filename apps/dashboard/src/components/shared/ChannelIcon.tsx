import { cn } from "@/lib/utils";
import Image from "next/image";

const channelIconPaths: Record<string, string> = {
  slack: "/channels/slack.svg",
  telegram: "/channels/telegram.svg",
  discord: "/channels/discord.svg",
  whatsapp: "/channels/whatsapp.svg",
  teams: "/channels/teams.svg",
  "google-chat": "/channels/google-chat.svg",
  webchat: "/channels/webchat.svg",
};

export function ChannelIcon({ type, className, size = 16 }: { type: string; className?: string; size?: number }) {
  const src = channelIconPaths[type] ?? channelIconPaths.webchat;
  return (
    <Image
      src={src}
      alt={type}
      width={size}
      height={size}
      className={cn("shrink-0", className)}
    />
  );
}
