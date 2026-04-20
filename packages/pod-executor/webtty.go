// Derived from github.com/yudai/gotty/webtty — the PTY↔WebSocket bridge.
// Stripped: window title, reconnect, preferences, base64 encoding.
// Added: JSON message framing with request IDs.
package main

import (
	"context"
	"encoding/json"
	"errors"
	"io"
	"sync"
)

var (
	ErrSlaveClosed  = errors.New("slave closed")
	ErrMasterClosed = errors.New("master closed")
)

// WebTTY bridges a PTY slave and a WebSocket master.
// Derived from GoTTY's webtty.WebTTY with the same two-goroutine architecture.
type WebTTY struct {
	masterConn io.ReadWriter
	slave      io.ReadWriteCloser

	bufferSize int
	writeMutex sync.Mutex

	// Current request ID for correlating output with commands.
	currentID string
	idMutex   sync.Mutex
}

func NewWebTTY(masterConn io.ReadWriter, slave io.ReadWriteCloser) *WebTTY {
	return &WebTTY{
		masterConn: masterConn,
		slave:      slave,
		bufferSize: 32 * 1024,
	}
}

// Run starts the bidirectional bridge. Blocks until ctx is canceled or
// one side closes. Mirrors GoTTY's webtty.Run().
func (wt *WebTTY) Run(ctx context.Context) error {
	errs := make(chan error, 2)

	// Goroutine 1: slave (PTY) → master (WebSocket)
	// From GoTTY: reads PTY output and forwards to master.
	go func() {
		errs <- func() error {
			buffer := make([]byte, wt.bufferSize)
			for {
				n, err := wt.slave.Read(buffer)
				if err != nil {
					return ErrSlaveClosed
				}

				err = wt.handleSlaveReadEvent(buffer[:n])
				if err != nil {
					return err
				}
			}
		}()
	}()

	// Goroutine 2: master (WebSocket) → slave (PTY)
	// From GoTTY: reads master messages and writes input to PTY.
	go func() {
		errs <- func() error {
			buffer := make([]byte, wt.bufferSize)
			for {
				n, err := wt.masterConn.Read(buffer)
				if err != nil {
					return ErrMasterClosed
				}

				err = wt.handleMasterReadEvent(buffer[:n])
				if err != nil {
					return err
				}
			}
		}()
	}()

	select {
	case <-ctx.Done():
		return ctx.Err()
	case err := <-errs:
		return err
	}
}

// handleSlaveReadEvent sends PTY output to the WebSocket master as JSON.
// GoTTY uses base64 + byte prefix; we use JSON with request ID correlation.
func (wt *WebTTY) handleSlaveReadEvent(data []byte) error {
	wt.idMutex.Lock()
	id := wt.currentID
	wt.idMutex.Unlock()

	resp := Response{ID: id, Type: "output", Data: sanitizeUTF8(data)}
	msg, err := json.Marshal(resp)
	if err != nil {
		return err
	}

	wt.writeMutex.Lock()
	defer wt.writeMutex.Unlock()

	_, err = wt.masterConn.Write(msg)
	return err
}

// handleMasterReadEvent parses a JSON command from the WebSocket and writes
// the input to the PTY. GoTTY uses a byte-prefix protocol with Input/Ping/Resize;
// we use JSON with {id, input} fields.
func (wt *WebTTY) handleMasterReadEvent(data []byte) error {
	var req Request
	if err := json.Unmarshal(data, &req); err != nil {
		// Send error back to master.
		resp := Response{Type: "error", Data: "invalid request: " + err.Error()}
		msg, _ := json.Marshal(resp)
		wt.writeMutex.Lock()
		wt.masterConn.Write(msg)
		wt.writeMutex.Unlock()
		return nil // Don't kill the connection on bad input.
	}

	wt.idMutex.Lock()
	wt.currentID = req.Input // not a bug: we set currentID to req.ID below
	wt.currentID = req.ID
	wt.idMutex.Unlock()

	_, err := wt.slave.Write([]byte(req.Input))
	if err != nil {
		resp := Response{ID: req.ID, Type: "error", Data: "pty write failed: " + err.Error()}
		msg, _ := json.Marshal(resp)
		wt.writeMutex.Lock()
		wt.masterConn.Write(msg)
		wt.writeMutex.Unlock()
		return ErrSlaveClosed
	}

	return nil
}
