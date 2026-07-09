import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DashboardSummary,
  SubmissionsByStatus,
  ApprovalMetrics,
  RecognitionByNominee,
  SubmissionTypeAnalysis,
  AdminReviewActivity,
  NominationReport,
  ReportDateRangeType,
  NominationTypeFilter,
} from '../models/analytics.models';

@Injectable({
  providedIn: 'root',
})
export class AnalyticsService {
  private apiUrl = `${environment.apiUrl}/analytics`;

  constructor(private http: HttpClient) {}

  getDashboardSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiUrl}/dashboard-summary`);
  }

  getSubmissionsByStatus(): Observable<SubmissionsByStatus> {
    return this.http.get<SubmissionsByStatus>(
      `${this.apiUrl}/submissions-by-status`,
    );
  }

  getApprovalMetrics(): Observable<ApprovalMetrics> {
    return this.http.get<ApprovalMetrics>(`${this.apiUrl}/approval-metrics`);
  }

  getRecognitionByNominee(
    topCount: number = 10,
  ): Observable<RecognitionByNominee> {
    return this.http.get<RecognitionByNominee>(
      `${this.apiUrl}/recognition-by-nominee`,
      {
        params: { topCount },
      },
    );
  }

  getSubmissionTypeAnalysis(): Observable<SubmissionTypeAnalysis> {
    return this.http.get<SubmissionTypeAnalysis>(
      `${this.apiUrl}/submission-type-analysis`,
    );
  }

  getAdminReviewActivity(): Observable<AdminReviewActivity> {
    return this.http.get<AdminReviewActivity>(
      `${this.apiUrl}/admin-review-activity`,
    );
  }

  getNominationReport(
    rangeType: ReportDateRangeType = ReportDateRangeType.CalendarMonth,
    typeFilter: NominationTypeFilter = NominationTypeFilter.All,
    referenceDate?: Date,
    customStartDate?: Date,
    customEndDate?: Date,
    topCount: number = 10,
  ): Observable<NominationReport> {
    const params: any = {
      rangeType,
      typeFilter,
      topCount,
    };

    if (referenceDate) {
      params.referenceDate = referenceDate.toISOString();
    }
    if (customStartDate) {
      params.customStartDate = customStartDate.toISOString();
    }
    if (customEndDate) {
      params.customEndDate = customEndDate.toISOString();
    }

    return this.http.get<NominationReport>(`${this.apiUrl}/nomination-report`, {
      params,
    });
  }
}
