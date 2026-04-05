"""Tests for vault_cli.core.client.VaultClient.

All HTTP interactions are mocked at the requests level.
These tests define the contract that the VaultClient implementation must satisfy.
"""

import hashlib
import json
import time

import pytest
from unittest.mock import MagicMock, patch, call


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _make_response(status_code=200, json_data=None, raise_for_status=None):
    """Create a mock requests.Response."""
    resp = MagicMock()
    resp.status_code = status_code
    resp.ok = 200 <= status_code < 300
    if json_data is not None:
        resp.json.return_value = json_data
    if raise_for_status:
        resp.raise_for_status.side_effect = raise_for_status
    else:
        resp.raise_for_status.return_value = None
    return resp


def _chunk_id(data: str) -> str:
    h = hashlib.sha256(data.encode("utf-8")).hexdigest()
    return f"h:{h[:12]}"


# ---------------------------------------------------------------------------
# Test: _path_to_id
# ---------------------------------------------------------------------------


class TestPathToId:
    """VaultClient._path_to_id converts paths to CouchDB document IDs."""

    def test_lowercases_path(self):
        """Path is lowercased for the document ID."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        assert (
            client._path_to_id("References/Some Person.md")
            == "references/some person.md"
        )

    def test_handles_root_level_path(self):
        """Root-level paths are just lowercased."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        assert client._path_to_id("North Star.md") == "north star.md"

    def test_underscore_prefix_gets_slash(self):
        """Paths starting with underscore get a leading slash to avoid CouchDB reserved _id prefix."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        result = client._path_to_id("_templates/test.md")
        assert result == "/_templates/test.md"

    def test_non_underscore_prefix_no_slash(self):
        """Normal paths do not get a leading slash."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        result = client._path_to_id("inbox/note.md")
        assert not result.startswith("/")

    def test_deeply_nested_path(self):
        """Deeply nested paths are fully lowercased."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        assert client._path_to_id("A/B/C/D.md") == "a/b/c/d.md"


# ---------------------------------------------------------------------------
# Test: _create_chunk_id
# ---------------------------------------------------------------------------


class TestCreateChunkId:
    """VaultClient._create_chunk_id generates content-addressed chunk IDs."""

    def test_produces_h_prefix(self):
        """Chunk IDs start with 'h:'."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        result = client._create_chunk_id("hello world")
        assert result.startswith("h:")

    def test_uses_first_12_hex_chars_of_sha256(self):
        """Chunk ID is h: + first 12 hex chars of SHA-256 of the data."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        data = "hello world"
        expected_hash = hashlib.sha256(data.encode("utf-8")).hexdigest()[:12]
        assert client._create_chunk_id(data) == f"h:{expected_hash}"

    def test_different_data_produces_different_ids(self):
        """Different content produces different chunk IDs."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        id1 = client._create_chunk_id("content A")
        id2 = client._create_chunk_id("content B")
        assert id1 != id2

    def test_same_data_produces_same_id(self):
        """Identical content produces the same chunk ID (content-addressed)."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        id1 = client._create_chunk_id("same content")
        id2 = client._create_chunk_id("same content")
        assert id1 == id2

    def test_empty_string(self):
        """Empty string still produces a valid chunk ID."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        result = client._create_chunk_id("")
        assert result.startswith("h:")
        assert len(result) == 14  # h: + 12 chars


# ---------------------------------------------------------------------------
# Test: _create_chunks
# ---------------------------------------------------------------------------


class TestCreateChunks:
    """VaultClient._create_chunks splits content at 50KB boundaries."""

    def test_small_content_single_chunk(self):
        """Content under 50KB produces a single chunk."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        content = "Short content"
        chunks = client._create_chunks(content)
        assert len(chunks) == 1
        assert chunks[0] == content

    def test_empty_content_produces_one_empty_chunk(self):
        """Empty content produces one chunk with empty string."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        chunks = client._create_chunks("")
        assert len(chunks) == 1
        assert chunks[0] == ""

    def test_large_content_splits_at_50kb(self):
        """Content over 50KB is split into multiple chunks."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        # Create content just over 100KB (will need 3 chunks)
        content = "x" * 110_000
        chunks = client._create_chunks(content)
        assert len(chunks) == 3
        assert len(chunks[0]) == 50_000
        assert len(chunks[1]) == 50_000
        assert len(chunks[2]) == 10_000

    def test_exact_boundary_produces_correct_chunks(self):
        """Content exactly at boundary doesn't produce empty trailing chunk."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        content = "x" * 50_000
        chunks = client._create_chunks(content)
        assert len(chunks) == 1

    def test_joined_chunks_equal_original(self):
        """Joining all chunks reproduces the original content."""
        from vault_cli.core.client import VaultClient

        client = VaultClient.__new__(VaultClient)
        content = "a" * 75_000 + "b" * 75_000
        chunks = client._create_chunks(content)
        assert "".join(chunks) == content


# ---------------------------------------------------------------------------
# Test: ping
# ---------------------------------------------------------------------------


class TestPing:
    """VaultClient.ping() tests CouchDB connectivity."""

    @patch("vault_cli.core.client.requests")
    def test_ping_success(self, mock_requests):
        """ping() returns dict with ok=True when server is reachable."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session
        mock_session.get.return_value = _make_response(
            200, {"db_name": "obsidian", "doc_count": 42}
        )

        from vault_cli.core.client import VaultClient

        client = VaultClient(
            host="localhost",
            port=5984,
            database="obsidian",
            username="admin",
            password="secret",
        )
        result = client.ping()
        assert result["ok"] is True

    @patch("vault_cli.core.client.requests")
    def test_ping_connection_refused(self, mock_requests):
        """ping() raises/returns error when connection is refused."""
        import requests as real_requests

        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session
        mock_session.get.side_effect = real_requests.ConnectionError(
            "Connection refused"
        )

        from vault_cli.core.client import VaultClient

        client = VaultClient(
            host="localhost",
            port=5984,
            database="obsidian",
            username="admin",
            password="secret",
        )
        with pytest.raises(Exception) as exc_info:
            client.ping()
        # Should mention connection/unreachable
        assert (
            "refused" in str(exc_info.value).lower()
            or "unavailable" in str(exc_info.value).lower()
            or "reachable" in str(exc_info.value).lower()
            or "connection" in str(exc_info.value).lower()
        )

    @patch("vault_cli.core.client.requests")
    def test_ping_unauthorized(self, mock_requests):
        """ping() raises error on 401 with auth failure message."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session
        resp = _make_response(401, {"error": "unauthorized"})
        mock_session.get.return_value = resp

        from vault_cli.core.client import VaultClient

        client = VaultClient(
            host="localhost",
            port=5984,
            database="obsidian",
            username="admin",
            password="wrong",
        )
        with pytest.raises(Exception) as exc_info:
            client.ping()
        error_msg = str(exc_info.value).lower()
        assert (
            "auth" in error_msg
            or "credential" in error_msg
            or "unauthorized" in error_msg
        )

    @patch("vault_cli.core.client.requests")
    def test_ping_database_not_found(self, mock_requests):
        """ping() raises error on 404 with database-not-found message."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session
        resp = _make_response(404, {"error": "not_found"})
        mock_session.get.return_value = resp

        from vault_cli.core.client import VaultClient

        client = VaultClient(
            host="localhost",
            port=5984,
            database="nonexistent",
            username="admin",
            password="secret",
        )
        with pytest.raises(Exception) as exc_info:
            client.ping()
        error_msg = str(exc_info.value).lower()
        assert "not found" in error_msg or "database" in error_msg


# ---------------------------------------------------------------------------
# Test: list_notes
# ---------------------------------------------------------------------------


class TestListNotes:
    """VaultClient.list_notes() returns note metadata, filtering out non-note docs."""

    @patch("vault_cli.core.client.requests")
    def test_filters_chunk_documents(self, mock_requests):
        """Chunk documents (h:*) are excluded from the listing."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        rows = [
            {
                "id": "note.md",
                "doc": {
                    "_id": "note.md",
                    "path": "Note.md",
                    "mtime": 1000,
                    "size": 100,
                },
            },
            {
                "id": "h:abc123def456",
                "doc": {"_id": "h:abc123def456", "type": "leaf", "data": "..."},
            },
        ]
        mock_session.get.return_value = _make_response(200, {"rows": rows})

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        notes = client.list_notes()

        paths = [n["path"] for n in notes]
        assert "Note.md" in paths
        assert not any("h:" in n.get("id", "") for n in notes)

    @patch("vault_cli.core.client.requests")
    def test_filters_system_documents(self, mock_requests):
        """System documents (_design/*, etc.) are excluded."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        rows = [
            {
                "id": "note.md",
                "doc": {
                    "_id": "note.md",
                    "path": "Note.md",
                    "mtime": 1000,
                    "size": 100,
                },
            },
            {"id": "_design/views", "doc": {"_id": "_design/views", "views": {}}},
        ]
        mock_session.get.return_value = _make_response(200, {"rows": rows})

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        notes = client.list_notes()
        assert len(notes) == 1
        assert notes[0]["path"] == "Note.md"

    @patch("vault_cli.core.client.requests")
    def test_filters_deleted_documents(self, mock_requests):
        """Soft-deleted documents are excluded."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        rows = [
            {
                "id": "active.md",
                "doc": {
                    "_id": "active.md",
                    "path": "Active.md",
                    "mtime": 1000,
                    "size": 100,
                },
            },
            {
                "id": "deleted.md",
                "doc": {
                    "_id": "deleted.md",
                    "path": "Deleted.md",
                    "mtime": 1000,
                    "size": 0,
                    "deleted": True,
                },
            },
        ]
        mock_session.get.return_value = _make_response(200, {"rows": rows})

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        notes = client.list_notes()
        assert len(notes) == 1
        assert notes[0]["path"] == "Active.md"

    @patch("vault_cli.core.client.requests")
    def test_filters_livesync_version_doc(self, mock_requests):
        """The obsydian_livesync_version document is excluded."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        rows = [
            {
                "id": "note.md",
                "doc": {
                    "_id": "note.md",
                    "path": "Note.md",
                    "mtime": 1000,
                    "size": 100,
                },
            },
            {
                "id": "obsydian_livesync_version",
                "doc": {"_id": "obsydian_livesync_version", "version": 1},
            },
        ]
        mock_session.get.return_value = _make_response(200, {"rows": rows})

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        notes = client.list_notes()
        assert len(notes) == 1

    @patch("vault_cli.core.client.requests")
    def test_returns_path_id_mtime_size(self, mock_requests):
        """Each listed note includes path, id, mtime, and size."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        rows = [
            {
                "id": "references/person.md",
                "doc": {
                    "_id": "references/person.md",
                    "path": "References/Person.md",
                    "mtime": 1709500000000,
                    "size": 1234,
                },
            },
        ]
        mock_session.get.return_value = _make_response(200, {"rows": rows})

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        notes = client.list_notes()
        assert len(notes) == 1
        note = notes[0]
        assert note["path"] == "References/Person.md"
        assert note["id"] == "references/person.md"
        assert note["mtime"] == 1709500000000
        assert note["size"] == 1234


# ---------------------------------------------------------------------------
# Test: read_note
# ---------------------------------------------------------------------------


class TestReadNote:
    """VaultClient.read_note() fetches metadata + content chunks and joins them."""

    @patch("vault_cli.core.client.requests")
    def test_reads_single_chunk_note(self, mock_requests):
        """Reads a note with a single content chunk."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        content = "# Hello World\n\nThis is a test note."
        chunk_id = _chunk_id(content)

        metadata_resp = _make_response(
            200,
            {
                "_id": "note.md",
                "_rev": "1-abc",
                "path": "Note.md",
                "children": [chunk_id],
                "ctime": 1709500000000,
                "mtime": 1709500000000,
                "size": len(content),
                "type": "plain",
            },
        )
        chunk_resp = _make_response(
            200,
            {
                "_id": chunk_id,
                "type": "leaf",
                "data": content,
            },
        )

        # First GET is for metadata, second for chunk
        mock_session.get.side_effect = [metadata_resp, chunk_resp]

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        result = client.read_note("Note.md")

        assert result is not None
        assert result["content"] == content
        assert result["path"] == "Note.md"

    @patch("vault_cli.core.client.requests")
    def test_reads_multi_chunk_note(self, mock_requests):
        """Reads a note split across multiple content chunks and joins them in order."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        chunk_data_1 = "First part. "
        chunk_data_2 = "Second part."
        chunk_id_1 = _chunk_id(chunk_data_1)
        chunk_id_2 = _chunk_id(chunk_data_2)

        metadata_resp = _make_response(
            200,
            {
                "_id": "note.md",
                "_rev": "1-abc",
                "path": "Note.md",
                "children": [chunk_id_1, chunk_id_2],
                "ctime": 1709500000000,
                "mtime": 1709500000000,
                "size": 24,
                "type": "plain",
            },
        )
        chunk_resp_1 = _make_response(
            200, {"_id": chunk_id_1, "type": "leaf", "data": chunk_data_1}
        )
        chunk_resp_2 = _make_response(
            200, {"_id": chunk_id_2, "type": "leaf", "data": chunk_data_2}
        )

        mock_session.get.side_effect = [metadata_resp, chunk_resp_1, chunk_resp_2]

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        result = client.read_note("Note.md")

        assert result["content"] == "First part. Second part."

    @patch("vault_cli.core.client.requests")
    def test_returns_none_for_missing_note(self, mock_requests):
        """Returns None when note does not exist (404)."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        resp = _make_response(404, {"error": "not_found"})
        mock_session.get.return_value = resp

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        result = client.read_note("nonexistent.md")

        assert result is None

    @patch("vault_cli.core.client.requests")
    def test_includes_metadata_fields(self, mock_requests):
        """Returned dict includes ctime, mtime, and path."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        content = "test"
        chunk_id = _chunk_id(content)

        metadata_resp = _make_response(
            200,
            {
                "_id": "note.md",
                "_rev": "1-abc",
                "path": "Note.md",
                "children": [chunk_id],
                "ctime": 1000,
                "mtime": 2000,
                "size": 4,
                "type": "plain",
            },
        )
        chunk_resp = _make_response(
            200, {"_id": chunk_id, "type": "leaf", "data": content}
        )
        mock_session.get.side_effect = [metadata_resp, chunk_resp]

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        result = client.read_note("Note.md")

        assert result["ctime"] == 1000
        assert result["mtime"] == 2000


# ---------------------------------------------------------------------------
# Test: write_note
# ---------------------------------------------------------------------------


class TestWriteNote:
    """VaultClient.write_note() creates chunk(s) and metadata document."""

    @patch("vault_cli.core.client.requests")
    def test_creates_chunk_with_sha256_id(self, mock_requests):
        """Chunk is created with h: + SHA-256 prefix as its ID."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        content = "# New Note\n\nHello world."
        expected_chunk_id = _chunk_id(content)

        # GET for existing doc (404 — new note)
        not_found_resp = _make_response(404, {"error": "not_found"})
        # GET for existing chunk (404 — new chunk)
        chunk_not_found = _make_response(404, {"error": "not_found"})
        # PUT for chunk creation
        chunk_put_resp = _make_response(
            201, {"ok": True, "id": expected_chunk_id, "rev": "1-xxx"}
        )
        # PUT for metadata creation
        meta_put_resp = _make_response(
            201, {"ok": True, "id": "new note.md", "rev": "1-yyy"}
        )

        mock_session.get.side_effect = [not_found_resp, chunk_not_found]
        mock_session.put.side_effect = [chunk_put_resp, meta_put_resp]

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        result = client.write_note("New Note.md", content)

        assert result["ok"] is True
        # Verify chunk was written with correct ID
        put_calls = mock_session.put.call_args_list
        assert len(put_calls) >= 1
        # The first PUT should be the chunk, check its URL contains the chunk ID
        first_put_url = (
            put_calls[0][0][0] if put_calls[0][0] else put_calls[0][1].get("url", "")
        )
        assert expected_chunk_id in str(first_put_url) or expected_chunk_id in str(
            put_calls[0]
        )

    @patch("vault_cli.core.client.requests")
    def test_creates_metadata_with_children_array(self, mock_requests):
        """Metadata document is created with children array pointing to chunk(s)."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        content = "Hello"
        expected_chunk_id = _chunk_id(content)

        not_found_resp = _make_response(404, {"error": "not_found"})
        chunk_not_found = _make_response(404, {"error": "not_found"})
        chunk_put_resp = _make_response(
            201, {"ok": True, "id": expected_chunk_id, "rev": "1-xxx"}
        )
        meta_put_resp = _make_response(
            201, {"ok": True, "id": "hello.md", "rev": "1-yyy"}
        )

        mock_session.get.side_effect = [not_found_resp, chunk_not_found]
        mock_session.put.side_effect = [chunk_put_resp, meta_put_resp]

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        result = client.write_note("Hello.md", content)
        assert result["ok"] is True

    @patch("vault_cli.core.client.requests")
    def test_handles_existing_doc_with_rev(self, mock_requests):
        """When updating an existing note, includes _rev in the metadata write."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        content = "Updated content"
        expected_chunk_id = _chunk_id(content)

        # Existing doc found
        existing_resp = _make_response(
            200,
            {
                "_id": "note.md",
                "_rev": "3-existing",
                "path": "Note.md",
                "children": ["h:oldchunk1234"],
                "ctime": 1000,
                "mtime": 1500,
                "size": 50,
                "type": "plain",
            },
        )
        chunk_not_found = _make_response(404, {"error": "not_found"})
        chunk_put_resp = _make_response(
            201, {"ok": True, "id": expected_chunk_id, "rev": "1-xxx"}
        )
        meta_put_resp = _make_response(
            201, {"ok": True, "id": "note.md", "rev": "4-new"}
        )

        mock_session.get.side_effect = [existing_resp, chunk_not_found]
        mock_session.put.side_effect = [chunk_put_resp, meta_put_resp]

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        result = client.write_note("Note.md", content)
        assert result["ok"] is True

        # Verify the metadata PUT includes _rev
        meta_put_call = mock_session.put.call_args_list[-1]
        # The metadata body should contain the existing _rev
        put_data = None
        if meta_put_call[1].get("json"):
            put_data = meta_put_call[1]["json"]
        elif meta_put_call[1].get("data"):
            put_data = json.loads(meta_put_call[1]["data"])
        if put_data:
            assert put_data.get("_rev") == "3-existing"

    @patch("vault_cli.core.client.requests")
    def test_preserves_ctime_on_update(self, mock_requests):
        """When updating, ctime is preserved from the original document."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        content = "Updated"
        expected_chunk_id = _chunk_id(content)

        existing_resp = _make_response(
            200,
            {
                "_id": "note.md",
                "_rev": "2-old",
                "path": "Note.md",
                "children": [],
                "ctime": 5000,
                "mtime": 6000,
                "size": 10,
                "type": "plain",
            },
        )
        chunk_not_found = _make_response(404, {"error": "not_found"})
        chunk_put_resp = _make_response(201, {"ok": True})
        meta_put_resp = _make_response(
            201, {"ok": True, "id": "note.md", "rev": "3-new"}
        )

        mock_session.get.side_effect = [existing_resp, chunk_not_found]
        mock_session.put.side_effect = [chunk_put_resp, meta_put_resp]

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        client.write_note("Note.md", content)

        meta_put_call = mock_session.put.call_args_list[-1]
        put_data = meta_put_call[1].get("json") or json.loads(
            meta_put_call[1].get("data", "{}")
        )
        if put_data:
            assert put_data.get("ctime") == 5000

    @patch("vault_cli.core.client.requests")
    def test_skips_chunk_creation_if_exists(self, mock_requests):
        """If the chunk already exists (content-addressed), it's not re-created."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        content = "Shared content"
        expected_chunk_id = _chunk_id(content)

        # New metadata doc
        not_found_resp = _make_response(404, {"error": "not_found"})
        # Chunk already exists
        chunk_exists_resp = _make_response(
            200, {"_id": expected_chunk_id, "type": "leaf", "data": content}
        )
        # Metadata PUT
        meta_put_resp = _make_response(
            201, {"ok": True, "id": "note.md", "rev": "1-new"}
        )

        mock_session.get.side_effect = [not_found_resp, chunk_exists_resp]
        mock_session.put.side_effect = [meta_put_resp]

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        result = client.write_note("Note.md", content)
        assert result["ok"] is True

        # Only one PUT call (metadata), chunk was not written
        assert mock_session.put.call_count == 1


# ---------------------------------------------------------------------------
# Test: delete_note
# ---------------------------------------------------------------------------


class TestDeleteNote:
    """VaultClient.delete_note() performs a LiveSync-compatible soft-delete."""

    @patch("vault_cli.core.client.requests")
    def test_sets_deleted_true(self, mock_requests):
        """Soft-delete sets deleted=true on the document."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        existing_resp = _make_response(
            200,
            {
                "_id": "note.md",
                "_rev": "1-abc",
                "path": "Note.md",
                "children": ["h:chunk123456"],
                "ctime": 1000,
                "mtime": 2000,
                "size": 100,
                "type": "plain",
            },
        )
        put_resp = _make_response(201, {"ok": True, "id": "note.md", "rev": "2-def"})

        mock_session.get.return_value = existing_resp
        mock_session.put.return_value = put_resp

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        result = client.delete_note("Note.md")

        assert result["ok"] is True
        put_data = mock_session.put.call_args[1].get("json") or json.loads(
            mock_session.put.call_args[1].get("data", "{}")
        )
        assert put_data["deleted"] is True

    @patch("vault_cli.core.client.requests")
    def test_clears_children(self, mock_requests):
        """Soft-delete clears the children array."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        existing_resp = _make_response(
            200,
            {
                "_id": "note.md",
                "_rev": "1-abc",
                "path": "Note.md",
                "children": ["h:chunk123456"],
                "ctime": 1000,
                "mtime": 2000,
                "size": 100,
                "type": "plain",
            },
        )
        put_resp = _make_response(201, {"ok": True})

        mock_session.get.return_value = existing_resp
        mock_session.put.return_value = put_resp

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        client.delete_note("Note.md")

        put_data = mock_session.put.call_args[1].get("json") or json.loads(
            mock_session.put.call_args[1].get("data", "{}")
        )
        assert put_data["children"] == []

    @patch("vault_cli.core.client.requests")
    def test_clears_data(self, mock_requests):
        """Soft-delete clears the data field."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        existing_resp = _make_response(
            200,
            {
                "_id": "note.md",
                "_rev": "1-abc",
                "path": "Note.md",
                "children": ["h:chunk123456"],
                "data": "old content",
                "ctime": 1000,
                "mtime": 2000,
                "size": 100,
                "type": "plain",
            },
        )
        put_resp = _make_response(201, {"ok": True})

        mock_session.get.return_value = existing_resp
        mock_session.put.return_value = put_resp

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        client.delete_note("Note.md")

        put_data = mock_session.put.call_args[1].get("json") or json.loads(
            mock_session.put.call_args[1].get("data", "{}")
        )
        assert put_data.get("data", "") == ""

    @patch("vault_cli.core.client.requests")
    def test_does_not_use_couchdb_destroy(self, mock_requests):
        """Soft-delete uses PUT (update), not DELETE (destroy)."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        existing_resp = _make_response(
            200,
            {
                "_id": "note.md",
                "_rev": "1-abc",
                "path": "Note.md",
                "children": [],
                "ctime": 1000,
                "mtime": 2000,
                "size": 0,
                "type": "plain",
            },
        )
        put_resp = _make_response(201, {"ok": True})

        mock_session.get.return_value = existing_resp
        mock_session.put.return_value = put_resp

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")
        client.delete_note("Note.md")

        # PUT should be called, DELETE should NOT
        assert mock_session.put.called
        assert not mock_session.delete.called


# ---------------------------------------------------------------------------
# Test: move_note
# ---------------------------------------------------------------------------


class TestMoveNote:
    """VaultClient.move_note() reads from old path, writes to new path, deletes old."""

    @patch("vault_cli.core.client.requests")
    def test_move_reads_writes_deletes(self, mock_requests):
        """move_note performs read, write, delete in sequence."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")

        # Mock read_note, write_note, delete_note at the method level
        with (
            patch.object(client, "read_note") as mock_read,
            patch.object(client, "write_note") as mock_write,
            patch.object(client, "delete_note") as mock_delete,
        ):
            mock_read.return_value = {
                "path": "Draft.md",
                "content": "# Draft\n\nContent here.",
                "ctime": 1000,
                "mtime": 2000,
            }
            mock_write.return_value = {
                "ok": True,
                "id": "references/final.md",
                "rev": "1-new",
            }
            mock_delete.return_value = {"ok": True}

            result = client.move_note("Draft.md", "References/Final.md")

            assert result["ok"] is True
            mock_read.assert_called_once_with("Draft.md")
            mock_write.assert_called_once_with(
                "References/Final.md", "# Draft\n\nContent here."
            )
            mock_delete.assert_called_once_with("Draft.md")

    @patch("vault_cli.core.client.requests")
    def test_move_raises_if_source_not_found(self, mock_requests):
        """move_note raises an error if the source note doesn't exist."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")

        with patch.object(client, "read_note") as mock_read:
            mock_read.return_value = None

            with pytest.raises(Exception) as exc_info:
                client.move_note("nonexistent.md", "new-location.md")
            assert "not found" in str(exc_info.value).lower()


# ---------------------------------------------------------------------------
# Test: search_notes (path-only search)
# ---------------------------------------------------------------------------


class TestSearchNotes:
    """VaultClient.search_notes() filters notes by path (case-insensitive)."""

    @patch("vault_cli.core.client.requests")
    def test_case_insensitive_path_search(self, mock_requests):
        """Search is case-insensitive on note paths."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")

        with patch.object(client, "list_notes") as mock_list:
            mock_list.return_value = [
                {
                    "path": "References/Agent Loop.md",
                    "id": "references/agent loop.md",
                    "mtime": 1000,
                    "size": 100,
                },
                {
                    "path": "Projects/ClosedClaw.md",
                    "id": "projects/closedclaw.md",
                    "mtime": 1000,
                    "size": 200,
                },
                {
                    "path": "North Star.md",
                    "id": "north star.md",
                    "mtime": 1000,
                    "size": 150,
                },
            ]

            results = client.search_notes("agent")
            assert len(results) == 1
            assert results[0]["path"] == "References/Agent Loop.md"

    @patch("vault_cli.core.client.requests")
    def test_search_returns_multiple_matches(self, mock_requests):
        """Search returns all matching notes."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")

        with patch.object(client, "list_notes") as mock_list:
            mock_list.return_value = [
                {
                    "path": "References/Note A.md",
                    "id": "references/note a.md",
                    "mtime": 1000,
                    "size": 100,
                },
                {
                    "path": "References/Note B.md",
                    "id": "references/note b.md",
                    "mtime": 1000,
                    "size": 100,
                },
                {
                    "path": "Projects/Other.md",
                    "id": "projects/other.md",
                    "mtime": 1000,
                    "size": 100,
                },
            ]

            results = client.search_notes("references")
            assert len(results) == 2

    @patch("vault_cli.core.client.requests")
    def test_search_no_matches(self, mock_requests):
        """Search returns empty list when nothing matches."""
        mock_session = MagicMock()
        mock_requests.Session.return_value = mock_session

        from vault_cli.core.client import VaultClient

        client = VaultClient(host="localhost", port=5984, database="obsidian")

        with patch.object(client, "list_notes") as mock_list:
            mock_list.return_value = [
                {"path": "Note.md", "id": "note.md", "mtime": 1000, "size": 100},
            ]

            results = client.search_notes("nonexistent")
            assert results == []
