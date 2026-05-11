"use client";

import { useState } from "react";
import { Dialog, DialogContent } from "@/ui/dialog";
import { getDialogWidthClassName } from "@/shell/page-container";
import type { McpServer } from "../data/integrations";
import { CredentialSetup } from "./credential-dialog";

export function ConnectorDirectoryDialog({
  open,
  onOpenChange,
  integrations,
  onSaveCredential,
  onAddCustomMcp,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  integrations: McpServer[];
  onSaveCredential: (
    server: McpServer,
    values: Record<string, string>,
  ) => Promise<void> | void;
  onAddCustomMcp: () => void;
}) {
  const [selectedName, setSelectedName] = useState<string | null>(null);

  function setOpen(nextOpen: boolean) {
    onOpenChange(nextOpen);
    if (!nextOpen) setSelectedName(null);
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent
        className={getDialogWidthClassName(
          "narrow",
          "max-h-[min(820px,calc(100vh-48px))] overflow-y-auto p-6",
        )}
      >
        <CredentialSetup
          integrations={integrations}
          selectedName={selectedName}
          onSelectedNameChange={setSelectedName}
          returnTo="/integrations"
          onSave={onSaveCredential}
          onSaved={() => setOpen(false)}
          onAddCustomMcp={() => {
            setOpen(false);
            onAddCustomMcp();
          }}
        />
      </DialogContent>
    </Dialog>
  );
}
