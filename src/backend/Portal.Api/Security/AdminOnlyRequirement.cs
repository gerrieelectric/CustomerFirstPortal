using Microsoft.AspNetCore.Authorization;

namespace Portal.Api.Security;

public sealed class AdminOnlyRequirement : IAuthorizationRequirement
{
}
