import { defineSkill, z } from "@harro/skill-sdk";

import manifest from "./skill.json" with { type: "json" };
const DRIVE_API = "https://www.googleapis.com/drive/v3";
const UPLOAD_API = "https://www.googleapis.com/upload/drive/v3";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function authHeaders(token: string) {
  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  };
}

async function drvFetch(ctx: Ctx, url: string, init?: RequestInit) {
  const res = await ctx.fetch(url, {
    ...init,
    headers: { Authorization: `Bearer ${ctx.credentials.access_token}`, ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Google Drive API ${res.status}: ${body}`);
  }
  if (res.status === 204) return {};
  return res.json();
}

async function drvPost(ctx: Ctx, url: string, body: unknown, method = "POST") {
  return drvFetch(ctx, url, {
    method,
    headers: authHeaders(ctx.credentials.access_token),
    body: JSON.stringify(body),
  });
}

async function drvDelete(ctx: Ctx, url: string) {
  return drvFetch(ctx, url, { method: "DELETE" });
}

const FILE_FIELDS = "id,name,mimeType,size,createdTime,modifiedTime,parents,owners,webViewLink,webContentLink,shared,trashed";

import doc from "./SKILL.md";

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Files ─────────────────────────────────────────────────────────

    list_files: {
      description: "List files in Google Drive, optionally filtered by query.",
      params: z.object({
        query: z.string().optional().describe("Drive search query (Drive query syntax)"),
        page_size: z.number().min(1).max(1000).default(10).describe("Results per page"),
        order_by: z.string().default("modifiedTime desc").describe("Sort order"),
        fields: z.string().optional().describe("Comma-separated fields to include"),
      }),
      returns: z.array(
        z.object({
          id: z.string().describe("File ID"),
          name: z.string().describe("File name"),
          mimeType: z.string().describe("MIME type"),
          size: z.string().optional().describe("File size in bytes"),
          modifiedTime: z.string().describe("Last modified time"),
          parents: z.array(z.string()).optional().describe("Parent folder IDs"),
          webViewLink: z.string().optional().describe("Web view link"),
        }),
      ),
      execute: async (params, ctx) => {
        const q = new URLSearchParams({
          pageSize: String(params.page_size),
          orderBy: params.order_by,
          fields: `files(${params.fields ?? "id,name,mimeType,size,modifiedTime,parents,webViewLink"})`,
        });
        if (params.query) q.set("q", params.query);
        const res = await drvFetch(ctx, `${DRIVE_API}/files?${q}`);
        return (res.files ?? []).map((f: any) => ({
          id: f.id,
          name: f.name,
          mimeType: f.mimeType,
          size: f.size,
          modifiedTime: f.modifiedTime ?? "",
          parents: f.parents,
          webViewLink: f.webViewLink,
        }));
      },
    },

    get_file: {
      description: "Get detailed metadata about a file.",
      params: z.object({
        file_id: z.string().describe("File ID"),
      }),
      returns: z.object({
        id: z.string().describe("File ID"),
        name: z.string().describe("File name"),
        mimeType: z.string().describe("MIME type"),
        size: z.string().optional().describe("File size in bytes"),
        createdTime: z.string().describe("Creation time"),
        modifiedTime: z.string().describe("Last modified time"),
        parents: z.array(z.string()).optional().describe("Parent folder IDs"),
        owners: z.array(z.any()).optional().describe("File owners"),
        webViewLink: z.string().optional().describe("Web view link"),
        webContentLink: z.string().optional().describe("Direct download link"),
        shared: z.boolean().describe("Whether the file is shared"),
      }),
      execute: async (params, ctx) => {
        const f = await drvFetch(ctx, `${DRIVE_API}/files/${params.file_id}?fields=${FILE_FIELDS}`);
        return {
          id: f.id,
          name: f.name,
          mimeType: f.mimeType,
          size: f.size,
          createdTime: f.createdTime ?? "",
          modifiedTime: f.modifiedTime ?? "",
          parents: f.parents,
          owners: f.owners,
          webViewLink: f.webViewLink,
          webContentLink: f.webContentLink,
          shared: f.shared ?? false,
        };
      },
    },

    create_file: {
      description: "Create a new file with optional text content.",
      params: z.object({
        name: z.string().describe("File name"),
        mime_type: z.string().optional().describe("MIME type (auto-detected if omitted)"),
        parents: z.array(z.string()).optional().describe("Parent folder IDs"),
        content: z.string().optional().describe("Text content for the file"),
      }),
      returns: z.object({
        id: z.string().describe("File ID"),
        name: z.string().describe("File name"),
        mimeType: z.string().describe("MIME type"),
        webViewLink: z.string().optional().describe("Web view link"),
      }),
      execute: async (params, ctx) => {
        if (params.content) {
          const metadata = {
            name: params.name,
            mimeType: params.mime_type,
            parents: params.parents,
          };
          const boundary = "skill_boundary";
          const body = [
            `--${boundary}`,
            "Content-Type: application/json; charset=UTF-8",
            "",
            JSON.stringify(metadata),
            `--${boundary}`,
            `Content-Type: ${params.mime_type ?? "text/plain"}`,
            "",
            params.content,
            `--${boundary}--`,
          ].join("\r\n");

          const res = await drvFetch(ctx, `${UPLOAD_API}/files?uploadType=multipart&fields=id,name,mimeType,webViewLink`, {
            method: "POST",
            headers: {
              Authorization: `Bearer ${ctx.credentials.access_token}`,
              "Content-Type": `multipart/related; boundary=${boundary}`,
            },
            body,
          });
          return { id: res.id, name: res.name, mimeType: res.mimeType, webViewLink: res.webViewLink };
        }
        const res = await drvPost(ctx, `${DRIVE_API}/files?fields=id,name,mimeType,webViewLink`, {
          name: params.name,
          mimeType: params.mime_type,
          parents: params.parents,
        });
        return { id: res.id, name: res.name, mimeType: res.mimeType, webViewLink: res.webViewLink };
      },
    },

    update_file: {
      description: "Update a file's name or content.",
      params: z.object({
        file_id: z.string().describe("File ID to update"),
        name: z.string().optional().describe("Updated file name"),
        content: z.string().optional().describe("Updated text content"),
      }),
      returns: z.object({
        id: z.string().describe("File ID"),
        name: z.string().describe("File name"),
        modifiedTime: z.string().describe("Last modified time"),
      }),
      execute: async (params, ctx) => {
        if (params.content) {
          const metadata: any = {};
          if (params.name) metadata.name = params.name;
          const boundary = "skill_boundary";
          const body = [
            `--${boundary}`,
            "Content-Type: application/json; charset=UTF-8",
            "",
            JSON.stringify(metadata),
            `--${boundary}`,
            "Content-Type: text/plain",
            "",
            params.content,
            `--${boundary}--`,
          ].join("\r\n");

          const res = await drvFetch(ctx, `${UPLOAD_API}/files/${params.file_id}?uploadType=multipart&fields=id,name,modifiedTime`, {
            method: "PATCH",
            headers: {
              Authorization: `Bearer ${ctx.credentials.access_token}`,
              "Content-Type": `multipart/related; boundary=${boundary}`,
            },
            body,
          });
          return { id: res.id, name: res.name, modifiedTime: res.modifiedTime };
        }
        const res = await drvFetch(ctx, `${DRIVE_API}/files/${params.file_id}?fields=id,name,modifiedTime`, {
          method: "PATCH",
          headers: authHeaders(ctx.credentials.access_token),
          body: JSON.stringify({ name: params.name }),
        });
        return { id: res.id, name: res.name, modifiedTime: res.modifiedTime };
      },
    },

    delete_file: {
      description: "Permanently delete a file (bypasses trash). Use trash_file for safer removal.",
      params: z.object({
        file_id: z.string().describe("File ID to delete"),
      }),
      returns: z.object({
        status: z.string().describe("Confirmation status"),
      }),
      execute: async (params, ctx) => {
        await drvDelete(ctx, `${DRIVE_API}/files/${params.file_id}`);
        return { status: "deleted" };
      },
    },

    copy_file: {
      description: "Copy a file.",
      params: z.object({
        file_id: z.string().describe("File ID to copy"),
        name: z.string().optional().describe("Name for the copy"),
        parents: z.array(z.string()).optional().describe("Parent folder IDs for the copy"),
      }),
      returns: z.object({
        id: z.string().describe("New file ID"),
        name: z.string().describe("File name"),
        webViewLink: z.string().optional().describe("Web view link"),
      }),
      execute: async (params, ctx) => {
        const body: any = {};
        if (params.name) body.name = params.name;
        if (params.parents) body.parents = params.parents;
        const res = await drvPost(ctx, `${DRIVE_API}/files/${params.file_id}/copy?fields=id,name,webViewLink`, body);
        return { id: res.id, name: res.name, webViewLink: res.webViewLink };
      },
    },

    move_file: {
      description: "Move a file from one folder to another.",
      params: z.object({
        file_id: z.string().describe("File ID to move"),
        from_folder: z.string().describe("Current parent folder ID"),
        to_folder: z.string().describe("Destination folder ID"),
      }),
      returns: z.object({
        id: z.string().describe("File ID"),
        name: z.string().describe("File name"),
        parents: z.array(z.string()).describe("Updated parent folder IDs"),
      }),
      execute: async (params, ctx) => {
        const res = await drvFetch(
          ctx,
          `${DRIVE_API}/files/${params.file_id}?addParents=${params.to_folder}&removeParents=${params.from_folder}&fields=id,name,parents`,
          {
            method: "PATCH",
            headers: authHeaders(ctx.credentials.access_token),
            body: JSON.stringify({}),
          },
        );
        return { id: res.id, name: res.name, parents: res.parents ?? [] };
      },
    },

    // ── Upload / Download ─────────────────────────────────────────────

    upload_file: {
      description: "Upload a local file to Google Drive.",
      params: z.object({
        local_path: z.string().describe("Local file path to upload"),
        parents: z.array(z.string()).optional().describe("Parent folder IDs"),
        mime_type: z.string().optional().describe("MIME type (auto-detected if omitted)"),
      }),
      returns: z.object({
        id: z.string().describe("File ID"),
        name: z.string().describe("File name"),
        mimeType: z.string().describe("MIME type"),
        size: z.string().optional().describe("File size"),
        webViewLink: z.string().optional().describe("Web view link"),
      }),
      execute: async (params, ctx) => {
        const { readFile } = await import("fs/promises");
        const { basename } = await import("path");
        const content = await readFile(params.local_path);
        const name = basename(params.local_path);
        const mimeType = params.mime_type ?? "application/octet-stream";

        const metadata = { name, mimeType, parents: params.parents };
        const boundary = "skill_upload_boundary";
        const metaPart = JSON.stringify(metadata);
        const header = [
          `--${boundary}`,
          "Content-Type: application/json; charset=UTF-8",
          "",
          metaPart,
          `--${boundary}`,
          `Content-Type: ${mimeType}`,
          "Content-Transfer-Encoding: base64",
          "",
        ].join("\r\n");
        const footer = `\r\n--${boundary}--`;
        const bodyStr = header + content.toString("base64") + footer;

        const res = await drvFetch(ctx, `${UPLOAD_API}/files?uploadType=multipart&fields=id,name,mimeType,size,webViewLink`, {
          method: "POST",
          headers: {
            Authorization: `Bearer ${ctx.credentials.access_token}`,
            "Content-Type": `multipart/related; boundary=${boundary}`,
          },
          body: bodyStr,
        });
        return { id: res.id, name: res.name, mimeType: res.mimeType, size: res.size, webViewLink: res.webViewLink };
      },
    },

    download_file: {
      description: "Download a file to a local path.",
      params: z.object({
        file_id: z.string().describe("File ID to download"),
        output_path: z.string().describe("Local path to save the file"),
      }),
      returns: z.object({
        file_path: z.string().describe("Saved file path"),
        size: z.number().describe("File size in bytes"),
        mimeType: z.string().describe("MIME type"),
      }),
      execute: async (params, ctx) => {
        const meta = await drvFetch(ctx, `${DRIVE_API}/files/${params.file_id}?fields=mimeType`);
        const res = await ctx.fetch(`${DRIVE_API}/files/${params.file_id}?alt=media`, {
          headers: { Authorization: `Bearer ${ctx.credentials.access_token}` },
        });
        if (!res.ok) throw new Error(`Download failed ${res.status}: ${await res.text()}`);
        const buf = Buffer.from(await res.arrayBuffer());
        const { writeFile } = await import("fs/promises");
        await writeFile(params.output_path, buf);
        return { file_path: params.output_path, size: buf.length, mimeType: meta.mimeType ?? "" };
      },
    },

    export_file: {
      description: "Export a Google Docs/Sheets/Slides file to a standard format.",
      params: z.object({
        file_id: z.string().describe("Google Docs/Sheets/Slides file ID"),
        mime_type: z.string().describe("Export MIME type (e.g. application/pdf, text/csv)"),
        output_path: z.string().describe("Local path to save the exported file"),
      }),
      returns: z.object({
        file_path: z.string().describe("Saved file path"),
        size: z.number().describe("File size in bytes"),
        mimeType: z.string().describe("Export MIME type"),
      }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(`${DRIVE_API}/files/${params.file_id}/export?mimeType=${encodeURIComponent(params.mime_type)}`, {
          headers: { Authorization: `Bearer ${ctx.credentials.access_token}` },
        });
        if (!res.ok) throw new Error(`Export failed ${res.status}: ${await res.text()}`);
        const buf = Buffer.from(await res.arrayBuffer());
        const { writeFile } = await import("fs/promises");
        await writeFile(params.output_path, buf);
        return { file_path: params.output_path, size: buf.length, mimeType: params.mime_type };
      },
    },

    // ── Folders ────────────────────────────────────────────────────────

    create_folder: {
      description: "Create a new folder.",
      params: z.object({
        name: z.string().describe("Folder name"),
        parents: z.array(z.string()).optional().describe("Parent folder IDs"),
      }),
      returns: z.object({
        id: z.string().describe("Folder ID"),
        name: z.string().describe("Folder name"),
        webViewLink: z.string().optional().describe("Web view link"),
      }),
      execute: async (params, ctx) => {
        const res = await drvPost(ctx, `${DRIVE_API}/files?fields=id,name,webViewLink`, {
          name: params.name,
          mimeType: "application/vnd.google-apps.folder",
          parents: params.parents,
        });
        return { id: res.id, name: res.name, webViewLink: res.webViewLink };
      },
    },

    list_folder_contents: {
      description: "List files and folders inside a specific folder.",
      params: z.object({
        folder_id: z.string().describe("Folder ID to list"),
        page_size: z.number().min(1).max(1000).default(10).describe("Results per page"),
      }),
      returns: z.array(
        z.object({
          id: z.string().describe("File ID"),
          name: z.string().describe("File name"),
          mimeType: z.string().describe("MIME type"),
          size: z.string().optional().describe("File size"),
          modifiedTime: z.string().describe("Last modified time"),
        }),
      ),
      execute: async (params, ctx) => {
        const q = new URLSearchParams({
          q: `'${params.folder_id}' in parents and trashed = false`,
          pageSize: String(params.page_size),
          fields: "files(id,name,mimeType,size,modifiedTime)",
        });
        const res = await drvFetch(ctx, `${DRIVE_API}/files?${q}`);
        return (res.files ?? []).map((f: any) => ({
          id: f.id,
          name: f.name,
          mimeType: f.mimeType,
          size: f.size,
          modifiedTime: f.modifiedTime ?? "",
        }));
      },
    },

    // ── Search ────────────────────────────────────────────────────────

    search: {
      description: "Search files by query and/or full-text content.",
      params: z.object({
        query: z.string().optional().describe("Drive query syntax (e.g. mimeType='...')"),
        full_text: z.string().optional().describe("Full-text search across file content"),
        page_size: z.number().min(1).max(1000).default(10).describe("Results per page"),
      }),
      returns: z.array(
        z.object({
          id: z.string().describe("File ID"),
          name: z.string().describe("File name"),
          mimeType: z.string().describe("MIME type"),
          size: z.string().optional().describe("File size"),
          modifiedTime: z.string().describe("Last modified time"),
          webViewLink: z.string().optional().describe("Web view link"),
        }),
      ),
      execute: async (params, ctx) => {
        const parts: string[] = [];
        if (params.query) parts.push(params.query);
        if (params.full_text) parts.push(`fullText contains '${params.full_text.replace(/'/g, "\\'")}'`);
        const q = new URLSearchParams({
          pageSize: String(params.page_size),
          fields: "files(id,name,mimeType,size,modifiedTime,webViewLink)",
        });
        if (parts.length > 0) q.set("q", parts.join(" and "));
        const res = await drvFetch(ctx, `${DRIVE_API}/files?${q}`);
        return (res.files ?? []).map((f: any) => ({
          id: f.id,
          name: f.name,
          mimeType: f.mimeType,
          size: f.size,
          modifiedTime: f.modifiedTime ?? "",
          webViewLink: f.webViewLink,
        }));
      },
    },

    // ── Sharing ───────────────────────────────────────────────────────

    share: {
      description: "Share a file or folder with a user or group.",
      params: z.object({
        file_id: z.string().describe("File or folder ID to share"),
        email: z.string().describe("Email address of the user or group"),
        role: z.enum(["reader", "writer", "commenter", "owner"]).default("reader").describe("Permission role"),
        type: z.enum(["user", "group", "domain", "anyone"]).default("user").describe("Grantee type"),
      }),
      returns: z.object({
        permission_id: z.string().describe("Permission ID"),
        role: z.string().describe("Granted role"),
        type: z.string().describe("Grantee type"),
        email: z.string().describe("Email address"),
      }),
      execute: async (params, ctx) => {
        const res = await drvPost(ctx, `${DRIVE_API}/files/${params.file_id}/permissions?fields=id,role,type,emailAddress`, {
          role: params.role,
          type: params.type,
          emailAddress: params.email,
        });
        return {
          permission_id: res.id,
          role: res.role,
          type: res.type,
          email: res.emailAddress ?? params.email,
        };
      },
    },

    list_permissions: {
      description: "List permissions on a file.",
      params: z.object({
        file_id: z.string().describe("File ID"),
      }),
      returns: z.array(
        z.object({
          permission_id: z.string().describe("Permission ID"),
          role: z.string().describe("Permission role"),
          type: z.string().describe("Grantee type"),
          email_address: z.string().optional().describe("Email address"),
          display_name: z.string().optional().describe("Display name"),
        }),
      ),
      execute: async (params, ctx) => {
        const res = await drvFetch(ctx, `${DRIVE_API}/files/${params.file_id}/permissions?fields=permissions(id,role,type,emailAddress,displayName)`);
        return (res.permissions ?? []).map((p: any) => ({
          permission_id: p.id,
          role: p.role,
          type: p.type,
          email_address: p.emailAddress,
          display_name: p.displayName,
        }));
      },
    },

    remove_permission: {
      description: "Remove a permission from a file.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        permission_id: z.string().describe("Permission ID to remove"),
      }),
      returns: z.object({
        status: z.string().describe("Confirmation status"),
      }),
      execute: async (params, ctx) => {
        await drvDelete(ctx, `${DRIVE_API}/files/${params.file_id}/permissions/${params.permission_id}`);
        return { status: "removed" };
      },
    },

    // ── Trash ─────────────────────────────────────────────────────────

    trash_file: {
      description: "Move a file to trash.",
      params: z.object({
        file_id: z.string().describe("File ID to trash"),
      }),
      returns: z.object({
        id: z.string().describe("File ID"),
        name: z.string().describe("File name"),
        trashed: z.boolean().describe("Trashed state"),
      }),
      execute: async (params, ctx) => {
        const res = await drvFetch(ctx, `${DRIVE_API}/files/${params.file_id}?fields=id,name,trashed`, {
          method: "PATCH",
          headers: authHeaders(ctx.credentials.access_token),
          body: JSON.stringify({ trashed: true }),
        });
        return { id: res.id, name: res.name, trashed: res.trashed };
      },
    },

    untrash_file: {
      description: "Restore a file from trash.",
      params: z.object({
        file_id: z.string().describe("File ID to restore"),
      }),
      returns: z.object({
        id: z.string().describe("File ID"),
        name: z.string().describe("File name"),
        trashed: z.boolean().describe("Trashed state"),
      }),
      execute: async (params, ctx) => {
        const res = await drvFetch(ctx, `${DRIVE_API}/files/${params.file_id}?fields=id,name,trashed`, {
          method: "PATCH",
          headers: authHeaders(ctx.credentials.access_token),
          body: JSON.stringify({ trashed: false }),
        });
        return { id: res.id, name: res.name, trashed: res.trashed };
      },
    },

    empty_trash: {
      description: "Permanently delete all files in trash. This is irreversible.",
      params: z.object({}),
      returns: z.object({
        status: z.string().describe("Confirmation status"),
      }),
      execute: async (_params, ctx) => {
        await drvDelete(ctx, `${DRIVE_API}/files/trash`);
        return { status: "trash emptied" };
      },
    },
  },
});
