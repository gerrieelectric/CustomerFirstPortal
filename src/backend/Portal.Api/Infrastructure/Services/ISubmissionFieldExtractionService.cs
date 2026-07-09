using Portal.Api.DTOs.Submissions;

namespace Portal.Application.Infrastructure.Services;

/// <summary>
/// A file provided for AI field extraction (content is buffered in memory).
/// </summary>
public sealed record ExtractionAttachment(string FileName, string ContentType, byte[] Content);

public interface ISubmissionFieldExtractionService
{
    /// <summary>
    /// Extracts customer-recognition form fields (company, contact, nominee, reason)
    /// from the supplied attachments using Azure OpenAI. Stateless - nothing is persisted.
    /// Returns a response with Status "Failed" (and null fields) if extraction fails.
    /// </summary>
    Task<ExtractedSubmissionFieldsResponse> ExtractFieldsAsync(
        IReadOnlyList<ExtractionAttachment> attachments,
        CancellationToken cancellationToken);
}
