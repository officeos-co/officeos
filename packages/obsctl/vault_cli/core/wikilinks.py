"""Wikilink extraction and replacement for Obsidian note content."""

import re

# Precompiled patterns
_FENCED_CODE_RE = re.compile(r"```.*?```", re.DOTALL)
_INLINE_CODE_RE = re.compile(r"`[^`]+`")
_WIKILINK_RE = re.compile(r"\[\[([^\]]+?)\]\]")


def extract_wikilinks(text):
    """Extract wikilink targets from text, ignoring code blocks and inline code.

    Returns a list of note names (strings). Handles:
    - [[Note]]
    - [[Note|Display]]
    - [[Note#Heading]]
    - [[Note#Heading|Display]]
    - Ignores wikilinks inside fenced code blocks and inline code
    - Ignores empty [[]] and whitespace-only wikilinks
    """
    # Step 1: Remove fenced code blocks
    # Match ``` ... ``` (with optional language)
    text_no_fenced = re.sub(r"```.*?```", "", text, flags=re.DOTALL)

    # Step 2: Remove inline code
    text_clean = re.sub(r"`[^`]+`", "", text_no_fenced)

    # Step 3: Extract wikilinks
    # Match [[...]] but not empty or whitespace-only
    pattern = re.compile(r"\[\[([^\]]+?)\]\]")
    matches = pattern.findall(text_clean)

    results = []
    for match in matches:
        # Strip display text (after |)
        note_part = match.split("|")[0]
        # Strip heading (after #)
        note_name = note_part.split("#")[0]
        # Strip whitespace
        note_name = note_name.strip()
        # Skip empty or whitespace-only
        if note_name:
            results.append(note_name)

    return results


def replace_wikilinks(text, old_name, new_name, return_count=False):
    """Replace wikilinks targeting old_name with new_name in text.

    Handles all wikilink variants:
    - [[Old Name]] -> [[New Name]]
    - [[Old Name|display]] -> [[New Name|display]]
    - [[Old Name#heading]] -> [[New Name#heading]]
    - [[Old Name#heading|display]] -> [[New Name#heading|display]]

    Respects code blocks: wikilinks inside fenced code blocks or inline code
    are NOT modified.

    Matching is case-insensitive. The new_name's exact casing is always used.

    Args:
        text: The note content to process.
        old_name: The current note name to find (without .md).
        new_name: The replacement note name (without .md).
        return_count: If True, return (new_text, count) tuple.

    Returns:
        Modified text string, or (text, count) if return_count=True.
    """
    if not text:
        return ("", 0) if return_count else ""

    # Build a map of protected regions (code blocks) that must not be modified.
    # We replace them with placeholders, do the wikilink replacement, then
    # restore them.
    protected = []
    placeholder_prefix = "\x00PROTECTED_"

    def _protect(match):
        idx = len(protected)
        protected.append(match.group(0))
        return f"{placeholder_prefix}{idx}\x00"

    # Protect fenced code blocks first (they may contain inline code)
    working = _FENCED_CODE_RE.sub(_protect, text)
    # Then protect inline code
    working = _INLINE_CODE_RE.sub(_protect, working)

    # Build regex that matches [[old_name...]] with optional #heading and |display
    # Escape old_name for regex safety
    escaped = re.escape(old_name)
    # Match: [[old_name]] or [[old_name|...]] or [[old_name#...]] or [[old_name#...|...]]
    # Case-insensitive on the name part only
    # The pattern matches the exact note name, not partial matches
    pattern = re.compile(
        r"\[\["
        r"(" + escaped + r")"  # group 1: the note name
        r"((?:#[^\]|]*)?)"  # group 2: optional #heading
        r"((?:\|[^\]]*)?)"  # group 3: optional |display
        r"\]\]",
        re.IGNORECASE,
    )

    count = 0

    def _replace(match):
        nonlocal count
        count += 1
        heading = match.group(2)  # e.g. "#Section" or ""
        display = match.group(3)  # e.g. "|My Text" or ""
        return f"[[{new_name}{heading}{display}]]"

    working = pattern.sub(_replace, working)

    # Restore protected regions
    for idx in range(len(protected) - 1, -1, -1):
        working = working.replace(f"{placeholder_prefix}{idx}\x00", protected[idx])

    if return_count:
        return working, count
    return working
