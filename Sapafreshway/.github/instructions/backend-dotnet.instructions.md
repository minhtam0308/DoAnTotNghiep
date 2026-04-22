---
description: "Use when editing Backend C# code (controllers, services, repositories, DTOs, mappings, tests). Enforces SapaFreshWay backend architecture, naming, async, nullability, and testing patterns."
name: "SapaFreshWay Backend .NET Conventions"
applyTo: "Backend/**/*.cs"
---

# SapaFreshWay Backend .NET Conventions

These are strict project rules unless a task explicitly asks to deviate.

## Architecture

- Keep layered boundaries strict: `SapaFreshWayAPI` controllers call BusinessAccessLayer services; services use DataAccessLayer repositories and UnitOfWork; domain models stay in DomainAccessLayer.
- Do not expose EF entities directly from controllers. Use DTOs for API contracts.

## Naming and DTOs

- Use PascalCase for classes, properties, and public methods; use camelCase for parameters and local variables.
- Allow both `Dto` and `DTO` suffix styles for consistency with existing modules; keep the local module style and do not rename solely for suffix normalization.

## Async and Cancellation

- Service and repository operations that can do I/O must be async and return `Task`/`Task<T>`.
- Include `CancellationToken ct = default` in new async public service methods and pass it through to lower layers when supported.
- Avoid `.Result` and `.Wait()` in backend code.

## Nullability

- Follow nullable reference types consistently: use `?` for optional references and avoid suppressing warnings unless needed.
- Use `= null!;` only for required properties initialized by model binding or mapping.

## Mapping

- Put AutoMapper rules in profile classes under mapping folders.
- Use explicit `.ForMember(...)` rules for non-trivial mappings and null-safe updates.
- For update DTO mappings, prevent null source members from overwriting destination values unless explicitly intended.

## Controllers and Errors

- Keep controllers thin: validation, authorization, orchestration, and response shaping only.
- Put business logic in services.
- For unhandled exceptions in API controllers, return consistent `StatusCode(500, new { ... })` payloads matching existing controller style.

## Dependency Injection

- Use constructor injection with interfaces.
- Register backend services with scoped lifetime unless a different lifetime is clearly required.

## Tests

- Use xUnit + Moq + FluentAssertions patterns already used in `Backend/Tests`.
- Follow Arrange-Act-Assert structure.
- Name tests as `MethodName_Condition_ExpectedResult`.

## Change Discipline

- Preserve existing public contracts, route shapes, and DTO fields unless explicitly asked to change them.
- Keep changes minimal and localized; avoid broad refactors in unrelated files.