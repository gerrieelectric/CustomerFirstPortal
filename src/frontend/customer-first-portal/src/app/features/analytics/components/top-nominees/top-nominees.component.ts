import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RecognitionByNominee } from '../../../../core/models/analytics.models';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';

@Component({
  selector: 'app-top-nominees',
  standalone: true,
  imports: [CommonModule, CardModule, TableModule],
  templateUrl: './top-nominees.component.html',
  styleUrl: './top-nominees.component.scss',
})
export class TopNomineesComponent {
  @Input() data: RecognitionByNominee | null = null;

  searchTerm = '';
  departmentFilter = '';
  approvalFilter: 'all' | 'approved' | 'notApproved' = 'all';
  sortField = 'totalRecognitions';
  sortOrder = -1;

  get departmentOptions(): string[] {
    const departments = (this.data?.topNominees ?? [])
      .map((nominee) => nominee.department?.trim())
      .filter((department): department is string => !!department);

    return [...new Set(departments)].sort((a, b) => a.localeCompare(b));
  }

  get filteredNominees() {
    const nominees = this.data?.topNominees ?? [];
    const normalizedSearch = this.searchTerm.trim().toLowerCase();

    return nominees.filter((nominee) => {
      const matchesSearch =
        !normalizedSearch ||
        nominee.nomineeName.toLowerCase().includes(normalizedSearch) ||
        nominee.nomineeEmail?.toLowerCase().includes(normalizedSearch) ||
        nominee.department?.toLowerCase().includes(normalizedSearch);

      const matchesDepartment =
        !this.departmentFilter || nominee.department === this.departmentFilter;

      const matchesApproval =
        this.approvalFilter === 'all' ||
        (this.approvalFilter === 'approved' &&
          nominee.approvedRecognitions > 0) ||
        (this.approvalFilter === 'notApproved' &&
          nominee.approvedRecognitions === 0);

      return matchesSearch && matchesDepartment && matchesApproval;
    });
  }
}
