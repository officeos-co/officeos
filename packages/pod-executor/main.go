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
