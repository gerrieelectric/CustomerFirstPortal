import { Component, OnDestroy, OnInit } from '@angular/core';
import { interval, Subscription } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import {
  SubmissionService,
  SubmissionListItem,
  PaginatedSubmissionsResponse,
} from '../../core/services/submission.service';

@Component({
  selector: 'app-admin-review',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CardModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    TagModule,
    MessageModule,
    SelectModule,
  ],
  template: `
    <div class="admin-review-container">
      <div class="page-header">
        <h1>Submissions List</h1>
        <p>Review recognition submissions and approve or reject them.</p>
      </div>

      <p-card styleClass="review-list-card">
        <div class="toolbar">
          <span class="p-input-icon-left search-wrap">
            <label class="sr-only" for="admin-review-search">
              Search nominee or submitter
            </label>
            <i class="pi pi-search"></i>
            <input
              id="admin-review-search"
              type="text"
              pInputText
              [value]="searchTerm"
              (input)="onSearchInput($event)"
              placeholder="Search nominee or submitter"
            />
          </span>
          <p-select
            [options]="statusOptions"
            [(ngModel)]="statusFilter"
            optionLabel="label"
            optionValue="value"
            placeholder="All Status"
            (onChange)="onFilterChange()"
            class="filter-dropdown"
          ></p-select>
          <p-select
            [options]="typeOptions"
            [(ngModel)]="typeFilter"
            optionLabel="label"
            optionValue="value"
            placeholder="All Types"
            (onChange)="onFilterChange()"
            class="filter-dropdown"
          ></p-select>
        </div>

        <p-message
          *ngIf="errorMessage"
          severity="error"
          [text]="errorMessage"
        ></p-message>

        <p-table
          [value]="submissions"
          [loading]="isLoading"
          dataKey="submissionId"
          styleClass="review-table"
          [tableStyle]="{ 'min-width': '60rem' }"
        >
          <ng-template pTemplate="header">
            <tr>
              <th
                scope="col"
                class="sortable-header"
                [attr.aria-sort]="ariaSort('nomineeName')"
              >
                <button
                  type="button"
                  class="sortable-button"
                  (click)="onSort('nomineeName')"
                  aria-label="Sort by nominee"
                >
                  <div class="header-content">
                    Nominee
                    <span class="sort-icon">
                      <i
                        *ngIf="sortField === 'nomineeName'"
                        class="pi"
                        [ngClass]="
                          sortDescending ? 'pi-arrow-down' : 'pi-arrow-up'
                        "
                      ></i>
                    </span>
                  </div>
                </button>
              </th>
              <th
                scope="col"
                class="sortable-header"
                [attr.aria-sort]="ariaSort('submittedByName')"
              >
                <button
                  type="button"
                  class="sortable-button"
                  (click)="onSort('submittedByName')"
                  aria-label="Sort by submitted by"
                >
                  <div class="header-content">
                    Submitted By
                    <span class="sort-icon">
                      <i
                        *ngIf="sortField === 'submittedByName'"
                        class="pi"
                        [ngClass]="
                          sortDescending ? 'pi-arrow-down' : 'pi-arrow-up'
                        "
                      ></i>
                    </span>
                  </div>
                </button>
              </th>
              <th
                scope="col"
                class="sortable-header"
                [attr.aria-sort]="ariaSort('submissionType')"
              >
                <button
                  type="button"
                  class="sortable-button"
                  (click)="onSort('submissionType')"
                  aria-label="Sort by type"
                >
                  <div class="header-content">
                    Type
                    <span class="sort-icon">
                      <i
                        *ngIf="sortField === 'submissionType'"
                        class="pi"
                        [ngClass]="
                          sortDescending ? 'pi-arrow-down' : 'pi-arrow-up'
                        "
                      ></i>
                    </span>
                  </div>
                </button>
              </th>
              <th scope="col">Company</th>
              <th
                scope="col"
                class="sortable-header"
                [attr.aria-sort]="ariaSort('status')"
              >
                <button
                  type="button"
                  class="sortable-button"
                  (click)="onSort('status')"
                  aria-label="Sort by status"
                >
                  <div class="header-content">
                    Status
                    <span class="sort-icon">
                      <i
                        *ngIf="sortField === 'status'"
                        class="pi"
                        [ngClass]="
                          sortDescending ? 'pi-arrow-down' : 'pi-arrow-up'
                        "
                      ></i>
                    </span>
                  </div>
                </button>
              </th>
              <th
                scope="col"
                class="sortable-header"
                [attr.aria-sort]="ariaSort('submittedUtc')"
              >
                <button
                  type="button"
                  class="sortable-button"
                  (click)="onSort('submittedUtc')"
                  aria-label="Sort by submitted date"
                >
                  <div class="header-content">
                    Submitted
                    <span class="sort-icon">
                      <i
                        *ngIf="sortField === 'submittedUtc'"
                        class="pi"
                        [ngClass]="
                          sortDescending ? 'pi-arrow-down' : 'pi-arrow-up'
                        "
                      ></i>
                    </span>
                  </div>
                </button>
              </th>
              <th scope="col">Actions</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-submission>
            <tr
              class="review-row"
              [class.review-row-actionable]="
                canOpenSubmission(submission.status)
              "
              [class.review-row-disabled]="
                !canOpenSubmission(submission.status)
              "
              (click)="onRowClick(submission)"
            >
              <td>
                <div class="primary">{{ submission.nomineeName }}</div>
              </td>
              <td>
                <div class="primary">{{ submission.submittedByName }}</div>
                <div class="secondary">{{ submission.submittedByEmail }}</div>
              </td>
              <td>{{ formatSubmissionType(submission.submissionType) }}</td>
              <td>{{ submission.companyName || '-' }}</td>
              <td>
                <p-tag
                  [value]="submission.status"
                  [severity]="statusSeverity(submission.status)"
                ></p-tag>
              </td>
              <td>{{ submission.submittedUtc | date: 'medium' }}</td>
              <td>
                <p-button
                  [label]="
                    submission.status === 'Submitted' ? 'Review' : 'View'
                  "
                  icon="pi pi-eye"
                  size="small"
                  severity="secondary"
                  (onClick)="onReviewButtonClick(submission, $event)"
                  [attr.aria-label]="
                    (submission.status === 'Submitted'
                      ? 'Review submission for '
                      : 'View submission for ') + submission.nomineeName
                  "
                  [disabled]="!canOpenSubmission(submission.status)"
                ></p-button>
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="7" class="empty-state">
                No submissions found for review.
              </td>
            </tr>
          </ng-template>
        </p-table>
      </p-card>
    </div>
  `,
  styles: [
    `
      .admin-review-container {
        max-width: 1200px;
        margin: 0 auto;
        padding: 28px 20px;
      }

      .page-header {
        margin-bottom: 24px;
      }

      .page-header h1 {
        margin: 0;
        font-size: 30px;
        color: var(--app-heading);
      }

      .page-header p {
        margin: 8px 0 0;
        color: var(--app-muted-text);
      }

      .toolbar {
        display: flex;
        gap: 12px;
        align-items: center;
        margin-bottom: 16px;
        flex-wrap: wrap;
      }

      .search-wrap {
        width: min(420px, 100%);
        position: relative;
        display: flex;
        align-items: center;
      }

      .search-wrap i {
        position: absolute;
        left: 12px;
        z-index: 1;
        color: var(--app-muted-text);
      }

      .search-wrap input {
        width: 100%;
        padding-left: 36px;
      }

      .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        padding: 0;
        margin: -1px;
        overflow: hidden;
        clip: rect(0, 0, 0, 0);
        border: 0;
      }

      .filter-dropdown {
        min-width: 140px;
      }

      .sortable-header {
        user-select: none;
        transition: background-color 0.2s;
        padding: 0;
      }

      .sortable-header:hover {
        background-color: var(--app-accent-soft);
      }

      .sortable-button {
        width: 100%;
        border: none;
        background: transparent;
        color: inherit;
        cursor: pointer;
        text-align: left;
        padding: 0.75rem;
      }

      .sortable-button:focus-visible {
        outline: 2px solid var(--app-accent);
        outline-offset: -2px;
      }

      .header-content {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      .sort-icon {
        display: inline-flex;
        align-items: center;
        color: var(--app-accent);
      }

      .primary {
        font-weight: 600;
      }

      .secondary {
        color: var(--app-muted-text);
        font-size: 0.85rem;
      }

      .empty-state {
        text-align: center;
        padding: 24px;
        color: var(--app-muted-text);
      }

      .review-row {
        cursor: default;
      }

      .review-row.review-row-actionable {
        cursor: pointer;
      }

      .review-row:hover {
        background: var(--app-accent-soft);
      }

      .review-row.review-row-disabled {
        cursor: default;
        opacity: 0.6;
      }

      .review-row.review-row-disabled:hover {
        background: transparent;
      }

      .review-row-disabled td {
        color: var(--app-muted-text);
      }

      @media (max-width: 768px) {
        .admin-review-container {
          padding: 20px 12px;
        }

        .toolbar {
          flex-direction: column;
          align-items: stretch;
        }

        .sortable-header {
          font-size: 0.9rem;
        }
      }
    `,
  ],
})
export class AdminReviewComponent implements OnInit, OnDestroy {
  submissions: SubmissionListItem[] = [];

  isLoading = false;

  searchTerm = '';
  errorMessage = '';

  sortField: string = 'submittedUtc';
  sortDescending: boolean = true;

  statusFilter: string | null = null;
  typeFilter: string | null = null;
  private filterExplicitlySet = false;

  statusOptions = [
    { label: 'All Status', value: null },
    { label: 'Submitted', value: 'Submitted' },
    { label: 'Approved', value: 'Approved' },
    { label: 'Rejected', value: 'Rejected' },
  ];

  typeOptions = [
    { label: 'All Types', value: null },
    { label: 'Employee to Employee', value: 'EmployeeToEmployee' },
    { label: 'On Behalf of Customer', value: 'EmployeeOnBehalfOfCustomer' },
    { label: 'Customer to Employee', value: 'CustomerToEmployee' },
  ];

  private readonly pageSize = 25;
  private refreshSubscription?: Subscription;
  private searchDebounceTimer?: ReturnType<typeof setTimeout>;

  constructor(
    private readonly submissionService: SubmissionService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const statusParam = this.route.snapshot.queryParamMap.get('status');
    const validStatuses = ['Submitted', 'Approved', 'Rejected'];

    // Parse query parameter: if explicitly set, use it; otherwise default to "Submitted"
    if (statusParam === 'All') {
      this.statusFilter = null;
      this.filterExplicitlySet = true;
    } else if (statusParam && validStatuses.includes(statusParam)) {
      this.statusFilter = statusParam;
      this.filterExplicitlySet = true;
    } else {
      // Default: show only "Submitted" status if no filter specified
      this.statusFilter = 'Submitted';
      this.filterExplicitlySet = false;
    }

    this.loadSubmissions();

    // Auto-refresh every 60 seconds; this helps catch updates from other admins
    this.refreshSubscription = interval(60_000).subscribe(() =>
      this.loadSubmissions(),
    );
  }

  ngOnDestroy(): void {
    this.refreshSubscription?.unsubscribe();
    if (this.searchDebounceTimer) {
      clearTimeout(this.searchDebounceTimer);
    }
  }

  onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchTerm = input.value;

    if (this.searchDebounceTimer) {
      clearTimeout(this.searchDebounceTimer);
    }

    this.searchDebounceTimer = setTimeout(() => {
      this.loadSubmissions();
    }, 300);
  }

  loadSubmissions(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.submissionService
      .listAdminSubmissions({
        pageNumber: 1,
        pageSize: this.pageSize,
        status: this.filterExplicitlySet
          ? (this.statusFilter ?? undefined)
          : this.statusFilter || 'Submitted',
        submissionType: this.typeFilter || undefined,
        searchTerm: this.searchTerm.trim() || undefined,
        sortBy: this.sortField,
        sortDescending: this.sortDescending,
      })
      .subscribe({
        next: (response: PaginatedSubmissionsResponse) => {
          this.submissions = response.items;
          this.isLoading = false;
        },
        error: (error) => {
          this.isLoading = false;
          this.submissions = [];

          if (error.status === 401 || error.status === 403) {
            this.errorMessage =
              'You do not have permission to access the review queue. ' +
              'Please verify your admin access.';
          } else if (error.status === 400) {
            this.errorMessage =
              'Invalid filter parameters. Please reset filters and try again.';
          } else if (error.status >= 500) {
            this.errorMessage =
              'Server error while loading submissions. Please try again in a moment.';
          } else {
            this.errorMessage =
              'Unable to load review queue. Please verify your connection and try again.';
          }
        },
      });
  }

  onSort(field: string): void {
    if (this.sortField === field) {
      this.sortDescending = !this.sortDescending;
    } else {
      this.sortField = field;
      this.sortDescending = true;
    }
    this.loadSubmissions();
  }

  onFilterChange(): void {
    // Mark filters as user-driven so selecting "All Status" doesn't fall back to "Submitted".
    this.filterExplicitlySet = true;
    this.loadSubmissions();
  }

  ariaSort(field: string): 'ascending' | 'descending' | 'none' {
    if (this.sortField !== field) {
      return 'none';
    }

    return this.sortDescending ? 'descending' : 'ascending';
  }

  openReview(submission: SubmissionListItem): void {
    this.router.navigate(['/admin-review', submission.submissionId]);
  }

  onRowClick(submission: SubmissionListItem): void {
    if (!this.canOpenSubmission(submission.status)) {
      return;
    }

    this.openReview(submission);
  }

  onReviewButtonClick(submission: SubmissionListItem, event: Event): void {
    event.stopPropagation();
    this.openReview(submission);
  }

  canOpenSubmission(status: string): boolean {
    return (
      status === 'Submitted' || status === 'Approved' || status === 'Rejected'
    );
  }

  statusSeverity(
    status: string,
  ): 'success' | 'secondary' | 'warn' | 'danger' | 'info' | 'contrast' {
    switch (status) {
      case 'Approved':
        return 'success';
      case 'Rejected':
        return 'danger';
      case 'Submitted':
        return 'info';
      default:
        return 'secondary';
    }
  }

  formatSubmissionType(value: string): string {
    switch (value) {
      case 'EmployeeToEmployee':
        return 'Employee to Employee';
      case 'EmployeeOnBehalfOfCustomer':
        return 'On Behalf of Customer';
      case 'CustomerToEmployee':
        return 'Customer to Employee';
      default:
        return value;
    }
  }
}
