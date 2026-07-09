// Dashboard Summary
export interface DashboardSummary {
  totalSubmissions: number;
  pendingReview: number;
  approved: number;
  rejected: number;
  gerrieSynced: number;
  gerrieSyncFailed: number;
  approvalRate: number;
  averageReviewTimeHours: number;
  generatedUtc: string;
}

// Submissions by Status
export interface SubmissionsByStatus {
  statuses: StatusBreakdown[];
  generatedUtc: string;
}

export interface StatusBreakdown {
  status: string;
  count: number;
  percentageOfTotal: number;
}

// Approval Metrics
export interface ApprovalMetrics {
  totalReviewed: number;
  totalApproved: number;
  totalRejected: number;
  approvalRate: number;
  rejectionRate: number;
  averageReviewTimeHours: number;
  minReviewTimeHours: number;
  maxReviewTimeHours: number;
  reviewedTodayCount: number;
  approvedTodayCount: number;
  generatedUtc: string;
}

// Recognition by Nominee
export interface RecognitionByNominee {
  topNominees: NomineeRecognitionItem[];
  generatedUtc: string;
}

export interface NomineeRecognitionItem {
  nomineeName: string;
  nomineeEmail?: string;
  department?: string;
  totalRecognitions: number;
  approvedRecognitions: number;
  averageServiceRating: number;
  mostRecentRecognition: string;
}

// Submission Type Analysis
export interface SubmissionTypeAnalysis {
  types: TypeBreakdown[];
  generatedUtc: string;
}

export interface TypeBreakdown {
  submissionType: string;
  count: number;
  approvedCount: number;
  rejectedCount: number;
  approvalRate: number;
  averageServiceRating: number;
}

// Admin Review Activity
export interface AdminReviewActivity {
  adminStats: AdminActivityItem[];
  generatedUtc: string;
}

export interface AdminActivityItem {
  adminUserId: number;
  adminName: string;
  adminEmail: string;
  totalReviewsCompleted: number;
  approvalsCount: number;
  rejectionsCount: number;
  averageReviewTimeHours: number;
  lastReviewUtc: string;
}

// Nomination Report
export interface NominationReport {
  rangeStartUtc: string;
  rangeEndUtc: string;
  rangeType: string;
  typeFilter: string;
  topNominated: NomineeReportItem[];
  topApproved: NomineeReportItem[];
  generatedUtc: string;
}

export interface NomineeReportItem {
  nomineeName: string;
  nomineeEmail?: string;
  department?: string;
  totalNominations: number;
  approvedNominations: number;
  mostRecentNominationUtc: string;
}

export enum ReportDateRangeType {
  MonthToDate = 0,
  CalendarMonth = 1,
  FiscalQuarter = 2,
  FiscalYear = 3,
  CalendarYear = 4,
  Custom = 5,
}

export enum NominationTypeFilter {
  All = 0,
  EmployeeToEmployee = 1,
  CustomerToEmployee = 2,
}
