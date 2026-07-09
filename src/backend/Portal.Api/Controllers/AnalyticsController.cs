using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Application.DTOs.Analytics;
using Portal.Application.Features.Analytics.GetAdminReviewActivity;
using Portal.Application.Features.Analytics.GetApprovalMetrics;
using Portal.Application.Features.Analytics.GetDashboardSummary;
using Portal.Application.Features.Analytics.GetRecognitionByNominee;
using Portal.Application.Features.Analytics.GetSubmissionsByStatus;
using Portal.Application.Features.Analytics.GetSubmissionTypeAnalysis;
using Portal.Application.Features.Analytics.GetNominationReport;

namespace Portal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get dashboard summary with overall KPIs
    /// </summary>
    [HttpGet("dashboard-summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get submission breakdown by status
    /// </summary>
    [HttpGet("submissions-by-status")]
    public async Task<ActionResult<SubmissionsByStatusDto>> GetSubmissionsByStatus(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSubmissionsByStatusQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get approval metrics and review statistics
    /// </summary>
    [HttpGet("approval-metrics")]
    public async Task<ActionResult<ApprovalMetricsDto>> GetApprovalMetrics(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetApprovalMetricsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get top nominees by recognition count
    /// </summary>
    [HttpGet("recognition-by-nominee")]
    public async Task<ActionResult<RecognitionByNomineeDto>> GetRecognitionByNominee([FromQuery] int topCount = 10, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetRecognitionByNomineeQuery { TopCount = topCount }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get submission analysis by type
    /// </summary>
    [HttpGet("submission-type-analysis")]
    public async Task<ActionResult<SubmissionTypeAnalysisDto>> GetSubmissionTypeAnalysis(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSubmissionTypeAnalysisQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get admin review activity and statistics
    /// </summary>
    [HttpGet("admin-review-activity")]
    public async Task<ActionResult<AdminReviewActivityDto>> GetAdminReviewActivity(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAdminReviewActivityQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get nomination reports with configurable date ranges and nomination type filters.
    /// Returns two leaderboards: most nominated employees and employees with most approved nominations.
    /// </summary>
    [HttpGet("nomination-report")]
    public async Task<ActionResult<NominationReportDto>> GetNominationReport(
        [FromQuery] ReportDateRangeType rangeType = ReportDateRangeType.CalendarMonth,
        [FromQuery] DateTime? referenceDate = null,
        [FromQuery] DateTime? customStartDate = null,
        [FromQuery] DateTime? customEndDate = null,
        [FromQuery] NominationTypeFilter typeFilter = NominationTypeFilter.All,
        [FromQuery] int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetNominationReportQuery
        {
            RangeType = rangeType,
            ReferenceDate = referenceDate,
            CustomStartDate = customStartDate,
            CustomEndDate = customEndDate,
            TypeFilter = typeFilter,
            TopCount = topCount
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
