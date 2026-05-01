# EnterpriseAgentOs Java Backend

Example Spring Boot backend using the same layer boundaries as the main backend:

- `api` contains GraphQL controllers and HTTP error mapping.
- `application` owns use cases, transactions, and event publication.
- `domain` contains domain objects, repository ports, domain services, events, and `Result<T>`.
- `infrastructure` contains database entities, Spring Data repositories, adapters, and explicit bean configuration.

The sample has two tables: `agents` and `tool_invocations`.

## Run

```bash
mvn spring-boot:run
```

GraphQL is available at `http://localhost:8080/graphql`.
GraphiQL is available at `http://localhost:8080/graphiql`.

## Example GraphQL

```graphql
query {
  agents {
    id
    name
    status
    toolInvocations {
      id
      toolName
      status
      failureReason
    }
  }
}
```

```graphql
mutation {
  recordToolInvocation(
    input: {
      agentId: "00000000-0000-0000-0000-000000000001"
      toolName: "shell"
      status: SUCCEEDED
    }
  ) {
    id
    toolName
    status
  }
}
```
