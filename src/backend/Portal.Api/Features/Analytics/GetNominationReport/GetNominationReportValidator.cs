using FluentValidation;

namespace Portal.Application.Features.Analytics.GetNominationReport;

public class GetNominationReportValidator : AbstractValidator<GetNominationReportQuery>
{
    public GetNominationReportValidator()
    {
        RuleFor(x => x.RangeType)
            .IsInEnum().WithMessage("Invalid range type.");

        RuleFor(x => x.TypeFilter)
            .IsInEnum().WithMessage("Invalid nomination type filter.");

        RuleFor(x => x.TopCount)
            .InclusiveBetween(1, 100).WithMessage("Top count must be between 1 and 100.");

        When(x => x.RangeType == ReportDateRangeType.Custom, () =>
        {
            RuleFor(x => x.CustomStartDate)
                .NotNull().WithMessage("Start date is required for custom range.");

            RuleFor(x => x.CustomEndDate)
                .NotNull().WithMessage("End date is required for custom range.");

            RuleFor(x => x)
                .Must(x => x.CustomStartDate == null || x.CustomEndDate == null || x.CustomStartDate <= x.CustomEndDate)
                .WithMessage("Start date must be on or before end date.")
                .When(x => x.CustomStartDate.HasValue && x.CustomEndDate.HasValue);
        });
    }
}
