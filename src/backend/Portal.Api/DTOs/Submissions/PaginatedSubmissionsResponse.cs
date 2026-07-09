namespace Portal.Application.DTOs.Submissions;

public class PaginatedSubmissionsResponse
{
    public List<SubmissionListItemDto> Items { get; set; } = [];

    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
