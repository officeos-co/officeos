"use client";

import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type FormEvent,
} from "react";
import { useRouter } from "next/navigation";

import {
  initialQuickstartMessages,
  initialQuickstartYaml,
} from "../data/quickstart-template";
import { useCreateAgent } from "../api/useAgents";
import { useQuickstartAgentChat } from "../api/useQuickstartAgentChat";

type QuickstartMessage = {
  id: string;
  role: "agent" | "user";
  content: string;
};

function readYamlValue(source: string, key: string) {
  const match = source.match(new RegExp(`^${key}:\\s*(.+)$`, "m"));
  return match?.[1]?.replace(/^["']|["']$/g, "").trim() || "";
}

export function useQuickstartTemplate() {
  const router = useRouter();
  const { createAgent, loading: isCreating } = useCreateAgent();
  const { quickstartAgentChat } = useQuickstartAgentChat();
  const [messages, setMessages] = useState<QuickstartMessage[]>(
    initialQuickstartMessages,
  );
  const [draft, setDraft] = useState("");
  const [yaml, setYaml] = useState(initialQuickstartYaml);
  const [isGenerating, setIsGenerating] = useState(false);
  const [generatedTarget, setGeneratedTarget] = useState<{
    provider: string;
    model: string;
  } | null>(null);
  const [codeScroll, setCodeScroll] = useState({
    canScroll: false,
    thumbSize: 100,
    thumbTop: 0,
  });
  const chatEndRef = useRef<HTMLDivElement | null>(null);
  const codeScrollerRef = useRef<HTMLElement | null>(null);

  const updateCodeScroll = useCallback(() => {
    const scroller = codeScrollerRef.current;
    if (!scroller) {
      return;
    }

    const scrollable = scroller.scrollHeight - scroller.clientHeight;
    const canScroll = scrollable > 0;
    const thumbSize = canScroll
      ? Math.max((scroller.clientHeight / scroller.scrollHeight) * 100, 12)
      : 100;
    const thumbTop = canScroll
      ? (scroller.scrollTop / scrollable) * (100 - thumbSize)
      : 0;

    setCodeScroll({
      canScroll,
      thumbSize,
      thumbTop,
    });
  }, []);

  const setCodeScroller = useCallback(
    (scroller: HTMLElement) => {
      codeScrollerRef.current?.removeEventListener("scroll", updateCodeScroll);
      codeScrollerRef.current = scroller;
      scroller.addEventListener("scroll", updateCodeScroll);
      requestAnimationFrame(updateCodeScroll);
    },
    [updateCodeScroll],
  );

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ block: "end", behavior: "smooth" });
  }, [messages, isGenerating]);

  useEffect(() => {
    return () => {
      const scroller = codeScrollerRef.current;
      scroller?.removeEventListener("scroll", updateCodeScroll);
    };
  }, [updateCodeScroll]);

  useEffect(() => {
    window.addEventListener("resize", updateCodeScroll);

    return () => {
      window.removeEventListener("resize", updateCodeScroll);
    };
  }, [updateCodeScroll]);

  useEffect(() => {
    requestAnimationFrame(updateCodeScroll);
  }, [updateCodeScroll, yaml]);

  async function submitPrompt(event?: FormEvent<HTMLFormElement>) {
    event?.preventDefault();

    const prompt = draft.trim();
    if (!prompt || isGenerating) {
      return;
    }

    setDraft("");
    setIsGenerating(true);
    setMessages((current) => [
      ...current,
      { id: `user-${Date.now()}`, role: "user", content: prompt },
    ]);

    try {
      const result = await quickstartAgentChat({
        message: prompt,
        currentYaml: yaml,
        messages: messages.map((message) => ({
          role: message.role,
          content: message.content,
        })),
        provider: null,
        model: null,
      });
      if (!result) {
        throw new Error("Quickstart generation returned no result.");
      }

      setYaml(result.configYaml);
      setGeneratedTarget({
        provider: result.provider,
        model: result.model,
      });
      setMessages((current) => [
        ...current,
        {
          id: `agent-${Date.now()}`,
          role: "agent",
          content: result.message,
        },
      ]);
    } catch (error) {
      setMessages((current) => [
        ...current,
        {
          id: `agent-error-${Date.now()}`,
          role: "agent",
          content:
            error instanceof Error
              ? error.message
              : "I could not generate the template.",
        },
      ]);
    } finally {
      setIsGenerating(false);
    }
  }

  async function useTemplate() {
    if (isCreating) {
      return;
    }

    try {
      const model =
        generatedTarget?.model ||
        readYamlValue(yaml, "model") ||
        "claude-sonnet-4-6";
      const created = await createAgent({
        name: readYamlValue(yaml, "name") || "Quickstart agent",
        provider: generatedTarget?.provider ?? "anthropic",
        model,
        systemPrompt: "",
        integrationSlugs: [],
        channelConnectionIds: [],
        configJson: yaml,
      });

      if (created?.id) {
        router.push(`/agents/${created.id}`);
      }
    } catch (error) {
      setMessages((current) => [
        ...current,
        {
          id: `agent-create-error-${Date.now()}`,
          role: "agent",
          content:
            error instanceof Error
              ? error.message
              : "I could not create the agent from this template.",
        },
      ]);
    }
  }

  return {
    chatEndRef,
    codeScroll,
    draft,
    isCreating,
    isGenerating,
    messages,
    setCodeScroller,
    setDraft,
    setYaml,
    submitPrompt,
    useTemplate,
    updateCodeScroll,
    yaml,
  };
}
