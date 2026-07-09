using FluentEmail.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portal.Application.Infrastructure.Caching;
using Portal.Application.Infrastructure.HostedServices;
using Portal.Application.Infrastructure.Integrations;
using Portal.Application.Infrastructure.Notifications;
using Portal.Application.Infrastructure.Persistence;
using Portal.Application.Infrastructure.Storage;
using System.Net;
using System.Net.Mail;

namespace Portal.Application.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureStorageOptions>(configuration.GetSection("AzureStorage"));
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.Configure<NotificationDispatchOptions>(configuration.GetSection("NotificationDispatch"));
        services.Configure<ReviewLinkOptions>(configuration.GetSection("ReviewLink"));
        services.Configure<GerrieSyncOptions>(configuration.GetSection("Gerrie:Sync"));
        services.Configure<PhotoCacheOptions>(configuration.GetSection("PhotoCache"));

        var emailOptions = configuration.GetSection("Email").Get<EmailOptions>() ?? new EmailOptions();
        var smtpClient = new SmtpClient(emailOptions.SmtpHost)
        {
            Port = emailOptions.SmtpPort,
            EnableSsl = emailOptions.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(emailOptions.Username))
        {
            smtpClient.Credentials = new NetworkCredential(emailOptions.Username, emailOptions.Password);
        }

        services
            .AddFluentEmail(emailOptions.From)
            .AddSmtpSender(smtpClient);

        services.AddHttpClient("GerrieSync", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IAttachmentStorageService, AzureBlobAttachmentStorageService>();
        services.AddScoped<IAdminSubmissionNotificationService, AdminSubmissionNotificationService>();
        services.AddScoped<INotificationDispatchProcessor, AdminSubmissionNotificationService>();
        services.AddScoped<IGerrieSyncService, GerrieSyncService>();
        services.AddSingleton<ICustomerSearchService, JdeCustomerSearchService>();
        services.AddHostedService<NotificationDispatchWorkerService>();
        services.AddHostedService<GerrieSyncWorkerService>();

        // Photo caching services
        services.AddScoped<IPhotoCache, DistributedPhotoCache>();
        services.AddScoped<IGraphPhotoService, GraphPhotoService>();

        services.AddDbContext<PortalDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(PortalDbContext).Assembly.FullName)
                          .CommandTimeout(30)));

        return services;
    }
}
