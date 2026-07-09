using Portal.Application.Features.Analytics.GetNominationReport;

namespace Portal.Tests;

public class DateRangeCalculatorTests
{
    [Fact]
    public void MonthToDate_ReturnsFirstOfMonthToToday()
    {
        // Arrange
        var referenceDate = new DateTime(2026, 7, 15); // July 15, 2026

        // Act
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            ReportDateRangeType.MonthToDate,
            referenceDate);

        // Assert
        Assert.Equal(new DateTime(2026, 7, 1), startUtc);
        Assert.Equal(new DateTime(2026, 7, 16), endExclusiveUtc); // Next day (exclusive)
    }

    [Fact]
    public void CalendarMonth_ReturnsFullMonth()
    {
        // Arrange
        var referenceDate = new DateTime(2026, 7, 15);

        // Act
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            ReportDateRangeType.CalendarMonth,
            referenceDate);

        // Assert
        Assert.Equal(new DateTime(2026, 7, 1), startUtc);
        Assert.Equal(new DateTime(2026, 8, 1), endExclusiveUtc); // First of next month (exclusive)
    }

    [Fact]
    public void FiscalQuarter_Q1_Apr1ToJun30()
    {
        // Arrange - May 15 is in Q1 (Apr-Jun)
        var referenceDate = new DateTime(2026, 5, 15);

        // Act
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            ReportDateRangeType.FiscalQuarter,
            referenceDate);

        // Assert
        Assert.Equal(new DateTime(2026, 4, 1), startUtc);
        Assert.Equal(new DateTime(2026, 7, 1), endExclusiveUtc);
    }

    [Fact]
    public void FiscalQuarter_Q2_Jul1ToSep30()
    {
        // Arrange - August 15 is in Q2 (Jul-Sep)
        var referenceDate = new DateTime(2026, 8, 15);

        // Act
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            ReportDateRangeType.FiscalQuarter,
            referenceDate);

        // Assert
        Assert.Equal(new DateTime(2026, 7, 1), startUtc);
        Assert.Equal(new DateTime(2026, 10, 1), endExclusiveUtc);
    }

    [Fact]
    public void FiscalQuarter_Q3_Oct1ToDec31()
    {
        // Arrange - November 15 is in Q3 (Oct-Dec)
        var referenceDate = new DateTime(2026, 11, 15);

        // Act
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            ReportDateRangeType.FiscalQuarter,
            referenceDate);

        // Assert
        Assert.Equal(new DateTime(2026, 10, 1), startUtc);
        Assert.Equal(new DateTime(2027, 1, 1), endExclusiveUtc);
    }

    [Fact]
    public void FiscalQuarter_Q4_Jan1ToMar31()
    {
        // Arrange - February 15 is in Q4 (Jan-Mar)
        var referenceDate = new DateTime(2026, 2, 15);

        // Act
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            ReportDateRangeType.FiscalQuarter,
            referenceDate);

        // Assert
        Assert.Equal(new DateTime(2026, 1, 1), startUtc);
        Assert.Equal(new DateTime(2026, 4, 1), endExclusiveUtc);
    }

    [Fact]
    public void FiscalYear_Apr1ToMar31()
    {
        // Arrange - July 15, 2026 is in FY2026 (Apr 1, 2026 - Mar 31, 2027)
        var referenceDate = new DateTime(2026, 7, 15);

        // Act
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            ReportDateRangeType.FiscalYear,
            referenceDate);

        // Assert
        Assert.Equal(new DateTime(2026, 4, 1), startUtc);
        Assert.Equal(new DateTime(2027, 4, 1), endExclusiveUtc);
    }

    [Fact]
    public void FiscalYear_BeforeMarch31_PreviousFiscalYear()
    {
        // Arrange - February 15, 2026 is in FY2025 (Apr 1, 2025 - Mar 31, 2026)
        var referenceDate = new DateTime(2026, 2, 15);

        // Act
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            ReportDateRangeType.FiscalYear,
            referenceDate);

        // Assert
        Assert.Equal(new DateTime(2025, 4, 1), startUtc);
        Assert.Equal(new DateTime(2026, 4, 1), endExclusiveUtc);
    }

    [Fact]
    public void CalendarYear_Jan1ToDec31()
    {
        // Arrange
        var referenceDate = new DateTime(2026, 7, 15);

        // Act
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            ReportDateRangeType.CalendarYear,
            referenceDate);

        // Assert
        Assert.Equal(new DateTime(2026, 1, 1), startUtc);
        Assert.Equal(new DateTime(2027, 1, 1), endExclusiveUtc);
    }

    [Fact]
    public void Custom_WithProvidedDates()
    {
        // Arrange
        var customStart = new DateTime(2026, 6, 1);
        var customEnd = new DateTime(2026, 8, 31);

        // Act
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            ReportDateRangeType.Custom,
            null,
            customStart,
            customEnd);

        // Assert
        Assert.Equal(new DateTime(2026, 6, 1), startUtc);
        Assert.Equal(new DateTime(2026, 9, 1), endExclusiveUtc); // End is exclusive
    }

    [Fact]
    public void Custom_WithoutDates_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            DateRangeCalculator.CalculateDateRange(ReportDateRangeType.Custom));
    }

    [Fact]
    public void MonthToDate_DefaultReferenceDate_UsesToday()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;

        // Act
        var (startUtc, endExclusiveUtc) = DateRangeCalculator.CalculateDateRange(
            ReportDateRangeType.MonthToDate);

        // Assert
        Assert.Equal(new DateTime(today.Year, today.Month, 1), startUtc);
        Assert.Equal(today.AddDays(1), endExclusiveUtc);
    }
}
