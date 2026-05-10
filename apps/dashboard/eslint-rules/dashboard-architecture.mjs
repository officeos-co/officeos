import path from "node:path";

const FEATURE_LANES = new Set(["api", "components", "hooks", "data"]);
const FEATURE_ROOT_FILES = new Set(["index.ts", "types.ts"]);
const FORBIDDEN_BUCKET_NAMES = new Set([
  "cards",
  "common",
  "dialogs",
  "forms",
  "helpers",
  "mutations",
  "queries",
  "shared",
  "tables",
  "tabs",
  "types",
  "utils",
]);

const APP_ROUTE_FILES = new Set([
  "page.tsx",
  "layout.tsx",
  "loading.tsx",
  "error.tsx",
  "not-found.tsx",
  "route.ts",
  "providers.tsx",
  "globals.css",
]);

const APP_ASSET_EXTENSIONS = new Set([".ico", ".png", ".jpg", ".jpeg", ".svg", ".webp"]);

const SHELL_ALLOWED_IMPORTS = [
  /^@\/ui(?:\/|$)/,
  /^@\/hooks(?:\/|$)/,
  /^@\/contexts(?:\/|$)/,
  /^@\/lib(?:\/|$)/,
  /^@\/types(?:\/|$)/,
  /^@\/features\/[^/]+$/,
];

const APOLLO_ALLOWED_FILES = [
  /^src\/features\/[^/]+\/api\/use[A-Z][A-Za-z0-9]*\.ts$/,
  /^src\/app\/providers\.tsx$/,
  /^src\/lib\/graphql\/client\.ts$/,
  /^src\/hooks\/useAuth\.ts$/,
];

function toProjectPath(filename) {
  if (!filename || filename === "<input>") {
    return filename;
  }

  return path.relative(process.cwd(), filename).replaceAll(path.sep, "/");
}

function basename(projectPath) {
  return projectPath.split("/").at(-1) ?? projectPath;
}

function isKebabCaseFileName(fileName, extensions) {
  const extension = extensions.find((candidate) => fileName.endsWith(candidate));

  if (!extension) {
    return false;
  }

  const stem = fileName.slice(0, -extension.length);
  return /^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$/.test(stem);
}

function isUseHookFileName(fileName) {
  return /^use[A-Z][A-Za-z0-9]*\.ts$/.test(fileName);
}

function isAllowedApolloPath(projectPath) {
  return APOLLO_ALLOWED_FILES.some((pattern) => pattern.test(projectPath));
}

function isPageFile(projectPath) {
  return /^src\/app\/.+\/page\.tsx$/.test(projectPath) || projectPath === "src/app/page.tsx";
}

function getFeatureFromPath(projectPath) {
  const match = projectPath.match(/^src\/features\/([^/]+)(?:\/|$)/);
  return match?.[1] ?? null;
}

function report(context, node, message) {
  context.report({ node, message });
}

function checkPath(context, projectPath, node) {
  const fileName = basename(projectPath);

  if (projectPath.startsWith("src/features/")) {
    const parts = projectPath.split("/");
    const feature = parts[2];
    const featureParts = parts.slice(3);

    if (featureParts.length === 0) {
      return;
    }

    if (featureParts.length === 1) {
      if (!FEATURE_ROOT_FILES.has(fileName)) {
        report(
          context,
          node,
          `DASH001: Feature root files are limited to index.ts and types.ts. Move ${fileName} into src/features/${feature}/api, components, hooks, or data.`,
        );
      }
      return;
    }

    const lane = featureParts[0];

    if (!FEATURE_LANES.has(lane)) {
      report(
        context,
        node,
        `DASH002: Feature folders may only contain api, components, hooks, data, index.ts, and types.ts. Found ${lane} in src/features/${feature}.`,
      );
      return;
    }

    if (featureParts.length > 2) {
      report(
        context,
        node,
        `DASH003: Feature ${lane} folders must stay flat. Use a precise file name instead of nested folders under src/features/${feature}/${lane}.`,
      );
      return;
    }

    if (FORBIDDEN_BUCKET_NAMES.has(fileName.replace(/\.[^.]+$/, ""))) {
      report(
        context,
        node,
        `DASH004: Bucket file names such as ${fileName} are not allowed. Use a domain-specific file name.`,
      );
    }

    if (lane === "api" && !isUseHookFileName(fileName)) {
      report(
        context,
        node,
        `DASH005: Feature API files must be named useX.ts and live directly under src/features/${feature}/api.`,
      );
    }

    if (lane === "hooks" && !isUseHookFileName(fileName)) {
      report(
        context,
        node,
        `DASH006: Feature hook files must be named useX.ts and live directly under src/features/${feature}/hooks.`,
      );
    }

    if (lane === "components" && !isKebabCaseFileName(fileName, [".tsx", ".ts"])) {
      report(
        context,
        node,
        `DASH007: Feature component files must use kebab-case .tsx or .ts names and live directly under src/features/${feature}/components.`,
      );
    }

    if (lane === "data" && !isKebabCaseFileName(fileName, [".ts"])) {
      report(
        context,
        node,
        `DASH008: Feature data files must use kebab-case .ts names and live directly under src/features/${feature}/data.`,
      );
    }
  }

  if (projectPath.startsWith("src/components/")) {
    report(
      context,
      node,
      "DASH009: src/components is deprecated. Put generic primitives in src/ui, shell/chrome in src/shell, and domain UI in src/features/<feature>/components.",
    );
  }

  if (projectPath.startsWith("src/ui/")) {
    const parts = projectPath.split("/");

    if (parts.length !== 3) {
      report(context, node, "DASH010: src/ui must stay flat.");
    }

    if (!isKebabCaseFileName(fileName, [".tsx", ".ts"])) {
      report(context, node, "DASH011: UI primitive files must use kebab-case .tsx or .ts names.");
    }
  }

  if (projectPath.startsWith("src/shell/")) {
    const parts = projectPath.split("/");

    if (parts.length !== 3) {
      report(context, node, "DASH012: src/shell must stay flat.");
    }

    if (!isKebabCaseFileName(fileName, [".tsx", ".ts"])) {
      report(context, node, "DASH013: Shell files must use kebab-case .tsx or .ts names.");
    }
  }

  if (projectPath.startsWith("src/hooks/")) {
    const parts = projectPath.split("/");

    if (parts.length !== 3) {
      report(context, node, "DASH014: src/hooks must stay flat.");
    }

    if (!isUseHookFileName(fileName)) {
      report(context, node, "DASH015: Shared hook files must be named useX.ts.");
    }
  }

  if (projectPath.startsWith("src/contexts/")) {
    const parts = projectPath.split("/");

    if (parts.length !== 3) {
      report(context, node, "DASH016: src/contexts must stay flat.");
    }

    if (!/^[A-Z][A-Za-z0-9]*Context\.tsx$/.test(fileName)) {
      report(context, node, "DASH017: Context files must be named XContext.tsx.");
    }
  }

  if (projectPath.startsWith("src/types/")) {
    const parts = projectPath.split("/");

    if (parts.length !== 3) {
      report(context, node, "DASH018: src/types must stay flat.");
    }

    if (!isKebabCaseFileName(fileName, [".ts"])) {
      report(context, node, "DASH019: Cross-feature type files must use kebab-case .ts names.");
    }
  }

  if (projectPath.startsWith("src/app/")) {
    const extension = path.extname(fileName);

    if (!APP_ROUTE_FILES.has(fileName) && !APP_ASSET_EXTENSIONS.has(extension)) {
      report(
        context,
        node,
        `DASH020: App route folders may only contain Next route files and static route assets. Found ${fileName}.`,
      );
    }
  }
}

function checkImport(context, projectPath, node) {
  const source = node.source.value;

  if (typeof source !== "string") {
    return;
  }

  if (source === "@apollo/client" && !isAllowedApolloPath(projectPath)) {
    report(
      context,
      node,
      "DASH021: Apollo imports are only allowed in feature API hooks, src/app/providers.tsx, src/lib/graphql/client.ts, and src/hooks/useAuth.ts.",
    );
  }

  if (source.startsWith("@/components")) {
    report(
      context,
      node,
      "DASH022: Do not import from @/components. Use @/ui, @/shell, or a feature public barrel.",
    );
  }

  if (projectPath.startsWith("src/ui/")) {
    if (
      source.startsWith("@/features/") ||
      source.startsWith("@/app/") ||
      source.startsWith("@/contexts/") ||
      source.startsWith("@/types/") ||
      source === "@apollo/client"
    ) {
      report(
        context,
        node,
        "DASH023: UI primitives must stay generic and may not import features, app routes, contexts, domain types, or Apollo.",
      );
    }
  }

  if (projectPath.startsWith("src/shell/")) {
    const isRelative = source.startsWith(".");
    const isAlias = source.startsWith("@/");
    const isAllowedAlias = SHELL_ALLOWED_IMPORTS.some((pattern) => pattern.test(source));

    if (isAlias && !isAllowedAlias) {
      report(
        context,
        node,
        "DASH024: Shell files may import only @/ui, @/hooks, @/contexts, @/lib, @/types, and feature public barrels.",
      );
    }

    if (source === "@apollo/client") {
      report(context, node, "DASH025: Shell files must not import Apollo directly.");
    }

    if (isRelative && source.startsWith("../")) {
      report(context, node, "DASH026: src/shell is flat; use ./file-name imports only.");
    }
  }

  const privateFeatureImport = source.match(/^@\/features\/([^/]+)\/(.+)/);

  if (privateFeatureImport) {
    const importedFeature = privateFeatureImport[1];
    const importedPath = privateFeatureImport[2];
    const currentFeature = getFeatureFromPath(projectPath);

    if (currentFeature === importedFeature) {
      report(
        context,
        node,
        `DASH027: Code inside src/features/${importedFeature} must use relative imports for private feature files.`,
      );
    } else {
      report(
        context,
        node,
        `DASH028: Do not import private feature files (${importedPath}) from src/features/${importedFeature}. Import its public barrel or move shared code to src/types, src/hooks, src/lib, src/ui, or src/shell.`,
      );
    }
  }
}

function checkNoGqlOutsideApi(context, projectPath, node) {
  const tag = node.tag;
  const isGqlTag =
    tag.type === "Identifier" && tag.name === "gql";

  if (isGqlTag && !isAllowedApolloPath(projectPath)) {
    report(
      context,
      node,
      "DASH029: gql documents must live in feature API hooks or tracked GraphQL operation files.",
    );
  }
}

function checkNoPageTypes(context, projectPath, node) {
  if (!isPageFile(projectPath)) {
    return;
  }

  report(
    context,
    node,
    "DASH030: Do not define TypeScript types, interfaces, or enums in page.tsx files. Move route-safe types to a feature API hook, feature types.ts, src/types, or keep values inline.",
  );
}

const dashboardArchitecturePlugin = {
  rules: {
    "path-naming": {
      meta: {
        type: "problem",
        docs: {
          description: "Enforce dashboard path flatness and file naming conventions.",
        },
        schema: [],
      },
      create(context) {
        const projectPath = toProjectPath(context.filename);

        return {
          Program(node) {
            checkPath(context, projectPath, node);
          },
        };
      },
    },
    "boundaries": {
      meta: {
        type: "problem",
        docs: {
          description: "Enforce dashboard import boundaries.",
        },
        schema: [],
      },
      create(context) {
        const projectPath = toProjectPath(context.filename);

        return {
          ImportDeclaration(node) {
            checkImport(context, projectPath, node);
          },
          TaggedTemplateExpression(node) {
            checkNoGqlOutsideApi(context, projectPath, node);
          },
        };
      },
    },
    "no-page-types": {
      meta: {
        type: "problem",
        docs: {
          description: "Disallow local type declarations in Next page files.",
        },
        schema: [],
      },
      create(context) {
        const projectPath = toProjectPath(context.filename);

        return {
          TSInterfaceDeclaration(node) {
            checkNoPageTypes(context, projectPath, node);
          },
          TSTypeAliasDeclaration(node) {
            checkNoPageTypes(context, projectPath, node);
          },
          TSEnumDeclaration(node) {
            checkNoPageTypes(context, projectPath, node);
          },
        };
      },
    },
  },
};

export default dashboardArchitecturePlugin;
