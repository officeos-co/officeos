"""Output formatting for obsidian-vault-cli."""

import json


def format_output(data, json_mode=False):
    """Format data for display.

    Args:
        data: data to format (string, list, dict, etc.)
        json_mode: if True, output as JSON string
    """
    if json_mode:
        return json.dumps(data, indent=2, default=str)

    if isinstance(data, str):
        return data
    if isinstance(data, list):
        return "\n".join(str(item) for item in data)
    if isinstance(data, dict):
        lines = []
        for key, value in data.items():
            lines.append(f"{key}: {value}")
        return "\n".join(lines)
    return str(data)
