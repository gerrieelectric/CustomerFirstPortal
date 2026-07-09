using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portal.Application.Infrastructure.Caching;
using Portal.Application.Infrastructure.Persistence;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Application.Infrastructure.Integrations;

public sealed class GerrieSyncService(
    PortalDbContext dbContext,
    IGraphPhotoService graphPhotoService,
    IConfiguration configuration,
    IOptions<GerrieSyncOptions> options,
    ILogger<GerrieSyncService> logger) : IGerrieSyncService
{
    private readonly PortalDbContext _dbContext = dbContext;
    private readonly IGraphPhotoService _graphPhotoService = graphPhotoService;
    private readonly IConfiguration _configuration = configuration;
    private readonly GerrieSyncOptions _options = options.Value;
    private readonly ILogger<GerrieSyncService> _logger = logger;

    public async Task SyncApprovedSubmissionAsync(
        Submission submission,
        string actorName,
        string? actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Gerrie sync is disabled; immediate sync skipped for submission {SubmissionId}", submission.SubmissionId);
            return;
        }

        try
        {
            _logger.LogInformation("Attempting immediate Gerrie sync for submission {SubmissionId}", submission.SubmissionId);

            var response = await SendToGerrieAsync(submission, cancellationToken);
            var externalRecordId = await TryExtractExternalRecordIdAsync(response, cancellationToken);

            submission.MarkGerrieSynced(
                !string.IsNullOrWhiteSpace(externalRecordId)
                    ? externalRecordId
                    : submission.SubmissionId.ToString());

            _dbContext.AuditEvents.Add(new AuditEvent
            {
                AuditEventId = Guid.NewGuid(),
                SubmissionId = submission.SubmissionId,
                ActorName = string.IsNullOrWhiteSpace(actorName) ? "System" : actorName,
                ActorEmail = actorEmail,
                EventType = AuditEventType.GerrieSyncSucceeded,
                EventDetail = "Immediate Gerrie sync succeeded during approval flow.",
                CreatedUtc = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Immediate Gerrie sync failed for submission {SubmissionId}; queueing for background retry.",
                submission.SubmissionId);

            await QueueSubmissionSyncAsync(submission, actorName, actorEmail, cancellationToken);
        }
    }

    public async Task QueueSubmissionSyncAsync(
        Submission submission,
        string actorName,
        string? actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Gerrie sync is disabled; queueing skipped for submission {SubmissionId}", submission.SubmissionId);
            return;
        }

        var hasPending = await _dbContext.GerrieSyncEvents
            .AnyAsync(syncEvent => syncEvent.SubmissionId == submission.SubmissionId
                                   && syncEvent.SyncStatus == GerrieSyncStatus.Pending,
                cancellationToken);

        if (hasPending)
        {
            _logger.LogInformation("Submission {SubmissionId} already has a pending Gerrie sync event.", submission.SubmissionId);
            return;
        }

        var queuedEvent = new GerrieSyncEvent
        {
            GerrieSyncEventId = Guid.NewGuid(),
            SubmissionId = submission.SubmissionId,
            AttemptNumber = 1,
            SyncStatus = GerrieSyncStatus.Pending,
            CreatedUtc = DateTime.UtcNow
        };

        var auditEvent = new AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            SubmissionId = submission.SubmissionId,
            ActorName = string.IsNullOrWhiteSpace(actorName) ? "System" : actorName,
            ActorEmail = actorEmail,
            EventType = AuditEventType.GerrieSyncQueued,
            EventDetail = $"Queued Gerrie sync event {queuedEvent.GerrieSyncEventId} (attempt {queuedEvent.AttemptNumber}).",
            CreatedUtc = DateTime.UtcNow
        };

        _dbContext.GerrieSyncEvents.Add(queuedEvent);
        _dbContext.AuditEvents.Add(auditEvent);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var pendingEvents = await _dbContext.GerrieSyncEvents
            .Where(syncEvent => syncEvent.SyncStatus == GerrieSyncStatus.Pending
                                && syncEvent.CreatedUtc <= now)
            .OrderBy(syncEvent => syncEvent.CreatedUtc)
            .Take(Math.Max(1, _options.BatchSize))
            .ToListAsync(cancellationToken);

        foreach (var syncEvent in pendingEvents)
        {
            await ProcessEventAsync(syncEvent, cancellationToken);
        }

        if (pendingEvents.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessEventAsync(GerrieSyncEvent syncEvent, CancellationToken cancellationToken)
    {
        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.SubmissionId == syncEvent.SubmissionId, cancellationToken);

        if (submission == null)
        {
            syncEvent.SyncStatus = GerrieSyncStatus.Failed;
            syncEvent.ResponseCode = "SubmissionMissing";
            syncEvent.ResponseMessage = "Submission was not found while processing sync event.";
            return;
        }

        _logger.LogInformation(
            "Processing Gerrie sync event {SyncEventId} for submission {SubmissionId}, attempt {Attempt}.",
            syncEvent.GerrieSyncEventId,
            syncEvent.SubmissionId,
            syncEvent.AttemptNumber);

        try
        {
            var response = await SendToGerrieAsync(submission, cancellationToken);

            syncEvent.SyncStatus = GerrieSyncStatus.Succeeded;
            syncEvent.ResponseCode = ((int)response.StatusCode).ToString();
            syncEvent.ResponseMessage = "Synced successfully.";

            var externalRecordId = await TryExtractExternalRecordIdAsync(response, cancellationToken);
            if (!string.IsNullOrWhiteSpace(externalRecordId))
            {
                syncEvent.ExternalRecordId = externalRecordId;
                submission.MarkGerrieSynced(externalRecordId);
            }
            else
            {
                submission.MarkGerrieSynced(submission.SubmissionId.ToString());
            }

            _dbContext.AuditEvents.Add(new AuditEvent
            {
                AuditEventId = Guid.NewGuid(),
                SubmissionId = submission.SubmissionId,
                ActorName = "System",
                ActorEmail = null,
                EventType = AuditEventType.GerrieSyncSucceeded,
                EventDetail = $"Gerrie sync succeeded. SyncEventId={syncEvent.GerrieSyncEventId}, Attempt={syncEvent.AttemptNumber}",
                CreatedUtc = DateTime.UtcNow
            });

            return;
        }
        catch (Exception ex)
        {
            var maxAttempts = Math.Max(1, _options.MaxAttempts);
            syncEvent.SyncStatus = GerrieSyncStatus.Failed;
            syncEvent.ResponseCode = "Exception";
            syncEvent.ResponseMessage = ex.Message[..Math.Min(ex.Message.Length, 3900)];

            if (syncEvent.AttemptNumber < maxAttempts)
            {
                var retryAttempt = syncEvent.AttemptNumber + 1;
                var delaySeconds = CalculateBackoffSeconds(syncEvent.AttemptNumber);

                _dbContext.GerrieSyncEvents.Add(new GerrieSyncEvent
                {
                    GerrieSyncEventId = Guid.NewGuid(),
                    SubmissionId = syncEvent.SubmissionId,
                    AttemptNumber = retryAttempt,
                    SyncStatus = GerrieSyncStatus.Pending,
                    ResponseCode = null,
                    ResponseMessage = null,
                    ExternalRecordId = null,
                    CreatedUtc = DateTime.UtcNow.AddSeconds(delaySeconds)
                });

                _dbContext.AuditEvents.Add(new AuditEvent
                {
                    AuditEventId = Guid.NewGuid(),
                    SubmissionId = submission.SubmissionId,
                    ActorName = "System",
                    ActorEmail = null,
                    EventType = AuditEventType.GerrieSyncQueued,
                    EventDetail =
                        $"Gerrie sync attempt {syncEvent.AttemptNumber} failed; retry attempt {retryAttempt} queued in {delaySeconds}s.",
                    CreatedUtc = DateTime.UtcNow
                });

                _logger.LogWarning(
                    ex,
                    "Gerrie sync attempt {Attempt} failed for submission {SubmissionId}. Retry {RetryAttempt} queued in {DelaySeconds}s.",
                    syncEvent.AttemptNumber,
                    submission.SubmissionId,
                    retryAttempt,
                    delaySeconds);

                return;
            }

            submission.MarkGerrieSyncFailed();
            _dbContext.AuditEvents.Add(new AuditEvent
            {
                AuditEventId = Guid.NewGuid(),
                SubmissionId = submission.SubmissionId,
                ActorName = "System",
                ActorEmail = null,
                EventType = AuditEventType.GerrieSyncFailed,
                EventDetail =
                    $"Gerrie sync permanently failed after {syncEvent.AttemptNumber} attempts. SyncEventId={syncEvent.GerrieSyncEventId}",
                CreatedUtc = DateTime.UtcNow
            });

            _logger.LogError(
                ex,
                "Gerrie sync permanently failed for submission {SubmissionId} after {Attempts} attempts.",
                submission.SubmissionId,
                syncEvent.AttemptNumber);
        }
    }

    private async Task<HttpResponseMessage> SendToGerrieAsync(Submission submission, CancellationToken cancellationToken)
    {
        // Use direct SQL ingestion to Gerrie.NowDB via stored procedure
        // This avoids need for HTTP API and keeps integration within DB tier
        try
        {
            var gerrieConnectionString = _configuration["Gerrie:ConnectionString"];
            if (string.IsNullOrWhiteSpace(gerrieConnectionString))
            {
                throw new InvalidOperationException("Gerrie:ConnectionString is not configured.");
            }

            using var connection = new Microsoft.Data.SqlClient.SqlConnection(gerrieConnectionString);
            await connection.OpenAsync(cancellationToken);

            var nomineeEmail = submission.NomineeEmail ?? submission.NomineeName;
            var nomineeName = submission.NomineeName;

            // Resolve Gerrie employee ID once in app code so the upsert can persist explicit linkage.
            var resolvedEmployeeId = await ResolveEmployeeIdAsync(connection, nomineeEmail, nomineeName, cancellationToken);
            var supportsEmployeeIdParameter = await SupportsEmployeeIdParameterAsync(connection, cancellationToken);

            if (!resolvedEmployeeId.HasValue)
            {
                _logger.LogWarning(
                    "Could not resolve Gerrie EmployeeId for submission {SubmissionId}. NomineeEmail={NomineeEmail}, NomineeName={NomineeName}",
                    submission.SubmissionId,
                    nomineeEmail,
                    nomineeName);
            }

            using var command = new Microsoft.Data.SqlClient.SqlCommand(
                "[dbo].[spExternalEmployeeAnnouncements_Upsert]",
                connection)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            // Map submission to stored procedure parameters
            command.Parameters.AddWithValue("@CustomerFirstSubmissionId", submission.SubmissionId);

            if (supportsEmployeeIdParameter)
            {
                var employeeIdParameter = command.Parameters.Add("@EmployeeId", System.Data.SqlDbType.Int);
                employeeIdParameter.Value = resolvedEmployeeId.HasValue ? resolvedEmployeeId.Value : DBNull.Value;
            }
            else
            {
                _logger.LogWarning(
                    "Gerrie stored procedure does not support @EmployeeId yet. Continuing sync without explicit EmployeeId for submission {SubmissionId}.",
                    submission.SubmissionId);
            }

            command.Parameters.AddWithValue("@NomineeEmail", nomineeEmail);

            // Format recognition reason as structured HTML before storing in Gerrie announcements table
            var formattedAnnouncement = FormatRecognitionAnnouncementHtml(submission);
            command.Parameters.AddWithValue("@RecognitionReason", formattedAnnouncement);

            command.Parameters.AddWithValue("@ApprovedUtc", submission.ApprovedUtc ?? DateTime.UtcNow);
            command.Parameters.AddWithValue("@Source", "CustomerFirst");

            // Fetch nominee photo via shared graph photo service (same path as p-select endpoint behavior)
            // and store as base64 so Gerrie.Now doesn't need Graph access.
            var photoBase64 = await TryFetchNomineePhotoBase64Async(nomineeEmail, cancellationToken);
            var photoParam = command.Parameters.Add("@EmployeePhotoBase64", System.Data.SqlDbType.VarChar, -1);
            photoParam.Value = photoBase64 != null ? (object)photoBase64 : DBNull.Value;

            // Execute the stored procedure and get result
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var id = reader["Id"]?.ToString() ?? submission.SubmissionId.ToString();

                _logger.LogInformation(
                    "Successfully ingested submission {SubmissionId} to Gerrie external announcements table. Gerrie record ID: {RecordId}, EmployeeId: {EmployeeId}",
                    submission.SubmissionId,
                    id,
                    resolvedEmployeeId);

                // Return a synthetic HttpResponseMessage to maintain compatibility with existing response handling
                var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { id = id }),
                        Encoding.UTF8,
                        "application/json")
                };
                return responseMessage;
            }

            throw new InvalidOperationException("Gerrie stored procedure did not return a result row.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest submission {SubmissionId} to Gerrie database", submission.SubmissionId);
            throw new HttpRequestException(
                $"Gerrie ingestion failed: {ex.Message[..Math.Min(ex.Message.Length, 500)]}",
                ex);
        }
    }

    private static async Task<bool> SupportsEmployeeIdParameterAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        CancellationToken cancellationToken)
    {
        using var command = new Microsoft.Data.SqlClient.SqlCommand(
            @"SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM sys.parameters p
                    WHERE p.object_id = OBJECT_ID(N'[dbo].[spExternalEmployeeAnnouncements_Upsert]')
                      AND p.name = N'@EmployeeId'
                ) THEN 1 ELSE 0 END",
            connection)
        {
            CommandType = System.Data.CommandType.Text,
            CommandTimeout = 30
        };

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is int intResult && intResult == 1;
    }

    private static async Task<int?> ResolveEmployeeIdAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        string nomineeEmail,
        string nomineeName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nomineeEmail) && string.IsNullOrWhiteSpace(nomineeName))
        {
            return null;
        }

        nomineeEmail ??= string.Empty;
        nomineeName = nomineeName?.Trim() ?? string.Empty;

        var emailLocalPart = nomineeEmail;
        if (!string.IsNullOrWhiteSpace(nomineeEmail))
        {
            var atIndex = nomineeEmail.IndexOf('@');
            if (atIndex > 0)
            {
                emailLocalPart = nomineeEmail[..atIndex];
            }
        }

        using var resolveCommand = new Microsoft.Data.SqlClient.SqlCommand(
            @"SELECT TOP 1 [Id]
              FROM [dbo].[Employees]
              WHERE LOWER(ISNULL([ForwardEmailTo], '')) = LOWER(@NomineeEmail)
                 OR LOWER(ISNULL([ExistingUser], '')) = LOWER(@NomineeEmail)
                 OR LOWER(ISNULL([ExistingUser], '')) = LOWER(@EmailLocalPart)
                 OR LOWER(ISNULL([CreatedBy], '')) = LOWER(@NomineeEmail)
                 OR (@NomineeName <> N'' AND LOWER(LTRIM(RTRIM(CONCAT(ISNULL([FirstName], ''), N' ', ISNULL([LastName], ''))))) = LOWER(@NomineeName))
                 OR (@NomineeName <> N'' AND LOWER(LTRIM(RTRIM(CONCAT(ISNULL([LastName], ''), N', ', ISNULL([FirstName], ''))))) = LOWER(@NomineeName))
              ORDER BY
                  CASE
                      WHEN LOWER(ISNULL([ForwardEmailTo], '')) = LOWER(@NomineeEmail) THEN 1
                      WHEN LOWER(ISNULL([ExistingUser], '')) = LOWER(@NomineeEmail) THEN 2
                      WHEN LOWER(ISNULL([ExistingUser], '')) = LOWER(@EmailLocalPart) THEN 3
                      WHEN LOWER(ISNULL([CreatedBy], '')) = LOWER(@NomineeEmail) THEN 4
                      WHEN @NomineeName <> N'' AND LOWER(LTRIM(RTRIM(CONCAT(ISNULL([FirstName], ''), N' ', ISNULL([LastName], ''))))) = LOWER(@NomineeName) THEN 5
                      WHEN @NomineeName <> N'' AND LOWER(LTRIM(RTRIM(CONCAT(ISNULL([LastName], ''), N', ', ISNULL([FirstName], ''))))) = LOWER(@NomineeName) THEN 6
                      ELSE 99
                  END,
                  [Id]",
            connection)
        {
            CommandType = System.Data.CommandType.Text,
            CommandTimeout = 30
        };

        resolveCommand.Parameters.AddWithValue("@NomineeEmail", nomineeEmail);
        resolveCommand.Parameters.AddWithValue("@EmailLocalPart", emailLocalPart);
        resolveCommand.Parameters.AddWithValue("@NomineeName", nomineeName);

        var result = await resolveCommand.ExecuteScalarAsync(cancellationToken);
        if (result is null || result == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt32(result);
    }

    private async Task<string?> TryFetchNomineePhotoBase64Async(string userEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return null;
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            var photoResult = await _graphPhotoService.GetUserPhotoAsync(userEmail, timeoutCts.Token);
            if (photoResult == null)
            {
                _logger.LogInformation("Photo service returned no image for {UserEmail}", userEmail);
                return null;
            }

            var (photoBytes, contentType) = photoResult.Value;
            if (photoBytes.Length == 0)
            {
                return null;
            }

            var mimeType = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType;
            var base64 = $"data:{mimeType};base64,{Convert.ToBase64String(photoBytes)}";
            _logger.LogInformation("Photo service fetched image for {UserEmail} ({Bytes} bytes)", userEmail, photoBytes.Length);
            return base64;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Photo service timed out for {UserEmail} after 5 seconds", userEmail);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Photo service failed for {UserEmail} — sync will continue without photo", userEmail);
            return null;
        }
    }

    private static async Task<string?> TryExtractExternalRecordIdAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("id", out var idElement))
            {
                return idElement.GetString();
            }

            if (root.TryGetProperty("recordId", out var recordIdElement))
            {
                return recordIdElement.GetString();
            }
        }
        catch
        {
            // Ignore parse failures and continue with fallback external ID.
        }

        return null;
    }

    private string FormatRecognitionAnnouncementHtml(Submission submission)
    {
        const string announcementFontStack = "'SF Pro Text', 'SF Pro Display', 'San Francisco', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif";

        // Determine recognition type based on submission type
        var recognitionType = submission.SubmissionType switch
        {
            SubmissionType.EmployeeToEmployee => "EMPLOYEE RECOGNITION",
            SubmissionType.EmployeeOnBehalfOfCustomer => "EMPLOYEE RECOGNITION",
            SubmissionType.CustomerToEmployee => "EMPLOYEE RECOGNITION",
            _ => "RECOGNITION"
        };

        var nomineeDisplay = !string.IsNullOrWhiteSpace(submission.NomineeName)
            ? submission.NomineeName
            : submission.NomineeEmail ?? "Team Member";

        var reason = submission.RecognitionReason ?? string.Empty;
        var customerContext = string.Empty;
        var isCustomerRecognition = submission.SubmissionType is SubmissionType.CustomerToEmployee or SubmissionType.EmployeeOnBehalfOfCustomer;
        if (isCustomerRecognition)
        {
            var contact = HttpUtility.HtmlEncode(submission.CustomerName ?? "a customer");
            if (!string.IsNullOrWhiteSpace(submission.CompanyName))
            {
                customerContext = $" by <strong>{contact}</strong> from <strong>{HttpUtility.HtmlEncode(submission.CompanyName)}</strong>";
            }
        }

        // Build HTML with styling from reference announcement pattern
        var html = $"""
                        <p align="center" style="text-align: center; margin: 0 0 8px 0;">
                            <span style="font-size: 20pt; font-family: {announcementFontStack}; color: #3366FF;">
                {HttpUtility.HtmlEncode(recognitionType)}
              </span>
            </p>
                                                <p style="font-family: {announcementFontStack}; font-size: 14pt; margin-top: 0;">
                            Congratulations to <strong>{HttpUtility.HtmlEncode(nomineeDisplay)}</strong> for being recognized{customerContext}!
            </p>
                        <p style="font-family: {announcementFontStack}; font-size: 14pt;">
              <br/>
            </p>
                        <p style="font-family: {announcementFontStack}; font-size: 14pt;">
              {reason}
            </p>
            """;

        return html;
    }

    private int CalculateBackoffSeconds(int attemptNumber)
    {
        var baseDelay = Math.Max(15, _options.BaseDelaySeconds);
        var factor = Math.Pow(2, Math.Min(5, Math.Max(0, attemptNumber - 1)));
        var delay = (int)(baseDelay * factor);

        // cap at 30 minutes
        return Math.Min(delay, 1800);
    }
}
