using MediatR;
using Microsoft.EntityFrameworkCore;
using Portal.Application.DTOs.Submissions;
using Portal.Application.Infrastructure.Persistence;
using Portal.Domain.Enums;

namespace Portal.Application.Features.Submissions.ListSubmissions;

public class ListSubmissionsQueryHandler : IRequestHandler<ListSubmissionsQuery, PaginatedSubmissionsResponse>
{
    private readonly PortalDbContext _dbContext;

    public ListSubmissionsQueryHandler(PortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedSubmissionsResponse> Handle(ListSubmissionsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Submissions
            .AsNoTracking()
            .Where(s => s.Status != SubmissionStatus.GerrieSynced);

        // Filters
        if (!string.IsNullOrEmpty(request.Status))
        {
            query = query.Where(s => s.Status.ToString() == request.Status);
        }

        if (!string.IsNullOrEmpty(request.SubmissionType))
        {
            query = query.Where(s => s.SubmissionType.ToString() == request.SubmissionType);
        }

        if (request.SubmittedAfter.HasValue)
        {
            query = query.Where(s => s.SubmittedUtc >= request.SubmittedAfter.Value);
        }

        if (request.SubmittedBefore.HasValue)
        {
            query = query.Where(s => s.SubmittedUtc <= request.SubmittedBefore.Value);
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(s =>
                s.NomineeName.ToLower().Contains(searchLower) ||
                s.NomineeEmail!.ToLower().Contains(searchLower) ||
                (s.CompanyName != null && s.CompanyName.ToLower().Contains(searchLower)) ||
                (s.CustomerName != null && s.CustomerName.ToLower().Contains(searchLower)));
        }

        // Sorting
        query = request.SortBy switch
        {
            "NomineeName" => request.SortDescending
                ? query.OrderByDescending(s => s.NomineeName)
                : query.OrderBy(s => s.NomineeName),
            "Status" => request.SortDescending
                ? query.OrderByDescending(s => s.Status)
                : query.OrderBy(s => s.Status),
            _ => request.SortDescending
                ? query.OrderByDescending(s => s.SubmittedUtc)
                : query.OrderBy(s => s.SubmittedUtc)
        };

        // Pagination
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Increase command timeout to prevent cancellation on slow connections
        _dbContext.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubmissionListItemDto
            {
                SubmissionId = s.SubmissionId,
                SubmissionType = s.SubmissionType.ToString(),
                Status = s.Status.ToString(),
                SubmittedByName = s.SubmittedByName,
                SubmittedByEmail = s.SubmittedByEmail,
                CompanyName = s.CompanyName,
                NomineeName = s.NomineeName,
                RecognitionReason = s.RecognitionReason,
                SubmittedUtc = s.SubmittedUtc,
                ReviewedByAdminUserId = s.ReviewedByAdminUserId,
                GerrieRecordId = s.GerrieRecordId
            })
            .ToListAsync(cancellationToken);

        return new PaginatedSubmissionsResponse
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
