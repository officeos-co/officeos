package main

import (
	"bytes"
	"encoding/json"
	"io"
	"mime/multipart"
	"net/http"
	"net/http/httptest"
	"net/url"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"

	"github.com/gorilla/websocket"
)

func startTestServer(t *testing.T, token string) *httptest.Server {
	t.Helper()
	return httptest.NewServer(NewHandler(token))
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

func TestUnauthorizedRestRequest(t *testing.T) {
	srv := startTestServer(t, "test-token")
	defer srv.Close()

	resp, err := http.Post(srv.URL+"/process/execute", "application/json", strings.NewReader(`{"command":"true"}`))
	if err != nil {
		t.Fatalf("request failed: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d", resp.StatusCode)
	}
}

func TestProcessExecute(t *testing.T) {
	srv := startTestServer(t, "test-token")
	defer srv.Close()

	cwd, err := filepath.EvalSymlinks(t.TempDir())
	if err != nil {
		t.Fatalf("eval symlinks: %v", err)
	}
	body, err := json.Marshal(ExecuteRequest{
		Command: "printf \"$EAOS_TEST:$PWD\" && exit 7",
		Cwd:     cwd,
		Envs:    map[string]string{"EAOS_TEST": "ok"},
		Timeout: 5,
	})
	if err != nil {
		t.Fatalf("marshal: %v", err)
	}
	req, err := http.NewRequest(http.MethodPost, srv.URL+"/process/execute", bytes.NewReader(body))
	if err != nil {
		t.Fatalf("new request: %v", err)
	}
	req.Header.Set("Authorization", "Bearer test-token")
	req.Header.Set("Content-Type", "application/json")

	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		t.Fatalf("request failed: %v", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		data, _ := io.ReadAll(resp.Body)
		t.Fatalf("expected 200, got %d: %s", resp.StatusCode, data)
	}

	var result ExecuteResponse
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		t.Fatalf("decode: %v", err)
	}
	if result.ExitCode != 7 {
		t.Fatalf("expected exit code 7, got %d", result.ExitCode)
	}
	if !strings.Contains(result.Result, "ok:"+cwd) {
		t.Fatalf("expected env and cwd in output, got %q", result.Result)
	}
}

func TestFileEndpoints(t *testing.T) {
	srv := startTestServer(t, "test-token")
	defer srv.Close()

	target := filepath.Join(t.TempDir(), "nested", "hello.txt")
	folderURL := srv.URL + "/files/folder?path=" + url.QueryEscape(filepath.Dir(target)) + "&mode=0755"
	req, err := http.NewRequest(http.MethodPost, folderURL, nil)
	if err != nil {
		t.Fatalf("new folder request: %v", err)
	}
	req.Header.Set("Authorization", "Bearer test-token")
	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		t.Fatalf("folder request failed: %v", err)
	}
	resp.Body.Close()
	if resp.StatusCode != http.StatusCreated {
		t.Fatalf("expected 201, got %d", resp.StatusCode)
	}

	var upload bytes.Buffer
	writer := multipart.NewWriter(&upload)
	part, err := writer.CreateFormFile("file", "hello.txt")
	if err != nil {
		t.Fatalf("form file: %v", err)
	}
	if _, err := part.Write([]byte("hello from rest")); err != nil {
		t.Fatalf("write multipart: %v", err)
	}
	if err := writer.Close(); err != nil {
		t.Fatalf("close multipart: %v", err)
	}

	uploadURL := srv.URL + "/files/upload?path=" + url.QueryEscape(target)
	req, err = http.NewRequest(http.MethodPost, uploadURL, &upload)
	if err != nil {
		t.Fatalf("new upload request: %v", err)
	}
	req.Header.Set("Authorization", "Bearer test-token")
	req.Header.Set("Content-Type", writer.FormDataContentType())
	resp, err = http.DefaultClient.Do(req)
	if err != nil {
		t.Fatalf("upload request failed: %v", err)
	}
	resp.Body.Close()
	if resp.StatusCode != http.StatusOK {
		t.Fatalf("expected 200, got %d", resp.StatusCode)
	}

	if data, err := os.ReadFile(target); err != nil || string(data) != "hello from rest" {
		t.Fatalf("unexpected written file: %q %v", data, err)
	}

	downloadURL := srv.URL + "/files/download?path=" + url.QueryEscape(target)
	req, err = http.NewRequest(http.MethodGet, downloadURL, nil)
	if err != nil {
		t.Fatalf("new download request: %v", err)
	}
	req.Header.Set("Authorization", "Bearer test-token")
	resp, err = http.DefaultClient.Do(req)
	if err != nil {
		t.Fatalf("download request failed: %v", err)
	}
	defer resp.Body.Close()
	data, err := io.ReadAll(resp.Body)
	if err != nil {
		t.Fatalf("read download: %v", err)
	}
	if string(data) != "hello from rest" {
		t.Fatalf("expected downloaded content, got %q", data)
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
