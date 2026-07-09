# Customer First → Gerrie.NowV2 Integration: Implementation Complete

## Overview

Customer First Employee Driven now automatically publishes approved employee recognition submissions to the Gerrie.NowV2 homepage feed in near real-time (target ≤60 seconds). The integration uses a dedicated database table in Gerrie.NowDB to store external submissions and merges them into the homepage feed via existing stored procedures.

**Status**: ✅ Development Complete | ⏳ Deployment & Testing Pending

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ Customer First Employee Driven (ASP.NET Core 8)             │
├─────────────────────────────────────────────────────────────┤
│ Approval Flow:                                              │
│  1. Admin reviews submission                                │
│  2. Clicks "Approve"                                        │
│  3. ReviewSubmissionCommandHandler.Handle() queues sync     │
│     via IGerrieSyncService.QueueSubmissionSyncAsync()       │
│  4. GerrieSyncEvent created (Pending status)                │
│                                                             │
│ Background Processing (GerrieSyncWorkerService):            │
│  5. Worker polls GerrieSyncEvents every 30s                 │
│  6. Calls GerrieSyncService.ProcessPendingAsync()           │
│  7. SendToGerrieAsync() executes direct SQL to Gerrie DB    │
│                                                             │
│ Error Handling:                                             │
│  - Retries up to 5 times with exponential backoff           │
│  - Max delay 30 minutes between retries                     │
│  - Audit trail in AuditEvents table                         │
└─────────────────────────────────────────────────────────────┘
                           ↓ (SQL)
┌─────────────────────────────────────────────────────────────┐
│ Gerrie.NowDB (SQL Server - Shared On-Prem)                  │
├─────────────────────────────────────────────────────────────┤
│ spExternalEmployeeAnnouncements_Upsert:                      │
│  - Receives submission metadata via parameters              │
│  - Looks up employee by email (case-insensitive)            │
│  - Upserts into ExternalEmployeeAnnouncements table         │
│  - Idempotent by SubmissionId (UNIQUE key)                  │
│                                                             │
│ ExternalEmployeeAnnouncements table:                        │
│  - CustomerFirstSubmissionId (UNIQUE)                       │
│  - EmployeeId (nullable, from Employees lookup)             │
│  - Announcement (recognition reason)                        │
│  - IsActive, SyncStatus, Timestamps                         │
│                                                             │
│ Homepage Read Path (spEmployeeAnnouncements_ReadAllPage):   │
│  - Merges EmployeeAnnouncements + ExternalEmployeeAnns     │
│  - Filters external rows where IsActive=1                   │
│  - Orders combined feed by CreatedDate DESC                 │
│  - Returns EmployeeAnnouncementModel shape to UI            │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ Gerrie.NowV2 Homepage (Blazor UI)                           │
├─────────────────────────────────────────────────────────────┤
│ - No code changes required (backward compatible)            │
│ - Existing EmployeeAnnouncementList.razor works unchanged   │
│ - External items display with CreatedBy='CustomerFirst'     │
│ - Fallback for unmatched employees (EmployeeId=NULL)        │
└─────────────────────────────────────────────────────────────┘
```

---

## Files Modified / Created

### Gerrie.NowV2 Database Layer

| File | Status | Purpose |
|------|--------|---------|
| `Gerrie.NowDB/dbo/Tables/ExternalEmployeeAnnouncements.sql` | ✅ Created | External submissions storage with email-based employee mapping |
| `Gerrie.NowDB/dbo/StoredProcedures/spExternalEmployeeAnnouncements_Upsert.sql` | ✅ Created | Idempotent ingestion entry point |
| `Gerrie.NowDB/dbo/StoredProcedures/spEmployeeAnnouncements_ReadAllPage.sql` | ✅ Updated | UNION native + external announcements for homepage |
| `Gerrie.NowDB/dbo/StoredProcedures/spEmployeeAnnouncements_GetCount.sql` | ✅ Updated | Combined count for pagination |
| `Gerrie.NowDB/Gerrie.NowDB.sqlproj` | ✅ Updated | Added schema references |

### Customer First Backend

| File | Status | Purpose |
|------|--------|---------|
| `Portal.Api/Infrastructure/Integrations/GerrieSyncService.cs` | ✅ Modified | Replaced HTTP with SQL ingestion via stored procedure |
| `Portal.Api/appsettings.json` | ✅ Modified | Added `ConnectionStrings:GerrieDatabase` and `Gerrie:ConnectionString` |
| `Portal.Api/Features/Submissions/ReviewSubmission/ReviewSubmissionCommandHandler.cs` | ✅ Verified | Already queues sync after approval (no changes needed) |

### Build Status

✅ **Backend compiles successfully** (Portal.Api and Portal.Domain)

---

## Configuration Required

### 1. Connection String Setup
Update each environment's `appsettings.{Environment}.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "...",
  "GerrieDatabase": "Server=SQL5BUR.gerrie.com;Database=Gerrie.NowDB;User Id=<service_account>;Password=<password>;Encrypt=false;"
}
```

### 2. SQL Permissions
Service account requires:
- `SELECT` on `dbo.Employees` (for email lookup)
- `SELECT, INSERT, UPDATE` on `dbo.ExternalEmployeeAnnouncements`
- `EXECUTE` on `dbo.spExternalEmployeeAnnouncements_Upsert`

### 3. Feature Flags (Already Configured)
```json
"Gerrie": {
  "Sync": {
    "Enabled": true,
    "WorkerIntervalSeconds": 30,     // How often to poll for pending syncs
    "MaxAttempts": 5,                // Retry attempts
    "BaseDelaySeconds": 60,          // Initial backoff delay
    "BatchSize": 25                  // Submissions per poll cycle
  }
}
```

---

## Deployment Steps

### Phase 1: Deploy Gerrie.NowDB Schema (FIRST - Critical)
1. Deploy SQL schema changes to Gerrie.NowDB
2. Verify tables and stored procedures created:
   ```sql
   SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
   WHERE TABLE_NAME = 'ExternalEmployeeAnnouncements'
   ```

### Phase 2: Deploy Customer First Backend
1. Update `appsettings.Production.json` with GerrieDatabase connection string
2. Deploy backend code
3. Restart API to load new configuration

### Phase 3: Verify Integration
1. Submit and approve a test form
2. Monitor GerrieSyncWorkerService logs for sync execution
3. Verify row appears in Gerrie.ExternalEmployeeAnnouncements
4. Verify announcement appears on Gerrie homepage (≤60 seconds)

---

## Testing Scenarios

### Happy Path
1. Submit form in Customer First → Approve → Appears on Gerrie homepage within 60s ✅

### Edge Cases
- **Unmatched Email**: Nominee email not in Gerrie Employees table
  - Result: Syncs successfully, EmployeeId=NULL, renders on homepage
  
- **Retry Scenario**: Database temporarily unavailable during approval
  - Result: Sync queued with backoff, retries automatically
  
- **Idempotency**: Admin accidentally triggers approval twice
  - Result: Single row in ExternalEmployeeAnnouncements (UNIQUE constraint)

### Performance Target
- **Sync Latency**: ≤60 seconds from approval to homepage appearance
- **Worker Poll**: Every 30 seconds
- **Max Proc Execution**: <5 seconds for typical submissions

---

## Monitoring & Operations

### Success Indicators (From Logs)
- Customer First: `"Successfully ingested submission {id} to Gerrie external announcements table"`
- Gerrie Homepage: New announcement appears with employee photo/name

### Failure Indicators
- Customer First: `"Gerrie sync attempt {N} failed for submission {id}"`
- Gerrie DB: No row in ExternalEmployeeAnnouncements after 2 minutes
- Homepage: Old feed only (no external items)

### Metrics to Track
```sql
-- Success rate
SELECT 
  COUNT(CASE WHEN SyncStatus='Synced' THEN 1 END) as Successful,
  COUNT(CASE WHEN SyncStatus='Failed' THEN 1 END) as Failed,
  COUNT(*) as Total
FROM GerrieSyncEvents

-- Latency
SELECT 
  AVG(DATEDIFF(SECOND, s.ApprovedUtc, ea.LastSyncedUtc)) as AvgLatencySeconds
FROM ExternalEmployeeAnnouncements ea
JOIN GerrieSyncEvents gs ON ea.CustomerFirstSubmissionId = gs.SubmissionId
JOIN Submissions s ON gs.SubmissionId = s.SubmissionId
WHERE ea.SyncStatus='Synced'
```

---

## Rollback Plan

If critical issues occur:

1. **Stop syncs**: Set `Gerrie:Sync:Enabled=false` in config and restart API
2. **Clear test data**: `DELETE FROM dbo.ExternalEmployeeAnnouncements WHERE Source='CustomerFirst'`
3. **Revert DB changes**: Drop ExternalEmployeeAnnouncements table and revert stored procedures
4. **Revert code**: Redeploy previous API version

---

## Design Decisions & Rationale

| Decision | Rationale |
|----------|-----------|
| **Direct SQL over HTTP API** | Simpler, fewer dependencies, no Gerrie API needed, operates within DB tier |
| **UNION in stored procedures** | Minimal Gerrie.NowV2 app changes, existing component works unchanged |
| **Email-based employee lookup** | Reuses existing Gerrie employee data, eliminates need for employee ID mapping |
| **Separate ExternalEmployeeAnnouncements table** | Preserves existing EmployeeAnnouncements logic, avoids one-per-employee constraint |
| **Existing retry infrastructure** | Leverages GerrieSyncEvent pattern already in Customer First, no new dependencies |
| **Recognition reason only as content** | Simplest, most relevant content for homepage display |

---

## Future Enhancements

- Photo sync (if available in Customer First)
- Nominator name display on homepage
- Direct link back to submission details
- Bulk import of historical approvals
- Gerrie.NowV2 admin UI for ExternalEmployeeAnnouncements management

---

## Support & Troubleshooting

**Q: How do I check if sync is running?**
A: Look for "GerrieSyncWorkerService started" in Customer First API logs and verify entries in GerrieSyncEvents table with recent timestamps.

**Q: How long before an approved submission appears on the homepage?**
A: Target ≤60 seconds. Actual latency depends on: (1) sync worker poll cycle (30s), (2) stored procedure execution (<5s), (3) Gerrie homepage cache (if any).

**Q: What if the nominee's email doesn't match anyone in Gerrie?**
A: The row still syncs (EmployeeId=NULL) and displays on the homepage. Gerrie.NowV2 component handles NULL gracefully with a fallback display.

**Q: Can I manually retry a failed sync?**
A: Yes. Update the GerrieSyncEvent status from 'Failed' to 'Pending' with a future CreatedUtc, or call QueueSubmissionSyncAsync from a test endpoint if you create one.

**Q: How do I delete a synced submission from Gerrie?**
A: Set `IsActive=0` on the row in ExternalEmployeeAnnouncements. The homepage query filters for `IsActive=1` only.

---

## Contact & Questions

For integration issues, check:
1. Customer First API logs for "GerrieSyncService" or "GerrieSyncWorkerService" messages
2. Gerrie DB logs for stored procedure execution errors
3. Verify GerrieDatabase connection string is valid in appsettings
4. Confirm SQL service account has required permissions on Gerrie.NowDB

---

**Version**: 1.0  
**Date**: 2026-06-23  
**Status**: Ready for Deployment  
