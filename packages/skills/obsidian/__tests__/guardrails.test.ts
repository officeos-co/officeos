import { describe, it, expect } from "bun:test";
import { checkRules, formatViolation } from "../core/guardrails.ts";

describe("checkRules", () => {
  describe("Rule 1: Folder placement", () => {
    it("passes when note is at root (no folder)", () => {
      const violations = checkRules("Note.md", "content", new Set(["Projects"]));
      const folderViolations = violations.filter(v => v.rule_name === "Folder placement");
      expect(folderViolations).toHaveLength(0);
    });

    it("passes when target folder exists", () => {
      const violations = checkRules("Projects/My Note.md", "content", new Set(["Projects"]));
      const folderViolations = violations.filter(v => v.rule_name === "Folder placement");
      expect(folderViolations).toHaveLength(0);
    });

    it("fires when target folder does not exist", () => {
      const violations = checkRules("NewFolder/My Note.md", "content", new Set(["Projects"]));
      const folderViolations = violations.filter(v => v.rule_name === "Folder placement");
      expect(folderViolations).toHaveLength(1);
      expect(folderViolations[0].message).toContain("NewFolder");
      expect(folderViolations[0].message).toContain("Projects");
    });
  });

  describe("Rule 2: Missing categories", () => {
    it("passes when categories frontmatter is present", () => {
      const content = `---\ncategories:\n  - "[[Projects]]"\n---\n# Note`;
      const violations = checkRules("Note.md", content, new Set());
      const catViolations = violations.filter(v => v.rule_name === "Properties system");
      expect(catViolations).toHaveLength(0);
    });

    it("fires when no categories in frontmatter", () => {
      const violations = checkRules("Note.md", "# Just a heading", new Set());
      const catViolations = violations.filter(v => v.rule_name === "Properties system");
      expect(catViolations).toHaveLength(1);
      expect(catViolations[0].message).toContain("Note");
    });

    it("passes when categories is an empty list", () => {
      const content = `---\ncategories: []\n---\n`;
      const violations = checkRules("Note.md", content, new Set());
      const catViolations = violations.filter(v => v.rule_name === "Properties system");
      expect(catViolations).toHaveLength(0);
    });
  });

  describe("Rule 3: Folder ↔ category mismatch", () => {
    it("fires when note has [[References]] category but is at root", () => {
      const content = `---\ncategories:\n  - "[[References]]"\n---\n`;
      const violations = checkRules("Some Note.md", content, new Set());
      const placementViolations = violations.filter(v => v.rule_name === "Placement rules");
      expect(placementViolations).toHaveLength(1);
      expect(placementViolations[0].message).toContain("categorized as");
    });

    it("fires when note is in References/ but lacks [[References]] category", () => {
      const content = `---\ncategories:\n  - "[[Projects]]"\n---\n`;
      const violations = checkRules("References/Some Note.md", content, new Set(["References"]));
      const placementViolations = violations.filter(v => v.rule_name === "Placement rules");
      expect(placementViolations).toHaveLength(1);
      expect(placementViolations[0].message).toContain("References/");
    });

    it("passes when References note is in References/ folder", () => {
      const content = `---\ncategories:\n  - "[[References]]"\n---\n`;
      const violations = checkRules("References/Some Note.md", content, new Set(["References"]));
      const placementViolations = violations.filter(v => v.rule_name === "Placement rules");
      expect(placementViolations).toHaveLength(0);
    });

    it("strips [[wikilink]] brackets when comparing category names", () => {
      const content = `---\ncategories:\n  - "[[References]]"\n---\n`;
      const violations = checkRules("References/Note.md", content, new Set(["References"]));
      const placementViolations = violations.filter(v => v.rule_name === "Placement rules");
      expect(placementViolations).toHaveLength(0);
    });
  });
});

describe("formatViolation", () => {
  it("formats violation with rule name, message, and reference", () => {
    const output = formatViolation({
      rule_name: "Folder placement",
      message: "Folder X does not exist.",
      reference: "vault design rules",
    });
    expect(output).toContain("⚠ Rule: Folder placement");
    expect(output).toContain("Folder X does not exist.");
    expect(output).toContain("See: vault design rules");
  });
});
