import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminReviewActivity } from '../../../../core/models/analytics.models';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';

@Component({
  selector: 'app-admin-activity',
  standalone: true,
  imports: [CommonModule, CardModule, TableModule],
  templateUrl: './admin-activity.component.html',
  styleUrl: './admin-activity.component.scss',
})
export class AdminActivityComponent {
  @Input() data: AdminReviewActivity | null = null;
}
