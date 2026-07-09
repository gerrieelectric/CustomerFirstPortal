import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';
import { DatePickerModule } from 'primeng/datepicker';
import { ButtonModule } from 'primeng/button';
import { AnalyticsService } from '../../../../core/services/analytics.service';
import {
  NominationReport,
  ReportDateRangeType,
  NominationTypeFilter,
} from '../../../../core/models/analytics.models';

@Component({
  selector: 'app-nomination-report',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    SelectModule,
    CardModule,
    MessageModule,
    DatePickerModule,
    ButtonModule,
  ],
  templateUrl: './nomination-report.component.html',
  styleUrl: './nomination-report.component.scss',
})
export class NominationReportComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Expose enum to template
  readonly ReportDateRangeType = ReportDateRangeType;

  // Filters
  selectedRangeType: ReportDateRangeType = ReportDateRangeType.CalendarMonth;
  selectedTypeFilter: NominationTypeFilter = NominationTypeFilter.All;
  referenceDate: Date | null = null;
  customStartDate: Date | null = null;
  customEndDate: Date | null = null;

  // Data
  report: NominationReport | null = null;
  isLoading = false;
  error: string | null = null;

  // Enum options for dropdowns
  readonly rangeTypeOptions = [
    { label: 'Month to Date', value: ReportDateRangeType.MonthToDate },
    { label: 'Calendar Month', value: ReportDateRangeType.CalendarMonth },
    { label: 'Fiscal Quarter', value: ReportDateRangeType.FiscalQuarter },
    { label: 'Fiscal Year', value: ReportDateRangeType.FiscalYear },
    { label: 'Calendar Year', value: ReportDateRangeType.CalendarYear },
    { label: 'Custom Date Range', value: ReportDateRangeType.Custom },
  ];

  readonly typeFilterOptions = [
    { label: 'All Nominations', value: NominationTypeFilter.All },
    {
      label: 'Employee to Employee',
      value: NominationTypeFilter.EmployeeToEmployee,
    },
    {
      label: 'Customer to Employee',
      value: NominationTypeFilter.CustomerToEmployee,
    },
  ];

  constructor(private analyticsService: AnalyticsService) {}

  ngOnInit(): void {
    this.loadReport();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onFilterChange(): void {
    // Validate custom date range before loading
    if (this.selectedRangeType === ReportDateRangeType.Custom) {
      if (!this.customStartDate || !this.customEndDate) {
        this.error = 'Please select both start and end dates for custom range.';
        this.report = null;
        return;
      }
      if (this.customStartDate > this.customEndDate) {
        this.error = 'Start date must be on or before end date.';
        this.report = null;
        return;
      }
    }
    this.loadReport();
  }

  private loadReport(): void {
    this.isLoading = true;
    this.error = null;

    this.analyticsService
      .getNominationReport(
        this.selectedRangeType,
        this.selectedTypeFilter,
        this.referenceDate ?? undefined,
        this.customStartDate ?? undefined,
        this.customEndDate ?? undefined,
        10,
      )
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.report = data;
          this.isLoading = false;
        },
        error: (err) => {
          this.error = err.error?.message || 'Failed to load nomination report';
          this.isLoading = false;
        },
      });
  }

  get showCustomDateInputs(): boolean {
    return this.selectedRangeType === ReportDateRangeType.Custom;
  }

  get showReferenceDateInput(): boolean {
    return this.selectedRangeType !== ReportDateRangeType.Custom;
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString();
  }

  formatDateTime(dateStr: string): string {
    return new Date(dateStr).toLocaleString();
  }

  exportTopNominatedCsv(): void {
    if (!this.report?.topNominated) return;

    const csv = this.generateCsv(
      'Most Nominated Employees',
      this.report.topNominated,
      this.report,
    );
    this.downloadCsv(csv, 'top-nominated-employees.csv');
  }

  exportTopApprovedCsv(): void {
    if (!this.report?.topApproved) return;

    const csv = this.generateCsv(
      'Most Approved Nominations',
      this.report.topApproved,
      this.report,
    );
    this.downloadCsv(csv, 'top-approved-nominations.csv');
  }

  exportAllReportsCsv(): void {
    if (!this.report) return;

    let csv = '';

    // Add report metadata
    csv += `Nomination Reports Export\n`;
    csv += `Generated: ${this.formatDateTime(this.report.generatedUtc)}\n`;
    csv += `Report Period: ${this.formatDate(this.report.rangeStartUtc)} to ${this.formatDate(this.report.rangeEndUtc)}\n`;
    csv += `Date Range Type: ${this.report.rangeType}\n`;
    csv += `Nomination Type: ${this.report.typeFilter}\n`;
    csv += '\n\n';

    // Export Most Nominated
    csv += this.generateCsv(
      'Most Nominated Employees',
      this.report.topNominated,
      this.report,
    );
    csv += '\n\n';

    // Export Most Approved
    csv += this.generateCsv(
      'Most Approved Nominations',
      this.report.topApproved,
      this.report,
    );

    this.downloadCsv(csv, 'nomination-reports.csv');
  }

  private generateCsv(
    title: string,
    data: any[],
    report: NominationReport,
  ): string {
    let csv = '';

    // Add title and metadata
    csv += `${title}\n`;
    csv += `Report Period: ${this.formatDate(report.rangeStartUtc)} to ${this.formatDate(report.rangeEndUtc)}\n`;
    csv += `Nomination Type: ${report.typeFilter}\n`;
    csv += `Generated: ${this.formatDateTime(report.generatedUtc)}\n`;
    csv += '\n';

    // Add headers
    csv +=
      'Name,Email,Department,Total Nominations,Approved Nominations,Most Recent Nomination Date\n';

    // Add rows
    data.forEach((item) => {
      const escapedName = this.escapeCsvField(item.nomineeName);
      const escapedEmail = this.escapeCsvField(item.nomineeEmail || '');
      const escapedDept = this.escapeCsvField(item.department || '');
      const mostRecent = new Date(
        item.mostRecentNominationUtc,
      ).toLocaleDateString();

      csv += `${escapedName},${escapedEmail},${escapedDept},${item.totalNominations},${item.approvedNominations},${mostRecent}\n`;
    });

    return csv;
  }

  private escapeCsvField(field: string): string {
    if (!field) return '';
    // If field contains comma, newline, or double quote, wrap in quotes and escape quotes
    if (field.includes(',') || field.includes('\n') || field.includes('"')) {
      return `"${field.replace(/"/g, '""')}"`;
    }
    return field;
  }

  private downloadCsv(csv: string, filename: string): void {
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
}
