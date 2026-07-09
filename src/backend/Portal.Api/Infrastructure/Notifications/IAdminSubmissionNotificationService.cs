using Portal.Domain.Entities;

namespace Portal.Application.Infrastructure.Notifications;

public interface IAdminSubmissionNotificationService
{
    Task NotifySubmissionCreatedAsync(Submission submission, CancellationToken cancellationToken);
}
