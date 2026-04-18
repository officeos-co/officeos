import type { SkillDefinition } from "@harro/skill-sdk";

export interface CredentialFieldManifest {
  key: string;
  label: string;
  kind: "password" | "text" | "textarea";
  required: boolean;
  placeholder?: string;
  help?: string;
}

export interface SkillManifest {
  name: string;
  title: string;
  logo: string;
  description: string;
  doc: string;
  actions: Record<
    string,
    {
      description: string;
      params: Record<string, unknown>;
      returns?: Record<string, unknown>;
    }
  >;
  credentialFields: CredentialFieldManifest[];
}

/**
 * Convert a Zod schema to a JSON Schema-like object for the manifest.
 *
 * Uses _def.typeName instead of instanceof checks because skill bundles
 * have their own copy of Zod (different class identity after esbuild).
 */
function zodToJsonSchema(schema: any): Record<string, unknown> {
  const typeName: string = schema?._def?.typeName ?? "";

  if (typeName === "ZodObject") {
    const shape = schema.shape ?? schema._def?.shape?.() ?? {};
    const properties: Record<string, unknown> = {};
    const required: string[] = [];
    for (const [key, value] of Object.entries(shape)) {
      properties[key] = zodToJsonSchema(value);
      const innerTypeName = (value as any)?._def?.typeName ?? "";
      if (innerTypeName !== "ZodOptional" && innerTypeName !== "ZodDefault") {
        required.push(key);
      }
    }
    return { type: "object", properties, required };
  }
  if (typeName === "ZodString") {
    return { type: "string", description: schema.description ?? undefined };
  }
  if (typeName === "ZodNumber") {
    return { type: "number", description: schema.description ?? undefined };
  }
  if (typeName === "ZodBoolean") {
    return { type: "boolean", description: schema.description ?? undefined };
  }
  if (typeName === "ZodEnum") {
    return {
      type: "string",
      enum: schema._def.values,
      description: schema.description ?? undefined,
    };
  }
  if (typeName === "ZodDefault") {
    const inner = zodToJsonSchema(schema._def.innerType);
    return { ...inner, default: schema._def.defaultValue() };
  }
  if (typeName === "ZodOptional") {
    return zodToJsonSchema(schema._def.innerType);
  }
  if (typeName === "ZodNullable") {
    const inner = zodToJsonSchema(schema._def.innerType);
    return { ...inner, nullable: true };
  }
  if (typeName === "ZodArray") {
    const items = zodToJsonSchema(schema._def.type);
    return { type: "array", items };
  }
  return { type: "any" };
}

/**
 * Extract a manifest from a loaded skill definition.
 */
export function extractManifest(def: SkillDefinition): SkillManifest {
  const actions: SkillManifest["actions"] = {};
  for (const [name, action] of Object.entries(def.actions)) {
    actions[name] = {
      description: action.description,
      params: zodToJsonSchema(action.params),
      returns: action.returns ? zodToJsonSchema(action.returns) : undefined,
    };
  }

  const credentialFields: CredentialFieldManifest[] = [];
  for (const [key, field] of Object.entries(def.credentials)) {
    credentialFields.push({
      key,
      label: field.label,
      kind: field.kind,
      required: field.required !== false,
      placeholder: field.placeholder,
      help: field.help,
    });
  }

  return {
    name: def.name,
    title: def.title,
    logo: def.logo,
    doc: def.doc,
    description: def.description,
    actions,
    credentialFields,
  };
}
