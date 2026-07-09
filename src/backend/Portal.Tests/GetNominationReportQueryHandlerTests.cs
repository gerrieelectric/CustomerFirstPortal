using Microsoft.EntityFrameworkCore;
using Portal.Application.Features.Analytics.GetNominationReport;
using Portal.Application.Infrastructure.Persistence;
using Portal.Domain.Entities;
using Portal.Domain.Enums;

namespace Portal.Tests;

public class GetNominationReportQueryHandlerTests
{
    private DbContextOptions<PortalDbContext> GetInMemoryOptions()
    {
        return new DbContextOptionsBuilder<PortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task Handle_FiltersBy_EmployeeToEmployee()
    {
        // Arrange
        var options = GetInMemoryOptions();

        using (var context = new PortalDbContext(options))
        {
            var today = DateTime.UtcNow;
            var submissions = new[]
            {
                new Submission
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionType = SubmissionType.EmployeeToEmployee,
                    Status = SubmissionStatus.Approved,
                    NomineeName = "Alice",
                    NomineeEmail = "alice@example.com",
                    NomineeDepartment = "Engineering",
                    SubmittedUtc = today,
                    SubmittedByName = "Bob",
                    SubmittedByEmail = "bob@example.com",
                    RecognitionReason = "Great work"
                },
                new Submission
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionType = SubmissionType.CustomerToEmployee,
                    Status = SubmissionStatus.Approved,
                    NomineeName = "Charlie",
                    NomineeEmail = "charlie@example.com",
                    NomineeDepartment = "Sales",
                    SubmittedUtc = today,
                    SubmittedByName = "Customer",
                    SubmittedByEmail = "customer@example.com",
                    RecognitionReason = "Excellent service",
                    CustomerName = "Big Customer"
                }
            };

            context.Submissions.AddRange(submissions);
            await context.SaveChangesAsync();
        }

        using (var context = new PortalDbContext(options))
        {
            var query = new GetNominationReportQuery
            {
                RangeType = ReportDateRangeType.CalendarMonth,
                TypeFilter = NominationTypeFilter.EmployeeToEmployee,
                TopCount = 10
            };

            var handler = new GetNominationReportQueryHandler(context);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Single(result.TopNominated); // Only Alice (EmployeeToEmployee)
            Assert.Equal("Alice", result.TopNominated[0].NomineeName);
            Assert.Equal(1, result.TopNominated[0].TotalNominations);
        }
    }

    [Fact]
    public async Task Handle_FiltersBy_CustomerToEmployee_IncludesOnBehalfOf()
    {
        // Arrange
        var options = GetInMemoryOptions();

        using (var context = new PortalDbContext(options))
        {
            var today = DateTime.UtcNow;
            var submissions = new[]
            {
                new Submission
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionType = SubmissionType.CustomerToEmployee,
                    Status = SubmissionStatus.Approved,
                    NomineeName = "Alice",
                    NomineeEmail = "alice@example.com",
                    NomineeDepartment = "Engineering",
                    SubmittedUtc = today,
                    SubmittedByName = "Customer",
                    SubmittedByEmail = "customer@example.com",
                    RecognitionReason = "Excellent service",
                    CustomerName = "Big Customer"
                },
                new Submission
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionType = SubmissionType.EmployeeOnBehalfOfCustomer,
                    Status = SubmissionStatus.Approved,
                    NomineeName = "Bob",
                    NomineeEmail = "bob@example.com",
                    NomineeDepartment = "Sales",
                    SubmittedUtc = today,
                    SubmittedByName = "Employee",
                    SubmittedByEmail = "employee@example.com",
                    RecognitionReason = "Customer request",
                    CustomerName = "Another Customer"
                },
                new Submission
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionType = SubmissionType.EmployeeToEmployee,
                    Status = SubmissionStatus.Approved,
                    NomineeName = "Charlie",
                    NomineeEmail = "charlie@example.com",
                    NomineeDepartment = "HR",
                    SubmittedUtc = today,
                    SubmittedByName = "Employee",
                    SubmittedByEmail = "employee@example.com",
                    RecognitionReason = "Great support"
                }
            };

            context.Submissions.AddRange(submissions);
            await context.SaveChangesAsync();
        }

        using (var context = new PortalDbContext(options))
        {
            var query = new GetNominationReportQuery
            {
                RangeType = ReportDateRangeType.CalendarMonth,
                TypeFilter = NominationTypeFilter.CustomerToEmployee,
                TopCount = 10
            };

            var handler = new GetNominationReportQueryHandler(context);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.TopNominated.Count); // Alice and Bob only
            Assert.Contains("Alice", result.TopNominated.Select(n => n.NomineeName));
            Assert.Contains("Bob", result.TopNominated.Select(n => n.NomineeName));
            Assert.DoesNotContain("Charlie", result.TopNominated.Select(n => n.NomineeName));
        }
    }

    [Fact]
    public async Task Handle_CountsApprovedCorrectly_IncludesGerrieSynced()
    {
        // Arrange
        var options = GetInMemoryOptions();

        using (var context = new PortalDbContext(options))
        {
            var today = DateTime.UtcNow;
            var submissions = new[]
            {
                new Submission
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionType = SubmissionType.EmployeeToEmployee,
                    Status = SubmissionStatus.Approved,
                    NomineeName = "Alice",
                    NomineeEmail = "alice@example.com",
                    SubmittedUtc = today,
                    SubmittedByName = "Bob",
                    SubmittedByEmail = "bob@example.com",
                    RecognitionReason = "Great"
                },
                new Submission
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionType = SubmissionType.EmployeeToEmployee,
                    Status = SubmissionStatus.GerrieSynced,
                    NomineeName = "Alice",
                    NomineeEmail = "alice@example.com",
                    SubmittedUtc = today,
                    SubmittedByName = "Charlie",
                    SubmittedByEmail = "charlie@example.com",
                    RecognitionReason = "Excellent"
                },
                new Submission
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionType = SubmissionType.EmployeeToEmployee,
                    Status = SubmissionStatus.GerrieSyncFailed,
                    NomineeName = "Alice",
                    NomineeEmail = "alice@example.com",
                    SubmittedUtc = today,
                    SubmittedByName = "David",
                    SubmittedByEmail = "david@example.com",
                    RecognitionReason = "Outstanding"
                },
                new Submission
                {
                    SubmissionId = Guid.NewGuid(),
                    SubmissionType = SubmissionType.EmployeeToEmployee,
                    Status = SubmissionStatus.Rejected,
                    NomineeName = "Alice",
                    NomineeEmail = "alice@example.com",
                    SubmittedUtc = today,
                    SubmittedByName = "Eve",
                    SubmittedByEmail = "eve@example.com",
                    RecognitionReason = "Attempted"
                }
            };

            context.Submissions.AddRange(submissions);
            await context.SaveChangesAsync();
        }

        using (var context = new PortalDbContext(options))
        {
            var query = new GetNominationReportQuery
            {
                RangeType = ReportDateRangeType.CalendarMonth,
                TypeFilter = NominationTypeFilter.All,
                TopCount = 10
            };

            var handler = new GetNominationReportQueryHandler(context);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            var alice = result.TopApproved.First();
            Assert.Equal(4, alice.TotalNominations); // All 4
            Assert.Equal(3, alice.ApprovedNominations); // Approved + GerrieSynced + GerrieSyncFailed, not Rejected
        }
    }

    [Fact]
    public async Task Handle_RanksTopApprovedByApprovedCount()
    {
        // Arrange
        var options = GetInMemoryOptions();

        using (var context = new PortalDbContext(options))
        {
            var today = DateTime.UtcNow;
            var submissions = new[]
            {
                // Alice: 3 approved
                new Submission { SubmissionId = Guid.NewGuid(), SubmissionType = SubmissionType.EmployeeToEmployee, Status = SubmissionStatus.Approved, NomineeName = "Alice", SubmittedUtc = today, SubmittedByName = "1", SubmittedByEmail = "1@test.com", RecognitionReason = "1" },
                new Submission { SubmissionId = Guid.NewGuid(), SubmissionType = SubmissionType.EmployeeToEmployee, Status = SubmissionStatus.Approved, NomineeName = "Alice", SubmittedUtc = today, SubmittedByName = "2", SubmittedByEmail = "2@test.com", RecognitionReason = "2" },
                new Submission { SubmissionId = Guid.NewGuid(), SubmissionType = SubmissionType.EmployeeToEmployee, Status = SubmissionStatus.Approved, NomineeName = "Alice", SubmittedUtc = today, SubmittedByName = "3", SubmittedByEmail = "3@test.com", RecognitionReason = "3" },
                
                // Bob: 2 approved
                new Submission { SubmissionId = Guid.NewGuid(), SubmissionType = SubmissionType.EmployeeToEmployee, Status = SubmissionStatus.Approved, NomineeName = "Bob", SubmittedUtc = today, SubmittedByName = "4", SubmittedByEmail = "4@test.com", RecognitionReason = "4" },
                new Submission { SubmissionId = Guid.NewGuid(), SubmissionType = SubmissionType.EmployeeToEmployee, Status = SubmissionStatus.Rejected, NomineeName = "Bob", SubmittedUtc = today, SubmittedByName = "5", SubmittedByEmail = "5@test.com", RecognitionReason = "5" },
                new Submission { SubmissionId = Guid.NewGuid(), SubmissionType = SubmissionType.EmployeeToEmployee, Status = SubmissionStatus.Approved, NomineeName = "Bob", SubmittedUtc = today, SubmittedByName = "6", SubmittedByEmail = "6@test.com", RecognitionReason = "6" }
            };

            context.Submissions.AddRange(submissions);
            await context.SaveChangesAsync();
        }

        using (var context = new PortalDbContext(options))
        {
            var query = new GetNominationReportQuery
            {
                RangeType = ReportDateRangeType.CalendarMonth,
                TypeFilter = NominationTypeFilter.All,
                TopCount = 10
            };

            var handler = new GetNominationReportQueryHandler(context);

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.TopApproved.Count);
            Assert.Equal("Alice", result.TopApproved[0].NomineeName); // 3 approved
            Assert.Equal("Bob", result.TopApproved[1].NomineeName); // 2 approved
        }
    }
}
