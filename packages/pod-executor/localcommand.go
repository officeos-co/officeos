// Derived from github.com/yudai/gotty/backend/localcommand — PTY process management.
// Kept: PTY spawning, Read/Write, Close with signal + timeout, ResizeTerminal.
// Stripped: WindowTitleVariables, factory pattern, options pattern.
// Changed: kr/pty → creack/pty (maintained fork).
package main

import (
	"os"
	"os/exec"
	"syscall"
	"time"
	"unsafe"

	"github.com/creack/pty"
)

const (
	defaultCloseSignal  = syscall.SIGINT
	defaultCloseTimeout = 10 * time.Second
)

// LocalCommand wraps a command running in a PTY.
// Implements io.ReadWriteCloser — same interface as GoTTY's LocalCommand.
type LocalCommand struct {
	cmd       *exec.Cmd
	pty       *os.File
	ptyClosed chan struct{}
}

// NewLocalCommand spawns a command in a new PTY.
// Derived from GoTTY's localcommand.New().
func NewLocalCommand(command string, argv []string) (*LocalCommand, error) {
	cmd := exec.Command(command, argv...)
	cmd.Env = os.Environ()

	ptmx, err := pty.Start(cmd)
	if err != nil {
		return nil, err
	}

	ptyClosed := make(chan struct{})
	lcmd := &LocalCommand{
		cmd:       cmd,
		pty:       ptmx,
		ptyClosed: ptyClosed,
	}

	// GoTTY pattern: goroutine that waits for process exit, then closes PTY
	// so that Read() breaks with EOF.
	go func() {
		defer func() {
			lcmd.pty.Close()
			close(lcmd.ptyClosed)
		}()
		lcmd.cmd.Wait()
	}()

	return lcmd, nil
}

func (lcmd *LocalCommand) Read(p []byte) (int, error) {
	return lcmd.pty.Read(p)
}

func (lcmd *LocalCommand) Write(p []byte) (int, error) {
	return lcmd.pty.Write(p)
}

// Close sends SIGINT, waits for timeout, then SIGKILL.
// Derived from GoTTY's LocalCommand.Close().
func (lcmd *LocalCommand) Close() error {
	if lcmd.cmd != nil && lcmd.cmd.Process != nil {
		lcmd.cmd.Process.Signal(defaultCloseSignal)
	}
	for {
		select {
		case <-lcmd.ptyClosed:
			return nil
		case <-time.After(defaultCloseTimeout):
			lcmd.cmd.Process.Signal(syscall.SIGKILL)
		}
	}
}

// ResizeTerminal sets the PTY window size.
// Taken directly from GoTTY's LocalCommand.ResizeTerminal().
func (lcmd *LocalCommand) ResizeTerminal(width int, height int) error {
	window := struct {
		row uint16
		col uint16
		x   uint16
		y   uint16
	}{
		uint16(height),
		uint16(width),
		0,
		0,
	}
	_, _, errno := syscall.Syscall(
		syscall.SYS_IOCTL,
		lcmd.pty.Fd(),
		syscall.TIOCSWINSZ,
		uintptr(unsafe.Pointer(&window)),
	)
	if errno != 0 {
		return errno
	}
	return nil
}
