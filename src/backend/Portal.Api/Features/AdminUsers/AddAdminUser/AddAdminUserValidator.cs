using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Portal.Application.Infrastructure.Persistence;

namespace Portal.Application.Features.AdminUsers.AddAdminUser;

public class AddAdminUserValidator : AbstractValidator<AddAdminUserCommand>
{
    private readonly PortalDbContext _dbContext;

    public AddAdminUserValidator(PortalDbContext dbContext)
    {
        _dbContext = dbContext;

        RuleFor(x => x.EntraObjectId)
            .NotEmpty().WithMessage("Entra Object ID is required")
            .MustAsync(async (id, ct) => !await _dbContext.AdminUsers.AsQueryable().AnyAsync(a => a.EntraObjectId == id, ct))
            .WithMessage("This user is already an admin");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .MustAsync(async (email, ct) => !await _dbContext.AdminUsers.AsQueryable().AnyAsync(a => a.Email == email, ct))
            .WithMessage("This email is already registered as an admin");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(255).WithMessage("Display name cannot exceed 255 characters");

        RuleFor(x => x.Department)
            .MaximumLength(255).WithMessage("Department cannot exceed 255 characters");

        RuleFor(x => x.CanReview)
            .NotNull().WithMessage("CanReview is required");

        RuleFor(x => x.ReceiveNotifications)
            .NotNull().WithMessage("ReceiveNotifications is required");
    }
}
