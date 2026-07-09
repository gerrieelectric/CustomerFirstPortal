using FluentValidation;
using Portal.Application.Features.Submissions.ReviewSubmission;

namespace Portal.Application.Features.Submissions.ReviewSubmission;

public class ReviewSubmissionValidator : AbstractValidator<ReviewSubmissionCommand>
{
    public ReviewSubmissionValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty().WithMessage("Submission ID is required.");

        RuleFor(x => x.ReviewDecision)
            .NotEmpty().WithMessage("Review decision is required.")
            .Must(x => x == "Approved" || x == "Rejected")
            .WithMessage("Review decision must be one of: Approved or Rejected.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");

        RuleFor(x => x.EditedRecognitionReason)
            .MaximumLength(2000).WithMessage("Recognition reason cannot exceed 2000 characters.")
            .NotEmpty().When(x => x.ReviewDecision == "Approved")
            .WithMessage("Recognition reason is required when approving a submission.");

        RuleFor(x => x.AdminUserId)
            .GreaterThan(0).WithMessage("Admin user ID is invalid.");

        RuleFor(x => x.AdminName)
            .NotEmpty().WithMessage("Admin name is required.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required.")
            .EmailAddress().WithMessage("Admin email must be a valid email address.");
    }
}
