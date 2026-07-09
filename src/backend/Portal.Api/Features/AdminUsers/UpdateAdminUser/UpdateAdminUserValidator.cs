using FluentValidation;

namespace Portal.Application.Features.AdminUsers.UpdateAdminUser;

public class UpdateAdminUserValidator : AbstractValidator<UpdateAdminUserCommand>
{
    public UpdateAdminUserValidator()
    {
        RuleFor(x => x.AdminUserId)
            .GreaterThan(0).WithMessage("Admin user id must be greater than 0");

        RuleFor(x => x.Department)
            .MaximumLength(255).WithMessage("Department cannot exceed 255 characters");
    }
}
