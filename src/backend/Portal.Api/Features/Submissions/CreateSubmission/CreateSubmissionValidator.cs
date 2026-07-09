using FluentValidation;
using Portal.Application.Features.Submissions.CreateSubmission;

namespace Portal.Application.Features.Submissions.CreateSubmission;

public class CreateSubmissionValidator : AbstractValidator<CreateSubmissionCommand>
{
    public CreateSubmissionValidator()
    {
        RuleFor(x => x.SubmissionType)
            .NotEmpty().WithMessage("Submission type is required.")
            .Must(x => x == "EmployeeToEmployee" || x == "EmployeeOnBehalfOfCustomer" || x == "CustomerToEmployee")
            .WithMessage("Submission type must be one of: EmployeeToEmployee, EmployeeOnBehalfOfCustomer, or CustomerToEmployee.");

        RuleFor(x => x.SubmittedByName)
            .NotEmpty().WithMessage("Submitter name is required.")
            .MaximumLength(100).WithMessage("Submitter name cannot exceed 100 characters.");

        RuleFor(x => x.SubmittedByEmail)
            .NotEmpty().WithMessage("Submitter email is required.")
            .EmailAddress().WithMessage("Submitter email must be a valid email address.")
            .MaximumLength(100).WithMessage("Submitter email cannot exceed 100 characters.");

        RuleFor(x => x.SubmittedByDepartment)
            .MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.SubmittedByDepartment))
            .WithMessage("Submitter department cannot exceed 100 characters.");

        RuleFor(x => x.NomineeName)
            .NotEmpty().WithMessage("Nominee name is required.")
            .MaximumLength(100).WithMessage("Nominee name cannot exceed 100 characters.");

        RuleFor(x => x.NomineeEmail)
            .NotEmpty().WithMessage("Nominee email is required.")
            .EmailAddress().WithMessage("Nominee email must be a valid email address.")
            .MaximumLength(100).WithMessage("Nominee email cannot exceed 100 characters.");

        RuleFor(x => x.NomineeDepartment)
            .MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.NomineeDepartment))
            .WithMessage("Nominee department cannot exceed 100 characters.");

        RuleFor(x => x.RecognitionReason)
            .NotEmpty().WithMessage("Recognition reason is required.")
            .MaximumLength(2000).WithMessage("Recognition reason cannot exceed 2000 characters.");

        RuleFor(x => x.ServiceRating)
            .InclusiveBetween(1, 5).When(x => x.ServiceRating.HasValue)
            .WithMessage("Service rating must be between 1 and 5 if provided.");

        RuleFor(x => x.CompanyName)
            .NotEmpty().When(x => x.SubmissionType == "CustomerToEmployee")
            .WithMessage("Company name is required for CustomerToEmployee submissions.")
            .MaximumLength(200).WithMessage("Company name cannot exceed 200 characters.");

        // Conditional validation: CustomerToEmployee requires customer info
        RuleFor(x => x.CustomerName)
            .NotEmpty().When(x => x.SubmissionType == "CustomerToEmployee")
            .WithMessage("Customer name is required for CustomerToEmployee submissions.")
            .MaximumLength(100).WithMessage("Customer name cannot exceed 100 characters.");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty().When(x => x.SubmissionType == "CustomerToEmployee")
            .WithMessage("Customer email is required for CustomerToEmployee submissions.")
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.CustomerEmail))
            .WithMessage("Customer email must be a valid email address.")
            .MaximumLength(100).WithMessage("Customer email cannot exceed 100 characters.");
    }
}
