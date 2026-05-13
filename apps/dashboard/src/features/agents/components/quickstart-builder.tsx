"use client";

import { useMemo } from "react";
import dynamic from "next/dynamic";
import Link from "next/link";
import { ArrowLeft, ArrowUp, FileText, Loader2, PlusIcon } from "lucide-react";
import type { ReactCodeMirrorProps } from "@uiw/react-codemirror";
import { yaml as yamlLanguage } from "@codemirror/lang-yaml";
import {
  defaultHighlightStyle,
  syntaxHighlighting,
} from "@codemirror/language";
import { EditorView } from "@codemirror/view";

import { cn } from "@/lib/utils";
import { isDevelopment } from "@/lib/env";
import { Button } from "@/ui/button";
import { HelpTooltip } from "@/ui/help-tooltip";
import { Label } from "@/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/select";
import { Textarea } from "@/ui/textarea";
import { useQuickstartTemplate } from "../hooks/useQuickstartTemplate";
import { getModelTooltip } from "../data/model-tooltips";

const CodeMirror = dynamic<ReactCodeMirrorProps>(
  () => import("@uiw/react-codemirror").then((mod) => mod.default),
  { ssr: false },
);

export function QuickstartBuilder() {
  const {
    activeFile,
    activePath,
    chatEndRef,
    codeScroll,
    draft,
    files,
    isCreating,
    isGenerating,
    messages,
    models,
    selectedModel,
    selectedModelInfo,
    setActiveContent,
    setActivePath,
    setCodeScroller,
    setDraft,
    setModel,
    submitPrompt,
    useTemplate,
    updateCodeScroll,
  } = useQuickstartTemplate();

  const editorExtensions = useMemo(
    () => [
      yamlLanguage(),
      syntaxHighlighting(defaultHighlightStyle, { fallback: true }),
      EditorView.lineWrapping,
      EditorView.theme({
        "&": {
          height: "100%",
          backgroundColor: "transparent",
          color: "var(--foreground)",
          fontSize: "12px",
        },
        ".cm-scroller": {
          height: "100%",
          overflow: "auto",
          fontFamily:
            "var(--font-geist-mono), ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace",
          lineHeight: "1.5",
        },
        ".cm-content": {
          minHeight: "100%",
          padding: "16px 28px 16px 0",
        },
        ".cm-line": {
          padding: "0 16px",
        },
        ".cm-gutters": {
          backgroundColor: "color-mix(in oklab, var(--muted) 40%, transparent)",
          color: "var(--muted-foreground)",
          borderRight: "1px solid var(--border)",
        },
        ".cm-activeLine": {
          backgroundColor: "color-mix(in oklab, var(--muted) 45%, transparent)",
        },
        ".cm-activeLineGutter": {
          backgroundColor: "color-mix(in oklab, var(--muted) 70%, transparent)",
          color: "var(--foreground)",
        },
        ".cm-selectionBackground, &.cm-focused .cm-selectionBackground": {
          backgroundColor: "color-mix(in oklab, var(--primary) 22%, transparent)",
        },
        "&.cm-focused": {
          outline: "none",
        },
        ".cm-cursor": {
          borderLeftColor: "var(--foreground)",
        },
        ".cm-matchingBracket": {
          backgroundColor: "color-mix(in oklab, var(--primary) 14%, transparent)",
          outline:
            "1px solid color-mix(in oklab, var(--primary) 40%, transparent)",
        },
      }),
    ],
    [],
  );

  return (
    <main className="flex h-svh max-h-svh overflow-hidden bg-background">
      <section className="relative flex min-h-0 min-w-[22rem] basis-[36%] flex-col overflow-hidden border-r border-border bg-background">
        <div className="min-h-0 flex-1 overflow-hidden px-4 pb-28 pt-6 sm:px-8 lg:px-12">
          <div className="mx-auto h-full w-full max-w-2xl overflow-y-auto pt-8">
            <div className="flex min-h-full flex-col justify-end gap-4">
              {messages.map((message) => (
                <div
                  key={message.id}
                  className={cn(
                    "flex",
                    message.role === "user" ? "justify-end" : "justify-start",
                  )}
                >
                  <div
                    className={cn(
                      "max-w-[82%] rounded-lg px-3 py-2 text-sm leading-6 shadow-sm",
                      message.role === "user"
                        ? "bg-primary text-primary-foreground"
                        : "border border-border bg-card text-card-foreground",
                    )}
                  >
                    {message.content}
                  </div>
                </div>
              ))}
              {isGenerating ? (
                <div className="flex justify-start">
                  <div className="flex items-center gap-2 rounded-lg border border-border bg-card px-3 py-2 text-sm text-muted-foreground shadow-sm">
                    <Loader2 className="size-3.5 animate-spin" />
                    Generating template
                  </div>
                </div>
              ) : null}
              <div ref={chatEndRef} />
            </div>
          </div>
        </div>

        <form
          onSubmit={submitPrompt}
          className="absolute inset-x-0 bottom-0 z-10 border-t border-border/70 bg-background/85 px-4 py-4 backdrop-blur sm:px-8 lg:px-12"
        >
          <div className="mx-auto flex max-w-2xl items-end gap-2 rounded-lg border border-border bg-card p-2 shadow-lg">
            <Textarea
              value={draft}
              onChange={(event) => setDraft(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter" && !event.shiftKey) {
                  event.preventDefault();
                  submitPrompt();
                }
              }}
              placeholder="Describe your agent..."
              rows={1}
              className="max-h-36 min-h-10 resize-none border-0 bg-transparent px-2 py-2 shadow-none focus-visible:ring-0"
            />
            <Button
              type="submit"
              size="icon"
              disabled={!draft.trim() || isGenerating || !selectedModel}
              aria-label="Generate template"
              className="size-9 rounded-lg"
            >
              {isGenerating ? (
                <Loader2 className="size-4 animate-spin" />
              ) : (
                <ArrowUp className="size-4" />
              )}
            </Button>
          </div>
        </form>
      </section>

      <section className="flex min-h-0 min-w-0 flex-[1.8] flex-col overflow-hidden bg-card">
        <div className="flex h-14 shrink-0 items-center justify-between border-b border-border px-4">
          <div className="flex min-w-0 items-center gap-2">
            <Button variant="ghost" size="icon-sm" aria-label="Back">
              <ArrowLeft className="size-4" />
            </Button>
            <div className="min-w-0 truncate text-sm font-medium">
              Generated blueprint
            </div>
          </div>
          <div className="flex min-w-0 items-center gap-2">
            {models.length === 0 && isDevelopment() ? (
              <Link
                href="/providers"
                target="_blank"
                rel="noopener noreferrer"
                className="flex h-8 items-center justify-center gap-2 rounded-lg border border-dashed border-border px-3 text-sm text-muted-foreground transition-colors hover:border-foreground hover:text-foreground"
              >
                <PlusIcon className="size-4" />
                Add provider
              </Link>
            ) : models.length === 0 ? (
              <div className="flex h-8 items-center rounded-lg border border-border px-3 text-sm text-muted-foreground">
                No models
              </div>
            ) : (
              <div className="flex min-w-0 items-center gap-2">
                <Label className="hidden items-center gap-1 text-xs text-muted-foreground sm:flex">
                  Model
                  <HelpTooltip>
                    Quickstart generation and the created agent both use this
                    provider model.
                  </HelpTooltip>
                </Label>
                <Select
                  value={selectedModel}
                  onValueChange={(value) => {
                    if (value) setModel(value);
                  }}
                >
                  <SelectTrigger
                    size="sm"
                    aria-label="Model"
                    className="w-48 max-w-[42vw]"
                  >
                    <SelectValue>
                      {selectedModelInfo?.displayName ?? selectedModel}
                    </SelectValue>
                  </SelectTrigger>
                  <SelectContent className="w-max min-w-(--anchor-width) max-w-[calc(100vw-2rem)]">
                    {models.map((modelOption) => (
                      <SelectItem
                        key={modelOption.id}
                        value={modelOption.id}
                        title={getModelTooltip(modelOption.id)}
                      >
                        {modelOption.displayName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
            <Button
              size="sm"
              onClick={useTemplate}
              disabled={isCreating || !selectedModel}
            >
              {isCreating ? "Creating..." : "Use this template"}
            </Button>
          </div>
        </div>

        <div className="flex h-10 shrink-0 items-end gap-1 overflow-x-auto border-b border-border bg-muted/35 px-3">
          {files.map((file) => (
            <button
              key={file.path}
              type="button"
              onClick={() => setActivePath(file.path)}
              className={cn(
                "flex h-8 shrink-0 items-center gap-1.5 rounded-t-md border border-b-0 px-3 text-xs font-medium transition-colors",
                file.path === activePath
                  ? "border-border bg-background text-foreground"
                  : "border-transparent text-muted-foreground hover:bg-background/70 hover:text-foreground",
              )}
            >
              <FileText className="size-3.5" />
              <span className="max-w-44 truncate">{file.path}</span>
            </button>
          ))}
        </div>

        <div className="min-h-0 flex-1 overflow-hidden">
          <div className="relative flex h-full overflow-hidden bg-background">
            <CodeMirror
              key={activeFile.path}
              value={activeFile.content}
              height="100%"
              minHeight="100%"
              basicSetup={{
                lineNumbers: true,
                foldGutter: true,
                highlightActiveLine: true,
                highlightActiveLineGutter: true,
                bracketMatching: true,
                closeBrackets: true,
                autocompletion: true,
              }}
              extensions={editorExtensions}
              theme="light"
              className="h-full min-w-0 flex-1"
              onCreateEditor={(view) => {
                setCodeScroller(view.scrollDOM);
                requestAnimationFrame(updateCodeScroll);
              }}
              onChange={(value) => setActiveContent(value)}
            />
            <div className="pointer-events-none absolute bottom-3 right-2 top-3 w-1 rounded-full bg-border/40">
              <div
                className={cn(
                  "absolute left-0 w-full rounded-full bg-muted-foreground/60 transition-opacity",
                  codeScroll.canScroll ? "opacity-100" : "opacity-40",
                )}
                style={{
                  height: `${codeScroll.thumbSize}%`,
                  top: `${codeScroll.thumbTop}%`,
                }}
              />
            </div>
            <div className="pointer-events-none absolute inset-x-12 top-0 h-8 bg-linear-to-b from-background to-transparent" />
            <div className="pointer-events-none absolute inset-x-12 bottom-0 h-8 bg-linear-to-t from-background to-transparent" />
          </div>
        </div>
      </section>
    </main>
  );
}
