using FluentEmail.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Domain.Entities;
using Portal.Domain.Enums;
using Portal.Application.Infrastructure.Persistence;
using System.Net;

namespace Portal.Application.Infrastructure.Notifications;

public class AdminSubmissionNotificationService : IAdminSubmissionNotificationService, INotificationDispatchProcessor
{
    private readonly PortalDbContext _dbContext;
    private readonly IFluentEmailFactory _emailFactory;
    private readonly ReviewLinkOptions _reviewLinkOptions;
    private readonly EmailOptions _emailOptions;
    private readonly NotificationDispatchOptions _notificationDispatchOptions;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<AdminSubmissionNotificationService> _logger;

    public AdminSubmissionNotificationService(
        PortalDbContext dbContext,
        IFluentEmailFactory emailFactory,
        IOptions<ReviewLinkOptions> reviewLinkOptions,
        IOptions<EmailOptions> emailOptions,
        IOptions<NotificationDispatchOptions> notificationDispatchOptions,
        IHostEnvironment hostEnvironment,
        ILogger<AdminSubmissionNotificationService> logger)
    {
        _dbContext = dbContext;
        _emailFactory = emailFactory;
        _reviewLinkOptions = reviewLinkOptions.Value;
        _emailOptions = emailOptions.Value;
        _notificationDispatchOptions = notificationDispatchOptions.Value;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task NotifySubmissionCreatedAsync(Submission submission, CancellationToken cancellationToken)
    {
        var recipients = await _dbContext.AdminUsers
            .AsNoTracking()
            .Where(a => a.IsActive && a.CanReview && a.ReceiveNotifications)
            .Select(a => new { a.Email, a.DisplayName })
            .ToListAsync(cancellationToken);

        if (recipients.Count == 0)
        {
            _logger.LogInformation(
                "No admin notification recipients configured for submission {SubmissionId}.",
                submission.SubmissionId);
            return;
        }

        var correlationId = Guid.NewGuid().ToString("N");
        var recipientList = recipients
            .Select(r => (Email: r.Email, DisplayName: r.DisplayName))
            .ToList();

        if (_hostEnvironment.IsDevelopment() && !string.IsNullOrWhiteSpace(_emailOptions.DevelopmentOverrideTo))
        {
            _logger.LogInformation(
                "Development environment detected. Routing submission notification emails to {OverrideEmail}.",
                _emailOptions.DevelopmentOverrideTo);

            recipientList =
            [
                (Email: _emailOptions.DevelopmentOverrideTo, DisplayName: "Development Inbox")
            ];
        }

        foreach (var recipient in recipientList)
        {
            var dispatch = new NotificationDispatch
            {
                NotificationDispatchId = Guid.NewGuid(),
                SubmissionId = submission.SubmissionId,
                NotificationType = NotificationDispatchType.SubmissionCreated,
                Status = NotificationDispatchStatus.Pending,
                RecipientEmail = recipient.Email,
                RecipientDisplayName = recipient.DisplayName,
                CorrelationId = correlationId,
                AttemptCount = 0,
                CreatedUtc = DateTime.UtcNow,
                NextAttemptUtc = DateTime.UtcNow
            };

            _dbContext.NotificationDispatches.Add(dispatch);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Queued {DispatchCount} notification dispatches for submission {SubmissionId}. CorrelationId={CorrelationId}",
            recipientList.Count,
            submission.SubmissionId,
            correlationId);

        // Best-effort immediate attempt; worker will continue retries if needed.
        await ProcessPendingAsync(cancellationToken);
    }

    public async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        if (!_notificationDispatchOptions.Enabled)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var pendingDispatches = await _dbContext.NotificationDispatches
            .Include(dispatch => dispatch.Submission)
            .Where(dispatch => dispatch.Status == NotificationDispatchStatus.Pending
                               && dispatch.NextAttemptUtc <= now)
            .OrderBy(dispatch => dispatch.NextAttemptUtc)
            .Take(Math.Max(1, _notificationDispatchOptions.BatchSize))
            .ToListAsync(cancellationToken);

        foreach (var dispatch in pendingDispatches)
        {
            await ProcessDispatchAsync(dispatch, cancellationToken);
        }

        if (pendingDispatches.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessDispatchAsync(NotificationDispatch dispatch, CancellationToken cancellationToken)
    {
        dispatch.AttemptCount += 1;
        dispatch.LastAttemptUtc = DateTime.UtcNow;

        var subject = BuildSubject(dispatch.Submission);
        var body = BuildBody(dispatch.Submission);

        _logger.LogInformation(
            "Sending notification dispatch {DispatchId} for submission {SubmissionId}, attempt {AttemptCount}. CorrelationId={CorrelationId}",
            dispatch.NotificationDispatchId,
            dispatch.SubmissionId,
            dispatch.AttemptCount,
            dispatch.CorrelationId ?? "(none)");

        try
        {
            var response = await _emailFactory
                .Create()
                .To(dispatch.RecipientEmail, dispatch.RecipientDisplayName)
                .Subject(subject)
                .Body(body, isHtml: true)
                .SendAsync(cancellationToken);

            if (response.Successful)
            {
                dispatch.Status = NotificationDispatchStatus.Succeeded;
                dispatch.SentUtc = DateTime.UtcNow;
                dispatch.LastError = null;

                _logger.LogInformation(
                    "Notification dispatch {DispatchId} succeeded for submission {SubmissionId}.",
                    dispatch.NotificationDispatchId,
                    dispatch.SubmissionId);

                return;
            }

            var errors = string.Join("; ", response.ErrorMessages);
            MarkDispatchFailure(dispatch, errors);
        }
        catch (Exception ex)
        {
            MarkDispatchFailure(dispatch, ex.Message, ex);
        }
    }

    private void MarkDispatchFailure(NotificationDispatch dispatch, string errorMessage, Exception? exception = null)
    {
        var maxAttempts = Math.Max(1, _notificationDispatchOptions.MaxAttempts);
        var truncatedError = errorMessage[..Math.Min(errorMessage.Length, 3900)];

        dispatch.LastError = truncatedError;

        if (dispatch.AttemptCount >= maxAttempts)
        {
            dispatch.Status = NotificationDispatchStatus.Failed;
            _logger.LogError(
                exception,
                "Notification dispatch {DispatchId} permanently failed after {AttemptCount} attempts. Submission={SubmissionId}. Error={Error}",
                dispatch.NotificationDispatchId,
                dispatch.AttemptCount,
                dispatch.SubmissionId,
                truncatedError);
            return;
        }

        var delaySeconds = CalculateBackoffSeconds(dispatch.AttemptCount);
        dispatch.Status = NotificationDispatchStatus.Pending;
        dispatch.NextAttemptUtc = DateTime.UtcNow.AddSeconds(delaySeconds);

        _logger.LogWarning(
            exception,
            "Notification dispatch {DispatchId} attempt {AttemptCount} failed. Submission={SubmissionId}. Retrying in {DelaySeconds}s. Error={Error}",
            dispatch.NotificationDispatchId,
            dispatch.AttemptCount,
            dispatch.SubmissionId,
            delaySeconds,
            truncatedError);
    }

    private int CalculateBackoffSeconds(int attemptCount)
    {
        var baseDelay = Math.Max(5, _notificationDispatchOptions.BaseDelaySeconds);
        var exponent = Math.Max(0, attemptCount - 1);
        var factor = Math.Pow(2, Math.Min(exponent, 5));
        var delay = (int)(baseDelay * factor);

        // cap at 15 minutes
        return Math.Min(delay, 900);
    }

    private static string BuildSubject(Submission submission)
    {
        return $"New submission awaiting review: {submission.NomineeName}";
    }

    private string BuildBody(Submission submission)
    {
        var reviewUrl = BuildReviewUrl(submission.SubmissionId);
        var submissionTypeLabel = FormatSubmissionType(submission.SubmissionType);
        var submittedDate = submission.SubmittedUtc.ToString("MMMM d, yyyy");

        var nomineeName = WebUtility.HtmlEncode(submission.NomineeName);
        var nomineeEmailEncoded = WebUtility.HtmlEncode(submission.NomineeEmail ?? string.Empty);
        var submittedByName = WebUtility.HtmlEncode(submission.SubmittedByName);
        var recognitionReason = WebUtility.HtmlEncode(submission.RecognitionReason ?? string.Empty)
            .Replace("\r\n", "<br>").Replace("\n", "<br>");

        var nomineeEmailLine = !string.IsNullOrWhiteSpace(submission.NomineeEmail)
            ? $"""<p style="margin:2px 0 0;color:#6b7280;font-size:13px;">{nomineeEmailEncoded}</p>"""
            : string.Empty;

        var customerMetaParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(submission.CompanyName))
        {
            customerMetaParts.Add($"Company: {WebUtility.HtmlEncode(submission.CompanyName)}");
        }

        if (!string.IsNullOrWhiteSpace(submission.CustomerName))
        {
            customerMetaParts.Add($"Contact: {WebUtility.HtmlEncode(submission.CustomerName)}");
        }

        if (!string.IsNullOrWhiteSpace(submission.CustomerEmail))
        {
            customerMetaParts.Add($"Contact Email: {WebUtility.HtmlEncode(submission.CustomerEmail)}");
        }

        var customerMeta = customerMetaParts.Count > 0
            ? $" &middot; {string.Join(" &middot; ", customerMetaParts)}"
            : string.Empty;

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f3f4f6;padding:32px 0;">
                <tr>
                  <td align="center">
                    <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background-color:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);">

                      <!-- Header -->
                      <tr>
                        <td style="background-color:#1e40af;padding:20px 32px;">
                          <p style="margin:0;color:#ffffff;font-size:16px;font-weight:bold;">Customer First Employee Driven &mdash; New Submission</p>
                        </td>
                      </tr>

                      <!-- Nominee -->
                      <tr>
                        <td style="padding:28px 32px 0;">
                          <p style="margin:0;color:#6b7280;font-size:11px;text-transform:uppercase;letter-spacing:0.8px;">Recognition for</p>
                          <p style="margin:6px 0 0;color:#111827;font-size:24px;font-weight:bold;line-height:1.2;">{nomineeName}</p>
                          {nomineeEmailLine}
                        </td>
                      </tr>

                      <!-- Recognition message -->
                      <tr>
                        <td style="padding:20px 32px 0;">
                          <div style="background-color:#f0fdf4;border-left:4px solid #16a34a;padding:14px 18px;border-radius:0 6px 6px 0;">
                            <p style="margin:0;color:#166534;font-size:14px;line-height:1.7;">{recognitionReason}</p>
                          </div>
                        </td>
                      </tr>

                      <!-- Meta + CTA -->
                      <tr>
                        <td style="padding:20px 32px 28px;">
                          <p style="margin:0 0 16px;color:#6b7280;font-size:12px;">Submitted by <strong style="color:#374151;">{submittedByName}</strong> &middot; {submissionTypeLabel}{customerMeta} &middot; {submittedDate}</p>
                          <a href="{reviewUrl}" style="display:inline-block;background-color:#1e40af;color:#ffffff;padding:11px 24px;border-radius:6px;text-decoration:none;font-size:14px;font-weight:bold;">Review Submission &rarr;</a>
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style="padding:14px 32px;background-color:#f9fafb;border-top:1px solid #e5e7eb;">
                          <p style="margin:0;color:#9ca3af;font-size:11px;">Automated notification from Customer First Employee Driven. Do not reply.</p>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string FormatSubmissionType(SubmissionType submissionType) => submissionType switch
    {
        SubmissionType.EmployeeToEmployee => "Employee to Employee",
        SubmissionType.EmployeeOnBehalfOfCustomer => "Employee on Behalf of Customer",
        SubmissionType.CustomerToEmployee => "Customer to Employee",
        _ => submissionType.ToString()
    };

    private string BuildReviewUrl(Guid submissionId)
    {
        var baseUrl = (_reviewLinkOptions.BaseUrl ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return $"/submissions-list/{submissionId}";
        }

        return $"{baseUrl}/submissions-list/{submissionId}";
    }
}
