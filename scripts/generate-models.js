/**
 * Generates idiomatic C# models from official Stedi OpenAPI specifications.
 * Run: node scripts/generate-models.js
 */
"use strict";

const fs = require("fs");
const path = require("path");

const ROOT = path.resolve(__dirname, "..");
const OPENAPI = path.join(ROOT, "openapi");
const OUT_DIR = path.join(ROOT, "src", "Stedi.Healthcare", "Models", "Generated");

const SPECS = [
  { file: "payers.json", domain: "Payers" },
  { file: "enrollment.json", domain: "Enrollment" },
  { file: "claims.json", domain: "Attachments" },
  { file: "healthcare.json", domain: "Healthcare" },
  { file: "manager.json", domain: "Manager" },
  { file: "core.json", domain: "Core" },
  { file: "event-destinations.json", domain: "Events" },
];

const KEYWORDS = new Set([
  "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
  "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
  "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
  "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
  "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
  "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
  "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
  "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
  "void", "volatile", "while", "record", "required", "nint", "nuint", "file", "scoped",
]);

const CLOSED_ENUMS = new Set([
  "EnrollmentStatus",
  "InitialEnrollmentStatus",
  "DocumentStatus",
  "EnrollmentSource",
  "EnrollmentHistoryChangeType",
  "ResponsibleParty",
  "FieldType",
  "TaxIdType",
  "AggregationPreference",
  "BatchStatus",
  "BatchType",
  "BatchSource",
  "BatchItemState",
  "CoverageType",
  "TransactionSupportValue",
  "TransactionFilterValue",
  "DestinationStatus",
  "DestinationInputStatus",
  "ExecutionStatus",
  "ExecutionMode",
  "TransactionStatus",
  "EnrollmentProcessType",
  "EnrollmentProcessTimeframe",
  "RequestedEffectiveDate",
  "SupportedAggregationType",
  "Program",
]);

const SKIP_NAME = /Exception|ErrorResponseContent|ThrottlingException|QuotaExceeded|UnauthorizedException|AccessDeniedException$|ForbiddenException|NotFoundException|ConflictException|TooManyRequests|InternalServer|InternalFailure|GatewayTimeout|ServiceUnavailable|AuthenticationFailed|ContentTooLarge|PdfRenderLimit/;

function loadSpec(file) {
  return JSON.parse(fs.readFileSync(path.join(OPENAPI, file), "utf8"));
}

function pascal(name) {
  if (!name) return "Value";
  const cleaned = String(name)
    .replace(/[^A-Za-z0-9]+/g, " ")
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2");
  let result = cleaned
    .split(" ")
    .filter(Boolean)
    .map((p) => p.charAt(0).toUpperCase() + p.slice(1))
    .join("");
  if (!result) result = "Value";
  if (/^[0-9]/.test(result)) result = "N" + result;
  return result;
}

function ident(name) {
  let n = pascal(name);
  if (KEYWORDS.has(n.toLowerCase())) n = "@" + n;
  return n;
}

function publicIdent(name) {
  const n = ident(name);
  return n.startsWith("@") ? n : n;
}

function xmlEscape(text) {
  return String(text)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

function summarize(desc) {
  if (!desc) return "";
  let t = String(desc).replace(/\r\n/g, "\n").trim();
  t = t.replace(/\[([^\]]+)\]\([^)]+\)/g, "$1");
  const first = t.split("\n").find((l) => l.trim()) || t;
  if (first.length > 350) return first.slice(0, 347) + "...";
  return first;
}

function xmlSummary(desc) {
  const s = summarize(desc);
  if (!s) return "";
  return `    /// <summary>\n    /// ${xmlEscape(s).replace(/\n/g, "\n    /// ")}\n    /// </summary>\n`;
}

function refName(ref) {
  if (!ref) return null;
  const parts = ref.split("/");
  return parts[parts.length - 1];
}

function unwrapSchema(spec, schema) {
  if (!schema) return { schema: { type: "object" }, name: null };
  if (schema.$ref) {
    const name = refName(schema.$ref);
    return { schema: spec.components.schemas[name] || {}, name };
  }
  return { schema, name: null };
}

function flattenObject(spec, schema) {
  const props = {};
  const required = new Set();
  const visit = (s, depth = 0) => {
    if (!s || depth > 8) return;
    if (s.$ref) {
      const n = refName(s.$ref);
      visit(spec.components.schemas[n], depth + 1);
      return;
    }
    if (s.allOf) s.allOf.forEach((p) => visit(p, depth + 1));
    if (s.oneOf) s.oneOf.forEach((p) => visit(p, depth + 1));
    if (s.anyOf) s.anyOf.forEach((p) => visit(p, depth + 1));
    if (s.properties) {
      for (const [k, v] of Object.entries(s.properties)) {
        if (!props[k]) props[k] = v;
      }
    }
    (s.required || []).forEach((r) => required.add(r));
  };
  visit(schema);
  return { properties: props, required: [...required], description: schema.description };
}

function isClosedEnum(name, schema) {
  if (!schema || !schema.enum || schema.type === "integer") return false;
  if (CLOSED_ENUMS.has(name)) return true;
  if (/non-compliant|other values|Payers may/i.test(schema.description || "")) return false;
  const values = schema.enum.filter((v) => v != null).map(String);
  if (values.length === 0 || values.length > 24) return false;
  return values.every((v) => /^[A-Z][A-Z0-9_]{3,}$/.test(v));
}

function collectRefs(spec, schema, into, visiting = new Set()) {
  if (!schema) return;
  if (schema.$ref) {
    const name = refName(schema.$ref);
    if (!name || visiting.has(name)) return;
    visiting.add(name);
    into.add(name);
    collectRefs(spec, spec.components.schemas[name], into, visiting);
    return;
  }
  const nest = [];
  if (schema.items) nest.push(schema.items);
  if (schema.additionalProperties && typeof schema.additionalProperties === "object") {
    nest.push(schema.additionalProperties);
  }
  for (const key of ["allOf", "oneOf", "anyOf"]) {
    if (Array.isArray(schema[key])) nest.push(...schema[key]);
  }
  if (schema.properties) nest.push(...Object.values(schema.properties));
  nest.forEach((n) => collectRefs(spec, n, into, visiting));
}

function reachableFromOperations(spec) {
  const names = new Set();
  for (const methods of Object.values(spec.paths || {})) {
    for (const [method, op] of Object.entries(methods)) {
      if (typeof op !== "object" || method === "parameters") continue;
      for (const p of op.parameters || []) {
        if (p.$ref) collectRefs(spec, p, names);
        else if (p.schema) collectRefs(spec, p.schema, names);
      }
      const req = op.requestBody && op.requestBody.content;
      if (req) {
        for (const body of Object.values(req)) {
          if (body.schema) collectRefs(spec, body.schema, names);
        }
      }
      for (const [code, resp] of Object.entries(op.responses || {})) {
        if (!/^2/.test(code) && code !== "302") continue;
        if (!resp || !resp.content) continue;
        for (const body of Object.values(resp.content)) {
          if (body.schema) collectRefs(spec, body.schema, names);
        }
      }
    }
  }
  return names;
}

function csType(spec, schema, generatedEnums, generatedObjects) {
  const unwrapped = unwrapSchema(spec, schema);
  const s = unwrapped.schema || {};
  const named = unwrapped.name;

  if (named && generatedEnums.has(named)) return generatedEnums.get(named) + "?";
  if (named && generatedObjects.has(named)) return generatedObjects.get(named) + "?";

  if (s.enum && named && generatedEnums.has(named)) return generatedEnums.get(named) + "?";

  if (s.oneOf || s.anyOf || s.allOf) {
    if (named && generatedObjects.has(named)) return generatedObjects.get(named) + "?";
    const flat = flattenObject(spec, s);
    if (Object.keys(flat.properties).length > 0) {
      return "JsonElement?";
    }
  }

  const types = Array.isArray(s.type) ? s.type.filter((t) => t !== "null") : [s.type];
  const t = types[0];

  if (s.items || t === "array") {
    const item = csType(spec, s.items || {}, generatedEnums, generatedObjects);
    return `IReadOnlyList<${item}>?`;
  }

  if (t === "integer") {
    if (s.format === "int64") return "long?";
    return "int?";
  }
  if (t === "number") return "decimal?";
  if (t === "boolean") return "bool?";
  if (t === "string") {
    if (s.format === "date-time") return "DateTimeOffset?";
    if (s.format === "uuid") return "string?";
    if (s.contentMediaType === "application/pdf") return "string?";
    return "string?";
  }
  if (t === "object" || s.properties || s.additionalProperties) {
    if (named && generatedObjects.has(named)) return generatedObjects.get(named) + "?";
    if (!s.properties && s.additionalProperties) {
      if (s.additionalProperties === true) return "IReadOnlyDictionary<string, JsonElement>?";
      const val = csType(spec, s.additionalProperties, generatedEnums, generatedObjects);
      return `IReadOnlyDictionary<string, ${val}>?`;
    }
    return "JsonElement?";
  }
  if (named && generatedObjects.has(named)) return generatedObjects.get(named) + "?";
  if (s.enum) return "string?";
  return "string?";
}

function cleanTypeName(name) {
  let n = name;
  n = n.replace(/RequestContent$/, "Request");
  n = n.replace(/ResponseContent$/, "Response");
  n = n.replace(/OutputPayload$/, "Payload");
  if (n === "Task") n = "EnrollmentTask";
  if (n === "Program") n = "PayerProgram";
  return ident(n);
}

function emitEnum(name, schema) {
  const typeName = ident(name);
  let code = xmlSummary(schema.description);
  code += `    [JsonConverter(typeof(JsonStringEnumConverter))]\n`;
  code += `    public enum ${typeName}\n    {\n`;
  const seen = new Set();
  for (const raw of schema.enum) {
    if (raw == null) continue;
    let member = String(raw);
    if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(member)) continue;
    if (KEYWORDS.has(member.toLowerCase())) member = "@" + member;
    if (seen.has(member.toLowerCase())) continue;
    seen.add(member.toLowerCase());
    code += `        ${member},\n`;
  }
  code += `    }\n\n`;
  return { name: typeName, code };
}

function emitClass(spec, originalName, typeName, schema, generatedEnums, generatedObjects) {
  const flat = flattenObject(spec, schema);
  const properties = flat.properties;
  const isStringAlias = schema.type === "string" && !schema.properties && !schema.enum;
  if (isStringAlias) return null;

  if (schema.type === "array") return null;

  let code = xmlSummary(schema.description || `Model generated from Stedi OpenAPI schema \`${originalName}\`.`);
  code += `    public sealed class ${typeName}\n    {\n`;

  const used = new Set([typeName.toLowerCase()]);
  const entries = Object.entries(properties);
  if (entries.length === 0 && !schema.additionalProperties) {
    code += `        /// <summary>Captures undeclared properties for forward compatibility.</summary>\n`;
    code += `        [JsonExtensionData]\n`;
    code += `        public Dictionary<string, JsonElement>? ExtensionData { get; set; }\n`;
    code += `    }\n\n`;
    return code;
  }

  for (const [jsonName, propSchema] of entries) {
    let propName = publicIdent(jsonName);
    if (used.has(propName.replace(/^@/, "").toLowerCase())) {
      propName = propName.replace(/^@/, "") + "Value";
      propName = ident(propName);
    }
    used.add(propName.replace(/^@/, "").toLowerCase());
    const unwrapped = unwrapSchema(spec, propSchema);
    const type = csType(spec, propSchema, generatedEnums, generatedObjects);
    const desc = (propSchema && propSchema.description) || (unwrapped.schema && unwrapped.schema.description) || "";
    code += xmlSummary(desc);
    if (jsonName !== propName.replace(/^@/, "")) {
      code += `        [JsonPropertyName("${jsonName}")]\n`;
    } else {
      // System.Text.Json is case-sensitive; keep explicit names for camelCase wire fields.
      code += `        [JsonPropertyName("${jsonName}")]\n`;
    }
    code += `        public ${type} ${propName} { get; set; }\n\n`;
  }

  code += `        /// <summary>Captures undeclared properties for forward compatibility.</summary>\n`;
  code += `        [JsonExtensionData]\n`;
  code += `        public Dictionary<string, JsonElement>? ExtensionData { get; set; }\n`;
  code += `    }\n\n`;
  return code;
}

function main() {
  fs.mkdirSync(OUT_DIR, { recursive: true });
  for (const existing of fs.readdirSync(OUT_DIR)) {
    if (existing.endsWith(".g.cs")) fs.unlinkSync(path.join(OUT_DIR, existing));
  }

  const generatedEnums = new Map(); // schemaName -> csharp name
  const generatedObjects = new Map();
  const files = new Map(); // domain -> code chunks
  const schemaHashes = new Map(); // csharpName -> hash
  const notes = [];

  const add = (domain, chunk) => {
    if (!files.has(domain)) files.set(domain, []);
    files.get(domain).push(chunk);
  };

  for (const specDef of SPECS) {
    const spec = loadSpec(specDef.file);
    const reachable = reachableFromOperations(spec);
    const schemas = spec.components && spec.components.schemas ? spec.components.schemas : {};

    // First pass: decide type names
    const localEnums = [];
    const localObjects = [];

    for (const name of [...reachable].sort()) {
      const schema = schemas[name];
      if (!schema) continue;
      if (SKIP_NAME.test(name) && !/EligibilityCheckError|ClaimsError|Warning|EditResponse/.test(name)) {
        continue;
      }

      if (schema.enum && isClosedEnum(name, schema)) {
        const typeName = ident(name);
        if (generatedEnums.has(name) || [...generatedEnums.values()].includes(typeName)) {
          generatedEnums.set(name, typeName);
          continue;
        }
        generatedEnums.set(name, typeName);
        localEnums.push({ name, schema, typeName });
        continue;
      }

      if (schema.enum && !schema.properties) {
        // open-ended / X12 codes: no dedicated type
        continue;
      }

      if (schema.type === "string" && !schema.properties) continue;
      if (schema.type === "integer" || schema.type === "number" || schema.type === "boolean") continue;
      if (schema.type === "array" && !schema.properties) continue;

      let typeName = cleanTypeName(name);
      const hash = JSON.stringify(schema);
      if (schemaHashes.has(typeName)) {
        if (schemaHashes.get(typeName) === hash) {
          generatedObjects.set(name, typeName);
          continue;
        }
        const prefixed = ident(specDef.domain + typeName);
        notes.push(`Renamed colliding schema ${name} from ${specDef.file} to ${prefixed}`);
        typeName = prefixed;
      }
      schemaHashes.set(typeName, hash);
      generatedObjects.set(name, typeName);
      localObjects.push({ name, schema, typeName });
    }

    for (const e of localEnums) {
      const emitted = emitEnum(e.name, e.schema);
      add(specDef.domain, emitted.code);
    }

    for (const o of localObjects) {
      const code = emitClass(spec, o.name, o.typeName, o.schema, generatedEnums, generatedObjects);
      if (code) add(specDef.domain, code);
    }
  }

  const header = `// <auto-generated />
#nullable enable
#pragma warning disable CS1591
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stedi.Healthcare.Models;

`;

  for (const [domain, chunks] of files) {
    const body = chunks.join("");
    const filePath = path.join(OUT_DIR, `${domain}Models.g.cs`);
    fs.writeFileSync(filePath, header + body, "utf8");
    console.log("Wrote", filePath, "chars", body.length);
  }

  const mapPath = path.join(OUT_DIR, "_schema-map.json");
  const map = {
    enums: Object.fromEntries(generatedEnums),
    objects: Object.fromEntries(generatedObjects),
    notes,
  };
  fs.writeFileSync(mapPath, JSON.stringify(map, null, 2));
  console.log("Types", generatedObjects.size, "enums", generatedEnums.size);
  notes.forEach((n) => console.log("NOTE", n));
}

main();
