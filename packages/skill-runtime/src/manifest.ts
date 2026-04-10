import type { SkillDefinition } from "@eaos/skill-sdk";
import { z } from "zod";

export interface SkillManifest {
  name: string;
  description: string;
  actions: Record<
    string,
    {
      description: string;
      params: Record<string, unknown>;
    }
  >;
  credentials: Record<string, { description: string }>;
}

/**
 * Convert a Zod schema to a JSON Schema-like object for the manifest.
 */
function zodToJsonSchema(schema: z.ZodType): Record<string, unknown> {
  // Use Zod's built-in shape inspection for objects
  if (schema instanceof z.ZodObject) {
    const shape = schema.shape as Record<string, z.ZodType>;
    const properties: Record<string, unknown> = {};
    const required: string[] = [];
    for (const [key, value] of Object.entries(shape)) {
      properties[key] = zodToJsonSchema(value);
      // Check if the field is required (not optional, not defaulted)
      if (
        !(value instanceof z.ZodOptional) &&
        !(value instanceof z.ZodDefault)
      ) {
        required.push(key);
      }
    }
    return { type: "object", properties, required };
  }
  if (schema instanceof z.ZodString) {
    return { type: "string", description: schema.description ?? undefined };
  }
  if (schema instanceof z.ZodNumber) {
    return { type: "number", description: schema.description ?? undefined };
  }
  if (schema instanceof z.ZodBoolean) {
    return { type: "boolean", description: schema.description ?? undefined };
  }
  if (schema instanceof z.ZodEnum) {
    return {
      type: "string",
      enum: (schema as z.ZodEnum<[string, ...string[]]>).options,
      description: schema.description ?? undefined,
    };
  }
  if (schema instanceof z.ZodDefault) {
    const inner = zodToJsonSchema((schema as z.ZodDefault<z.ZodType>)._def.innerType);
    return { ...inner, default: (schema as z.ZodDefault<z.ZodType>)._def.defaultValue() };
  }
  if (schema instanceof z.ZodOptional) {
    return zodToJsonSchema((schema as z.ZodOptional<z.ZodType>)._def.innerType);
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
    };
  }

  const credentials: SkillManifest["credentials"] = {};
  for (const [key, schema] of Object.entries(def.credentials)) {
    credentials[key] = {
      description: schema.description ?? key,
    };
  }

  return {
    name: def.name,
    description: def.description,
    actions,
    credentials,
  };
}
