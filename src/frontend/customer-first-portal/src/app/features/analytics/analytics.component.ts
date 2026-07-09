import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, interval } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AnalyticsService } from '../../core/services/analytics.service';
import {
  DashboardSummary,
  SubmissionsByStatus,
  ApprovalMetrics,
  RecognitionByNominee,
  SubmissionTypeAnalysis,
  AdminReviewActivity,
} from '../../core/models/analytics.models';
import { KpiCardComponent } from '../shared/components/kpi-card/kpi-card.component';
import { StatusBreakdownComponent } from './components/status-breakdown/status-breakdown.component';
import { ApprovalMetricsComponent } from './components/approval-metrics/approval-metrics.component';
import { TopNomineesComponent } from './components/top-nominees/top-nominees.component';
import { SubmissionTypeChartComponent } from './components/submission-type-chart/submission-type-chart.component';
import { NominationReportComponent } from './components/nomination-report/nomination-report.component';
import { MessageModule } from 'primeng/message';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [
    CommonModule,
    KpiCardComponent,
    StatusBreakdownComponent,
    ApprovalMetricsComponent,
    TopNomineesComponent,
    SubmissionTypeChartComponent,
    NominationReportComponent,
    MessageModule,
  ],
  templateUrl: './analytics.component.html',
  styleUrl: './analytics.component.scss',
})
export class AnalyticsComponent implements OnInit, OnDestroy {
  private readonly autoRefreshMs = 60000;
  private destroy$ = new Subject<void>();

  // Data
  dashboardSummary: DashboardSummary | null = null;
  submissionsByStatus: SubmissionsByStatus | null = null;
  approvalMetrics: ApprovalMetrics | null = null;
  recognitionByNominee: RecognitionByNominee | null = null;
  submissionTypeAnalysis: SubmissionTypeAnalysis | null = null;
  adminReviewActivity: AdminReviewActivity | null = null;

  // Loading states
  isLoading = true;
  error: string | null = null;

  constructor(private analyticsService: AnalyticsService) {}

  ngOnInit(): void {
    this.loadAnalyticsData();

    interval(this.autoRefreshMs)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        if (!this.isLoading) {
          this.loadAnalyticsData();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAnalyticsData(): void {
    this.isLoading = true;
    this.error = null;

    // Load all analytics data in parallel
    this.analyticsService
      .getDashboardSummary()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => (this.dashboardSummary = data),
        error: (err) => this.handleError('Dashboard Summary', err),
      });

    this.analyticsService
      .getSubmissionsByStatus()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => (this.submissionsByStatus = data),
        error: (err) => this.handleError('Submissions by Status', err),
      });

    this.analyticsService
      .getApprovalMetrics()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => (this.approvalMetrics = data),
        error: (err) => this.handleError('Approval Metrics', err),
      });

    this.analyticsService
      .getRecognitionByNominee(10)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => (this.recognitionByNominee = data),
        error: (err) => this.handleError('Recognition by Nominee', err),
      });

    this.analyticsService
      .getSubmissionTypeAnalysis()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => (this.submissionTypeAnalysis = data),
        error: (err) => this.handleError('Submission Type Analysis', err),
      });

    this.analyticsService
      .getAdminReviewActivity()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.adminReviewActivity = data;
          this.isLoading = false;
        },
        error: (err) => {
          this.handleError('Admin Review Activity', err);
          this.isLoading = false;
        },
      });
  }

  private handleError(section: string, error: any): void {
    console.error(`Error loading ${section}:`, error);
    this.error = `Failed to load analytics data. Please try again.`;
  }
}
