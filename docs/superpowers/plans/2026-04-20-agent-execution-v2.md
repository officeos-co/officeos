# Agent Execution Architecture v2 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Rust agent-core with a Go PTY-over-WebSocket server (cloned from GoTTY) and move the agent turn loop into the C# backend.

**Architecture:** The agent pod becomes a dumb PTY exposed over WebSocket — a Go binary cloned from GoTTY, stripped to ~200 lines. The backend gains a new `AgentTurnService` that orchestrates LLM calls, parses tool calls, and sends bash commands to the pod. Memory and personality live in Postgres. The LLM sees a single `bash` tool.

**Tech Stack:** Go 1.22+ (pod-executor), C# ASP.NET Core 9 (backend), PostgreSQL (memory/personality), `creack/pty` + `gorilla/websocket` (Go deps).

**Spec:** `docs/superpowers/specs/2026-04-20-agent-execution-v2-design.md`

---

## File Structure

### New: `packages/pod-executor/`

```
packages/pod-executor/
├── go.mod
├── go.sum
├── main.go              # Entry point: parse env, start server
├── server.go            # WebSocket server, auth, PTY lifecycle
├── server_test.go       # Integration tests
├── Dockerfile           # Alpine-based, static binary
└── CLAUDE.md            # Conventions for this package
```

### New/Modified: `apps/backend/`

```
# New files:
src/EnterpriseAgentOs.Domain/Models/AgentMemoryRecord.cs
src/EnterpriseAgentOs.Domain/Models/AgentPersonalityRecord.cs
src/EnterpriseAgentOs.Domain/Interfaces/Agents/IAgentMemoryRepository.cs
src/EnterpriseAgentOs.Domain/Interfaces/Agents/IAgentPersonalityRepository.cs
src/EnterpriseAgentOs.Infrastructure/Persistence/Repositories/AgentMemoryRepository.cs
src/EnterpriseAgentOs.Infrastructure/Persistence/Repositories/AgentPersonalityRepository.cs
src/EnterpriseAgentOs.Application/Services/Agents/AgentTurnService.cs
src/EnterpriseAgentOs.Application/Services/Agents/PodConnection.cs
src/EnterpriseAgentOs.Application/Services/Agents/PromptComposer.cs

# Modified files:
src/EnterpriseAgentOs.Infrastructure/Persistence/EaosDbContext.cs
src/EnterpriseAgentOs.Infrastructure/Adapters/Kubernetes/KubernetesAgentDeployer.cs
src/EnterpriseAgentOs.Api/Extensions/ServiceCollectionExtensions.cs
src/EnterpriseAgentOs.Application/Services/AgentLogs/AgentLogService.cs
```

---

## Task 1: Go PTY Server — Clone GoTTY and Strip Down

**Files:**
- Create: `packages/pod-executor/go.mod`
- Create: `packages/pod-executor/main.go`
- Create: `packages/pod-executor/server.go`
- Create: `packages/pod-executor/server_test.go`

This task clones GoTTY's PTY-over-WebSocket pattern into a minimal Go binary. We keep only the PTY spawning, WebSocket bridging, and add bearer token auth. Everything else (HTML, xterm.js, TLS, CLI flags, config files, reconnect) is removed.

- [ ] **Step 1: Initialize Go module**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/packages/pod-executor
go mod init github.com/harkro123/pod-executor
go get github.com/creack/pty@v1.1.24
go get github.com/gorilla/websocket@v1.5.3
```

Note: We use `creack/pty` (maintained fork) instead of GoTTY's `kr/pty` (archived).

- [ ] **Step 2: Write main.go — entry point**

```go
// packages/pod-executor/main.go
package main

import (
	"log"
	"os"
)

func main() {
	token := os.Getenv("AGENT_TOKEN")
	if token == "" {
		log.Fatal("AGENT_TOKEN env var is required")
	}

	port := os.Getenv("PORT")
	if port == "" {
		port = "42617"
	}

	log.Printf("pod-executor starting on :%s", port)
	if err := Serve(":"+port, token); err != nil {
		log.Fatalf("server error: %v", err)
	}
}
```

- [ ] **Step 3: Write server.go — PTY-over-WebSocket server**

This is the core — cloned from GoTTY's `webtty/webtty.go` + `server/handlers.go` + `backend/localcommand/local_command.go`, collapsed into a single file.

```go
// packages/pod-executor/server.go
package main

import (
	"encoding/json"
	"io"
	"log"
	"net/http"
	"os"
	"os/exec"
	"sync"
	"unicode/utf8"

	"github.com/creack/pty"
	"github.com/gorilla/websocket"
)

// Request from backend: a command to write to PTY stdin.
type Request struct {
	ID    string `json:"id"`
	Input string `json:"input"`
}

// Response streamed back: raw PTY output.
type Response struct {
	ID   string `json:"id"`
	Type string `json:"type"` // "output" or "error"
	Data string `json:"data"`
}

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool { return true },
}

// Serve starts the WebSocket server on addr. Each connection spawns a new
// PTY running /bin/bash. The token is validated from the ?token= query param.
func Serve(addr, expectedToken string) error {
	mux := http.NewServeMux()

	mux.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("ok"))
	})

	mux.HandleFunc("/ws", func(w http.ResponseWriter, r *http.Request) {
		// Auth: validate bearer token from query param.
		token := r.URL.Query().Get("token")
		if token != expectedToken {
			http.Error(w, "unauthorized", http.StatusUnauthorized)
			return
		}

		conn, err := upgrader.Upgrade(w, r, nil)
		if err != nil {
			log.Printf("websocket upgrade failed: %v", err)
			return
		}
		defer conn.Close()

		handleConnection(conn)
	})

	log.Printf("listening on %s", addr)
	return http.ListenAndServe(addr, mux)
}

func handleConnection(conn *websocket.Conn) {
	// Spawn bash PTY — cloned from GoTTY's localcommand.go
	cmd := exec.Command("/bin/bash")
	cmd.Env = os.Environ()

	ptmx, err := pty.Start(cmd)
	if err != nil {
		log.Printf("pty start failed: %v", err)
		sendError(conn, "", "pty start failed: "+err.Error())
		return
	}
	defer func() {
		ptmx.Close()
		cmd.Process.Kill()
		cmd.Wait()
	}()

	var wg sync.WaitGroup
	var currentID string
	var mu sync.Mutex

	// Goroutine 1: PTY stdout → WebSocket (from GoTTY's webtty.Run slave→master)
	wg.Add(1)
	go func() {
		defer wg.Done()
		buf := make([]byte, 32*1024)
		for {
			n, err := ptmx.Read(buf)
			if err != nil {
				if err != io.EOF {
					log.Printf("pty read error: %v", err)
				}
				return
			}
			if n == 0 {
				continue
			}

			data := sanitizeUTF8(buf[:n])

			mu.Lock()
			id := currentID
			mu.Unlock()

			resp := Response{ID: id, Type: "output", Data: data}
			msg, _ := json.Marshal(resp)
			if err := conn.WriteMessage(websocket.TextMessage, msg); err != nil {
				log.Printf("ws write error: %v", err)
				return
			}
		}
	}()

	// Goroutine 2: WebSocket → PTY stdin (from GoTTY's webtty.Run master→slave)
	go func() {
		for {
			_, message, err := conn.ReadMessage()
			if err != nil {
				if websocket.IsUnexpectedCloseError(err,
					websocket.CloseGoingAway,
					websocket.CloseNormalClosure) {
					log.Printf("ws read error: %v", err)
				}
				ptmx.Close()
				return
			}

			var req Request
			if err := json.Unmarshal(message, &req); err != nil {
				sendError(conn, "", "invalid request: "+err.Error())
				continue
			}

			mu.Lock()
			currentID = req.ID
			mu.Unlock()

			if _, err := ptmx.Write([]byte(req.Input)); err != nil {
				log.Printf("pty write error: %v", err)
				sendError(conn, req.ID, "pty write failed: "+err.Error())
				return
			}
		}
	}()

	wg.Wait()
}

func sendError(conn *websocket.Conn, id, msg string) {
	resp := Response{ID: id, Type: "error", Data: msg}
	data, _ := json.Marshal(resp)
	conn.WriteMessage(websocket.TextMessage, data)
}

// sanitizeUTF8 replaces invalid UTF-8 sequences with the replacement character.
func sanitizeUTF8(b []byte) string {
	if utf8.Valid(b) {
		return string(b)
	}
	s := make([]byte, 0, len(b))
	for len(b) > 0 {
		r, size := utf8.DecodeRune(b)
		if r == utf8.RuneError && size == 1 {
			s = append(s, []byte("\uFFFD")...)
		} else {
			s = append(s, b[:size]...)
		}
		b = b[size:]
	}
	return string(s)
}
```

- [ ] **Step 4: Write server_test.go — integration test**

```go
// packages/pod-executor/server_test.go
package main

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"

	"github.com/gorilla/websocket"
)

func startTestServer(t *testing.T, token string) *httptest.Server {
	t.Helper()
	mux := http.NewServeMux()

	mux.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("ok"))
	})

	mux.HandleFunc("/ws", func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Query().Get("token") != token {
			http.Error(w, "unauthorized", http.StatusUnauthorized)
			return
		}
		conn, err := upgrader.Upgrade(w, r, nil)
		if err != nil {
			t.Fatalf("upgrade: %v", err)
		}
		defer conn.Close()
		handleConnection(conn)
	})

	return httptest.NewServer(mux)
}

func TestHealthEndpoint(t *testing.T) {
	srv := startTestServer(t, "test-token")
	defer srv.Close()

	resp, err := http.Get(srv.URL + "/health")
	if err != nil {
		t.Fatalf("health request failed: %v", err)
	}
	defer resp.Body.Close()
	if resp.StatusCode != 200 {
		t.Fatalf("expected 200, got %d", resp.StatusCode)
	}
}

func TestUnauthorizedConnection(t *testing.T) {
	srv := startTestServer(t, "test-token")
	defer srv.Close()

	wsURL := "ws" + strings.TrimPrefix(srv.URL, "http") + "/ws?token=wrong"
	_, resp, err := websocket.DefaultDialer.Dial(wsURL, nil)
	if err == nil {
		t.Fatal("expected error for wrong token")
	}
	if resp != nil && resp.StatusCode != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d", resp.StatusCode)
	}
}

func TestShellCommand(t *testing.T) {
	srv := startTestServer(t, "test-token")
	defer srv.Close()

	wsURL := "ws" + strings.TrimPrefix(srv.URL, "http") + "/ws?token=test-token"
	conn, _, err := websocket.DefaultDialer.Dial(wsURL, nil)
	if err != nil {
		t.Fatalf("dial failed: %v", err)
	}
	defer conn.Close()

	// Send a simple echo command.
	req := Request{ID: "test-1", Input: "echo HELLO_POD_EXECUTOR\n"}
	msg, _ := json.Marshal(req)
	if err := conn.WriteMessage(websocket.TextMessage, msg); err != nil {
		t.Fatalf("write failed: %v", err)
	}

	// Read output until we see our marker string.
	conn.SetReadDeadline(time.Now().Add(5 * time.Second))
	var output strings.Builder
	for {
		_, data, err := conn.ReadMessage()
		if err != nil {
			t.Fatalf("read failed (output so far: %q): %v", output.String(), err)
		}
		var resp Response
		if err := json.Unmarshal(data, &resp); err != nil {
			t.Fatalf("unmarshal failed: %v", err)
		}
		output.WriteString(resp.Data)
		if strings.Contains(output.String(), "HELLO_POD_EXECUTOR") {
			break
		}
	}

	if !strings.Contains(output.String(), "HELLO_POD_EXECUTOR") {
		t.Fatalf("expected HELLO_POD_EXECUTOR in output, got: %q", output.String())
	}
}
```

- [ ] **Step 5: Run tests**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/packages/pod-executor
go test -v -timeout 30s ./...
```

Expected: 3 tests pass (TestHealthEndpoint, TestUnauthorizedConnection, TestShellCommand).

- [ ] **Step 6: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add packages/pod-executor/
git commit -m "feat(pod-executor): Go PTY-over-WebSocket server cloned from GoTTY

Minimal Go binary that exposes a bash PTY over WebSocket. Each
connection spawns /bin/bash, bridges stdin/stdout via JSON frames.
Auth via bearer token in query param.

Replaces packages/agent-core/ (Rust) for the pod execution layer."
```

---

## Task 2: Dockerfile and CLAUDE.md for pod-executor

**Files:**
- Create: `packages/pod-executor/Dockerfile`
- Create: `packages/pod-executor/CLAUDE.md`

- [ ] **Step 1: Write Dockerfile**

```dockerfile
# packages/pod-executor/Dockerfile
FROM golang:1.22-alpine AS builder

WORKDIR /app
COPY go.mod go.sum ./
RUN go mod download
COPY *.go ./
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-s -w" -o pod-executor .

FROM alpine:3.20

RUN apk add --no-cache bash curl git python3 && \
    rm -rf /var/cache/apk/*

COPY --from=builder /app/pod-executor /usr/local/bin/pod-executor

EXPOSE 42617

ENTRYPOINT ["pod-executor"]
```

- [ ] **Step 2: Build and verify image size**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/packages/pod-executor
docker build -t harkro123/eaos-pod-executor:latest .
docker images harkro123/eaos-pod-executor:latest --format "{{.Size}}"
```

Expected: Image size under 100MB.

- [ ] **Step 3: Write CLAUDE.md**

```markdown
# pod-executor — Go PTY-over-WebSocket Server

Minimal Go binary that exposes a bash PTY over WebSocket. Runs inside each agent pod. Cloned from GoTTY, stripped to essentials.

## Commands

\`\`\`bash
go test -v ./...
go build -o pod-executor .
docker build -t harkro123/eaos-pod-executor:latest .
\`\`\`

## What this binary does

- WebSocket server on PORT (default 42617)
- Each connection spawns /bin/bash via PTY
- Bridges stdin/stdout between WebSocket and PTY
- Auth: AGENT_TOKEN env var, validated from ?token= query param
- Health check: GET /health

## What this binary does NOT do

No LLM calls, no prompt composition, no memory, no bootstrap, no config fetching, no personality files, no knowledge of agents/sessions/users, no Playwright/browser.

## Env vars

| Var | Required | Description |
|-----|----------|-------------|
| AGENT_TOKEN | Yes | Bearer token for WebSocket auth |
| PORT | No | Server port (default: 42617) |

## WebSocket Protocol

Backend → Pod: `{"id": "abc", "input": "echo hello\n"}`
Pod → Backend: `{"id": "abc", "type": "output", "data": "hello\n"}`

## Anti-patterns

- Do not add LLM or agent logic. The backend owns the turn loop.
- Do not add config files. All config comes from env vars.
- Do not add outbound connections. This binary only responds to WebSocket requests.
- Do not add tools/commands beyond PTY. Bash is the tool.
```

- [ ] **Step 4: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add packages/pod-executor/Dockerfile packages/pod-executor/CLAUDE.md
git commit -m "feat(pod-executor): add Dockerfile and CLAUDE.md"
```

---

## Task 3: Database — Memory and Personality Tables

**Files:**
- Create: `src/EnterpriseAgentOs.Domain/Models/AgentMemoryRecord.cs`
- Create: `src/EnterpriseAgentOs.Domain/Models/AgentPersonalityRecord.cs`
- Modify: `src/EnterpriseAgentOs.Infrastructure/Persistence/EaosDbContext.cs`

- [ ] **Step 1: Create AgentMemoryRecord model**

```csharp
// src/EnterpriseAgentOs.Domain/Models/AgentMemoryRecord.cs
namespace EnterpriseAgentOs.Domain.Models;

public class AgentMemoryRecord
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string Key { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AgentRecord Agent { get; set; } = null!;
}
```

- [ ] **Step 2: Create AgentPersonalityRecord model**

```csharp
// src/EnterpriseAgentOs.Domain/Models/AgentPersonalityRecord.cs
namespace EnterpriseAgentOs.Domain.Models;

public class AgentPersonalityRecord
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string FileName { get; set; } = null!;  // e.g. "SOUL.md", "IDENTITY.md"
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AgentRecord Agent { get; set; } = null!;
}
```

- [ ] **Step 3: Add DbSets and configure in EaosDbContext**

Add to `src/EnterpriseAgentOs.Infrastructure/Persistence/EaosDbContext.cs`:

After existing DbSet declarations (around line 30):

```csharp
public DbSet<AgentMemoryRecord> AgentMemories => Set<AgentMemoryRecord>();
public DbSet<AgentPersonalityRecord> AgentPersonalities => Set<AgentPersonalityRecord>();
```

In `OnModelCreating`, add after existing AgentRecord configuration (after line 44):

```csharp
modelBuilder.Entity<AgentMemoryRecord>(e =>
{
    e.HasKey(m => m.Id);
    e.Property(m => m.Key).HasMaxLength(512).IsRequired();
    e.Property(m => m.Content).HasColumnType("text").IsRequired();
    e.HasIndex(m => new { m.AgentId, m.Key }).IsUnique();
    e.HasOne(m => m.Agent).WithMany()
        .HasForeignKey(m => m.AgentId)
        .OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<AgentPersonalityRecord>(e =>
{
    e.HasKey(p => p.Id);
    e.Property(p => p.FileName).HasMaxLength(128).IsRequired();
    e.Property(p => p.Content).HasColumnType("text").IsRequired();
    e.HasIndex(p => new { p.AgentId, p.FileName }).IsUnique();
    e.HasOne(p => p.Agent).WithMany()
        .HasForeignKey(p => p.AgentId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

- [ ] **Step 4: Create EF migration**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend
dotnet ef migrations add AddAgentMemoryAndPersonality \
  --project src/EnterpriseAgentOs.Infrastructure \
  --startup-project src/EnterpriseAgentOs.Api
```

Expected: Migration file created in `Persistence/Migrations/`.

- [ ] **Step 5: Build to verify**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend
dotnet build EnterpriseAgentOs.sln
```

Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add apps/backend/src/EnterpriseAgentOs.Domain/Models/AgentMemoryRecord.cs \
        apps/backend/src/EnterpriseAgentOs.Domain/Models/AgentPersonalityRecord.cs \
        apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/EaosDbContext.cs \
        apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/Migrations/
git commit -m "feat(backend): add AgentMemory and AgentPersonality tables

Memory (key-value per agent) and personality (markdown files per agent)
stored in Postgres. Replaces PVC-based personality file seeding."
```

---

## Task 4: Repositories for Memory and Personality

**Files:**
- Create: `src/EnterpriseAgentOs.Domain/Interfaces/Agents/IAgentMemoryRepository.cs`
- Create: `src/EnterpriseAgentOs.Domain/Interfaces/Agents/IAgentPersonalityRepository.cs`
- Create: `src/EnterpriseAgentOs.Infrastructure/Persistence/Repositories/AgentMemoryRepository.cs`
- Create: `src/EnterpriseAgentOs.Infrastructure/Persistence/Repositories/AgentPersonalityRepository.cs`
- Modify: `src/EnterpriseAgentOs.Api/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Create IAgentMemoryRepository interface**

```csharp
// src/EnterpriseAgentOs.Domain/Interfaces/Agents/IAgentMemoryRepository.cs
namespace EnterpriseAgentOs.Domain.Interfaces.Agents;

public interface IAgentMemoryRepository
{
    Task<AgentMemoryRecord?> GetAsync(Guid agentId, string key, CancellationToken ct = default);
    Task<IReadOnlyList<AgentMemoryRecord>> ListAsync(Guid agentId, CancellationToken ct = default);
    Task UpsertAsync(Guid agentId, string key, string content, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid agentId, string key, CancellationToken ct = default);
}
```

- [ ] **Step 2: Create IAgentPersonalityRepository interface**

```csharp
// src/EnterpriseAgentOs.Domain/Interfaces/Agents/IAgentPersonalityRepository.cs
namespace EnterpriseAgentOs.Domain.Interfaces.Agents;

public interface IAgentPersonalityRepository
{
    Task<IReadOnlyList<AgentPersonalityRecord>> ListAsync(Guid agentId, CancellationToken ct = default);
    Task UpsertAsync(Guid agentId, string fileName, string content, CancellationToken ct = default);
}
```

- [ ] **Step 3: Create AgentMemoryRepository**

```csharp
// src/EnterpriseAgentOs.Infrastructure/Persistence/Repositories/AgentMemoryRepository.cs
namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class AgentMemoryRepository : IAgentMemoryRepository
{
    private readonly EaosDbContext _db;

    public AgentMemoryRepository(EaosDbContext db) => _db = db;

    public async Task<AgentMemoryRecord?> GetAsync(Guid agentId, string key, CancellationToken ct = default)
        => await _db.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);

    public async Task<IReadOnlyList<AgentMemoryRecord>> ListAsync(Guid agentId, CancellationToken ct = default)
        => await _db.AgentMemories
            .Where(m => m.AgentId == agentId)
            .OrderBy(m => m.Key)
            .ToListAsync(ct);

    public async Task UpsertAsync(Guid agentId, string key, string content, CancellationToken ct = default)
    {
        var existing = await _db.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);

        if (existing is not null)
        {
            existing.Content = content;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.AgentMemories.Add(new AgentMemoryRecord
            {
                AgentId = agentId,
                Key = key,
                Content = content,
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid agentId, string key, CancellationToken ct = default)
    {
        var record = await _db.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);
        if (record is null) return false;
        _db.AgentMemories.Remove(record);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
```

- [ ] **Step 4: Create AgentPersonalityRepository**

```csharp
// src/EnterpriseAgentOs.Infrastructure/Persistence/Repositories/AgentPersonalityRepository.cs
namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class AgentPersonalityRepository : IAgentPersonalityRepository
{
    private readonly EaosDbContext _db;

    public AgentPersonalityRepository(EaosDbContext db) => _db = db;

    public async Task<IReadOnlyList<AgentPersonalityRecord>> ListAsync(Guid agentId, CancellationToken ct = default)
        => await _db.AgentPersonalities
            .Where(p => p.AgentId == agentId)
            .OrderBy(p => p.FileName)
            .ToListAsync(ct);

    public async Task UpsertAsync(Guid agentId, string fileName, string content, CancellationToken ct = default)
    {
        var existing = await _db.AgentPersonalities
            .FirstOrDefaultAsync(p => p.AgentId == agentId && p.FileName == fileName, ct);

        if (existing is not null)
        {
            existing.Content = content;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.AgentPersonalities.Add(new AgentPersonalityRecord
            {
                AgentId = agentId,
                FileName = fileName,
                Content = content,
            });
        }

        await _db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Register in DI**

In `src/EnterpriseAgentOs.Api/Extensions/ServiceCollectionExtensions.cs`, add to `AddRepositories()`:

```csharp
services.AddScoped<IAgentMemoryRepository, AgentMemoryRepository>();
services.AddScoped<IAgentPersonalityRepository, AgentPersonalityRepository>();
```

- [ ] **Step 6: Build to verify**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend
dotnet build EnterpriseAgentOs.sln
```

Expected: Build succeeds.

- [ ] **Step 7: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Agents/ \
        apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/Repositories/AgentMemoryRepository.cs \
        apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/Repositories/AgentPersonalityRepository.cs \
        apps/backend/src/EnterpriseAgentOs.Api/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(backend): add memory and personality repositories

IAgentMemoryRepository (CRUD key-value per agent) and
IAgentPersonalityRepository (upsert markdown files per agent).
Registered as scoped services."
```

---

## Task 5: PodConnection — WebSocket Client to Pod PTY

**Files:**
- Create: `src/EnterpriseAgentOs.Application/Services/Agents/PodConnection.cs`

This is the backend's WebSocket client that connects to the Go PTY server and sends bash commands.

- [ ] **Step 1: Write PodConnection**

```csharp
// src/EnterpriseAgentOs.Application/Services/Agents/PodConnection.cs
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace EnterpriseAgentOs.Application.Services.Agents;

/// <summary>
/// WebSocket connection to an agent pod's PTY server.
/// Sends bash commands, receives streamed output.
/// Disposable — one instance per turn.
/// </summary>
public sealed class PodConnection : IDisposable
{
    private readonly ClientWebSocket _ws = new();
    private readonly string _promptMarker;
    private static int _counter;

    public PodConnection(string promptMarker = "__EAOS_DONE:")
    {
        _promptMarker = promptMarker;
    }

    /// <summary>
    /// Connect to the pod's PTY WebSocket server.
    /// </summary>
    public async Task ConnectAsync(string podName, string ns, Guid agentId, CancellationToken ct)
    {
        var uri = new Uri($"ws://{podName}.{ns}.svc.cluster.local:42617/ws?token={agentId}");
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        await _ws.ConnectAsync(uri, cts.Token);

        // Set a custom PS1 so we can detect command completion and exit codes.
        await SendRawAsync($"export PS1='{_promptMarker}$?__\\n$ '\n", ct);
        // Wait for the prompt to appear after PS1 is set.
        await ReadUntilPromptAsync(ct);
    }

    /// <summary>
    /// Execute a bash command and return the full output + exit code.
    /// </summary>
    public async Task<(string Output, int ExitCode)> ExecuteAsync(string command, CancellationToken ct)
    {
        var id = $"cmd-{Interlocked.Increment(ref _counter)}";
        var request = JsonSerializer.Serialize(new { id, input = command + "\n" });
        await SendRawAsync(request, ct);

        return await ReadUntilPromptAsync(ct);
    }

    private async Task SendRawAsync(string text, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private async Task<(string Output, int ExitCode)> ReadUntilPromptAsync(CancellationToken ct)
    {
        var output = new StringBuilder();
        var buf = new byte[64 * 1024];
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMinutes(5)); // max command runtime

        while (true)
        {
            var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token);
            if (result.MessageType == WebSocketMessageType.Close) break;

            var text = Encoding.UTF8.GetString(buf, 0, result.Count);

            // Parse JSON response frames from the pod.
            try
            {
                using var doc = JsonDocument.Parse(text);
                var data = doc.RootElement.GetProperty("data").GetString() ?? "";
                output.Append(data);
            }
            catch (JsonException)
            {
                // Raw text fallback.
                output.Append(text);
            }

            var full = output.ToString();
            var markerIdx = full.LastIndexOf(_promptMarker, StringComparison.Ordinal);
            if (markerIdx >= 0)
            {
                // Extract exit code from marker: __EAOS_DONE:0__
                var afterMarker = full[(markerIdx + _promptMarker.Length)..];
                var endIdx = afterMarker.IndexOf("__", StringComparison.Ordinal);
                var exitCodeStr = endIdx >= 0 ? afterMarker[..endIdx] : "0";
                int.TryParse(exitCodeStr, out var exitCode);

                // Return output without the marker line and prompt.
                var cleanOutput = full[..markerIdx].TrimEnd('\n', '\r');
                return (cleanOutput, exitCode);
            }
        }

        return (output.ToString(), -1);
    }

    public void Dispose()
    {
        if (_ws.State == WebSocketState.Open)
        {
            try
            {
                _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "turn complete",
                    CancellationToken.None).GetAwaiter().GetResult();
            }
            catch { /* best-effort close */ }
        }
        _ws.Dispose();
    }
}
```

- [ ] **Step 2: Build to verify**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend
dotnet build EnterpriseAgentOs.sln
```

Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/PodConnection.cs
git commit -m "feat(backend): add PodConnection WebSocket client to pod PTY

Connects to agent pod, sets PS1 completion marker, sends bash commands,
reads streamed output until prompt marker, extracts exit code."
```

---

## Task 6: PromptComposer — Build System Prompt from Postgres

**Files:**
- Create: `src/EnterpriseAgentOs.Application/Services/Agents/PromptComposer.cs`

- [ ] **Step 1: Write PromptComposer**

```csharp
// src/EnterpriseAgentOs.Application/Services/Agents/PromptComposer.cs
namespace EnterpriseAgentOs.Application.Services.Agents;

/// <summary>
/// Composes the system prompt from personality files and memory stored in Postgres.
/// Replaces the Rust agent-core's trait-based prompt composition.
/// </summary>
public sealed class PromptComposer
{
    private readonly IAgentPersonalityRepository _personality;
    private readonly IAgentMemoryRepository _memory;

    public PromptComposer(
        IAgentPersonalityRepository personality,
        IAgentMemoryRepository memory)
    {
        _personality = personality;
        _memory = memory;
    }

    /// <summary>
    /// Build the full system prompt for an agent turn.
    /// Order: SOUL.md → IDENTITY.md → user prompt → AGENTS.md → memory recall.
    /// </summary>
    public async Task<string> ComposeAsync(Guid agentId, string? userPrompt, CancellationToken ct = default)
    {
        var personalityFiles = await _personality.ListAsync(agentId, ct);
        var memories = await _memory.ListAsync(agentId, ct);

        var sections = new List<string>();

        // Personality files in defined order.
        var orderedNames = new[] { "SOUL.md", "IDENTITY.md", "BOOTSTRAP.md", "AGENTS.md", "TOOLS.md" };
        foreach (var name in orderedNames)
        {
            var file = personalityFiles.FirstOrDefault(p =>
                string.Equals(p.FileName, name, StringComparison.OrdinalIgnoreCase));
            if (file is not null)
            {
                sections.Add(file.Content);
            }
        }

        // Any additional personality files not in the predefined order.
        foreach (var file in personalityFiles)
        {
            if (!orderedNames.Contains(file.FileName, StringComparer.OrdinalIgnoreCase))
            {
                sections.Add(file.Content);
            }
        }

        // User-configured system prompt.
        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            sections.Add(userPrompt);
        }

        // Agent memories.
        if (memories.Count > 0)
        {
            var memorySection = "## Memory\n\n" +
                string.Join("\n\n", memories.Select(m => $"### {m.Key}\n{m.Content}"));
            sections.Add(memorySection);
        }

        return string.Join("\n\n---\n\n", sections);
    }
}
```

- [ ] **Step 2: Register in DI**

In `src/EnterpriseAgentOs.Api/Extensions/ServiceCollectionExtensions.cs`, add to `AddApplicationServices()`:

```csharp
services.AddScoped<PromptComposer>();
```

- [ ] **Step 3: Build to verify**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend
dotnet build EnterpriseAgentOs.sln
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/PromptComposer.cs \
        apps/backend/src/EnterpriseAgentOs.Api/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(backend): add PromptComposer for system prompt from Postgres

Composes system prompt from personality files + memory records.
Replaces Rust agent-core's trait-based prompt sections."
```

---

## Task 7: AgentTurnService — The Turn Loop

**Files:**
- Create: `src/EnterpriseAgentOs.Application/Services/Agents/AgentTurnService.cs`
- Modify: `src/EnterpriseAgentOs.Api/Extensions/ServiceCollectionExtensions.cs`

This is the core: receives a user message, composes prompt, calls LLM, parses tool calls, sends bash to pod, loops.

- [ ] **Step 1: Write AgentTurnService**

```csharp
// src/EnterpriseAgentOs.Application/Services/Agents/AgentTurnService.cs
using System.Text.Json;

namespace EnterpriseAgentOs.Application.Services.Agents;

public sealed class AgentTurnService
{
    private readonly IAgentRepository _agents;
    private readonly IAgentLogService _logs;
    private readonly PromptComposer _promptComposer;
    private readonly LlmProviderDispatcher _llm;
    private readonly IProviderService _providers;
    private readonly ILogger<AgentTurnService> _logger;

    public AgentTurnService(
        IAgentRepository agents,
        IAgentLogService logs,
        PromptComposer promptComposer,
        LlmProviderDispatcher llm,
        IProviderService providers,
        ILogger<AgentTurnService> logger)
    {
        _agents = agents;
        _logs = logs;
        _promptComposer = promptComposer;
        _llm = llm;
        _providers = providers;
        _logger = logger;
    }

    /// <summary>
    /// Run a single agent turn: compose prompt, call LLM, execute tool calls, loop.
    /// Fires as an async Task — caller does not await completion.
    /// </summary>
    public async Task RunTurnAsync(Guid agentId, string userMessage, string correlationId, CancellationToken ct)
    {
        var agent = await _agents.GetAsync(agentId, ct);
        if (agent is null)
        {
            _logger.LogError("Agent {AgentId} not found for turn", agentId);
            return;
        }

        if (string.IsNullOrEmpty(agent.PodName))
        {
            _logger.LogError("Agent {AgentId} has no pod", agentId);
            return;
        }

        _logger.LogInformation("Starting turn for agent {AgentId}, correlation {CorrelationId}",
            agentId, correlationId);

        // 1. Compose system prompt.
        var systemPrompt = await _promptComposer.ComposeAsync(agentId, agent.Prompt, ct);

        // 2. Build conversation messages. For now, just the user message.
        //    TODO: Load conversation history from AgentLogs or a dedicated table.
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userMessage },
        };

        // 3. Define the bash tool.
        var tools = new[]
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "bash",
                    description = "Execute a bash command in the agent's operating system. " +
                        "Use this for all file operations, shell commands, package management, " +
                        "code execution, and system tasks.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            command = new
                            {
                                type = "string",
                                description = "The bash command to execute",
                            }
                        },
                        required = new[] { "command" },
                    }
                }
            }
        };

        // 4. Connect to pod.
        using var pod = new PodConnection();
        try
        {
            await pod.ConnectAsync(agent.PodName, "default", agentId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to pod {PodName} for agent {AgentId}",
                agent.PodName, agentId);
            await _logs.AppendAsync(new AgentLogRecord
            {
                AgentId = agentId,
                Type = AgentLogType.Error,
                Content = $"Failed to connect to pod: {ex.Message}",
                CorrelationId = correlationId,
                Time = DateTime.UtcNow,
            });
            return;
        }

        // 5. Turn loop: call LLM → parse tool calls → execute → repeat.
        const int maxIterations = 25;
        for (var i = 0; i < maxIterations; i++)
        {
            // Build request body.
            var requestBody = JsonSerializer.SerializeToElement(new
            {
                model = agent.Model ?? "auto",
                messages,
                tools,
                stream = true,
            });

            // Resolve API key.
            var provider = agent.Provider;
            var apiKey = Domain.Services.KnownProviders.IsKeyless(provider)
                ? "platform"
                : await _providers.GetDecryptedKeyAsync(provider, ct) ?? "";

            // Call LLM.
            HttpResponseMessage llmResponse;
            try
            {
                llmResponse = await _llm.DispatchAsync(provider, apiKey, agent.Model ?? "auto", requestBody, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM call failed for agent {AgentId}", agentId);
                await _logs.AppendAsync(new AgentLogRecord
                {
                    AgentId = agentId,
                    Type = AgentLogType.Error,
                    Content = $"LLM call failed: {ex.Message}",
                    CorrelationId = correlationId,
                    Time = DateTime.UtcNow,
                });
                return;
            }

            // Parse SSE response to extract assistant message and tool calls.
            var (assistantContent, toolCalls) = await ParseSseResponseAsync(llmResponse, ct);

            // Log assistant response.
            if (!string.IsNullOrEmpty(assistantContent))
            {
                await _logs.AppendAsync(new AgentLogRecord
                {
                    AgentId = agentId,
                    Type = AgentLogType.MessageOut,
                    Content = assistantContent,
                    CorrelationId = correlationId,
                    Time = DateTime.UtcNow,
                });
            }

            // Add assistant message to conversation.
            if (toolCalls.Count > 0)
            {
                messages.Add(new
                {
                    role = "assistant",
                    content = assistantContent ?? "",
                    tool_calls = toolCalls.Select(tc => new
                    {
                        id = tc.Id,
                        type = "function",
                        function = new { name = tc.Name, arguments = tc.Arguments }
                    }).ToList(),
                });
            }
            else
            {
                messages.Add(new { role = "assistant", content = assistantContent ?? "" });
                _logger.LogInformation("Turn complete for agent {AgentId} after {Iterations} iterations",
                    agentId, i + 1);
                return; // No tool calls — turn is complete.
            }

            // Execute tool calls.
            foreach (var tc in toolCalls)
            {
                string toolResult;
                if (tc.Name == "bash")
                {
                    var args = JsonSerializer.Deserialize<JsonElement>(tc.Arguments);
                    var command = args.GetProperty("command").GetString() ?? "";

                    await _logs.AppendAsync(new AgentLogRecord
                    {
                        AgentId = agentId,
                        Type = AgentLogType.ToolCall,
                        Tool = "bash",
                        Content = command,
                        CorrelationId = correlationId,
                        Time = DateTime.UtcNow,
                    });

                    var (output, exitCode) = await pod.ExecuteAsync(command, ct);
                    toolResult = exitCode == 0
                        ? output
                        : $"[exit code {exitCode}]\n{output}";

                    await _logs.AppendAsync(new AgentLogRecord
                    {
                        AgentId = agentId,
                        Type = AgentLogType.ToolResult,
                        Tool = "bash",
                        Content = toolResult.Length > 10000
                            ? toolResult[..10000] + "\n[truncated]"
                            : toolResult,
                        CorrelationId = correlationId,
                        Time = DateTime.UtcNow,
                    });
                }
                else
                {
                    toolResult = $"Unknown tool: {tc.Name}";
                }

                messages.Add(new
                {
                    role = "tool",
                    tool_call_id = tc.Id,
                    content = toolResult,
                });
            }
        }

        _logger.LogWarning("Agent {AgentId} hit max iterations ({Max})", agentId, maxIterations);
    }

    private record ToolCall(string Id, string Name, string Arguments);

    private static async Task<(string? Content, List<ToolCall> ToolCalls)> ParseSseResponseAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        var content = new System.Text.StringBuilder();
        var toolCalls = new Dictionary<int, (string Id, string Name, System.Text.StringBuilder Args)>();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new System.IO.StreamReader(stream);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (!line.StartsWith("data: ")) continue;
            var data = line[6..];
            if (data == "[DONE]") break;

            try
            {
                using var doc = JsonDocument.Parse(data);
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0) continue;

                var delta = choices[0].GetProperty("delta");

                if (delta.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String)
                {
                    content.Append(c.GetString());
                }

                if (delta.TryGetProperty("tool_calls", out var tcs))
                {
                    foreach (var tc in tcs.EnumerateArray())
                    {
                        var idx = tc.GetProperty("index").GetInt32();
                        if (!toolCalls.ContainsKey(idx))
                        {
                            var id = tc.GetProperty("id").GetString() ?? "";
                            var name = tc.GetProperty("function").GetProperty("name").GetString() ?? "";
                            toolCalls[idx] = (id, name, new System.Text.StringBuilder());
                        }

                        if (tc.TryGetProperty("function", out var fn) &&
                            fn.TryGetProperty("arguments", out var args) &&
                            args.ValueKind == JsonValueKind.String)
                        {
                            toolCalls[idx].Args.Append(args.GetString());
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Skip malformed SSE lines.
            }
        }

        var result = toolCalls.Values
            .Select(tc => new ToolCall(tc.Id, tc.Name, tc.Args.ToString()))
            .ToList();

        return (content.Length > 0 ? content.ToString() : null, result);
    }
}
```

- [ ] **Step 2: Register in DI**

In `src/EnterpriseAgentOs.Api/Extensions/ServiceCollectionExtensions.cs`, add to `AddApplicationServices()`:

```csharp
services.AddScoped<AgentTurnService>();
```

- [ ] **Step 3: Build to verify**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend
dotnet build EnterpriseAgentOs.sln
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/AgentTurnService.cs \
        apps/backend/src/EnterpriseAgentOs.Api/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(backend): add AgentTurnService — the agent turn loop

Orchestrates LLM calls, parses tool calls, sends bash commands to pod
via PodConnection, logs everything. Single bash tool, max 25 iterations."
```

---

## Task 8: Wire Message Delivery to AgentTurnService

**Files:**
- Modify: `src/EnterpriseAgentOs.Application/Services/AgentLogs/AgentLogService.cs`

Replace the old `KickAgentPodAsync` (which sent a user_message JSON to the pod's WebSocket) with a call to `AgentTurnService.RunTurnAsync`.

- [ ] **Step 1: Add AgentTurnService dependency to AgentLogService**

In `AgentLogService` constructor, add `AgentTurnService turnService` parameter and store as `_turnService`.

- [ ] **Step 2: Replace KickAgentPodAsync with turn service call**

Replace the body of `SendMessageAsync` (around lines 49-82). Instead of calling `KickAgentPodAsync`, fire-and-forget `AgentTurnService.RunTurnAsync`:

```csharp
public async Task SendMessageAsync(Guid agentId, string content, CancellationToken ct = default)
{
    var agent = await _repository.GetAsync(agentId, ct);
    if (agent is null) return;

    var correlationId = Guid.NewGuid().ToString("N");

    // Log the inbound message.
    await AppendAsync(new AgentLogRecord
    {
        AgentId = agentId,
        Type = AgentLogType.MessageIn,
        Content = content,
        CorrelationId = correlationId,
        Time = DateTime.UtcNow,
    });

    if (string.IsNullOrEmpty(agent.PodName))
    {
        _logger.LogWarning("Agent {AgentId} has no pod, message queued only", agentId);
        return;
    }

    // Fire-and-forget the turn loop.
    _ = Task.Run(async () =>
    {
        try
        {
            await _turnService.RunTurnAsync(agentId, content, correlationId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Turn failed for agent {AgentId}", agentId);
        }
    }, CancellationToken.None);
}
```

- [ ] **Step 3: Remove KickAgentPodAsync method**

Delete the entire `KickAgentPodAsync` method (lines ~86-141 in the current file). It is no longer needed.

- [ ] **Step 4: Build to verify**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend
dotnet build EnterpriseAgentOs.sln
```

Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add apps/backend/src/EnterpriseAgentOs.Application/Services/AgentLogs/AgentLogService.cs
git commit -m "feat(backend): wire message delivery to AgentTurnService

SendMessageAsync now fires AgentTurnService.RunTurnAsync instead of
the old KickAgentPodAsync WebSocket message forwarding. Removes the
direct pod WebSocket call that caused 'Unable to connect' errors."
```

---

## Task 9: Update KubernetesAgentDeployer for New Image

**Files:**
- Modify: `src/EnterpriseAgentOs.Infrastructure/Adapters/Kubernetes/KubernetesAgentDeployer.cs`

- [ ] **Step 1: Update env vars and image**

In `DeployAsync`, change the container env vars. The pod no longer needs `BACKEND_URL` or `ZEROCLAW_AGENT_ID`. It only needs `AGENT_TOKEN`:

Replace the env vars section (around lines 66-70):

```csharp
// Old:
// new V1EnvVar("ZEROCLAW_AGENT_ID", agentId.ToString()),
// new V1EnvVar("BACKEND_URL", "http://eaos-backend-prod.{namespace}...")

// New:
new V1EnvVar("AGENT_TOKEN", agentId.ToString()),
```

- [ ] **Step 2: Update the image reference in KubernetesConfig**

The image should now point to `harkro123/eaos-pod-executor:latest` instead of `harkro123/zeroclaw:latest`. This is configured via `KubernetesConfig.Image` which is set in `appsettings.json`.

In `src/EnterpriseAgentOs.Api/appsettings.json`, update the Kubernetes image config:

```json
"Image": "harkro123/eaos-pod-executor:latest"
```

- [ ] **Step 3: Update ServiceUrl format**

The ServiceUrl method (around line 32-33) currently points to `/ws/chat`. Update to `/ws`:

```csharp
private string ServiceUrl(Guid id) =>
    $"ws://{PodName(id)}.{_namespace}.svc.cluster.local:42617/ws";
```

- [ ] **Step 4: Remove PVC creation if no longer needed**

The pod-executor is stateless — it doesn't need a PVC for personality files. However, agents may still need filesystem persistence for their work (git repos, files they create). Keep the PVC for now but mount it at `/home` instead of `/zeroclaw-data`:

Update the volume mount path (around line 96):

```csharp
// Old: MountPath = "/zeroclaw-data"
// New:
MountPath = "/home"
```

- [ ] **Step 5: Build to verify**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend
dotnet build EnterpriseAgentOs.sln
```

Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add apps/backend/src/EnterpriseAgentOs.Infrastructure/Adapters/Kubernetes/KubernetesAgentDeployer.cs \
        apps/backend/src/EnterpriseAgentOs.Api/appsettings.json
git commit -m "feat(backend): update KubernetesAgentDeployer for pod-executor

New image (eaos-pod-executor), single AGENT_TOKEN env var,
mount PVC at /home, update ServiceUrl to /ws."
```

---

## Task 10: Seed Default Personality Files on Agent Creation

**Files:**
- Modify: `src/EnterpriseAgentOs.Application/Services/Agents/AgentService.cs`

When an agent is created, seed default personality files into Postgres.

- [ ] **Step 1: Add IAgentPersonalityRepository dependency**

Add `IAgentPersonalityRepository personalityRepo` to AgentService constructor and store as `_personalityRepo`.

- [ ] **Step 2: Seed personality files in CreateAsync**

After the agent record is created (after line 97 `await _repository.AddAsync(record, ct)`), seed default personality files:

```csharp
// Seed default personality files.
await _personalityRepo.UpsertAsync(record.Id, "SOUL.md",
    "You are an autonomous AI agent running in your own operating system. " +
    "You have full access to a bash terminal. You can install packages, " +
    "write code, run scripts, and manage files. Be helpful, precise, and proactive.", ct);

await _personalityRepo.UpsertAsync(record.Id, "IDENTITY.md",
    $"Your name is {record.Name}. You were created as an EnterpriseAgentOS agent.", ct);
```

- [ ] **Step 3: Build to verify**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend
dotnet build EnterpriseAgentOs.sln
```

Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add apps/backend/src/EnterpriseAgentOs.Application/Services/Agents/AgentService.cs
git commit -m "feat(backend): seed default personality files on agent creation

Creates SOUL.md and IDENTITY.md in Postgres when a new agent is created.
Replaces the Rust include_str! embedded template seeding."
```

---

## Task 11: CI Pipeline for pod-executor

**Files:**
- Create: `.github/workflows/build-pod-executor.yml`

- [ ] **Step 1: Write the workflow**

```yaml
# .github/workflows/build-pod-executor.yml
name: Build Pod Executor

on:
  push:
    branches: [main]
    paths:
      - "packages/pod-executor/**"

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-go@v5
        with:
          go-version: "1.22"

      - name: Test
        working-directory: packages/pod-executor
        run: go test -v ./...

      - name: Build
        working-directory: packages/pod-executor
        run: CGO_ENABLED=0 GOOS=linux go build -ldflags="-s -w" -o pod-executor .

      - uses: docker/login-action@v3
        with:
          username: harkro123
          password: ${{ secrets.DOCKERHUB_TOKEN }}

      - uses: docker/build-push-action@v5
        with:
          context: packages/pod-executor
          push: true
          tags: harkro123/eaos-pod-executor:latest
```

- [ ] **Step 2: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add .github/workflows/build-pod-executor.yml
git commit -m "ci: add build-pod-executor workflow

Triggers on packages/pod-executor/** changes. Runs tests, builds
static binary, pushes Docker image to harkro123/eaos-pod-executor:latest."
```

---

## Task 12: Update CLAUDE.md Files

**Files:**
- Modify: `/Users/harrokrog/Desktop/EnterpriseAgentOs/CLAUDE.md`
- Modify: `/Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend/CLAUDE.md`

- [ ] **Step 1: Update root CLAUDE.md**

In the package table, replace the `packages/agent-core/` row:

```markdown
| `packages/pod-executor/` | Go PTY-over-WebSocket — bash execution engine in agent pods | `packages/pod-executor/CLAUDE.md` |
```

In the system mental model, update the pod description:

```
backend spawns pods running ──► packages/pod-executor/  (Go binary, image: harkro123/eaos-pod-executor:latest)
  Pod boots with AGENT_TOKEN only. Exposes a bash PTY over WebSocket on port 42617.
  Backend connects per-turn, sends bash commands, receives streamed output.
  No LLM calls, no prompt composition, no memory — pure OS execution.

  Personality files and memory are stored in Postgres, composed by the backend.
  The agent turn loop runs in the backend as an async Task.
```

In the CI/CD table, replace `build-zeroclaw-image.yml`:

```markdown
| `build-pod-executor.yml` | `packages/pod-executor/**` | `harkro123/eaos-pod-executor:latest` | No deploy — new pods pick up `:latest` on next spawn |
```

- [ ] **Step 2: Update backend CLAUDE.md**

Add to the Application services description that `AgentTurnService` orchestrates the agent turn loop, `PodConnection` connects to pods, and `PromptComposer` builds system prompts.

Add `AgentMemoryRecord` and `AgentPersonalityRecord` to the domain models section.

- [ ] **Step 3: Commit**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git add CLAUDE.md apps/backend/CLAUDE.md
git commit -m "docs: update CLAUDE.md files for agent execution v2

Replace agent-core references with pod-executor. Document new
AgentTurnService, PodConnection, PromptComposer, and memory/personality tables."
```

---

## Summary

| Task | What | Key files |
|------|------|-----------|
| 1 | Go PTY server (clone GoTTY) | `packages/pod-executor/main.go`, `server.go`, `server_test.go` |
| 2 | Dockerfile + CLAUDE.md | `packages/pod-executor/Dockerfile`, `CLAUDE.md` |
| 3 | DB tables (memory + personality) | Domain models, EaosDbContext, migration |
| 4 | Repositories | Interfaces + implementations + DI |
| 5 | PodConnection (WS client) | `PodConnection.cs` |
| 6 | PromptComposer | `PromptComposer.cs` |
| 7 | AgentTurnService (turn loop) | `AgentTurnService.cs` |
| 8 | Wire message delivery | `AgentLogService.cs` modification |
| 9 | Update K8s deployer | `KubernetesAgentDeployer.cs`, `appsettings.json` |
| 10 | Seed personality on create | `AgentService.cs` modification |
| 11 | CI pipeline | `.github/workflows/build-pod-executor.yml` |
| 12 | Update docs | `CLAUDE.md` files |
