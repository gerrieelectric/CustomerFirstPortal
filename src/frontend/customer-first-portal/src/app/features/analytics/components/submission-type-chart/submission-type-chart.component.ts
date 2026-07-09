import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SubmissionTypeAnalysis } from '../../../../core/models/analytics.models';
import { CardModule } from 'primeng/card';
import { ProgressBarModule } from 'primeng/progressbar';

@Component({
  selector: 'app-submission-type-chart',
  standalone: true,
  imports: [CommonModule, CardModule, ProgressBarModule],
  templateUrl: './submission-type-chart.component.html',
  styleUrl: './submission-type-chart.component.scss',
})
export class SubmissionTypeChartComponent {
  @Input() data: SubmissionTypeAnalysis | null = null;

  getSubmissionTypeLabel(submissionType: string): string {
    switch (submissionType) {
      case 'EmployeeToEmployee':
        return 'Employee to Employee';
      case 'CustomerToEmployee':
        return 'Customer To Employee';
      default:
        return submissionType;
    }
  }
}
