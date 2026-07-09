import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SubmissionsByStatus } from '../../../../core/models/analytics.models';
import { CardModule } from 'primeng/card';
import { ProgressBarModule } from 'primeng/progressbar';

@Component({
  selector: 'app-status-breakdown',
  standalone: true,
  imports: [CommonModule, CardModule, ProgressBarModule],
  templateUrl: './status-breakdown.component.html',
  styleUrl: './status-breakdown.component.scss',
})
export class StatusBreakdownComponent {
  @Input() data: SubmissionsByStatus | null = null;
}
