# Customer First Portal — ITIL Service Management Documentation

Scope: the Customer First Portal (Angular SPA + Portal.Api + SQL Server + Azure Blob + Entra ID + Gerrie integration), mapped to ITIL 4 practices. This complements `docs/PROCESS_FLOWS.md` (technical flows) with the service-management view: what the service is, how it's requested, run, changed, and recovered.

## 1. Service Definition (Service Catalog entry)

| Field | Value |
|---|---|
| Service name | Customer First Employee Recognition Portal |
| Service description | Employee-to-employee and employee/customer-driven recognition submission, admin review/approval, email notification, analytics, and downstream sync to Gerrie |
| Service owner | Product owner for Customer First / Employee Experience (assign in org) |
| Service type | Business application (internal, Tier 2) |
| Customers | All employees (submitters), designated Admin reviewers, leadership (analytics consumers) |
| Consumption channels | Web SPA (`http://*/` behind corporate auth), email (review links, notifications) |
| Dependencies | Microsoft Entra ID (auth), Microsoft Graph (directory/photos), SQL Server (data), Azure Blob Storage (attachments), SMTP relay (email), Gerrie SQL database (stored procedure `spExternalEmployeeAnnouncements_Upsert`) |
| Hosting | On-prem SQL Server + Azure App Service/Container Apps or on-prem (per `/infrastructure`) |
| Data classification | Internal / contains employee & customer PII (names, emails, department, recognition text, photos) |

## 2. Service Request Management

Requests are the normal, pre-approved way users interact with the service — not incidents.

| Request type | Initiator | Channel | Fulfillment |
|---|---|---|---|
| Submit employee recognition | Any employee | SPA: `/submissions/employee` | Immediate (`POST /api/submissions`), auto-routed to review queue |
| Submit customer-driven recognition (on behalf of / from customer) | Any employee | SPA: `/submissions/customer` | Immediate, same workflow, customer fields populated |
| Attach supporting file | Submitter | SPA attachment upload | Stored in Blob, linked to submission |
| Request admin access (RBAC) | Manager/Admin | Out-of-band (ticket) → Admin Settings page | Existing admin adds user via `POST /api/adminusers`; standard change, no CAB needed (data change, not code) |
| Edit a submission before decision | Admin | SPA review detail | `PATCH /api/submissions/{id}` |
| Manual re-sync to Gerrie | Admin/Support | (Current build: automatic retry only — no UI button yet; see Known Gaps) | Background worker retries with backoff |

**Standard Change**: adding/removing an Admin user is a pre-authorized standard change — low risk, repeatable, executed directly in Admin Settings, logged via admin identity cache epoch bump (immediate effect, no deployment).

## 3. Incident Management

### Incident categories mapped to this app

| Category | Example symptom | Likely component | Initial diagnostic |
|---|---|---|---|
| Authentication failure | User can't log in / "unauthorized" | Entra ID / token config | Check Entra app registration, `Entra:TenantId`/`ClientId`, token expiry |
| Submission not saving | 400/500 on submit | `SubmissionsController`/validators | Check `CreateSubmissionValidator`, API logs by `CorrelationId` |
| Attachment upload failing | Upload spinner/error | Azure Blob Storage | Check `AzureStorage:ConnectionString`, blob container permissions, file size limits |
| Email notifications not arriving | Admins not notified of new submissions | SMTP / `NotificationDispatchWorkerService` | Query `NotificationDispatch` table for `Status=Failed` rows and `LastError` |
| Gerrie sync failures | Approved items not appearing in Gerrie | `GerrieSyncWorkerService` / stored procedure | Query `GerrieSyncEvent` for `SyncStatus=Failed`, check `ResponseMessage`, verify Gerrie DB connectivity and stored-proc signature (`@EmployeeId` support) |
| Directory/photo lookups failing | Nominee picker empty, no profile photos | Microsoft Graph | Check Graph token scopes (`User.Read`, `User.ReadBasic.All`), throttling (429s), `GraphPhotoService` cache |
| Analytics page blank/slow | Dashboard fails to load | Analytics queries / SQL perf | Check query execution plans on `Submissions`/`SubmissionReviews`, index usage |
| AI suggestion errors | Suggested-edits panel fails or stale | `SuggestionGenerationService` | Check `SubmissionSuggestion.Status = Failed` + `ErrorMessage`, AI provider availability |

### Severity guide

| Severity | Definition | Example | Target response / restore |
|---|---|---|---|
| P1 — Critical | Service down for all users, or data loss risk | API unreachable, DB connection pool exhausted, auth outage | Respond 15 min / restore 4 hr |
| P2 — High | Core workflow broken for a user segment | Submission creation fails for all users; admins can't approve | Respond 30 min / restore 8 hr |
| P3 — Medium | Degraded but workaround exists | Gerrie sync delayed (retries queued), emails delayed | Respond next business day / restore 2 business days |
| P4 — Low | Cosmetic / isolated / single-user | Profile photo missing for one user, analytics chart mislabeled | Respond 3 business days |

### Built-in diagnostic aids (already in code — use first)
- **Correlation IDs** (`CorrelationIdMiddleware`) — thread every support ticket's correlation ID through API logs.
- **Audit trail** (`AuditEvent` table) — per-submission history: Submitted, AttachmentAdded/Deleted, ReviewOpened, Approved, Rejected, GerrieSyncQueued/Succeeded/Failed, AdminUpdated. First place to check "what happened to this submission."
- **Retry queues as tables** — `NotificationDispatch` and `GerrieSyncEvent` are durable, queryable retry logs; no log-diving required for dispatch/sync incidents.
- **Admin identity cache epoch** — if a newly added/removed admin doesn't see effect immediately, the epoch bump should have invalidated cache within the configured TTL (`Security:AdminIdentityCacheSeconds`, default 30s).

## 4. Problem Management

Known structural risks to track as Problem records (root-cause investigation, not one-off incidents):

| Problem | Root cause area | Workaround | Permanent fix direction |
|---|---|---|---|
| Gerrie sync depends on fuzzy name/email matching against `Employees` table | `ResolveEmployeeIdAsync` uses OR'd LIKE/email heuristics | Manual correction in Gerrie after the fact | Require a stable employee key (Entra Object ID) in Gerrie schema |
| Graph API calls on the request path (`/me`, directory search, photos) add latency/availability coupling | Synchronous Graph calls inside controller actions | Retry/backoff already present; photo caching mitigates | Consider async pre-warming or a resilience library (Polly) with circuit breaker |
| Stored-procedure parameter drift (`@EmployeeId` optional via `SupportsEmployeeIdParameterAsync` schema probe) | Gerrie DB schema can change independently of this app | Code probes schema at runtime | Contract/versioning agreement with Gerrie DB owner; migrate probe to config flag |
| AI suggestion generation cost/latency | LLM call per distinct `RecognitionReason` hash | Cached by hash, reused across views | Monitor token costs; consider batching/model tiering |

## 5. Change Management

| Change type | Examples | Process |
|---|---|---|
| Standard | Admin user add/remove, notification group changes | Pre-approved, self-service in Admin Settings, no CAB |
| Normal | New feature, schema migration (`dotnet ef migrations`), controller/endpoint changes, infra changes | PR review → CI (`.github/workflows`: backend build/test, frontend build/test, DB migration validation) → staged deploy → approval → production deploy |
| Emergency | Security patch, P1 hotfix | Expedited review (minimum one reviewer), deploy immediately, retrospective review within 48h |

### Release pipeline (from repo CI/CD skeleton)
1. Backend build & test (`dotnet build`, `dotnet test`).
2. Frontend build & test (`npm run build`, `npm test`).
3. Database migration validation — DACPAC build + publish-profile dry run before touching on-prem SQL Server.
4. Deploy (Azure App Service/Container Apps or on-prem per `/infrastructure`).

### Change risk checklist specific to this app
- EF Core migrations are additive where possible (see `AddSubmissionSuggestions`, `AddNotificationDispatchQueue`, `AddCompanyNameToSubmission` — all additive). Maintain that pattern to avoid downtime.
- Any change to `SubmissionStatus` enum values must account for the deliberate `GerrieSynced` status bypass (`MarkGerrieSynced` currently leaves `Status=Approved`) — changing this affects UI/analytics filters.
- Changes to Entra app registration or Graph scopes require coordinated AAD change + redeploy (both frontend `environment.ts` and backend `appsettings.json`).

## 6. Access Management

| Role | Granted by | Scope |
|---|---|---|
| Employee (default) | Any authenticated Entra ID user in tenant | Submit recognitions, view own submissions |
| Admin Reviewer | Entry in `AdminUsers` table (`CanReview=true`, `IsActive=true`) OR Entra `Admin` app role | Review/approve/reject, admin settings, analytics |
| Notification recipient | `AdminUsers.ReceiveNotifications=true` | Receives new-submission emails (subset of admins) |
| Super admin / RBAC manager | Out-of-scope in current code — any active admin can manage other admins via Admin Settings UI | Add/remove/edit admin users |

De-provisioning: when an employee leaves or changes role, disable/remove their Entra account (removes portal access) and, if they were an admin, set `AdminUsers.IsActive=false` (soft delete via `DELETE /api/adminusers/{id}`) — cache epoch ensures near-immediate effect.

## 7. Availability & Capacity Management

- **Availability dependencies**: Entra ID, Microsoft Graph, SQL Server, Azure Blob Storage, SMTP, Gerrie SQL DB. Service health is only as good as the weakest dependency; Graph/Gerrie failures degrade gracefully (sync/email retried async) but authentication/DB failures are hard outages.
- **Capacity signals to monitor**: `NotificationDispatch` backlog size (pending rows + growing `AttemptCount`), `GerrieSyncEvent` failure rate, SQL Server connection pool saturation, Blob Storage throughput/quota, Graph API throttling (429 responses).
- **Degraded-mode behavior already built in**: notification and Gerrie sync are decoupled via background workers with exponential backoff (capped 30 min) — submission creation and admin review continue to function even if email/Gerrie are down.

## 8. Continuity & Backup

| Asset | Backup approach (verify against actual ops config) | Recovery note |
|---|---|---|
| SQL Server database (Submissions, AdminUsers, Reviews, Audit, Sync events) | Standard SQL Server backup/restore (on-prem) — confirm backup schedule with DBA team | Point-in-time restore; audit/review history is the authoritative record of what happened before restore |
| Azure Blob attachments | Azure Storage redundancy (LRS/ZRS/GRS per account config) | Attachments are referenced by blob name in SQL — a DB restore without matching blob state can leave dangling/missing references; coordinate restores |
| Configuration secrets (Entra, SMTP, Gerrie, Storage) | Should be in a secrets manager/Key Vault, not source control | Confirm `appsettings.*.json` secrets are externalized before go-live |

## 9. Known Gaps / Recommendations for ITIL Maturity

1. No manual "retry Gerrie sync" or "resend notification" admin action exists yet in the UI — currently relies entirely on the background worker's automatic retry. Recommended for faster incident resolution (README lists "manual retry capability" as a planned Gerrie feature, not yet implemented in controllers found).
2. No dedicated `/health` or `/ready` endpoint was found in the controllers scanned — confirm one exists in `Program.cs` for monitoring/alerting integration (IMPLEMENTATION_STATUS.md lists a HealthCheck endpoint as planned).
3. No explicit rate limiting/throttling middleware observed on write endpoints (`POST /api/submissions`) — consider for abuse prevention.
4. RBAC has no distinct "super admin" tier — any admin can add/remove any other admin. If separation of duties is required, add a tier.

## 10. Quick Reference — Where to Look

| Need | Source of truth |
|---|---|
| What happened to submission X | `AuditEvent` table filtered by `SubmissionId` |
| Why didn't an email send | `NotificationDispatch` table, `Status`, `LastError`, `AttemptCount` |
| Why didn't Gerrie sync | `GerrieSyncEvent` table, `SyncStatus`, `ResponseMessage` |
| Who can approve | `AdminUsers` table, `CanReview=true AND IsActive=true`, or Entra `Admin` role |
| Current submission state machine | `Portal.Domain.Enums.SubmissionStatus` + `Submission.MarkApproved/MarkRejected/MarkGerrieSynced/MarkGerrieSyncFailed` |
| Full technical process flows | `docs/PROCESS_FLOWS.md` |
