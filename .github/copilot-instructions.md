# CustomerFirstPortal Copilot Instructions

This document provides Copilot with workspace-specific guidance for the Customer First Portal project.

## Project Overview

- **Type**: Monorepo with Angular 19 frontend, ASP.NET Core 8 API, on-prem SQL Server backend
- **Purpose**: Employee recognition platform with admin review workflow, email notifications, and Gerrie integration
- **Phase 1 Scope**: Two submission forms, admin approval flow, analytics, and Gerrie posting

## Architecture Principles

1. **Clean Architecture**: Backend uses layered architecture (API → Application → Domain → Infrastructure)
2. **CQRS Pattern**: Separate read/write paths for clarity in complex workflows
3. **DDD Concepts**: Aggregate roots, value objects, and domain events shape the domain layer
4. **Security-First**: All endpoints require authentication; sensitive operations are audited
5. **Hybrid Cloud**: On-prem SQL Server + Azure services (Blob, Email) for flexibility

## Coding Conventions

### Backend (.NET)

- Use async/await throughout; no sync-over-async patterns
- Validation happens in Application layer validators (FluentValidation)
- Return `Result<T>` for operations that can fail; throw for exceptional cases
- Use dependency injection for all services; no service locator
- Add audit events for submit/edit/approve/reject/sync operations
- Log correlation IDs for request tracing

### Frontend (Angular)

- Use typed HTTP services with interceptors for auth and error handling
- Prefer reactive programming with RxJS; use `async` pipe in templates
- Guard routes with role-based `CanActivate` guards for admin pages
- Keep components small; extract logic to services
- Use smart/dumb component pattern: containers call services, presenters show data

## File Structure Expected

```
Backend: src/backend/Portal.Api/
  - Program.cs (DI, middleware)
  - appsettings*.json
  - Features/ (feature folders with handlers, validators)
  - Domain/Entities/
  - Infrastructure/Persistence/ (EF, migrations)
  - Infrastructure/Storage/ (Blob service)
  - Infrastructure/Notifications/ (Email service)
  - Infrastructure/Integrations/Gerrie/ (Gerrie client)

Frontend: src/frontend/customer-first-portal/
  - src/app/core/ (auth, guards, interceptors)
  - src/app/features/submissions/ (forms)
  - src/app/features/admin-review/ (review page)
  - src/app/features/admin-list/ (sortable list)
  - src/app/features/settings/ (admin config)
  - src/app/features/analytics/ (KPI dashboard)
  - src/app/shared/ (pipes, directives, utility components)
```

## Development Workflow

1. **Database Changes**: Use EF Core migrations; update seed data if needed
2. **API Changes**: Update shared contracts in `src/shared/` for frontend consumption
3. **Feature Work**: Create feature folder, add handlers/validators, wire in DI
4. **Testing**: Write unit tests in same folder as implementation; integration tests in dedicated test project
5. **Code Review**: Ensure tests pass, no security warnings, and follows conventions above

## Key Integrations

- **Entra ID**: Frontend uses `@azure/msal-angular`; backend validates via `Microsoft.Identity.Web`
- **EF Core**: Maps to on-prem SQL Server; migrations committed to repo
- **Azure Blob**: Uses `Azure.Storage.Blobs`; secure access via signed URLs
- **Email**: Uses SMTP or SendGrid; configurable per environment
- **Gerrie**: Custom adapter in Infrastructure; handles mapping, retries, and logging

## Common Tasks

- **Add a new field to submission form**: Update Domain entity → EF migration → API handler → Angular form
- **Add admin permission check**: Use `[Authorize(Policy = "AdminOnly")]` on endpoint; add route guard on frontend
- **Enable new analytics KPI**: Add query to Analytics handler → Expose endpoint → Add widget to Angular dashboard
- **Fix Gerrie sync failure**: Check Infrastructure/Integrations/Gerrie logs, verify payload mapping, retry manually

## Testing Strategy

- Unit tests for validation, state transitions, and RBAC
- Integration tests for database operations and Gerrie sync
- E2E tests for happy path: submit → notify → review → approve/reject → sync
- Attach sample data in test seeds for reproducible scenarios

## Deployment Notes

- SQL migrations run automatically on startup (EF Core)
- Feature flags/configuration loaded from `appsettings.{Environment}.json`
- Entra app registration must allow redirect URIs for frontend callback
- Azure Blob connection requires Azure AD identity or account key (recommend Managed Identity in production)

## Troubleshooting

- **401 Unauthorized on API**: Check Entra ID token in browser DevTools, verify `Authorization` header present
- **Gerrie sync failures**: Check Infrastructure logs for payload/response; manual retry via admin UI or logs
- **Email not sending**: Verify SMTP credentials in config; check Application logs for send errors
- **List filtering slow**: Run SQL query analyzer on admin-list query; consider index on filtered columns

---

Use this as a reference when generating code, suggesting refactors, or helping debug issues in this workspace.
