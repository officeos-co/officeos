// Derived from github.com/yudai/gotty/server — HTTP/WebSocket server.
// Kept: WebSocket upgrade, wsWrapper (io.ReadWriter adapter), connection handling.
// Stripped: HTML serving, TLS, basic auth, gzip, asset serving, connection limits,
//
//	once mode, random URLs, title templates, config file, CLI flags.
//
// Added: Daytona-like REST toolbox endpoints, bearer auth, JSON message protocol.
package main

import (
	"bytes"
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"log"
	"mime"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
	"syscall"
	"time"
	"unicode/utf8"

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

// ExecuteRequest matches Daytona Toolbox's /process/execute request shape.
type ExecuteRequest struct {
	Command string            `json:"command"`
	Cwd     string            `json:"cwd,omitempty"`
	Envs    map[string]string `json:"envs,omitempty"`
	Timeout int               `json:"timeout,omitempty"`
}

// ExecuteResponse matches Daytona Toolbox's /process/execute response shape.
type ExecuteResponse struct {
	Result   string `json:"result"`
	ExitCode int    `json:"exitCode"`
}

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool { return true },
}

// Serve starts the toolbox HTTP server. REST endpoints are the primary backend
// interface; the WebSocket endpoint remains available for interactive PTY use.
func Serve(addr, expectedToken string) error {
	log.Printf("listening on %s", addr)
	return http.ListenAndServe(addr, NewHandler(expectedToken))
}

func NewHandler(expectedToken string) http.Handler {
	mux := http.NewServeMux()

	mux.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("ok"))
	})

	mux.HandleFunc("/process/execute", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
			return
		}
		if !authorizeBearer(w, r, expectedToken) {
			return
		}
		handleProcessExecute(w, r)
	})

	mux.HandleFunc("/files/download", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodGet {
			http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
			return
		}
		if !authorizeBearer(w, r, expectedToken) {
			return
		}
		handleFileDownload(w, r)
	})

	mux.HandleFunc("/files/upload", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
			return
		}
		if !authorizeBearer(w, r, expectedToken) {
			return
		}
		handleFileUpload(w, r)
	})

	mux.HandleFunc("/files/folder", func(w http.ResponseWriter, r *http.Request) {
		if r.Method != http.MethodPost {
			http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
			return
		}
		if !authorizeBearer(w, r, expectedToken) {
			return
		}
		handleFolderCreate(w, r)
	})

	mux.HandleFunc("/ws", func(w http.ResponseWriter, r *http.Request) {
		// Auth: token from query param. GoTTY uses HTTP Basic Auth +
		// InitMessage token; we simplify to query param only.
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

		log.Printf("new connection from %s", r.RemoteAddr)
		err = processWSConn(r.Context(), conn)
		if err != nil {
			log.Printf("connection closed: %v", err)
		}
	})

	return mux
}

func authorizeBearer(w http.ResponseWriter, r *http.Request, expectedToken string) bool {
	if r.Header.Get("Authorization") != "Bearer "+expectedToken {
		http.Error(w, "unauthorized", http.StatusUnauthorized)
		return false
	}
	return true
}

func handleProcessExecute(w http.ResponseWriter, r *http.Request) {
	var req ExecuteRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "invalid JSON body: "+err.Error(), http.StatusBadRequest)
		return
	}
	if strings.TrimSpace(req.Command) == "" {
		http.Error(w, "command is required", http.StatusBadRequest)
		return
	}

	timeout := 10 * time.Second
	if req.Timeout > 0 {
		timeout = time.Duration(req.Timeout) * time.Second
	}

	ctx, cancel := context.WithTimeout(r.Context(), timeout)
	defer cancel()

	cmd := exec.CommandContext(ctx, "/bin/bash", "-lc", req.Command)
	if req.Cwd != "" {
		cmd.Dir = req.Cwd
	}
	cmd.Env = os.Environ()
	for key, value := range req.Envs {
		cmd.Env = append(cmd.Env, key+"="+value)
	}

	var output bytes.Buffer
	cmd.Stdout = &output
	cmd.Stderr = &output

	err := cmd.Run()
	exitCode := exitCodeFromError(err)
	if ctx.Err() == context.DeadlineExceeded {
		exitCode = 124
	}
	if err != nil && output.Len() == 0 {
		output.WriteString(err.Error())
	}

	writeJSON(w, http.StatusOK, ExecuteResponse{
		Result:   sanitizeUTF8(output.Bytes()),
		ExitCode: exitCode,
	})
}

func handleFileDownload(w http.ResponseWriter, r *http.Request) {
	path := r.URL.Query().Get("path")
	if path == "" {
		http.Error(w, "path is required", http.StatusBadRequest)
		return
	}

	file, err := os.Open(path)
	if err != nil {
		http.Error(w, err.Error(), http.StatusNotFound)
		return
	}
	defer file.Close()

	if contentType := mime.TypeByExtension(filepath.Ext(path)); contentType != "" {
		w.Header().Set("Content-Type", contentType)
	} else {
		w.Header().Set("Content-Type", "application/octet-stream")
	}
	_, _ = io.Copy(w, file)
}

func handleFileUpload(w http.ResponseWriter, r *http.Request) {
	path := r.URL.Query().Get("path")
	if path == "" {
		http.Error(w, "path is required", http.StatusBadRequest)
		return
	}

	file, _, err := r.FormFile("file")
	if err != nil {
		http.Error(w, "file is required: "+err.Error(), http.StatusBadRequest)
		return
	}
	defer file.Close()

	if err := os.MkdirAll(filepath.Dir(path), 0755); err != nil && filepath.Dir(path) != "." {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	dst, err := os.Create(path)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	defer dst.Close()

	if _, err := io.Copy(dst, file); err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	writeJSON(w, http.StatusOK, map[string]bool{"ok": true})
}

func handleFolderCreate(w http.ResponseWriter, r *http.Request) {
	path := r.URL.Query().Get("path")
	if path == "" {
		http.Error(w, "path is required", http.StatusBadRequest)
		return
	}

	mode := os.FileMode(0755)
	if rawMode := r.URL.Query().Get("mode"); rawMode != "" {
		parsed, err := strconv.ParseUint(rawMode, 8, 32)
		if err != nil {
			http.Error(w, "invalid mode", http.StatusBadRequest)
			return
		}
		mode = os.FileMode(parsed)
	}

	if err := os.MkdirAll(path, mode); err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusCreated)
}

func exitCodeFromError(err error) int {
	if err == nil {
		return 0
	}

	var exitErr *exec.ExitError
	if errors.As(err, &exitErr) {
		if status, ok := exitErr.Sys().(syscall.WaitStatus); ok {
			return status.ExitStatus()
		}
	}

	return -1
}
func writeJSON(w http.ResponseWriter, status int, value any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(value)
}

// processWSConn handles a single WebSocket connection.
// Derived from GoTTY's server.processWSConn():
// 1. Create a LocalCommand (PTY slave)
// 2. Wrap the WebSocket conn as io.ReadWriter (GoTTY's wsWrapper)
// 3. Bridge them with WebTTY.Run()
func processWSConn(ctx context.Context, conn *websocket.Conn) error {
	// Spawn bash PTY — GoTTY's factory.New() pattern, simplified.
	slave, err := NewLocalCommand("/bin/bash", []string{})
	if err != nil {
		sendError(conn, "", fmt.Sprintf("failed to start command: %v", err))
		return err
	}
	defer slave.Close()

	// Set reasonable default terminal size.
	slave.ResizeTerminal(200, 50)

	// wsWrapper: GoTTY's adapter that makes websocket.Conn implement io.ReadWriter.
	wrapper := &wsWrapper{conn}

	// WebTTY: GoTTY's bidirectional bridge between master (WebSocket) and slave (PTY).
	tty := NewWebTTY(wrapper, slave)

	return tty.Run(ctx)
}

// wsWrapper adapts gorilla/websocket.Conn to io.ReadWriter.
// Taken directly from GoTTY's server/ws_wrapper.go.
type wsWrapper struct {
	*websocket.Conn
}

func (wsw *wsWrapper) Write(p []byte) (int, error) {
	writer, err := wsw.Conn.NextWriter(websocket.TextMessage)
	if err != nil {
		return 0, err
	}
	defer writer.Close()
	return writer.Write(p)
}

func (wsw *wsWrapper) Read(p []byte) (int, error) {
	for {
		msgType, reader, err := wsw.Conn.NextReader()
		if err != nil {
			return 0, err
		}

		if msgType != websocket.TextMessage {
			continue
		}

		return reader.Read(p)
	}
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
