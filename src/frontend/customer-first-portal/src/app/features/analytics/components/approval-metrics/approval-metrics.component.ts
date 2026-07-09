import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApprovalMetrics } from '../../../../core/models/analytics.models';
import { CardModule } from 'primeng/card';

@Component({
  selector: 'app-approval-metrics',
  standalone: true,
  imports: [CommonModule, CardModule],
  templateUrl: './approval-metrics.component.html',
  styleUrl: './approval-metrics.component.scss',
})
export class ApprovalMetricsComponent {
  @Input() data: ApprovalMetrics | null = null;
}
