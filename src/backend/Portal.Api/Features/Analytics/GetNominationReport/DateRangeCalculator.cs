namespace Portal.Application.Features.Analytics.GetNominationReport;

/// <summary>
/// Utility for calculating date ranges for reports.
/// Fiscal year starts April 1.
/// </summary>
public static class DateRangeCalculator
{
    private const int FiscalYearStartMonth = 4; // April

    /// <summary>
    /// Calculates the date range for a given report type.
    /// </summary>
    /// <param name="rangeType">The type of date range (preset or custom).</param>
    /// <param name="referenceDate">The date to use for calculating presets (defaults to today UTC).</param>
    /// <param name="customStartDate">Required if rangeType is Custom.</param>
    /// <param name="customEndDate">Required if rangeType is Custom.</param>
    /// <returns>Tuple of (startUtc, endExclusiveUtc). End date is exclusive.</returns>
    public static (DateTime startUtc, DateTime endExclusiveUtc) CalculateDateRange(
        ReportDateRangeType rangeType,
        DateTime? referenceDate = null,
        DateTime? customStartDate = null,
        DateTime? customEndDate = null)
    {
        var refDate = referenceDate?.Date ?? DateTime.UtcNow.Date;

        return rangeType switch
        {
            ReportDateRangeType.MonthToDate =>
                GetMonthToDate(refDate),

            ReportDateRangeType.CalendarMonth =>
                GetCalendarMonth(refDate),

            ReportDateRangeType.FiscalQuarter =>
                GetFiscalQuarter(refDate),

            ReportDateRangeType.FiscalYear =>
                GetFiscalYear(refDate),

            ReportDateRangeType.CalendarYear =>
                GetCalendarYear(refDate),

            ReportDateRangeType.Custom =>
                GetCustomRange(customStartDate, customEndDate),

            _ => throw new ArgumentException($"Unknown range type: {rangeType}")
        };
    }

    private static (DateTime, DateTime) GetMonthToDate(DateTime referenceDate)
    {
        var startUtc = new DateTime(referenceDate.Year, referenceDate.Month, 1);
        var endExclusiveUtc = referenceDate.AddDays(1);
        return (startUtc, endExclusiveUtc);
    }

    private static (DateTime, DateTime) GetCalendarMonth(DateTime referenceDate)
    {
        var startUtc = new DateTime(referenceDate.Year, referenceDate.Month, 1);
        var endExclusiveUtc = startUtc.AddMonths(1);
        return (startUtc, endExclusiveUtc);
    }

    private static (DateTime, DateTime) GetFiscalQuarter(DateTime referenceDate)
    {
        var (fiscalYear, quarterInYear) = GetFiscalYearAndQuarter(referenceDate);

        // Calculate start month of the quarter
        // Q1: Apr (month 4), Q2: Jul (month 7), Q3: Oct (month 10), Q4: Jan (month 1)
        int startMonth = (quarterInYear - 1) * 3 + FiscalYearStartMonth;
        int startYear = fiscalYear;

        if (startMonth > 12)
        {
            startMonth -= 12;
            startYear += 1;
        }

        var startUtc = new DateTime(startYear, startMonth, 1);
        var endExclusiveUtc = startUtc.AddMonths(3);
        return (startUtc, endExclusiveUtc);
    }

    private static (DateTime, DateTime) GetFiscalYear(DateTime referenceDate)
    {
        var (fiscalYear, _) = GetFiscalYearAndQuarter(referenceDate);

        // Fiscal year starts in April of the fiscal year label
        var startUtc = new DateTime(fiscalYear, FiscalYearStartMonth, 1);
        var endExclusiveUtc = startUtc.AddYears(1);
        return (startUtc, endExclusiveUtc);
    }

    private static (DateTime, DateTime) GetCalendarYear(DateTime referenceDate)
    {
        var startUtc = new DateTime(referenceDate.Year, 1, 1);
        var endExclusiveUtc = startUtc.AddYears(1);
        return (startUtc, endExclusiveUtc);
    }

    private static (DateTime, DateTime) GetCustomRange(DateTime? customStartDate, DateTime? customEndDate)
    {
        if (!customStartDate.HasValue || !customEndDate.HasValue)
        {
            throw new ArgumentException("Custom range requires both start and end dates.");
        }

        var startUtc = customStartDate.Value.Date;
        var endExclusiveUtc = customEndDate.Value.Date.AddDays(1);
        return (startUtc, endExclusiveUtc);
    }

    /// <summary>
    /// Determines the fiscal year and quarter number (1-4) for a given date.
    /// Fiscal year is labeled by its start year (e.g., FY2026 starts Apr 1, 2026).
    /// </summary>
    private static (int fiscalYear, int quarterInYear) GetFiscalYearAndQuarter(DateTime referenceDate)
    {
        int year = referenceDate.Year;
        int month = referenceDate.Month;

        if (month < FiscalYearStartMonth)
        {
            // January–March belong to previous fiscal year
            year--;
        }

        int quarterInYear = (month - FiscalYearStartMonth + 12) % 12 / 3 + 1;

        return (year, quarterInYear);
    }
}
