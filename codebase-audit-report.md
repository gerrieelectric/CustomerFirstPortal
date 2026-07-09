# Codebase Audit Report

## Executive Summary

- **Overall assessment:** The application has a reasonable architectural skeleton (feature folders, MediatR, FluentValidation, policy-based authorization, parameterized SQL) but is **not production-safe in its current state**. Two findings alone justify stopping any production rollout: a **verified stored XSS chain** from Portal user input to the Gerrie homepage, and **live credentials committed to source control across both repositories**.
- **Main risk areas:** (1) Secrets management — passwords, client secrets, storage keys, and an OpenAI key are in git; (2) the Portal→Gerrie HTML pipeline; (3) concurrency — optimistic locking is configured but never enforced; (4) frontend resource lifecycle discipline (subscription/memory leaks); (5) essentially **zero test coverage** on a workflow-heavy application.
- **Most dangerous findings:** Stored XSS via `RecognitionReason` (verified line-by-line, see Critical Issue 1); committed secrets including an Entra client secret and Azure Storage account key (Critical Issue 2); missing `DbUpdateConcurrencyException` handling (Critical Issue 3); unencrypted SQL connections (Critical Issue 4).
- **Severity of current state:** High. The security posture problems are exploitable by any authenticated employee with no special skill (paste an `<img onerror>` into a recognition form and wait for admin approval). The maintainability problems are moderate; the reliability problems are latent and will surface under concurrency and scale.

## Audit Coverage

Reviewed the following areas:

- **Angular frontend** (`src/frontend/customer-first-portal`): core auth (MSAL config, `auth.service.ts`, `auth.interceptor.ts`, `admin.guard.ts`), submissions feature (employee-recognition, customer-recognition components), admin-review + admin-review-detail, admin-settings, analytics, environments, routing.
- **.NET backend** (`src/backend`): `Program.cs`, all `appsettings*.json` variants, `Controllers/`, `Features/` (CreateSubmission, ReviewSubmission, ListSubmissions, AdminUsers), `Security/` (`AdminOnlyHandler`, `AdminIdentityResolver`), `Infrastructure/Persistence` (configurations), `Infrastructure/Storage` (blob/SAS), `Infrastructure/Notifications` (`AdminSubmissionNotificationService`), `Infrastructure/Integrations` (`GerrieSyncService`), `Portal.Tests`.
- **Data layer:** EF Core entity configurations, Gerrie.NowDB schema (`EmployeeAnnouncements`, `ExternalEmployeeAnnouncements`, `spExternalEmployeeAnnouncements_Upsert`), `NowSupportLibrary/DataAccess/SqlDataAccess.cs` (Dapper).
- **Cross-cutting:** Gerrie Blazor `Program.cs`, `EmployeeAnnouncementList.razor`, `.gitignore`, pipeline YAML, `infrastructure/`, project docs.

Not fully inspected (requires runtime/deployment access):

- SQL Server execution plans and real index usage under load.
- Full Gerrie.NowV2 page inventory beyond integration path/hot surfaces.
- Complete `CustomerFirstPortal.Database` sqlproj internals beyond observed mappings.
- Full git history depth of secret exposure.

## Critical Issues (🔴)

### 1) Stored XSS: user-supplied recognition text rendered unencoded on Gerrie homepage

- **Title:** Stored XSS Through Portal→Gerrie Announcement Pipeline
- **Severity:** Critical
- **Area:** Cross-Cutting
- **Location:**
  - `src/backend/Portal.Api/Infrastructure/Integrations/GerrieSyncService.cs` (`FormatRecognitionAnnouncementHtml`)
  - `Gerrie.Now2.Blazor/Pages/EmployeeAnnouncements/EmployeeAnnouncementList.razor`
- **What is wrong:** `recognitionType` and `nomineeDisplay` are HTML-encoded, but `reason` (from `submission.RecognitionReason`) is interpolated raw into HTML.
- **Why it is a problem:** This value is user-controlled and then rendered by Gerrie using `MarkupString`, which executes unsafe HTML.
- **Real-world impact:** Any authenticated employee can inject script payloads that execute in other users’ browsers when loading Gerrie announcements.
- **How to fix it:**
  - Encode `reason` with `HttpUtility.HtmlEncode(reason)` before interpolation.
  - If formatting is needed, convert encoded newlines to `<br/>` safely.
  - Add defense in depth by sanitizing before `MarkupString` rendering in Gerrie.
  - Backfill sanitize existing rows in `ExternalEmployeeAnnouncements`.
- **Confidence level:** High

### 2) Credentials and secrets committed to source control

- **Title:** Committed Secrets Across Both Repositories
- **Severity:** Critical
- **Area:** Cross-Cutting
- **Location:**
  - `src/backend/Portal.Api/appsettings.json`
  - `src/backend/Portal.Api/appsettings.Production.json`
  - `src/backend/Portal.Api/appsettings.Local.json`
  - `src/backend/Portal.Api/appsettings.Development.json`
  - `src/backend/Portal.Api/artifacts/build-temp/*`
  - `Gerrie.Now2.Blazor/appsettings.json`
  - `Gerrie.Now2.Blazor/appsettings.Development.json`
- **What is wrong:** SQL passwords, SMTP credentials, Entra client secrets, storage account keys, and OpenAI keys are committed.
- **Why it is a problem:** Repo compromise immediately becomes environment compromise.
- **Real-world impact:** Database, mail, storage, identity, and AI resources can be hijacked.
- **How to fix it:**
  - Rotate all exposed secrets immediately.
  - Purge git history (BFG or `git filter-repo`).
  - Move secrets to Key Vault / environment variables / user-secrets.
  - Remove committed build artifacts carrying secret copies.
  - Fix `.gitignore` casing/coverage.
- **Confidence level:** High

### 3) Optimistic concurrency configured but not enforced

- **Title:** RowVersion Present, Conflict Handling Missing
- **Severity:** Critical
- **Area:** Backend / Database
- **Location:**
  - `src/backend/Portal.Api/Infrastructure/Persistence/Configurations/SubmissionConfiguration.cs`
  - `src/backend/Portal.Api/Features/Submissions/ReviewSubmission/ReviewSubmissionCommandHandler.cs`
- **What is wrong:** `RowVersion` is configured, but write paths do not catch `DbUpdateConcurrencyException` or return 409 conflicts.
- **Why it is a problem:** Concurrent admin actions can silently overwrite each other.
- **Real-world impact:** Lost updates, contradictory audit trails, unstable approval outcomes.
- **How to fix it:** Round-trip rowversion in DTOs/commands and handle conflicts with explicit `409 Conflict` responses.
- **Confidence level:** High

### 4) SQL transport configuration allows interception risk

- **Title:** Insecure SQL Connection Settings (`Encrypt=false` / `TrustServerCertificate=True`)
- **Severity:** Critical
- **Area:** Cross-Cutting
- **Location:** Connection strings in Portal and Gerrie appsettings files.
- **What is wrong:** SQL transport is either unencrypted or trust-relaxed.
- **Why it is a problem:** Credentials and data can be intercepted or MITM’d.
- **Real-world impact:** Confidentiality and integrity failures in transit.
- **How to fix it:** Enforce `TrustServerCertificate=False` with proper SQL server certificates.
- **Confidence level:** High

### 5) Gerrie announcement authorization guard is commented out

- **Title:** Disabled Role Gate on Announcement Surface
- **Severity:** Critical (pending role intent verification)
- **Area:** Cross-Cutting
- **Location:** `Gerrie.Now2.Blazor/Pages/EmployeeAnnouncements/EmployeeAnnouncementList.razor`
- **What is wrong:** `AuthorizeView Roles="ITAdmin, HRAdmin, AnnouncementAdmin"` is commented out.
- **Why it is a problem:** Privileged UI actions may become reachable to broader audiences.
- **Real-world impact:** Potential unauthorized viewing/management depending on server-side checks.
- **How to fix it:** Restore role checks and verify server-side authorization on write operations.
- **Confidence level:** Medium

## High Priority Issues (🟠)

### 6) Fire-and-forget background task for AI suggestions

- **Title:** Unmanaged Background Task in Submission Creation
- **Severity:** High
- **Area:** Backend
- **Location:** `src/backend/Portal.Api/Features/Submissions/CreateSubmission/CreateSubmissionCommandHandler.cs`
- **What is wrong:** Uses `Task.Run(..., CancellationToken.None)` without proper hosting/lifecycle integration.
- **Why it is a problem:** Exceptions can be lost and tasks outlive request scope.
- **Real-world impact:** Silent suggestion failures and unstable behavior during shutdown/restarts.
- **How to fix it:** Move to hosted background queue/service with cancellation and structured error handling.
- **Confidence level:** High

### 7) Frontend subscription and memory leaks in high-traffic components

- **Title:** Lifecycle Leaks in Admin and Submission Components
- **Severity:** High
- **Area:** Frontend
- **Location:**
  - `admin-review-detail.component.ts`
  - `employee-recognition.component.ts`
  - `customer-recognition.component.ts`
- **What is wrong:** Multiple raw subscriptions with missing `OnDestroy` cleanup; unbounded in-memory photo caching.
- **Why it is a problem:** Long-lived sessions accumulate leaked subscriptions/data.
- **Real-world impact:** UI slowdown, stale handlers firing, brittle behavior over time.
- **How to fix it:** Use `takeUntilDestroyed`/`DestroyRef`, clear caches on destroy, cap cache size.
- **Confidence level:** High

### 8) Non-atomic create + attachment upload leaves orphaned submissions

- **Title:** Create-Then-Upload Failure Path Not Recovered
- **Severity:** High
- **Area:** Cross-Cutting
- **Location:** `customer-recognition.component.ts` submit path.
- **What is wrong:** Submission can succeed while attachment upload fails; no guided retry workflow.
- **Why it is a problem:** Records enter review missing expected evidence.
- **Real-world impact:** Incomplete submissions, manual rework, review confusion.
- **How to fix it:** Add retry UX tied to submission id and/or server orchestration endpoint.
- **Confidence level:** High

### 9) Duplicate Authorization header risk in admin check call

- **Title:** Manual Bearer Header Conflicts with Auth Interceptor
- **Severity:** High
- **Area:** Frontend
- **Location:** `src/app/core/auth/auth.service.ts` (`isAdminAsync`)
- **What is wrong:** Method manually sets `Authorization` on an API call while interceptor also injects token.
- **Why it is a problem:** Duplicate headers can break through proxy/server combinations.
- **Real-world impact:** Intermittent admin detection failures.
- **How to fix it:** Remove manual `Authorization` header and rely on interceptor.
- **Confidence level:** High

### 10) Directory search endpoint can be expensive under load

- **Title:** Large Graph Pagination and Missing Timeout Guardrails
- **Severity:** High
- **Area:** Backend
- **Location:** `SubmissionsController` directory search flow.
- **What is wrong:** High page sizes / repeated paging with no strict timeout budget.
- **Why it is a problem:** Slow upstream calls can tie up server resources.
- **Real-world impact:** Latency spikes and reduced API responsiveness.
- **How to fix it:** Tighten result limits, enforce cancellation/timeout, short-circuit early.
- **Confidence level:** Medium

### 11) EF migrations executed at app startup

- **Title:** Startup Migration Strategy Risks Multi-Instance Deployments
- **Severity:** High
- **Area:** Backend / Deployment
- **Location:** `src/backend/Portal.Api/Program.cs`
- **What is wrong:** App startup includes `Database.MigrateAsync()`.
- **Why it is a problem:** Startup races and failed boots in scaled deployments.
- **Real-world impact:** Deployment instability and possible downtime.
- **How to fix it:** Move migration execution to CI/CD release step or dedicated migration job.
- **Confidence level:** High

### 12) Minimal test suite allows severe defects to ship

- **Title:** Near-Zero Automated Coverage
- **Severity:** High
- **Area:** Cross-Cutting
- **Location:** `src/backend/Portal.Tests/UnitTest1.cs`
- **What is wrong:** Placeholder-only tests with no real assertions.
- **Why it is a problem:** Security and correctness regressions are undetected.
- **Real-world impact:** High defect escape rate to production.
- **How to fix it:** Build focused unit/integration tests around auth, state transitions, sync, and encoding.
- **Confidence level:** High

## Medium Priority Issues (🟡)

### 13) Admin policy DB lookup per protected request

- **Title:** Authorization Path Performs Frequent DB Reads
- **Severity:** Medium
- **Area:** Backend
- **Location:** `AdminOnlyHandler` / `AdminIdentityResolver`
- **What is wrong:** Repeated DB checks without cache strategy.
- **Why it is a problem:** Adds avoidable latency and DB load.
- **Real-world impact:** Throughput degradation at scale.
- **How to fix it:** Add short-TTL cache with invalidation on admin updates.
- **Confidence level:** High

### 14) Missing centralized 401/403 handling in frontend HTTP pipeline

- **Title:** Expired Auth Flows Bubble as Component Errors
- **Severity:** Medium
- **Area:** Frontend
- **Location:** `auth.interceptor.ts`
- **What is wrong:** Interceptor attaches tokens but does not normalize auth failure behavior.
- **Why it is a problem:** Inconsistent UX and recovery handling.
- **Real-world impact:** Broken sessions requiring manual refresh/relogin.
- **How to fix it:** Add auth-failure branch in interceptor to trigger re-auth/logout workflow.
- **Confidence level:** High

### 15) `AdminUsers.Email` uniqueness enforced in app layer only

- **Title:** Missing DB-Level Unique Constraint on Email
- **Severity:** Medium
- **Area:** Database
- **Location:** Admin user model/configuration and validator.
- **What is wrong:** Validator checks duplicates but DB does not enforce uniqueness.
- **Why it is a problem:** Race conditions can bypass app checks.
- **Real-world impact:** Ambiguous admin identity resolution.
- **How to fix it:** Add unique index/constraint on normalized email.
- **Confidence level:** Medium

### 16) Correlation-id tracing promised but not consistently implemented

- **Title:** Diagnostics Gap Across Multi-Step Workflows
- **Severity:** Medium
- **Area:** Cross-Cutting
- **Location:** Logging/middleware path in API startup.
- **What is wrong:** No clear, consistent request correlation id propagation.
- **Why it is a problem:** Hard to trace failures across API + async sync + notifications.
- **Real-world impact:** Slower incident response and root cause analysis.
- **How to fix it:** Introduce correlation-id middleware and structured log enrichment.
- **Confidence level:** High

### 17) Ad-hoc timer/debounce patterns can outlive components

- **Title:** Timer Management Not Lifecycle-Bound
- **Severity:** Medium
- **Area:** Frontend
- **Location:** Admin review/search/retry message flows.
- **What is wrong:** `setTimeout`-based behavior without consistent destroy-time cleanup.
- **Why it is a problem:** Timer callbacks can execute after component destruction.
- **Real-world impact:** Flicker, stale updates, intermittent UI errors.
- **How to fix it:** Replace with RxJS timer/debounce streams tied to destroy lifecycle.
- **Confidence level:** High

### 18) Stale-data warning path partially disabled in admin review UX

- **Title:** Incomplete Frontend Guard for Concurrent Review State
- **Severity:** Medium
- **Area:** Frontend
- **Location:** `admin-review-detail.component.ts` stale-check section.
- **What is wrong:** Warning behavior appears partially commented/disabled.
- **Why it is a problem:** Users proceed with stale assumptions until submit conflict.
- **Real-world impact:** Lost editing effort and user frustration.
- **How to fix it:** Re-enable and align stale-state warning with backend conflict model.
- **Confidence level:** Medium

### 19) Attachment type controls rely heavily on extension allow-list

- **Title:** Content Validation Depth for Risky File Types
- **Severity:** Medium
- **Area:** Backend / Security
- **Location:** API attachment validation configuration.
- **What is wrong:** Allow-list includes complex email/document container formats.
- **Why it is a problem:** Extension checks alone are weak against disguised content.
- **Real-world impact:** Increased malware/social-engineering exposure for reviewers.
- **How to fix it:** Add magic-byte/content checks and reconsider risky file formats.
- **Confidence level:** Medium

### 20) Production configuration drift indicators

- **Title:** Environment Configuration Integrity Risk
- **Severity:** Medium
- **Area:** Deployment
- **Location:** `appsettings.Production.json`
- **What is wrong:** Signals of dev-targeted values and placeholders in production config.
- **Why it is a problem:** Promotes misdeployments and fragile releases.
- **Real-world impact:** Runtime failures and cross-environment confusion.
- **How to fix it:** Enforce config validation gates and environment-specific secret injection.
- **Confidence level:** High

### 21) Singleton-heavy service registration in Blazor app

- **Title:** Potential Cross-Session State Leakage Risk
- **Severity:** Medium
- **Area:** Cross-Cutting
- **Location:** `Gerrie.Now2.Blazor/Program.cs`
- **What is wrong:** Many data services are singleton; safe only if strictly stateless.
- **Why it is a problem:** Future stateful fields can leak data across users.
- **Real-world impact:** Privacy and correctness failures under future changes.
- **How to fix it:** Prefer scoped registrations unless statelessness is enforced/tested.
- **Confidence level:** Medium

## Low Priority Issues (🟢)

### 22) Hardcoded MSAL identity values in frontend config

- **Title:** Environment Agility Constraint in Auth Config
- **Severity:** Low
- **Area:** Frontend
- **Location:** `msal.config.ts`
- **What is wrong:** Client/authority values hardcoded rather than environment-driven.
- **Why it is a problem:** Increases deploy friction and config drift risk.
- **Real-world impact:** Slower environment changes and rollout errors.
- **How to fix it:** Move values to env config and validate at startup.
- **Confidence level:** High

### 23) `any` in error handling weakens type safety

- **Title:** Unstructured Error Typing in Critical Components
- **Severity:** Low
- **Area:** Frontend
- **Location:** Admin review detail error handlers.
- **What is wrong:** `any` used where typed error contracts should exist.
- **Why it is a problem:** Reduces compile-time safety and refactor confidence.
- **Real-world impact:** Increased chance of error-path regressions.
- **How to fix it:** Use `HttpErrorResponse`/typed error models.
- **Confidence level:** High

### 24) Dual-query pagination pattern (count + page)

- **Title:** Typical but Non-Optimal Paging Query Shape
- **Severity:** Low
- **Area:** Backend / Database
- **Location:** submissions list query handler.
- **What is wrong:** Separate count and data queries.
- **Why it is a problem:** Slightly higher DB cost per list request.
- **Real-world impact:** Minor overhead at current scale.
- **How to fix it:** Keep as-is unless profiling shows pressure.
- **Confidence level:** High

### 25) Optional suggestion service without explicit UX signal

- **Title:** Feature Availability Not Transparent to Users
- **Severity:** Low
- **Area:** Backend / UX
- **Location:** suggestion generation integration path.
- **What is wrong:** Service can be absent and behavior is subtle.
- **Why it is a problem:** Users cannot distinguish failure from intentional absence.
- **Real-world impact:** Confusing expectations around AI suggestion behavior.
- **How to fix it:** Return explicit capability/status in API or UI state.
- **Confidence level:** Medium

### 26) Validator-driven duplicate checks can be chatty under bursts

- **Title:** Validation Query Efficiency Opportunity
- **Severity:** Low
- **Area:** Backend
- **Location:** admin add validator path.
- **What is wrong:** Per-item duplicate checks require DB roundtrips.
- **Why it is a problem:** Avoidable overhead in bulk operations.
- **Real-world impact:** Limited unless batch usage grows.
- **How to fix it:** Batch pre-checks for bulk admin imports.
- **Confidence level:** Medium

### 27) Accessibility and large-list rendering polish gaps

- **Title:** Form Labeling and Grid Scalability Improvements Needed
- **Severity:** Low
- **Area:** Frontend
- **Location:** recognition forms and admin list/table templates.
- **What is wrong:** Some label associations and large-data rendering optimizations are incomplete.
- **Why it is a problem:** Accessibility friction and potential UI lag at higher volumes.
- **Real-world impact:** Reduced usability for assistive tech and heavy-admin scenarios.
- **How to fix it:** Complete label/aria linkage and evaluate virtualized rendering where needed.
- **Confidence level:** Medium

### 28) Documented infrastructure path not implemented in repo

- **Title:** Deployment Documentation Mismatch
- **Severity:** Low
- **Area:** Deployment / Maintainability
- **Location:** repo docs vs empty `infrastructure/` directory.
- **What is wrong:** Documentation references deployment scripts not present.
- **Why it is a problem:** Onboarding and release process inconsistency.
- **Real-world impact:** Manual deploy drift and fragile operations.
- **How to fix it:** Add IaC assets or update docs to current truth.
- **Confidence level:** High

## Security Risks

Concrete vulnerabilities:

1. Stored XSS via `RecognitionReason` flowing to Gerrie `MarkupString` rendering.
2. Committed secrets in appsettings files and build artifacts across both repos.
3. SQL transport with insecure encryption/trust settings.
4. Commented-out role gate on Gerrie announcement surface.

Probable vulnerabilities requiring targeted verification:

1. Server-side authorization depth on Gerrie write paths.
2. Risky attachment formats with extension-based checks only.
3. Historical secret exposure in git history/clones/forks.

## Performance Concerns

Frontend:

- Subscription leaks in admin/submission components increase memory over long sessions.
- Unbounded photo cache growth in recognition flows.
- Timer/debounce patterns not consistently lifecycle-bound.

Backend:

- Admin authorization DB checks per request without cache.
- Directory search path can become expensive under upstream latency.
- Fire-and-forget background work lacks managed execution guarantees.

Database/data:

- Concurrency control configured but not effectively used in write pipeline.
- Index adequacy for status/date filter combinations requires runtime validation.

## Architecture & Design Flaws

- **Tight Portal→Gerrie DB coupling:** Portal writes directly to Gerrie DB via stored proc and shared credentials.
- **Presentation concerns inside integration service:** announcement HTML styling assembled in backend integration logic.
- **Lifecycle pattern inconsistency in frontend:** some areas use robust cleanup patterns; high-traffic surfaces do not.
- **Docs-vs-implementation drift:** test strategy/infrastructure/correlation claims are stronger than current implementation.
- **Config sprawl:** multiple appsettings variants with sensitive and sometimes inconsistent values.

## UI / UX Issues

- Submission success with attachment failure lacks robust guided recovery.
- Auth expiry/error handling is inconsistent from user perspective.
- Stale-review warning path appears partially disabled.
- Accessibility labeling/assistive semantics need tightening on some forms.
- Positive: admin review error messages are relatively specific and actionable.

## Reliability / Stability Risks

- Concurrent admin review actions can overwrite state unexpectedly.
- Background suggestion tasks may fail silently.
- Startup migrations can destabilize scaled deployments.
- Lifecycle leaks degrade SPA stability over time.
- Upstream Graph slowness can amplify API response instability.

## Testing Gaps

Current backend tests are effectively placeholder-only.

Highest-value missing tests:

1. Encoding regression tests for `FormatRecognitionAnnouncementHtml`.
2. Authorization tests for `AdminOnlyHandler` and identity fallback logic.
3. Submission state-transition and concurrency conflict tests.
4. Integration tests for Gerrie sync idempotency and retry behavior.
5. Frontend lifecycle/error-path tests (subscription cleanup, auth failures, attachment failure recovery).
6. End-to-end flow: submit → notify → review → approve/reject → sync.

## Top 10 Highest-Value Fixes

1. Encode and sanitize `RecognitionReason` before storage/rendering.
2. Rotate all exposed secrets and purge git history.
3. Enforce secure SQL transport (`Encrypt=True`, certificate validation).
4. Implement backend concurrency conflict handling with rowversion and 409 responses.
5. Reinstate and verify Gerrie role-based authorization controls.
6. Replace unmanaged background tasks with hosted queue/service pattern.
7. Add lifecycle-bound subscription cleanup in high-traffic Angular components.
8. Remove duplicate token header behavior in `isAdminAsync`.
9. Add admin-identity lookup caching with proper invalidation.
10. Implement focused test suite for security, state transitions, and integration seams.

## Remediation Plan

### Phase 1 — Immediate fixes

Objectives:

- Remove exploitable security vulnerabilities and exposed credentials.
- Prevent data integrity loss in core review workflow.

Specific tasks:

- Fix XSS path (`RecognitionReason` encoding + Gerrie render defense).
- Rotate all leaked secrets and remove from git history.
- Correct `.gitignore` and remove committed secret-bearing artifacts.
- Enforce secure SQL connection encryption settings.
- Implement rowversion conflict handling with explicit 409 behavior.
- Restore/validate Gerrie authorization surface.

Expected benefit:

- Closes all known critical exploit paths and largest blast-radius risks.

Dependencies / prerequisites:

- Access to rotate identity/database/storage/mail keys.
- Coordination for data cleanup and controlled release window.

### Phase 2 — Stabilization

Objectives:

- Improve runtime stability and incident diagnosability.
- Reduce defect escape through targeted tests.

Specific tasks:

- Angular cleanup refactor (`takeUntilDestroyed`, cache/timer cleanup).
- Introduce centralized auth failure handling in HTTP interceptor.
- Replace fire-and-forget background work pattern.
- Add timeout/cancellation guardrails for directory search.
- Add DB uniqueness constraint for admin emails.
- Implement correlation-id middleware and structured logging propagation.
- Build focused unit/integration tests for auth, concurrency, sync, and encoding.

Expected benefit:

- More predictable behavior under load and easier production debugging.

Dependencies / prerequisites:

- Phase 1 merged to avoid baking insecure behavior into tests.

### Phase 3 — Structural improvements

Objectives:

- Reduce coupling and improve long-term maintainability/deployability.

Specific tasks:

- Move DB migrations out of app startup into deployment pipeline.
- Reduce direct DB coupling between Portal and Gerrie (or harden least privilege).
- Relocate presentation HTML templating concerns from integration layer.
- Implement robust attachment orchestration and cleanup strategies.
- Populate real IaC in `infrastructure/` and align docs to reality.
- Add broad e2e coverage of the full business workflow.

Expected benefit:

- Better change safety, independent release cadence, and lower operational risk.

Dependencies / prerequisites:

- Stable tests and observability from Phase 2.

## Suspected Issues Requiring Verification

1. Exact server-side authorization coverage for all Gerrie announcement write actions.
2. Actual DB indexing sufficiency for real admin-list query patterns under production volumes.
3. Behavior of duplicated Authorization headers across all deployed proxy/IIS paths.
4. Full depth and distribution of secret leakage in git history and downstream clones.
5. Whether existing external announcements already contain malicious payloads requiring cleanup.
6. End-to-end telemetry presence and trace continuity across API, async jobs, and integrations.
