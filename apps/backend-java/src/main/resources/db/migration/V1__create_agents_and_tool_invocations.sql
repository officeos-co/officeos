CREATE TABLE agents (
    id UUID PRIMARY KEY,
    name VARCHAR(120) NOT NULL,
    status VARCHAR(32) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE TABLE tool_invocations (
    id UUID PRIMARY KEY,
    agent_id UUID NOT NULL REFERENCES agents(id) ON DELETE CASCADE,
    tool_name VARCHAR(120) NOT NULL,
    status VARCHAR(32) NOT NULL,
    failure_reason VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX idx_tool_invocations_agent_id ON tool_invocations(agent_id);

INSERT INTO agents (id, name, status, created_at)
VALUES ('00000000-0000-0000-0000-000000000001', 'Example coding agent', 'ACTIVE', CURRENT_TIMESTAMP);
