using FluentValidation;
using Portal.Application.Features.Submissions.UpdateSubmission;

namespace Portal.Application.Features.Submissions.UpdateSubmission;

public class UpdateSubmissionValidator : AbstractValidator<UpdateSubmissionCommand>
{
    public UpdateSubmissionValidator()
    {
        RuleFor(x => x.SubmissionId)
            .NotEmpty().WithMessage("Submission ID is required.");

        RuleFor(x => x.NomineeName)
            .MaximumLength(100).WithMessage("Nominee name cannot exceed 100 characters.")
            .NotEmpty().When(x => !string.IsNullOrEmpty(x.NomineeName))
            .WithMessage("Nominee name cannot be empty if provided.");

        RuleFor(x => x.NomineeEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.NomineeEmail))
            .WithMessage("Nominee email must be a valid email address.")
            .MaximumLength(100).WithMessage("Nominee email cannot exceed 100 characters.");

        RuleFor(x => x.RecognitionReason)
            .MaximumLength(2000).WithMessage("Recognition reason cannot exceed 2000 characters.")
            .NotEmpty().When(x => !string.IsNullOrEmpty(x.RecognitionReason))
            .WithMessage("Recognition reason cannot be empty if provided.");

        RuleFor(x => x.ServiceRating)
            .InclusiveBetween(1, 5).When(x => x.ServiceRating.HasValue)
            .WithMessage("Service rating must be between 1 and 5 if provided.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Company name cannot exceed 200 characters.");

        RuleFor(x => x.CustomerName)
            .MaximumLength(100).WithMessage("Customer name cannot exceed 100 characters.");

        RuleFor(x => x.CustomerEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.CustomerEmail))
            .WithMessage("Customer email must be a valid email address.")
            .MaximumLength(100).WithMessage("Customer email cannot exceed 100 characters.");
    }
}
