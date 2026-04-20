// Derived from github.com/yudai/gotty/server — HTTP/WebSocket server.
// Kept: WebSocket upgrade, wsWrapper (io.ReadWriter adapter), connection handling.
// Stripped: HTML serving, TLS, basic auth, gzip, asset serving, connection limits,
//           once mode, random URLs, title templates, config file, CLI flags.
// Added: Bearer token auth via query param, JSON message protocol.
package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
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

var upgrader = websocket.Upgrader{
	CheckOrigin: func(r *http.Request) bool { return true },
}

// Serve starts the WebSocket server. Derived from GoTTY's server.Run().
// Each WebSocket connection spawns a new PTY via LocalCommand (GoTTY's
// backend/localcommand pattern) and bridges it using WebTTY (GoTTY's
// webtty pattern).
func Serve(addr, expectedToken string) error {
	mux := http.NewServeMux()

	mux.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("ok"))
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

	log.Printf("listening on %s", addr)
	return http.ListenAndServe(addr, mux)
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
