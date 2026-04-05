"""Wrapped error handlers for CLI commands — context, CouchDB body, hints."""

import click


def _format_couch_error(operation, path, exc):
    """Format an HTTP/connection error with operation context and hints, then exit.

    Produces messages like:
        Error writing "References/Mathe I.md":
          CouchDB 409 Conflict — document update conflict.
          Hint: The doc was modified externally. Re-read and retry.
    """
    import requests

    if isinstance(exc, requests.HTTPError) and exc.response is not None:
        status = exc.response.status_code
        try:
            body = exc.response.json()
        except Exception:
            body = {"error": exc.response.text}

        error_str = body.get("error", "")
        reason = body.get("reason", "")

        hint = _hint_for_status(status, operation)
        hint_line = f"\n  Hint: {hint}" if hint else ""

        click.echo(
            f'Error {operation} "{path}":\n'
            f"  CouchDB {status} {error_str} — {reason}{hint_line}",
            err=True,
        )
    elif isinstance(exc, ConnectionError):
        click.echo(
            f'Error {operation} "{path}":\n'
            f"  Connection refused. Is the server running?\n"
            f"  Check: vault ping",
            err=True,
        )
    else:
        click.echo(f'Error {operation} "{path}":\n  {exc}', err=True)

    raise SystemExit(1)


def _hint_for_status(status, operation):
    """Return a human-readable hint for common CouchDB status codes."""
    hints = {
        409: "The doc was modified externally. Re-read and retry.",
        401: "Authentication failed — check credentials in config.",
        403: "Permission denied — check CouchDB user permissions.",
        404: "Document not found — it may have been deleted.",
    }
    return hints.get(status)


def handle_write_error(path, exc):
    """Format a write error with context and hints, then exit."""
    _format_couch_error("writing", path, exc)


def handle_delete_error(path, exc):
    """Format a delete error with context and hints, then exit."""
    _format_couch_error("deleting", path, exc)
