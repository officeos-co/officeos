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
		processWSConn(r.Context(), conn)
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

	req := Request{ID: "test-1", Input: "echo HELLO_POD_EXECUTOR\n"}
	msg, _ := json.Marshal(req)
	if err := conn.WriteMessage(websocket.TextMessage, msg); err != nil {
		t.Fatalf("write failed: %v", err)
	}

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
