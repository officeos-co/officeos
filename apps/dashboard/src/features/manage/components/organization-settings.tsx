"use client";

import { useEffect, useState } from "react";
import { Building2Icon, Trash2Icon } from "lucide-react";
import { toast } from "sonner";
import { PageContainer } from "@/shell/page-container";
import { PageHeader } from "@/shell/page-header";
import { Button } from "@/ui/button";
import { Skeleton } from "@/ui/skeleton";
import { useOrganizationContext } from "../api/useOrganization";
import { OrganizationCreateDialog } from "./organization-create-dialog";
import { OrganizationDeleteDialog } from "./organization-delete-dialog";
import { OrganizationInvitesMenu } from "./organization-invites-menu";

export function OrganizationSettings() {
  const { context, loading, error } = useOrganizationContext();
  const [setupOpen, setSetupOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  useEffect(() => {
    if (error) {
      toast.error("Failed to load organization", {
        description: error.message,
      });
    }
  }, [error]);

  if (loading && !context) {
    return (
      <>
        <PageHeader
          page="Organization"
          subtitle="Manage your active organization."
          width="thin"
        />
        <PageContainer width="thin" className="flex flex-1 flex-col gap-3 pb-4">
          {Array.from({ length: 5 }).map((_, index) => (
            <Skeleton key={index} className="h-14 w-full rounded-md" />
          ))}
        </PageContainer>
      </>
    );
  }

  const organization = context?.currentOrganization;
  if (!organization) return null;
  const isIndividual = organization.kind === "individual";

  return (
    <>
      <PageHeader
        page="Organization"
        subtitle="Manage your active organization."
        width="thin"
        action={
          <div className="flex items-center gap-2">
            <OrganizationInvitesMenu />
            {isIndividual && (
              <Button size="sm" onClick={() => setSetupOpen(true)}>
                <Building2Icon className="size-3.5" />
                Set up organization
              </Button>
            )}
          </div>
        }
      />
      <PageContainer width="thin" className="flex flex-1 flex-col gap-5 pb-4">
        <section className="rounded-md border p-4">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <div className="flex items-center gap-2">
                <Building2Icon className="size-4 text-primary" />
                <h2 className="truncate text-sm font-medium">
                  {organization.name}
                </h2>
              </div>
              <p className="mt-1 text-xs text-muted-foreground">
                {isIndividual
                  ? "This individual organization belongs to your account. Deleting it deletes the account."
                  : "This shared organization is the active collaboration context."}
              </p>
            </div>
            <Button
              size="icon-sm"
              variant="destructive"
              aria-label="Delete organization and account"
              onClick={() => setDeleteOpen(true)}
            >
              <Trash2Icon className="size-3.5" />
            </Button>
          </div>
        </section>
      </PageContainer>

      <OrganizationCreateDialog
        open={setupOpen}
        onOpenChange={setSetupOpen}
        defaultName={organization.name}
      />
      <OrganizationDeleteDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        organizationName={organization.name}
      />
    </>
  );
}
