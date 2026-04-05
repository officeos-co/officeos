"""In-memory vault index for graph operations, tags, and search."""

import os

from vault_cli.core.wikilinks import extract_wikilinks
from vault_cli.core.frontmatter import parse_frontmatter


class VaultIndex:
    """In-memory index of vault notes for graph and search operations."""

    def __init__(self):
        self.notes = {}  # path -> content
        self._links = {}  # path -> list of wikilink target names
        self._backlinks = {}  # note_name (lowercase) -> list of source paths
        self._tags = {}  # tag_name -> count
        self._note_names = {}  # lowercase basename (without .md) -> path

    def build(self, notes):
        """Build the index from a list of (path, content) tuples.

        Args:
            notes: list of (path, content) tuples
        """
        self.notes = {}
        self._links = {}
        self._backlinks = {}
        self._tags = {}
        self._note_names = {}

        # First pass: index all notes and extract data
        for path, content in notes:
            self.notes[path] = content

            # Build name -> path mapping (basename without .md, lowercase)
            basename = os.path.basename(path)
            if basename.lower().endswith(".md"):
                name = basename[:-3]
            else:
                name = basename
            self._note_names[name.lower()] = path

            # Extract wikilinks
            links = extract_wikilinks(content)
            # Deduplicate
            unique_links = list(dict.fromkeys(links))
            self._links[path] = unique_links

            # Extract tags from frontmatter
            metadata, _ = parse_frontmatter(content)
            tags = metadata.get("tags", [])
            if isinstance(tags, list):
                for tag in tags:
                    if tag and str(tag).strip():
                        tag_str = str(tag).strip()
                        self._tags[tag_str] = self._tags.get(tag_str, 0) + 1

        # Second pass: build backlinks
        for path, links in self._links.items():
            for link_name in links:
                link_lower = link_name.lower()
                if link_lower not in self._backlinks:
                    self._backlinks[link_lower] = []
                self._backlinks[link_lower].append(path)

    def get_links(self, path):
        """Get outgoing wikilink targets for a note.

        Returns list of note names that the given note links to.
        """
        return self._links.get(path, [])

    def get_backlinks(self, path):
        """Get incoming links for a note.

        Returns list of paths that link to this note via [[wikilinks]].
        Matching is case-insensitive on the note's basename (without .md).
        """
        basename = os.path.basename(path)
        if basename.lower().endswith(".md"):
            name = basename[:-3]
        else:
            name = basename

        return self._backlinks.get(name.lower(), [])

    def get_unresolved(self):
        """Get wikilink targets that don't correspond to any existing note.

        Returns list of wikilink target names that have no matching note.
        """
        unresolved = set()
        all_link_targets = set()

        for path, links in self._links.items():
            for link_name in links:
                all_link_targets.add(link_name)

        for link_name in all_link_targets:
            if link_name.lower() not in self._note_names:
                unresolved.add(link_name)

        return sorted(unresolved)

    def get_orphans(self):
        """Get notes with zero incoming links.

        Returns list of note paths that are not linked to by any other note.
        """
        orphans = []
        for path in self.notes:
            basename = os.path.basename(path)
            if basename.lower().endswith(".md"):
                name = basename[:-3]
            else:
                name = basename

            backlinks = self._backlinks.get(name.lower(), [])
            if len(backlinks) == 0:
                orphans.append(path)

        return orphans

    def get_all_tags(self):
        """Get all tags with counts.

        Returns dict of {tag_name: count}.
        """
        return dict(self._tags)

    def search_content(self, query, context=False):
        """Full-text search across all note content.

        Args:
            query: search string (case-insensitive)
            context: if True, include surrounding lines in results

        Returns list of match dicts with path, line, line_number, and optionally context.
        """
        results = []
        query_lower = query.lower()

        for path, content in self.notes.items():
            lines = content.split("\n")
            for i, line in enumerate(lines):
                if query_lower in line.lower():
                    match = {
                        "path": path,
                        "line": line,
                        "line_number": i + 1,
                    }
                    if context:
                        start = max(0, i - 2)
                        end = min(len(lines), i + 3)
                        match["context"] = lines[start:end]
                    results.append(match)

        return results
