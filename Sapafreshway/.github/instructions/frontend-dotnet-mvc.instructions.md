---
description: "Use when editing Frontend ASP.NET MVC/Razor code (controllers, services, DTOs, viewmodels, views, scripts, styles) in customer and staff web apps. Enforces SapaFreshWay frontend structure and coding conventions."
name: "SapaFreshWay Frontend MVC Conventions"
applyTo: Frontend/**/*.cs, Frontend/**/*.cshtml, Frontend/**/*.css, Frontend/**/*.js
---

# SapaFreshWay Frontend MVC Conventions

These are strict project rules unless a task explicitly asks to deviate.

## Scope

- Apply these rules to both frontend apps: `WebSapaFreshWayForCustomer` and `WebSapaFreshWayForStaff`.
- Preserve existing app differences (customer app is simpler; staff app uses interface-based API services).

## Project Structure

- Keep MVC separation: controllers under `Controllers`, views under `Views`, shared UI in `Views/Shared`, static assets in `wwwroot`.
- Keep service code in `Services`, API interface abstractions under `Services/Api/Interfaces` where that pattern already exists.
- Keep DTOs and ViewModels organized by feature/domain folders where the module already follows that approach.

## Controllers

- Use controller names in `[Feature]Controller` format and action methods returning `IActionResult` or `Task<IActionResult>`.
- Prefer async actions for operations involving API calls, I/O, or database-backed endpoints.
- Keep controllers thin: input validation, orchestration, and view/result shaping only.
- Use attributes consistently: HTTP verb attributes, anti-forgery for form posts, and authorization policies where required.

## Services and HTTP Calls

- Use dependency injection for services and HttpClient usage.
- Prefer `IHttpClientFactory`/configured clients instead of creating unmanaged HttpClient instances.
- In staff app, keep interface + implementation pairing for API services and reuse base service abstractions when available.
- Handle failed HTTP responses explicitly and map errors into user-safe messages.

## DTOs and ViewModels

- Keep API contract types in DTO folders and page/view state types in ViewModel folders.
- Use clear suffixes: `Dto`, `Response`, `Request`, and `ViewModel` according to purpose.
- Preserve existing request/response contracts unless the task explicitly requires API shape changes.

## Razor Views and Layouts

- Keep feature views under matching `Views/[ControllerName]` folders.
- Keep shared partials in `Views/Shared` or feature-specific partials inside the owning feature folder.
- Name partial views with a leading underscore and descriptive suffix where useful (for example modal/list/form partials).
- Reuse existing shared layouts and section patterns rather than introducing unrelated layout systems.

## Validation and Errors

- Validate `ModelState` in form post actions and return the current view with validation errors when invalid.
- On service/API failures, log errors server-side and show safe, actionable feedback in the UI.
- Do not leak sensitive exception details to end users.

## Assets (CSS and JS)

- Keep global styles/scripts in shared files and feature-specific behavior in feature files under `wwwroot/css` and `wwwroot/js`.
- Prefer existing libraries and patterns already used by the app (for example Bootstrap, jQuery validation, toastr, SweetAlert) unless a task requires change.
- Avoid inline scripts/styles in views when logic can be placed in static files.

## Change Discipline

- Keep changes localized to the relevant feature area; avoid cross-module refactors unless requested.
- Preserve route patterns, view names, and data contract behavior unless explicitly asked to change them.