"""Frontmatter parsing and manipulation helpers."""

import frontmatter


def parse_frontmatter(content):
    """Parse YAML frontmatter from note content.

    Returns (metadata_dict, body_string).
    """
    if not content:
        return {}, ""

    # Check if content starts with frontmatter delimiter
    if not content.startswith("---"):
        return {}, content

    try:
        post = frontmatter.loads(content)
        metadata = dict(post.metadata)
        # If no metadata was parsed, return original content as body
        if not metadata:
            return {}, content
        return metadata, post.content
    except Exception:
        # If parsing fails (e.g., unclosed frontmatter), return empty metadata
        return {}, content


def build_note(metadata, body):
    """Build a complete note string from metadata dict and body.

    If metadata is None or empty, returns just the body.
    """
    if not metadata:
        return body

    post = frontmatter.Post(body, **metadata)
    return frontmatter.dumps(post) + "\n"


def set_property(content, key, value):
    """Set a property in the note's frontmatter. Creates frontmatter if needed."""
    metadata, body = parse_frontmatter(content)
    metadata[key] = value
    return build_note(metadata, body)


def remove_property(content, key):
    """Remove a property from the note's frontmatter. No-op if not present."""
    metadata, body = parse_frontmatter(content)
    if not metadata:
        return content
    if key not in metadata:
        return build_note(metadata, body)
    del metadata[key]
    if not metadata:
        return body
    return build_note(metadata, body)
