"use client";

import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type FormEvent,
} from "react";
import { useRouter } from "next/navigation";

import { initialQuickstartFiles, initialQuickstartMessages } from "../data/quickstart-template";
import {
  useQuickstartAgentChat,
  type QuickstartFile,
} from "../api/useQuickstartAgentChat";

type QuickstartMessage = {
  id: string;
  role: "agent" | "user";
  content: string;
};

export function useQuickstartTemplate() {
  const router = useRouter();
  const {
    applyQuickstartBlueprint,
    applying: isCreating,
    quickstartAgentChat,
  } = useQuickstartAgentChat();
  const [messages, setMessages] = useState<QuickstartMessage[]>(
    initialQuickstartMessages,
  );
  const [draft, setDraft] = useState("");
  const [files, setFiles] = useState<QuickstartFile[]>(initialQuickstartFiles);
  const [activePath, setActivePath] = useState(initialQuickstartFiles[0].path);
  const [isGenerating, setIsGenerating] = useState(false);
  const [codeScroll, setCodeScroll] = useState({
    canScroll: false,
    thumbSize: 100,
    thumbTop: 0,
  });
  const chatEndRef = useRef<HTMLDivElement | null>(null);
  const codeScrollerRef = useRef<HTMLElement | null>(null);

  const activeFile = useMemo(
    () =>
      files.find((file) => file.path === activePath) ??
      files[0] ?? { path: "workspace.yaml", content: "" },
    [activePath, files],
  );

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

  const setActiveContent = useCallback(
    (content: string) => {
      setFiles((current) =>
        current.map((file) =>
          file.path === activeFile.path ? { ...file, content } : file,
        ),
      );
    },
    [activeFile.path],
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
  }, [activeFile.content, updateCodeScroll]);

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
        currentFiles: files,
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

      setFiles(result.files);
      setActivePath((current) =>
        result.files.some((file) => file.path === current)
          ? current
          : result.files[0]?.path || "workspace.yaml",
      );
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
      const created = await applyQuickstartBlueprint({
        files,
        provider: null,
        model: null,
      });

      const firstAgent = created?.agents[0];
      if (firstAgent?.id) {
        router.push(`/agents/${firstAgent.id}`);
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
    activeFile,
    activePath,
    chatEndRef,
    codeScroll,
    draft,
    files,
    isCreating,
    isGenerating,
    messages,
    setActiveContent,
    setActivePath,
    setCodeScroller,
    setDraft,
    submitPrompt,
    useTemplate,
    updateCodeScroll,
  };
}
