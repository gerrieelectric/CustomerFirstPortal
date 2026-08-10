using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using MediatR;
using Portal.Api.Security;
using Portal.Application.Behaviors;
using Portal.Application.Features.Submissions.CreateSubmission;
using Portal.Application.Infrastructure;
using Portal.Application.Infrastructure.Persistence;
using Portal.Application.Infrastructure.Services;
using Portal.Api.Infrastructure.Serialization;
using Portal.Api.Infrastructure;
using Scalar.AspNetCore;
using System.Data.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

ApplyDevelopmentSqlConnectionOverrides(builder.Configuration, builder.Environment.IsDevelopment());

ValidateSqlTransportSecurity(builder.Configuration, builder.Environment.IsDevelopment());

// ── Authentication ────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("Entra"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new AdminOnlyRequirement()));
});
builder.Services.AddScoped<IAdminIdentityResolver, AdminIdentityResolver>();
builder.Services.AddSingleton<IAdminIdentityCacheEpoch, AdminIdentityCacheEpoch>();
builder.Services.AddScoped<IAuthorizationHandler, Portal.Api.Security.AdminOnlyHandler>();

// ── Infrastructure ────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Azure OpenAI for Suggestion Generation ────────────────────────────────────
var azureOpenAiConfig = builder.Configuration.GetSection("AzureOpenAI");
if (azureOpenAiConfig.Exists())
{
    var endpoint = azureOpenAiConfig["Endpoint"];
    var apiKey = azureOpenAiConfig["ApiKey"];

    if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
    {
        // Register suggestion service
        builder.Services.Configure<SuggestionGenerationOptions>(
            builder.Configuration.GetSection("AzureOpenAI:Suggestions"));
        builder.Services.AddScoped<ISuggestionGenerationService, SuggestionGenerationService>();
        builder.Services.AddSingleton<ISubmissionSuggestionQueue, SubmissionSuggestionQueue>();
        builder.Services.AddHostedService<SubmissionSuggestionBackgroundService>();

        // Register attachment field extraction service
        builder.Services.Configure<FieldExtractionOptions>(
            builder.Configuration.GetSection("AzureOpenAI:FieldExtraction"));
        builder.Services.AddScoped<ISubmissionFieldExtractionService, SubmissionFieldExtractionService>();
    }
}

// ── MediatR & Validation ──────────────────────────────────────────────────────
builder.Services.AddMediatR(config =>
    config.RegisterServicesFromAssembly(typeof(CreateSubmissionCommand).Assembly));

builder.Services.AddValidatorsFromAssemblyContaining<CreateSubmissionValidator>();

builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(FluentValidationBehavior<,>));

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedCorsOrigins = new[]
{
    "https://cfpdev.gerrie.com",
    "https://cfpapidev.gerrie.com",
    "http://localhost:4200",
    "https://localhost:4200"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("PortalCors", policy =>
        policy.WithOrigins(allowedCorsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddHttpClient();

// ── API ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
    options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeJsonConverter());
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Microsoft Entra access token"
        };

        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document, null)] = new List<string>()
        });

        return Task.CompletedTask;
    });
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PortalDbContext>("database");

var app = builder.Build();

var apiDocsEnabled = app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("ApiDocs:Enabled");
var migrateOnStartup = builder.Configuration.GetValue<bool>("Database:MigrateOnStartup");

// ── Middleware ────────────────────────────────────────────────────────────────
if (apiDocsEnabled)
{
    app.Map("/openapi/ui.json", subApp =>
    {
        subApp.Run(async context =>
        {
            context.Response.Redirect("/openapi/v1.json", permanent: false);
            await Task.CompletedTask;
        });
    });

    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Note: HTTPS redirection disabled for IIS (handles HTTPS termination at reverse proxy)
// IIS will enforce HTTPS at the binding level

app.UseCors("PortalCors");

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");

// ── Apply Pending Migrations (opt-in) ────────────────────────────────────────
if (migrateOnStartup)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortalDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database migration failed");
        throw;
    }
}
else
{
    app.Logger.LogInformation("Skipping startup database migrations (Database:MigrateOnStartup=false)");
}

// ── Controller Mapping ────────────────────────────────────────────────────────
app.MapControllers();

app.Run();

static void ValidateSqlTransportSecurity(IConfiguration configuration, bool isDevelopment)
{
    var allowInsecureInDevelopment =
        configuration.GetValue<bool>("Security:AllowInsecureSqlTransportInDevelopment");

    // Opt-in flag for environments where the SQL Server uses a self-signed/internal certificate.
    // Encryption is still required; only certificate chain validation is relaxed.
    var allowTrustedServerCertificate =
        configuration.GetValue<bool>("Security:AllowTrustedServerCertificate");

    var sqlConnectionStrings = new (string Name, string? Value)[]
    {
        ("ConnectionStrings:DefaultConnection", configuration.GetConnectionString("DefaultConnection")),
        ("ConnectionStrings:GerrieDatabase", configuration.GetConnectionString("GerrieDatabase")),
        ("Gerrie:ConnectionString", configuration["Gerrie:ConnectionString"])
    };

    var violations = new List<string>();

    foreach (var (name, value) in sqlConnectionStrings)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            continue;
        }

        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = value };

            var hasEncrypt = builder.TryGetValue("Encrypt", out var encryptObj);
            var encryptIsTrue = hasEncrypt && bool.TryParse(Convert.ToString(encryptObj), out var encrypt) && encrypt;

            var hasTrustServerCert = builder.TryGetValue("TrustServerCertificate", out var trustObj);
            var trustServerCertIsFalse =
                hasTrustServerCert && bool.TryParse(Convert.ToString(trustObj), out var trust) && !trust;

            if (!encryptIsTrue)
            {
                violations.Add($"{name} must set Encrypt=True");
            }

            if (!trustServerCertIsFalse && !allowTrustedServerCertificate)
            {
                violations.Add($"{name} must set TrustServerCertificate=False");
            }
        }
        catch (Exception ex)
        {
            violations.Add($"{name} is not a valid connection string ({ex.Message})");
        }
    }

    if (violations.Count == 0)
    {
        return;
    }

    var message = "Insecure SQL transport configuration detected: " + string.Join("; ", violations);
    if (isDevelopment && allowInsecureInDevelopment)
    {
        Console.WriteLine("WARNING: " + message);
        return;
    }

    throw new InvalidOperationException(message);
}

static void ApplyDevelopmentSqlConnectionOverrides(ConfigurationManager configuration, bool isDevelopment)
{
    if (!isDevelopment)
    {
        return;
    }

    var allowInsecureInDevelopment =
        configuration.GetValue<bool>("Security:AllowInsecureSqlTransportInDevelopment");

    if (!allowInsecureInDevelopment)
    {
        return;
    }

    var connectionStringKeys = new[]
    {
        "ConnectionStrings:DefaultConnection",
        "ConnectionStrings:GerrieDatabase",
        "Gerrie:ConnectionString"
    };

    var overrides = new Dictionary<string, string?>();

    foreach (var key in connectionStringKeys)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            continue;
        }

        try
        {
            var csb = new DbConnectionStringBuilder { ConnectionString = value };
            csb["Encrypt"] = "True";
            csb["TrustServerCertificate"] = "True";
            overrides[key] = csb.ConnectionString;
        }
        catch
        {
            // Skip malformed values; validation will report them.
        }
    }

    if (overrides.Count > 0)
    {
        configuration.AddInMemoryCollection(overrides);
    }
}
