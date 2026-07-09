using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Portal.Api.Security;
using Portal.Application.Features.AdminUsers;
using Portal.Application.Features.AdminUsers.AddAdminUser;
using Portal.Application.Features.AdminUsers.ListAdminUsers;
using Portal.Application.Features.AdminUsers.RemoveAdminUser;
using Portal.Application.Features.AdminUsers.UpdateAdminUser;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Portal.Api.Controllers;

[ApiController]
[Route("api/admin-users")]
[Authorize(Policy = "AdminOnly")]
public class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IAdminIdentityCacheEpoch _adminIdentityCacheEpoch;

    public AdminUsersController(
        IMediator mediator,
        ITokenAcquisition tokenAcquisition,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IAdminIdentityCacheEpoch adminIdentityCacheEpoch)
    {
        _mediator = mediator;
        _tokenAcquisition = tokenAcquisition;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _adminIdentityCacheEpoch = adminIdentityCacheEpoch;
    }

    /// <summary>
    /// Search users in Microsoft Graph for admin selection
    /// </summary>
    [HttpGet("directory-search")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<List<DirectoryUserDto>>> SearchDirectoryUsers(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return Ok(new List<DirectoryUserDto>());
        }

        try
        {
            var graphTimeoutSeconds = Math.Max(5, _configuration.GetValue<int?>("Graph:RequestTimeoutSeconds") ?? 10);
            var scopes = (_configuration["Graph:Scopes"] ?? "User.Read User.ReadBasic.All")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var baseUrl = _configuration["Graph:BaseUrl"] ?? "https://graph.microsoft.com/v1.0";

            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes, user: User);

            var escapedQuery = query.Trim().Replace("'", "''");
            var requestUrl =
                $"{baseUrl}/users?$top=15&$select=id,displayName,mail,userPrincipalName,department,givenName,surname&$filter=accountEnabled eq true and (startsWith(displayName,'{escapedQuery}') or startsWith(mail,'{escapedQuery}') or startsWith(userPrincipalName,'{escapedQuery}') or startsWith(givenName,'{escapedQuery}') or startsWith(surname,'{escapedQuery}'))";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var client = _httpClientFactory.CreateClient();
            using var graphTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            graphTimeoutCts.CancelAfter(TimeSpan.FromSeconds(graphTimeoutSeconds));
            using var response = await client.SendAsync(request, graphTimeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var graphError = await response.Content.ReadAsStringAsync(cancellationToken);
                return StatusCode((int)response.StatusCode, graphError);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var users = new List<DirectoryUserDto>();
            if (!document.RootElement.TryGetProperty("value", out var valueElement))
            {
                return Ok(users);
            }

            foreach (var item in valueElement.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                var displayName = item.TryGetProperty("displayName", out var displayNameProp) ? displayNameProp.GetString() : null;
                var mail = item.TryGetProperty("mail", out var mailProp) ? mailProp.GetString() : null;
                var upn = item.TryGetProperty("userPrincipalName", out var upnProp) ? upnProp.GetString() : null;
                var department = item.TryGetProperty("department", out var departmentProp) ? departmentProp.GetString() : null;

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                var email = !string.IsNullOrWhiteSpace(mail) ? mail : (upn ?? string.Empty);
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                users.Add(new DirectoryUserDto
                {
                    EntraObjectId = id,
                    DisplayName = displayName,
                    Email = email,
                    Department = department
                });
            }

            return Ok(users);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                message = "Microsoft Graph request timed out. Please retry."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new
            {
                message = "Unable to query Microsoft Graph. Ensure API delegated Graph permissions are configured and consented (User.ReadBasic.All).",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get list of all admin users
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<List<AdminUserDto>>> ListAdminUsers(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = new ListAdminUsersQuery { IsActive = isActive };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Add a new admin user
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserDto>> AddAdminUser(
        [FromBody] AddAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = AddAdminUserCommand.FromRequest(request);
            var result = await _mediator.Send(command, cancellationToken);
            _adminIdentityCacheEpoch.Invalidate();
            return CreatedAtAction(nameof(ListAdminUsers), new { }, result);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new
            {
                message = "An admin user with this Entra object ID or email already exists."
            });
        }
    }

    /// <summary>
    /// Update an existing admin user's permissions/status
    /// </summary>
    [HttpPut("{adminUserId}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserDto>> UpdateAdminUser(
        int adminUserId,
        [FromBody] UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = UpdateAdminUserCommand.FromRequest(adminUserId, request);
            var result = await _mediator.Send(command, cancellationToken);

            if (result == null)
            {
                return NotFound("Admin user not found");
            }

            _adminIdentityCacheEpoch.Invalidate();

            return Ok(result);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new
            {
                message = "An admin user with this Entra object ID or email already exists."
            });
        }
    }

    /// <summary>
    /// Remove an admin user
    /// </summary>
    [HttpDelete("{adminUserId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<bool>> RemoveAdminUser(
        int adminUserId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveAdminUserCommand { AdminUserId = adminUserId };
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFound("Admin user not found");
        }

        _adminIdentityCacheEpoch.Invalidate();

        return Ok(true);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            return sqlEx.Number is 2601 or 2627;
        }

        return false;
    }
}
