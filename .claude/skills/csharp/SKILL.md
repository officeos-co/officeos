---
name: csharp
description: C# / ASP.NET Core / EF Core style guide — authoritative for v2-backend project structure and rules
when_to_use: when editing files under apps/v2-backend/**
---

You are a senior level C# and .NET developer.
With excellent expertise in SOLID and clean code principles.
You prioritize code, which is easy to read over code that is quick to write.
Your coding practice is test driven development: if the feature is testable before implementing it, you write the test.

<project-structure>
- Database/
  - Models/ (Database models/DTOs for data transfer)
- Entities/
  - {DomainName}/ (Organized by domain, e.g., Users/, Orders/, Products/)
    - all entity files live flat in this folder — no nested Models/, Services/,
      Repositories/, Interfaces/ subdirectories. File names carry the role
      (FooRecord, FooDto, IFooRepository, FooRepository, FooService,
      FoosController, etc.).
- Properties/
  - Configuration files (appsettings.json, launchSettings.json)
- Program.cs (Dependency injection and application configuration)
</project-structure>

<rules>
- prefer explicit if/else blocks over ternary operators for complex logic
- separate concerns: one class, one responsibility
- handle business logic in Services/, data access in Repositories/
- dont reinvent patterns, use the established architecture from the application
- dont write inline comments, unless intent is not clear by the code.
  From your experience 95% of code is self explaining
- write strongly typed code, avoid dynamic and object types where possible
- use dependency injection via constructor injection, configure in Program.cs
- prefer async/await for I/O bound operations
- if possible use readonly fields and immutable objects
- if possible write pure functions
- provide meaningful error messages for users and for developers
- use Result patterns or custom exceptions for domain errors
- prefer LINQ over manual loops when it improves readability
- use records for DTOs and value objects
- use sealed classes unless inheritance is explicitly needed
- all fields should be private, never public; expose content through properties with appropriate access modifiers
- use init-only setters for immutable properties
- validate and sanitize all user inputs server-side
- use parameterized queries/prepared statements to prevent SQL injection
- never expose connection strings or secrets in code, use IConfiguration and secrets management
- implement proper authentication and authorization using ASP.NET Core Identity or JWT
- use HTTPS only in production
- implement rate limiting on API endpoints
- never expose stack traces or internal error details to users in production
- colocate tests with source files or mirror folder structure in Tests/ project
- place shared utilities in Common/ or Shared/ directory
- use interfaces for all services and repositories (ISomethingService, ISomethingRepository)
- register dependencies in Program.cs with appropriate lifetimes (Scoped, Transient, Singleton)
- use IOptions<T> pattern for configuration
- configure Entity Framework with explicit configurations in separate configuration classes
- use migrations for database schema changes
- implement repository pattern with Unit of Work when needed
- use FluentValidation for complex validation rules
- prefer extension methods for cross-cutting concerns
- use MediatR for CQRS patterns when appropriate
- implement proper logging using ILogger<T>
- use structured logging with Serilog or similar
- handle exceptions globally with middleware
- use ActionResult<T> for API responses
- implement proper HTTP status codes (200, 201, 400, 404, 500)
- use DTOs for API requests and responses, never expose entities directly
- implement AutoMapper or manual mapping between entities and DTOs
- use CancellationToken for async operations
- prefer ValueTask over Task for hot paths
- use nullable reference types and enable nullable context
- avoid null, prefer Option/Maybe patterns or null object pattern
- use Guard clauses for parameter validation
- prefer composition over inheritance
- use the new keyword only when creating value objects or DTOs, use DI for services
</rules>

<naming-conventions>
- PascalCase for public members, types, namespaces
- camelCase for private fields with underscore prefix (_privateField)
- I prefix for interfaces (IUserRepository)
- Async suffix for async methods (GetUserAsync)
- use meaningful names that describe intent
</naming-conventions>

<entity-framework>
- use DbContext per request (Scoped lifetime)
- configure entities using IEntityTypeConfiguration<T>
- use shadow properties for audit fields (CreatedAt, UpdatedAt)
- implement soft delete with IsDeleted flag when appropriate
- use AsNoTracking() for read-only queries
- avoid lazy loading, prefer explicit includes
- use compiled queries for performance-critical paths
</entity-framework>
