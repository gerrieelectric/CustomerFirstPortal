using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Portal.Application.Infrastructure.Integrations;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Tests;

public class GerrieSyncServiceTests
{
    [Fact]
    public void FormatRecognitionAnnouncementHtml_EncodesRecognitionReasonHtml()
    {
        var service = CreateService();
        var submission = new Submission
        {
            SubmissionType = SubmissionType.EmployeeToEmployee,
            NomineeName = "Taylor Test",
            RecognitionReason = "<script>alert('xss')</script>Thanks!"
        };

        var html = InvokeFormatRecognitionAnnouncementHtml(service, submission);

        Assert.Contains("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt;Thanks!", html);
        Assert.DoesNotContain("<script>", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatRecognitionAnnouncementHtml_ConvertsNewlinesToBreaksAfterEncoding()
    {
        var service = CreateService();
        var submission = new Submission
        {
            SubmissionType = SubmissionType.EmployeeToEmployee,
            NomineeName = "Jordan Example",
            RecognitionReason = "Line one\r\nLine two"
        };

        var html = InvokeFormatRecognitionAnnouncementHtml(service, submission);

        Assert.Contains("Line one<br/>Line two", html);
    }

    private static GerrieSyncService CreateService()
    {
        var configuration = new ConfigurationBuilder().Build();

        return new GerrieSyncService(
            dbContext: null!,
            graphPhotoService: null!,
            configuration: configuration,
            options: Options.Create(new GerrieSyncOptions()),
            logger: NullLogger<GerrieSyncService>.Instance);
    }

    private static string InvokeFormatRecognitionAnnouncementHtml(GerrieSyncService service, Submission submission)
    {
        var method = typeof(GerrieSyncService)
            .GetMethod("FormatRecognitionAnnouncementHtml", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find FormatRecognitionAnnouncementHtml via reflection.");

        return (string)(method.Invoke(service, [submission])
            ?? throw new InvalidOperationException("FormatRecognitionAnnouncementHtml returned null."));
    }
}
