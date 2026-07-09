using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Portal.Application.DTOs.Submissions;
using Portal.Application.Features.Submissions.CreateSubmission;
using Portal.Application.Features.Submissions.GetSubmission;
using Portal.Application.Features.Submissions.ListSubmissions;
using Portal.Application.Features.Submissions.ReviewSubmission;
using Portal.Application.Features.Submissions.UpdateSubmission;
using Portal.Application.Features.AdminUsers;
using Portal.Domain.Entities;
using Portal.Domain.Enums;
using Portal.Application.Infrastructure.Caching;
using Portal.Application.Infrastructure.Persistence;
using Portal.Application.Infrastructure.Storage;
using Portal.Application.Infrastructure.Services;
using Portal.Application.Infrastructure.Integrations;
using Portal.Api.DTOs.Submissions;
using Portal.Api.Security;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Portal.Api.Features.Submissions;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubmissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly PortalDbContext _dbContext;
    private readonly IAttachmentStorageService _attachmentStorage;
    private readonly AzureStorageOptions _storageOptions;
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IGraphPhotoService _graphPhotoService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubmissionsController> _logger;
    private readonly IAdminIdentityResolver _adminIdentityResolver;
    private readonly ICustomerSearchService _customerSearchService;
    private readonly ISuggestionGenerationService? _suggestionService;
    private readonly ISubmissionFieldExtractionService? _fieldExtractionService;

    public SubmissionsController(
        IMediator mediator,
        PortalDbContext dbContext,
        IAttachmentStorageService attachmentStorage,
        Microsoft.Extensions.Options.IOptions<AzureStorageOptions> storageOptions,
        ITokenAcquisition tokenAcquisition,
        IHttpClientFactory httpClientFactory,
        IGraphPhotoService graphPhotoService,
        IConfiguration configuration,
        ILogger<SubmissionsController> logger,
        IAdminIdentityResolver adminIdentityResolver,
        ICustomerSearchService customerSearchService,
        ISuggestionGenerationService? suggestionService = null,
        ISubmissionFieldExtractionService? fieldExtractionService = null)
    {
        _mediator = mediator;
        _dbContext = dbContext;
        _attachmentStorage = attachmentStorage;
        _storageOptions = storageOptions.Value;
        _tokenAcquisition = tokenAcquisition;
        _httpClientFactory = httpClientFactory;
        _graphPhotoService = graphPhotoService;
        _configuration = configuration;
        _logger = logger;
        _adminIdentityResolver = adminIdentityResolver;
        _customerSearchService = customerSearchService;
        _suggestionService = suggestionService;
        _fieldExtractionService = fieldExtractionService;
    }

    /// <summary>
    /// Create a new submission
    /// </summary>
    /// <param name="request">Submission details</param>
    /// <returns>Created submission with ID</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SubmissionResponse>> CreateSubmission(
        [FromBody] CreateSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorProfile = await ResolveAuthenticatedUserProfileAsync(cancellationToken);
        request.SubmittedByName = actorProfile.DisplayName;
        request.SubmittedByEmail = actorProfile.Email;
        request.SubmittedByDepartment = actorProfile.Department;
        request.SubmittedByEntraObjectId = actorProfile.EntraObjectId;

        var command = CreateSubmissionCommand.FromRequest(request);
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetSubmission), new { id = result.SubmissionId }, result);
    }

    /// <summary>
    /// List directory users from Microsoft Graph for nominee selection.
    /// </summary>
    [HttpGet("directory-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<DirectoryUserDto>>> GetDirectoryUsers(CancellationToken cancellationToken = default)
    {
        const int maxUsers = 2000;
        var users = new List<DirectoryUserDto>(capacity: 250);

        try
        {
            var scopes = (_configuration["Graph:Scopes"] ?? "User.Read User.ReadBasic.All")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var baseUrl = _configuration["Graph:BaseUrl"] ?? "https://graph.microsoft.com/v1.0";

            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes, user: User);
            var nextUrl = $"{baseUrl}/users?$top=999&$select=id,displayName,mail,userPrincipalName,department,accountEnabled&$filter=accountEnabled eq true";

            while (!string.IsNullOrWhiteSpace(nextUrl) && users.Count < maxUsers)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var client = _httpClientFactory.CreateClient();
                using var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var graphError = await response.Content.ReadAsStringAsync(cancellationToken);
                    return StatusCode((int)response.StatusCode, graphError);
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                if (document.RootElement.TryGetProperty("value", out var valueElement))
                {
                    foreach (var item in valueElement.EnumerateArray())
                    {
                        if (users.Count >= maxUsers)
                        {
                            break;
                        }

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
                }

                nextUrl = document.RootElement.TryGetProperty("@odata.nextLink", out var nextLinkProp)
                    ? nextLinkProp.GetString()
                    : null;
            }

            var deduped = users
                .GroupBy(u => u.EntraObjectId)
                .Select(g => g.First())
                .OrderBy(u => u.DisplayName)
                .ToList();

            return Ok(deduped);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load directory users from Graph for nominee selection.");
            return StatusCode(502, new
            {
                message = "Unable to query Microsoft Graph directory users.",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Search directory users from Microsoft Graph for nominee selection.
    /// </summary>
    [HttpGet("directory-search")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<DirectoryUserDto>>> SearchDirectoryUsers(
        [FromQuery] string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return Ok(new List<DirectoryUserDto>());
        }

        try
        {
            var scopes = (_configuration["Graph:Scopes"] ?? "User.Read User.ReadBasic.All")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var baseUrl = _configuration["Graph:BaseUrl"] ?? "https://graph.microsoft.com/v1.0";

            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes, user: User);

            var escapedQuery = query.Trim().Replace("'", "''");
            var requestUrl =
                $"{baseUrl}/users?$top=30&$select=id,displayName,mail,userPrincipalName,department,givenName,surname&$filter=accountEnabled eq true and (startsWith(displayName,'{escapedQuery}') or startsWith(mail,'{escapedQuery}') or startsWith(userPrincipalName,'{escapedQuery}') or startsWith(givenName,'{escapedQuery}') or startsWith(surname,'{escapedQuery}'))";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken);

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
                    Department = department,
                    ProfilePhotoUrl = $"/api/submissions/user-photo/{id}"
                });
            }

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to search directory users from Graph for nominee selection.");
            return StatusCode(502, new
            {
                message = "Unable to query Microsoft Graph directory users.",
                details = ex.Message
            });
        }
    }

    [HttpGet("customer-search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<string>>> SearchCustomers(
        [FromQuery] string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return Ok(new List<string>());
        }

        var customers = await _customerSearchService.SearchCustomersAsync(query, cancellationToken);
        return Ok(customers.ToList());
    }

    /// <summary>
    /// Get user profile photo from Microsoft Graph.
    /// Results are cached to improve performance on repeated requests.
    /// </summary>
    [HttpGet("user-photo/{userId}")]
    [Authorize]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any, NoStore = false)] // 7 days
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetUserProfilePhoto(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest("User ID is required.");
        }

        var photoResult = await _graphPhotoService.GetUserPhotoAsync(userId, cancellationToken);

        if (photoResult == null)
        {
            return NoContent();
        }

        var (photoBytes, contentType) = photoResult.Value;
        return File(photoBytes, contentType);
    }

    private async Task<AuthenticatedUserProfile> ResolveAuthenticatedUserProfileAsync(CancellationToken cancellationToken)
    {
        var fallbackName = User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst("name")?.Value
            ?? "Unknown";

        var fallbackEmail = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("upn")?.Value
            ?? "unknown@company.com";

        var fallbackDepartment = User.FindFirst("department")?.Value;
        var objectIdValue = User.FindFirst("oid")?.Value
            ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? User.FindFirst("objectidentifier")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        Guid? fallbackEntraObjectId = null;
        if (!string.IsNullOrWhiteSpace(objectIdValue) && Guid.TryParse(objectIdValue, out var parsedObjectId))
        {
            fallbackEntraObjectId = parsedObjectId;
        }

        try
        {
            var scopes = (_configuration["Graph:Scopes"] ?? "User.Read")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var baseUrl = _configuration["Graph:BaseUrl"] ?? "https://graph.microsoft.com/v1.0";

            var accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes, user: User);

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/me?$select=id,displayName,mail,userPrincipalName,department");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Graph /me call failed with status code {StatusCode}. Falling back to token claims.", (int)response.StatusCode);
                return new AuthenticatedUserProfile(fallbackName, fallbackEmail, fallbackDepartment, fallbackEntraObjectId);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var displayName = document.RootElement.TryGetProperty("displayName", out var displayNameProp)
                ? displayNameProp.GetString()
                : null;
            var mail = document.RootElement.TryGetProperty("mail", out var mailProp)
                ? mailProp.GetString()
                : null;
            var upn = document.RootElement.TryGetProperty("userPrincipalName", out var upnProp)
                ? upnProp.GetString()
                : null;
            var department = document.RootElement.TryGetProperty("department", out var departmentProp)
                ? departmentProp.GetString()
                : null;
            var graphObjectId = document.RootElement.TryGetProperty("id", out var idProp)
                ? idProp.GetString()
                : null;

            Guid? resolvedObjectId = fallbackEntraObjectId;
            if (!string.IsNullOrWhiteSpace(graphObjectId) && Guid.TryParse(graphObjectId, out var parsedGraphObjectId))
            {
                resolvedObjectId = parsedGraphObjectId;
            }

            var resolvedName = string.IsNullOrWhiteSpace(displayName) ? fallbackName : displayName;
            var resolvedEmail = !string.IsNullOrWhiteSpace(mail)
                ? mail
                : (string.IsNullOrWhiteSpace(upn) ? fallbackEmail : upn);
            var resolvedDepartment = string.IsNullOrWhiteSpace(department) ? fallbackDepartment : department;

            return new AuthenticatedUserProfile(resolvedName, resolvedEmail, resolvedDepartment, resolvedObjectId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to resolve user profile from Graph. Falling back to token claims.");
            return new AuthenticatedUserProfile(fallbackName, fallbackEmail, fallbackDepartment, fallbackEntraObjectId);
        }
    }

    private sealed record AuthenticatedUserProfile(string DisplayName, string Email, string? Department, Guid? EntraObjectId);

    /// <summary>
    /// Get a submission by ID
    /// </summary>
    /// <param name="id">Submission ID</param>
    /// <returns>Submission details or 404 if not found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SubmissionResponse>> GetSubmission(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SubmissionId == id, cancellationToken);

        if (submission == null)
        {
            return NotFound();
        }

        if (!await CanCurrentUserAccessSubmissionAsync(submission, cancellationToken))
        {
            return Forbid();
        }

        var query = new GetSubmissionQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// List submissions with filtering and pagination (admin only)
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (1-100)</param>
    /// <param name="status">Filter by status (Submitted, Approved, Rejected, etc.)</param>
    /// <param name="submissionType">Filter by type (EmployeeToEmployee, EmployeeOnBehalfOfCustomer, CustomerToEmployee)</param>
    /// <param name="searchTerm">Search by nominee name or email</param>
    /// <param name="sortBy">Sort field: SubmittedUtc (default), NomineeName, Status</param>
    /// <param name="sortDescending">Sort order descending</param>
    /// <returns>Paginated list of submissions</returns>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedSubmissionsResponse>> ListSubmissions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? submissionType = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        var query = new ListSubmissionsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status,
            SubmissionType = submissionType,
            SearchTerm = searchTerm,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Review a submission (approve/reject) - admin only
    /// </summary>
    /// <param name="id">Submission ID</param>
    /// <param name="request">Review decision and optional notes</param>
    /// <returns>Updated submission</returns>
    [HttpPost("{id}/review")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> ReviewSubmission(
        [FromRoute] Guid id,
        [FromBody] AdminReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var adminUser = await ResolveCurrentAdminUserAsync(cancellationToken);
        if (adminUser == null)
        {
            _logger.LogWarning("Unable to resolve active admin user mapping for review request.");
            return StatusCode(StatusCodes.Status403Forbidden,
                "Authenticated user is not mapped to an active admin account.");
        }

        var adminName = string.IsNullOrWhiteSpace(adminUser.DisplayName)
            ? (User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value ?? "Unknown")
            : adminUser.DisplayName;
        var adminEmail = string.IsNullOrWhiteSpace(adminUser.Email)
            ? (User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("preferred_username")?.Value ?? "unknown@company.com")
            : adminUser.Email;

        var command = ReviewSubmissionCommand.FromRequest(id, request, adminUser.AdminUserId, adminName, adminEmail);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    private async Task<AdminUser?> ResolveCurrentAdminUserAsync(CancellationToken cancellationToken)
    {
        return await _adminIdentityResolver.ResolveActiveAdminAsync(User, cancellationToken);
    }

    /// <summary>
    /// Update a submission in Submitted status
    /// </summary>
    /// <param name="id">Submission ID</param>
    /// <param name="request">Fields to update (all optional)</param>
    /// <returns>Updated submission</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> UpdateSubmission(
        [FromRoute] Guid id,
        [FromBody] UpdateSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SubmissionId == id, cancellationToken);

        if (submission == null)
        {
            return NotFound();
        }

        if (!await CanCurrentUserAccessSubmissionAsync(submission, cancellationToken))
        {
            return Forbid();
        }

        var command = UpdateSubmissionCommand.FromRequest(id, request);
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Extract submission form fields from attachments using AI.
    /// Stateless: files are analyzed but not stored. Used by the customer
    /// recognition form's attachment-first popup to pre-fill fields.
    /// </summary>
    [HttpPost("extract-fields")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExtractedSubmissionFieldsResponse>> ExtractSubmissionFields(
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("At least one non-empty file is required.");
        }

        var maxBytes = _storageOptions.MaxFileSizeMB * 1024L * 1024L;
        var attachments = new List<ExtractionAttachment>(files.Count);

        foreach (var file in files)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("A non-empty file is required.");
            }

            if (file.Length > maxBytes)
            {
                return BadRequest($"File exceeds max size of {_storageOptions.MaxFileSizeMB} MB.");
            }

            var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
            if (_storageOptions.AllowedExtensions.Length > 0 && !_storageOptions.AllowedExtensions.Contains(extension))
            {
                return BadRequest($"File type .{extension} is not allowed.");
            }

            await using var stream = file.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);

            attachments.Add(new ExtractionAttachment(
                Path.GetFileName(file.FileName),
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                buffer.ToArray()));
        }

        if (_fieldExtractionService == null)
        {
            return Ok(new ExtractedSubmissionFieldsResponse { Status = "Disabled" });
        }

        var result = await _fieldExtractionService.ExtractFieldsAsync(attachments, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Upload an attachment for a submission.
    /// </summary>
    [HttpPost("{id}/attachments")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttachmentUploadResponse>> UploadAttachment(
        [FromRoute] Guid id,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("A non-empty file is required.");
        }

        var maxBytes = _storageOptions.MaxFileSizeMB * 1024L * 1024L;
        if (file.Length > maxBytes)
        {
            return BadRequest($"File exceeds max size of {_storageOptions.MaxFileSizeMB} MB.");
        }

        var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
        if (_storageOptions.AllowedExtensions.Length > 0 && !_storageOptions.AllowedExtensions.Contains(extension))
        {
            return BadRequest($"File type .{extension} is not allowed.");
        }

        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.SubmissionId == id, cancellationToken);

        if (submission == null)
        {
            return NotFound();
        }

        if (!await CanCurrentUserAccessSubmissionAsync(submission, cancellationToken))
        {
            return Forbid();
        }

        await using var stream = file.OpenReadStream();
        var blobName = await _attachmentStorage.UploadAsync(stream, file.FileName, file.ContentType, cancellationToken);

        var uploadedBy = User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst("name")?.Value
            ?? "Unknown";

        var attachment = new SubmissionAttachment
        {
            SubmissionAttachmentId = Guid.NewGuid(),
            SubmissionId = id,
            OriginalFileName = Path.GetFileName(file.FileName),
            BlobName = blobName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            FileSizeBytes = file.Length,
            UploadedByName = uploadedBy,
            UploadedUtc = DateTime.UtcNow
        };

        _dbContext.SubmissionAttachments.Add(attachment);

        var auditEvent = new AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            SubmissionId = id,
            ActorName = uploadedBy,
            ActorEmail = User.FindFirst(ClaimTypes.Email)?.Value,
            EventType = AuditEventType.AttachmentAdded,
            EventDetail = $"Attachment uploaded: {attachment.OriginalFileName} ({attachment.FileSizeBytes} bytes)",
            CreatedUtc = DateTime.UtcNow
        };

        _dbContext.AuditEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new AttachmentUploadResponse
        {
            SubmissionAttachmentId = attachment.SubmissionAttachmentId,
            SubmissionId = attachment.SubmissionId,
            OriginalFileName = attachment.OriginalFileName,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes,
            UploadedUtc = attachment.UploadedUtc
        };

        return CreatedAtAction(nameof(GetSubmission), new { id }, response);
    }

    /// <summary>
    /// List all attachments for a submission.
    /// </summary>
    [HttpGet("{id}/attachments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<AttachmentListItemDto>>> ListAttachments(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.SubmissionId == id, cancellationToken);

        if (submission == null)
            return NotFound();

        if (!await CanCurrentUserAccessSubmissionAsync(submission, cancellationToken))
        {
            return Forbid();
        }

        var attachments = await _dbContext.SubmissionAttachments
            .AsNoTracking()
            .Where(a => a.SubmissionId == id)
            .OrderByDescending(a => a.UploadedUtc)
            .Select(a => new AttachmentListItemDto
            {
                SubmissionAttachmentId = a.SubmissionAttachmentId,
                OriginalFileName = a.OriginalFileName,
                ContentType = a.ContentType,
                FileSizeBytes = a.FileSizeBytes,
                UploadedByName = a.UploadedByName,
                UploadedUtc = a.UploadedUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(attachments);
    }

    /// <summary>
    /// Generate a download URL (SAS) for an attachment. URL expires in 1 hour.
    /// </summary>
    [HttpGet("{id}/attachments/{attachmentId}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttachmentDownloadResponse>> GetAttachmentDownloadUrl(
        [FromRoute] Guid id,
        [FromRoute] Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SubmissionId == id, cancellationToken);

        if (submission == null)
            return NotFound();

        if (!await CanCurrentUserAccessSubmissionAsync(submission, cancellationToken))
        {
            return Forbid();
        }

        var attachment = await _dbContext.SubmissionAttachments
            .FirstOrDefaultAsync(a => a.SubmissionAttachmentId == attachmentId && a.SubmissionId == id, cancellationToken);

        if (attachment == null)
            return NotFound();

        var expiresIn = TimeSpan.FromHours(1);
        var sasUrl = await _attachmentStorage.GetDownloadUrlAsync(
            attachment.BlobName,
            attachment.OriginalFileName,
            expiresIn,
            cancellationToken);
        var expiresUtc = DateTime.UtcNow.Add(expiresIn);

        return Ok(new AttachmentDownloadResponse
        {
            SubmissionAttachmentId = attachment.SubmissionAttachmentId,
            OriginalFileName = attachment.OriginalFileName,
            SasUrl = sasUrl,
            ExpiresUtc = expiresUtc
        });
    }

    /// <summary>
    /// Delete an attachment from both blob storage and database.
    /// </summary>
    [HttpDelete("{id}/attachments/{attachmentId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAttachment(
        [FromRoute] Guid id,
        [FromRoute] Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var submission = await _dbContext.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SubmissionId == id, cancellationToken);

        if (submission == null)
            return NotFound();

        if (!await CanCurrentUserAccessSubmissionAsync(submission, cancellationToken))
        {
            return Forbid();
        }

        var attachment = await _dbContext.SubmissionAttachments
            .FirstOrDefaultAsync(a => a.SubmissionAttachmentId == attachmentId && a.SubmissionId == id, cancellationToken);

        if (attachment == null)
            return NotFound();

        // Delete from blob storage
        await _attachmentStorage.DeleteAsync(attachment.BlobName, cancellationToken);

        // Delete from database
        _dbContext.SubmissionAttachments.Remove(attachment);

        // Create audit event
        var deletedBy = User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst("name")?.Value
            ?? "Unknown";

        var auditEvent = new AuditEvent
        {
            AuditEventId = Guid.NewGuid(),
            SubmissionId = id,
            ActorName = deletedBy,
            ActorEmail = User.FindFirst(ClaimTypes.Email)?.Value,
            EventType = AuditEventType.AttachmentDeleted,
            EventDetail = $"Attachment deleted: {attachment.OriginalFileName}",
            CreatedUtc = DateTime.UtcNow
        };

        _dbContext.AuditEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Get AI-generated suggestions for a submission's recognition reason (admin only).
    /// Suggestions are precomputed and persisted, so this endpoint is read-only.
    /// </summary>
    /// <param name="id">Submission ID</param>
    /// <returns>Suggested edits with diffs, or 404 if not found</returns>
    [HttpGet("{id}/suggested-edits")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SuggestedEditsResponse>> GetSuggestedEdits(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (_suggestionService == null)
        {
            _logger.LogWarning("Suggestion service not configured. AI suggestions are unavailable.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "AI suggestion service is not configured."
            });
        }

        // Verify submission exists
        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.SubmissionId == id, cancellationToken);

        if (submission == null)
            return NotFound();

        // Fetch persisted suggestions
        var suggestion = await _suggestionService.GetSuggestionsAsync(id, cancellationToken);

        if (suggestion == null)
        {
            _logger.LogWarning("No suggestions found for submission {SubmissionId}. Suggestions may not have been generated yet.", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Suggestions have not been generated for this submission yet.",
                status = "pending"
            });
        }

        // Deserialize suggestions from JSON
        var suggestionsList = new List<EditSuggestion>();
        if (!string.IsNullOrEmpty(suggestion.SuggestionsJson))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                suggestionsList = System.Text.Json.JsonSerializer.Deserialize<List<EditSuggestion>>(
                    suggestion.SuggestionsJson, options) ?? [];
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize suggestions for submission {SubmissionId}.", id);
            }
        }

        var response = new SuggestedEditsResponse
        {
            SubmissionId = suggestion.SubmissionId,
            OriginalReason = suggestion.OriginalReason,
            FullEditedReason = suggestion.FullEditedReason,
            Suggestions = suggestionsList,
            Status = suggestion.Status,
            GeneratedUtc = suggestion.GeneratedUtc,
            ModelName = suggestion.ModelName,
            PromptVersion = suggestion.PromptVersion
        };

        return Ok(response);
    }

    /// <summary>
    /// Manually regenerate suggestions for a submission (admin only, use with caution).
    /// This will discard existing suggestions and call the AI model again.
    /// </summary>
    /// <param name="id">Submission ID</param>
    /// <returns>Updated suggested edits</returns>
    [HttpPost("{id}/regenerate-suggestions")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SuggestedEditsResponse>> RegenerateSuggestedEdits(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (_suggestionService == null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "AI suggestion service is not configured."
            });
        }

        // Verify submission exists
        var submission = await _dbContext.Submissions
            .FirstOrDefaultAsync(s => s.SubmissionId == id, cancellationToken);

        if (submission == null)
            return NotFound();

        // Regenerate suggestions (this will remove old ones and create new ones)
        var suggestion = await _suggestionService.RegenerateSuggestionsAsync(id, cancellationToken);

        if (suggestion == null)
        {
            _logger.LogWarning("Regeneration failed for submission {SubmissionId}.", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Failed to regenerate suggestions. Please try again later."
            });
        }

        // Deserialize suggestions from JSON
        var suggestionsList = new List<EditSuggestion>();
        if (!string.IsNullOrEmpty(suggestion.SuggestionsJson))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                suggestionsList = System.Text.Json.JsonSerializer.Deserialize<List<EditSuggestion>>(
                    suggestion.SuggestionsJson, options) ?? [];
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize regenerated suggestions for submission {SubmissionId}.", id);
            }
        }

        var response = new SuggestedEditsResponse
        {
            SubmissionId = suggestion.SubmissionId,
            OriginalReason = suggestion.OriginalReason,
            FullEditedReason = suggestion.FullEditedReason,
            Suggestions = suggestionsList,
            Status = suggestion.Status,
            GeneratedUtc = suggestion.GeneratedUtc,
            ModelName = suggestion.ModelName,
            PromptVersion = suggestion.PromptVersion
        };

        return Ok(response);
    }

    private async Task<bool> CanCurrentUserAccessSubmissionAsync(
        Submission submission,
        CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        if (await _adminIdentityResolver.IsActiveAdminAsync(User, cancellationToken))
        {
            return true;
        }

        var identityClaims = _adminIdentityResolver.ExtractIdentityClaims(User);

        if (identityClaims.EntraObjectId.HasValue
            && submission.SubmittedByEntraObjectId.HasValue
            && identityClaims.EntraObjectId.Value == submission.SubmittedByEntraObjectId.Value)
        {
            return true;
        }

        var normalizedSubmitterEmail = NormalizeEmail(submission.SubmittedByEmail);
        if (!string.IsNullOrWhiteSpace(identityClaims.Email)
            && !string.IsNullOrWhiteSpace(normalizedSubmitterEmail)
            && string.Equals(identityClaims.Email, normalizedSubmitterEmail, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return email.Trim().ToLowerInvariant();
    }
}
