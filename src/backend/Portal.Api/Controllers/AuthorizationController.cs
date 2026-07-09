using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Portal.Api.Controllers;

[ApiController]
[Route("api/authorization")]
[Authorize]
public class AuthorizationController(IAuthorizationService authorizationService) : ControllerBase
{
    private readonly IAuthorizationService _authorizationService = authorizationService;

    [HttpGet("me/is-admin")]
    public async Task<ActionResult<bool>> IsCurrentUserAdmin()
    {
        var authorizationResult = await _authorizationService.AuthorizeAsync(User, "AdminOnly");
        return Ok(authorizationResult.Succeeded);
    }
}

