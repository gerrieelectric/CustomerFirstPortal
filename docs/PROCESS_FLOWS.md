# Customer First Portal — Process Flow Documentation

Employee recognition platform: Angular 19 SPA + ASP.NET Core 10 API (Clean Architecture) + SQL Server, with Entra ID auth, Azure Blob attachments, AI-assisted text suggestions, email notifications, and one-way sync to the "Gerrie" announcements system.

## 1. System Context

```mermaid
flowchart LR
    U[Employee / Nominator] -->|browser| FE[Angular SPA]
    A[Admin Reviewer] -->|browser| FE
    FE -->|REST + Bearer token| API[Portal.Api]
    API <-->|OAuth2 / OIDC| ENTRA[Microsoft Entra ID]
    API -->|Graph API| GRAPH[Microsoft Graph<br/>users, photos]
    API -->|EF Core| SQL[(SQL Server<br/>Submissions DB)]
    API -->|Blob SDK| BLOB[(Azure Blob Storage<br/>Attachments)]
    API -->|SMTP| SMTP[Email / SMTP Relay]
    API -->|ADO.NET stored proc| GERRIE[(Gerrie DB<br/>spExternalEmployeeAnnouncements_Upsert)]
    API -->|LLM call| AI[Suggestion/Extraction AI Service]
```

## 2. Core Domain Lifecycle (Submission State Machine)

```mermaid
stateDiagram-v2
    [*] --> Submitted: Employee submits form
    Submitted --> Approved: Admin approves
    Submitted --> Rejected: Admin rejects
    Approved --> GerrieSyncFailed: Gerrie sync attempt fails
    GerrieSyncFailed --> Approved: Background retry succeeds
    Approved --> [*]: Synced / terminal
    Rejected --> [*]: Terminal
```

Notes:
- `Draft` and `GerrieSynced` exist in the enum but current code keeps synced records at `Approved` (`MarkGerrieSynced` intentionally does not set `GerrieSynced`, to avoid surfacing a legacy status in the UI).
- Only `Submitted` submissions `CanBeReviewed()`. Already-decided submissions cannot be re-reviewed (idempotent review endpoint).

## 3. Submission Creation Flow (Employee → Employee / Employee on behalf of Customer)

```mermaid
sequenceDiagram
    actor Emp as Employee
    participant FE as Angular SPA
    participant API as SubmissionsController
    participant Graph as MS Graph
    participant DB as SQL Server
    participant Queue as Notification Queue
    participant SMTP as Email

    Emp->>FE: Fill recognition form (nominee, reason, rating, attachments)
    FE->>API: GET /api/submissions/directory-users (nominee picker)
    API->>Graph: GET /users (delegated token)
    Graph-->>API: Directory list
    API-->>FE: DirectoryUserDto[]
    FE->>API: POST /api/submissions (CreateSubmissionRequest)
    API->>Graph: GET /me (resolve submitter identity)
    Graph-->>API: displayName, mail, department, oid
    API->>DB: INSERT Submission (Status=Submitted)
    API->>Queue: Enqueue NotificationDispatch (SubmissionCreated)
    API-->>FE: 201 Created + SubmissionResponse
    opt Attachments
        FE->>API: POST /api/submissions/{id}/attachments
        API->>BLOB: Upload file
        API->>DB: INSERT SubmissionAttachment
    end
    Note over Queue,SMTP: Background worker (NotificationDispatchWorkerService)
    Queue->>SMTP: Send "new submission" email to admin notification group
```

## 4. Admin Review & Decision Flow

```mermaid
sequenceDiagram
    actor Admin
    participant FE as Angular SPA
    participant API as SubmissionsController
    participant AI as Suggestion Service
    participant DB as SQL Server
    participant Gerrie as GerrieSyncService

    Admin->>FE: Opens Admin Review list / email deep link
    FE->>API: GET /api/submissions?status=Submitted (filter/sort/page)
    API->>DB: Query submissions
    API-->>FE: PaginatedSubmissionsResponse
    Admin->>FE: Opens a submission detail
    FE->>API: GET /api/submissions/{id}
    FE->>API: GET /api/submissions/{id}/suggested-edits
    API->>AI: Generate/reuse cached suggestions (hash of RecognitionReason)
    AI-->>API: Suggested edits + full edited text
    API-->>FE: SuggestedEditsResponse
    Admin->>FE: Accept/edit text, choose Approve or Reject
    alt Approve
        FE->>API: POST /api/submissions/{id}/review {Decision=Approved}
        API->>DB: Update Status=Approved, ApprovedUtc, ReviewedByAdminUserId
        API->>DB: INSERT SubmissionReview, AuditEvent(Approved)
        API-->>FE: 200 OK
        Note over Gerrie: Picked up by background GerrieSyncWorkerService
        Gerrie->>DB: Poll Approved + not yet synced
        Gerrie->>Gerrie: Call stored procedure (employee match, photo fetch, HTML format)
        alt Sync success
            Gerrie->>DB: MarkGerrieSynced(GerrieRecordId), INSERT GerrieSyncEvent(Succeeded)
        else Sync failure
            Gerrie->>DB: MarkGerrieSyncFailed(), INSERT GerrieSyncEvent(Failed), retry w/ backoff
        end
    else Reject
        FE->>API: POST /api/submissions/{id}/review {Decision=Rejected, ReviewNotes}
        API->>DB: Update Status=Rejected, RejectedUtc
        API->>DB: INSERT SubmissionReview, AuditEvent(Rejected)
        API-->>FE: 200 OK
    end
```

### Review guardrails
- `[Authorize(Policy = "AdminOnly")]` — enforced by `AdminOnlyHandler`, which checks the `Admin` role claim OR an active `AdminUser` row (by Entra Object ID, then email), cached per-request via `IAdminIdentityResolver` + `IMemoryCache` with a cache epoch for instant invalidation on admin-list changes.
- Submission can only be reviewed while `Status == Submitted` (re-review attempts are rejected/no-op).
- `UpdateSubmission` (`PATCH /api/submissions/{id}`) lets an admin edit fields pre-decision; uses optimistic concurrency (`RowVersion`).

## 5. Attachment Handling

```mermaid
flowchart TD
    A[POST /api/submissions/id/attachments] --> B{Validate file type/size}
    B -->|invalid| R1[400 Bad Request]
    B -->|valid| C[Upload to Azure Blob Storage]
    C --> D[Insert SubmissionAttachment row]
    D --> E[AuditEvent: AttachmentAdded]
    F[GET .../attachments/id/download] --> G[Stream blob via IAttachmentStorageService]
    H[DELETE .../attachments/id] --> I[Remove blob + row]
    I --> J[AuditEvent: AttachmentDeleted]
```

## 6. AI-Assisted Suggestion & Field Extraction Flow

```mermaid
flowchart TD
    S[Admin opens submission for review] --> H{Suggestion row exists<br/>with matching ReasonHash?}
    H -->|Yes, Status=Success| C[Return cached SubmissionSuggestion]
    H -->|No / Stale / Failed| G[Call SuggestionGenerationService]
    G --> M[LLM generates spelling/grammar/name/format edits]
    M --> P[Persist SubmissionSuggestion: OriginalReason, FullEditedReason, SuggestionsJson, ModelName, PromptVersion]
    P --> C
    C --> D[Frontend renders diff view: suggestion-diff-display]
    D --> R[Admin accepts all / some / none]
    X[POST /extract-fields] --> Y[SubmissionFieldExtractionService parses free text into structured fields]
    Y --> Z[Pre-fills nominee/customer/reason fields in form]
```

Suggestions are generated once per distinct `RecognitionReason` (via content hash) and reused on every subsequent view to control AI cost; `POST /{id}/regenerate-suggestions` forces a fresh pass.

## 7. Notification Dispatch Flow (background worker)

```mermaid
flowchart LR
    Create[Submission created] --> Enqueue[INSERT NotificationDispatch<br/>Status=Pending, NextAttemptUtc=now]
    Worker[NotificationDispatchWorkerService<br/>polls every N seconds] --> Pick{Due & Pending rows?}
    Pick -->|yes| Send[Send email via SMTP]
    Send -->|success| Done[Status=Succeeded, SentUtc set]
    Send -->|failure| Retry[Status=Failed, AttemptCount++, LastError set,<br/>NextAttemptUtc = backoff]
    Retry --> Worker
```

## 8. Gerrie Sync Flow (background worker, one-way outbound)

```mermaid
flowchart LR
    Approve[Submission Approved] --> Poll[GerrieSyncWorkerService polls Approved & unsynced]
    Poll --> Match[Resolve EmployeeId by email/name match in Gerrie Employees table]
    Match --> Photo[Fetch nominee photo via Graph, base64-encode]
    Photo --> Format[Format recognition HTML announcement]
    Format --> Exec[Execute spExternalEmployeeAnnouncements_Upsert]
    Exec -->|OK| Success[MarkGerrieSynced + GerrieSyncEvent:Succeeded]
    Exec -->|Exception| Fail[MarkGerrieSyncFailed + GerrieSyncEvent:Failed<br/>attempt++, exponential backoff capped at 30 min]
    Fail --> Poll
```

## 9. Admin User Management (RBAC) Flow

```mermaid
sequenceDiagram
    actor SuperAdmin
    participant FE
    participant API as AdminUsersController
    participant Graph as MS Graph
    participant DB

    SuperAdmin->>FE: Admin Settings page
    FE->>API: GET /api/adminusers/directory-search?query=
    API->>Graph: Search directory users
    SuperAdmin->>FE: Select user, toggle CanReview/ReceiveNotifications
    FE->>API: POST /api/adminusers (AddAdminUserCommand)
    API->>DB: INSERT AdminUser
    API->>DB: Bump AdminIdentityCacheEpoch (invalidate cache)
    SuperAdmin->>FE: Edit / deactivate
    FE->>API: PUT /api/adminusers/{id} or DELETE
    API->>DB: UPDATE / soft-delete (IsActive=false)
```

## 10. Analytics Flow

```mermaid
flowchart LR
    Dash[Analytics page] --> Q1[GET /dashboard-summary]
    Dash --> Q2[GET /submissions-by-status]
    Dash --> Q3[GET /approval-metrics]
    Dash --> Q4[GET /recognition-by-nominee]
    Dash --> Q5[GET /submission-type-analysis]
    Dash --> Q6[GET /admin-review-activity]
    Dash --> Q7[GET /nomination-report?dateRangeType=...&type=...]
    Q1 & Q2 & Q3 & Q4 & Q5 & Q6 & Q7 --> DB[(Read queries over Submissions/Reviews)]
```
All analytics endpoints are `AdminOnly` and read-only (CQRS queries, no handlers mutate state).

## 11. Frontend Route Map

| Route | Component | Guard |
|---|---|---|
| `/submissions` | SubmissionsListComponent | authGuard |
| `/submissions/employee` | EmployeeRecognitionComponent (submit form) | authGuard |
| `/submissions/customer` | CustomerRecognitionComponent (submit form) | authGuard |
| `/admin-review` | AdminReviewComponent (queue) | adminGuard |
| `/admin-review/:submissionId` | AdminReviewDetailComponent | adminGuard |
| `/admin-settings` | AdminSettingsComponent (RBAC) | adminGuard |
| `/analytics` | AnalyticsComponent | adminGuard |

`adminMatchGuard` hides admin routes from the router config entirely for non-admins (lazy route protection in addition to `canActivate`).

## 12. End-to-End Happy Path (Summary)

1. Employee signs in via Entra ID → SPA obtains token.
2. Employee submits recognition (`POST /api/submissions`) → `Submitted`, notification queued.
3. Background worker emails the admin notification group with a secure review link.
4. Admin opens review queue/detail, optionally applies AI suggestions, approves or rejects.
5. Approve → audit logged → background worker syncs to Gerrie (retried with backoff on failure).
6. Reject → audit logged → terminal, no sync.
7. Admin analytics dashboard reports on volumes/approvals/top nominees at any time.

## 13. Key Cross-Cutting Mechanisms

- **CQRS**: MediatR commands/queries per feature (`Features/<Area>/<Action>`), validated with FluentValidation (`FluentValidationBehavior`).
- **Correlation**: `CorrelationIdMiddleware` tags every request/log for traceability across the async workers.
- **Optimistic concurrency**: `RowVersion` on `Submission` and `SubmissionSuggestion` guards concurrent admin edits.
- **Caching**: Admin identity cache (memory cache + epoch invalidation) and Graph profile-photo distributed cache (`DistributedPhotoCache`, 7-day response cache on the photo endpoint).
