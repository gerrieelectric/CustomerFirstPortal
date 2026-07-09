using MediatR;
using Portal.Application.DTOs.Submissions;

namespace Portal.Application.Features.Submissions.ListSubmissions;

public class ListSubmissionsQuery : IRequest<PaginatedSubmissionsResponse>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string? Status { get; set; }
    public string? SubmissionType { get; set; }
    public DateTime? SubmittedAfter { get; set; }
    public DateTime? SubmittedBefore { get; set; }
    public string? SearchTerm { get; set; } // Search by nominee name or email
    public string? SortBy { get; set; } = "SubmittedUtc"; // SubmittedUtc, NomineeName, Status
    public bool SortDescending { get; set; } = true;
}
