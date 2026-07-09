import {
  Component,
  OnInit,
  HostListener,
  DestroyRef,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
  FormGroup,
  FormControl,
} from '@angular/forms';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TextareaModule } from 'primeng/textarea';
import { MessageModule } from 'primeng/message';
import { TooltipModule } from 'primeng/tooltip';
import {
  AdminReviewRequest,
  SubmissionResponse,
  AttachmentListItem,
  SubmissionService,
  SuggestedEditsResponse,
} from '../../core/services/submission.service';

@Component({
  selector: 'app-admin-review-detail',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    ButtonModule,
    TextareaModule,
    MessageModule,
    TooltipModule,
  ],
  template: `
    <div class="admin-review-detail-container">
      <div class="page-header">
        <h1>
          {{ isReadOnlyView ? 'Submission Details' : 'Review Submission' }}
        </h1>
        <p>
          {{
            isReadOnlyView
              ? 'This submission has already been reviewed. You can view the details, but the form is read only.'
              : 'Approve or reject the submission. Final Recognition Reason is required for approval.'
          }}
        </p>
      </div>

      <p-message
        *ngIf="errorMessage"
        severity="error"
        [text]="errorMessage"
        styleClass="error-message"
      ></p-message>
      <p-message
        *ngIf="successMessage"
        severity="success"
        [text]="successMessage"
      ></p-message>

      <p-card *ngIf="isLoading" styleClass="review-card">
        <ng-template pTemplate="content">
          <div class="loading-state">
            <i class="pi pi-spin pi-spinner"></i>
            Loading submission details...
          </div>
        </ng-template>
      </p-card>

      <p-card
        *ngIf="!isLoading && submission"
        styleClass="review-card"
        [ngClass]="{ 'stale-submission': isStaleData }"
      >
        <ng-template pTemplate="title">
          {{ submission.nomineeName }}
          <span *ngIf="isStaleData" class="stale-badge">
            <i class="pi pi-exclamation-triangle"></i> Stale
          </span>
        </ng-template>
        <ng-template pTemplate="subtitle">
          Submitted by {{ submission.submittedByName }}
          <span *ngIf="submission.submittedUtc" class="submission-time">
            on {{ submission.submittedUtc | date: 'medium' }}
          </span>
        </ng-template>

        <form [formGroup]="reviewForm">
          <div class="detail-grid">
            <div
              *ngIf="
                submission.submissionType === 'CustomerToEmployee' ||
                submission.submissionType === 'EmployeeOnBehalfOfCustomer'
              "
              class="customer-details"
            >
              <div class="label">Customer Details</div>
              <div class="customer-details-grid">
                <div>
                  <span class="secondary-label">Company</span>
                  <div class="meta-value">
                    {{ submission.companyName || '-' }}
                  </div>
                </div>
                <div>
                  <span class="secondary-label">Contact Name</span>
                  <div class="meta-value">
                    {{ submission.customerName || '-' }}
                  </div>
                </div>
                <div>
                  <span class="secondary-label">Contact Email</span>
                  <div class="meta-value">
                    {{ submission.customerEmail || '-' }}
                  </div>
                </div>
              </div>
            </div>

            <div>
              <div class="label">Original Recognition Reason</div>
              <p class="reason" [innerHTML]="highlightOriginalMistakes()"></p>
            </div>

            <div>
              <div class="label">
                Suggested Recognition Reason
                <span class="secondary-label">
                  {{ suggestedEdits?.modelName }} • Generated
                  {{ suggestedEdits?.generatedUtc | date: 'short' }}
                </span>
              </div>
              <div
                *ngIf="suggestedEdits && !isReadOnlyView"
                class="highlighted-text-display"
                [innerHTML]="highlightSuggestedCorrections()"
              ></div>
            </div>

            <div
              [ngClass]="{
                'edited-reason-required': showEditedReasonRequiredState,
              }"
            >
              <div class="label">
                Final Recognition Reason
                <span *ngIf="showEditedReasonRequiredState" class="required">
                  (required for Approve)
                </span>
                <span *ngIf="currentDecision !== 'Approved'" class="optional"
                  >(optional)
                </span>
              </div>
              <textarea
                rows="4"
                pTextarea
                [formControl]="editedReasonControl"
                [readonly]="isReadOnlyView"
                placeholder="Provide the final recognition text used when approving this submission"
              ></textarea>
              <div
                *ngIf="editedReasonValidationError"
                class="validation-message"
              >
                {{ editedReasonValidationError }}
              </div>
            </div>

            <div
              *ngIf="
                submission.attachments && submission.attachments.length > 0
              "
            >
              <div class="label">Attachments</div>
              <div class="attachments-list">
                <div
                  class="attachment-item"
                  *ngFor="let attachment of submission.attachments"
                >
                  <div class="attachment-info">
                    <i class="pi pi-file"></i>
                    <div class="attachment-details">
                      <div class="attachment-name">
                        {{ attachment.originalFileName }}
                      </div>
                      <div class="attachment-meta">
                        {{ formatFileSize(attachment.fileSizeBytes) }} •
                        Uploaded by {{ attachment.uploadedByName }} on
                        {{ attachment.uploadedUtc | date: 'short' }}
                      </div>
                    </div>
                  </div>
                  <p-button
                    icon="pi pi-download"
                    [rounded]="true"
                    [text]="true"
                    severity="info"
                    (onClick)="
                      downloadAttachment(
                        submission.submissionId,
                        attachment.submissionAttachmentId,
                        attachment.originalFileName
                      )
                    "
                    [loading]="
                      downloadingAttachmentId ===
                      attachment.submissionAttachmentId
                    "
                    pTooltip="Download file"
                    tooltipPosition="top"
                  ></p-button>
                </div>
              </div>
            </div>

            <div>
              <div class="label">
                Decision Notes
                <span class="optional">(optional)</span>
              </div>
              <textarea
                rows="4"
                pTextarea
                [formControl]="notesControl"
                [readonly]="isReadOnlyView"
                placeholder="Add any notes or comments for the review decision"
              ></textarea>
              <div *ngIf="notesControl.invalid" class="validation-message">
                Notes cannot exceed 1000 characters
              </div>
            </div>
          </div>

          <div
            class="review-actions"
            *ngIf="!isReadOnlyView; else readOnlyActions"
          >
            <p-button
              label="Reject"
              severity="danger"
              icon="pi pi-times"
              (onClick)="setCurrentDecisionAndSubmit('Rejected')"
              [loading]="isSubmitting"
              [disabled]="isStaleData || isSubmitting"
            ></p-button>

            <p-button
              label="Approve"
              severity="success"
              icon="pi pi-check"
              (onClick)="setCurrentDecisionAndSubmit('Approved')"
              [loading]="isSubmitting"
              [disabled]="
                isStaleData ||
                isSubmitting ||
                (currentDecision === 'Approved' && editedReasonValidationError)
              "
            ></p-button>

            <p-button
              label="Back to Queue"
              severity="secondary"
              variant="outlined"
              (onClick)="goBack()"
              [disabled]="isSubmitting || hasUnsavedChanges()"
            ></p-button>
          </div>

          <ng-template #readOnlyActions>
            <div class="review-actions">
              <p-button
                label="Back to Queue"
                severity="secondary"
                variant="outlined"
                (onClick)="goBack()"
              ></p-button>
            </div>
          </ng-template>
        </form>
      </p-card>
    </div>
  `,
  styles: [
    `
      .admin-review-detail-container {
        max-width: 960px;
        margin: 0 auto;
        padding: 28px 20px;
      }

      .page-header {
        margin-bottom: 20px;
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

      .review-card {
        margin-top: 16px;
      }

      .review-card.stale-submission {
        border-left: 4px solid #ff9800;
      }

      .loading-state {
        display: flex;
        align-items: center;
        gap: 12px;
        justify-content: center;
        padding: 24px;
        color: var(--app-muted-text);
      }

      .stale-badge {
        margin-left: 8px;
        padding: 4px 8px;
        background: #fff3cd;
        border: 1px solid #ffc107;
        border-radius: 4px;
        font-size: 0.85rem;
        color: #856404;
        display: inline-flex;
        align-items: center;
        gap: 4px;
      }

      .submission-time {
        display: block;
        margin-top: 4px;
        font-size: 0.9rem;
        color: var(--app-muted-text);
        font-weight: normal;
      }

      .error-message {
        margin-bottom: 16px;
      }

      .detail-grid {
        display: grid;
        gap: 16px;
      }

      .label {
        font-size: 0.85rem;
        font-weight: 600;
        margin-bottom: 8px;
        color: var(--app-text);
      }

      .label .optional {
        font-weight: normal;
        color: var(--app-muted-text);
        font-style: italic;
      }

      .label .required {
        color: #d32f2f;
        margin-left: 2px;
      }

      .label .secondary-label {
        display: block;
        font-weight: normal;
        color: var(--app-muted-text);
        font-size: 0.8rem;
        margin-top: 4px;
        font-style: italic;
      }

      .customer-details {
        border: 1px solid var(--app-border);
        border-radius: var(--app-control-radius);
        background: var(--app-surface-muted);
        padding: 12px;
      }

      .customer-details-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 12px;
      }

      .customer-details-grid .secondary-label {
        font-size: 0.8rem;
        color: var(--app-muted-text);
      }

      .meta-value {
        font-weight: 500;
        color: var(--app-text);
      }

      .reason {
        margin: 0;
        padding: 12px;
        border-radius: var(--app-control-radius);
        background: var(--app-surface-muted);
        border: 1px solid var(--app-border);
        line-height: 1.5;
      }

      .highlighted-text-display {
        margin: 0 0 8px 0;
        padding: 12px;
        border-radius: var(--app-control-radius);
        background: var(--app-surface-muted);
        border: 1px solid var(--app-border);
        line-height: 1.5;
        font-size: 0.9rem;
        color: var(--app-text);
        word-break: break-word;
        white-space: pre-wrap;
      }

      .attachments-list {
        display: flex;
        flex-direction: column;
        gap: 12px;
      }

      .attachment-item {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 12px;
        border: 1px solid var(--app-border);
        border-radius: var(--app-control-radius);
        background: var(--app-surface);
        transition: background-color 0.2s ease;
      }

      .attachment-item:hover {
        background: var(--app-accent-soft);
      }

      .attachment-info {
        display: flex;
        align-items: flex-start;
        gap: 12px;
        flex: 1;
        min-width: 0;
      }

      .attachment-info i {
        font-size: 1.5rem;
        color: var(--app-muted-text);
        margin-top: 2px;
        flex-shrink: 0;
      }

      .attachment-details {
        flex: 1;
        min-width: 0;
      }

      .attachment-name {
        font-weight: 500;
        color: var(--app-text);
        word-break: break-word;
        margin-bottom: 4px;
      }

      .attachment-meta {
        font-size: 0.85rem;
        color: var(--app-muted-text);
      }

      .detail-grid textarea {
        display: block;
        width: 100%;
        border: 1px solid var(--app-border);
        border-radius: var(--app-control-radius);
        padding: 8px 12px;
        background: var(--app-surface-muted);
        color: var(--app-text);
      }

      .detail-grid textarea:focus {
        outline: none;
        border-color: var(--app-accent);
        box-shadow: 0 0 0 3px
          color-mix(in srgb, var(--app-accent) 22%, transparent);
      }

      .detail-grid .edited-reason-required .label {
        background: #fff3cd;
        padding: 6px 8px;
        border-radius: 4px;
      }

      .validation-message {
        margin-top: 4px;
        font-size: 0.8rem;
        color: #d32f2f;
        display: flex;
        align-items: center;
        gap: 4px;
      }

      .review-actions {
        display: flex;
        gap: 10px;
        flex-wrap: wrap;
        justify-content: flex-end;
        margin-top: 20px;
        padding-top: 16px;
        border-top: 1px solid var(--app-border);
      }

      @media (max-width: 768px) {
        .admin-review-detail-container {
          padding: 20px 12px;
        }

        .review-actions {
          flex-direction: column;
        }

        .review-actions p-button {
          width: 100%;
        }
      }
    `,
  ],
})
export class AdminReviewDetailComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  submission: SubmissionResponse | null = null;
  suggestedEdits: SuggestedEditsResponse | null = null;
  submissionId = '';
  originalSubmittedUtc: string | null = null;

  isLoading = false;
  isSubmitting = false;
  isStaleData = false;
  currentDecision: 'Approved' | 'Rejected' | null = null;
  isLoadingSuggestions = false;
  isRegeneratingSuggestions = false;

  errorMessage = '';
  successMessage = '';
  staleDateWarning = '';
  editedReasonValidationError = '';
  downloadingAttachmentId: string | null = null;

  readonly reviewForm: FormGroup;
  private retryCount = 0;
  private readonly maxRetries = 3;
  private readonly retryDelayMs = 1000;
  private readonly timeoutHandles = new Set<ReturnType<typeof setTimeout>>();

  // Getter properties to avoid strict mode form control access issues
  get notesControl(): FormControl {
    return this.reviewForm.get('notes') as FormControl;
  }

  get editedReasonControl(): FormControl {
    return this.reviewForm.get('editedRecognitionReason') as FormControl;
  }

  get showEditedReasonRequiredState(): boolean {
    return (
      this.currentDecision === 'Approved' &&
      !(this.editedReasonControl.value?.trim() || '')
    );
  }

  get isReadOnlyView(): boolean {
    return (
      this.submission?.status === 'Approved' ||
      this.submission?.status === 'Rejected'
    );
  }

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly submissionService: SubmissionService,
    private readonly fb: FormBuilder,
    private readonly sanitizer: DomSanitizer,
  ) {
    this.reviewForm = this.fb.group({
      notes: ['', [Validators.maxLength(1000)]],
      editedRecognitionReason: ['', [Validators.maxLength(2000)]],
    });

    this.destroyRef.onDestroy(() => {
      this.timeoutHandles.forEach((handle) => clearTimeout(handle));
      this.timeoutHandles.clear();
    });

    // Listen to form changes to track unsaved changes
    this.reviewForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.updateValidationStates();
      });
  }

  ngOnInit(): void {
    this.submissionId = this.route.snapshot.paramMap.get('submissionId') ?? '';

    if (!this.submissionId) {
      this.errorMessage =
        'Submission ID is missing. Unable to load submission.';
      return;
    }

    this.loadSubmission();
  }

  @HostListener('window:beforeunload', ['$event'])
  unloadNotification($event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) {
      $event.returnValue = true;
    }
  }

  hasUnsavedChanges(): boolean {
    return this.reviewForm.dirty;
  }

  updateValidationStates(): void {
    const editedReason = this.editedReasonControl.value?.trim() || '';

    // Clear error if not required (user didn't select Approved)
    if (this.currentDecision !== 'Approved') {
      this.editedReasonValidationError = '';
    } else if (this.currentDecision === 'Approved') {
      // Validate when Approved is selected
      if (!editedReason) {
        this.editedReasonValidationError =
          'Final recognition reason is required for Approve.';
      } else {
        this.editedReasonValidationError = '';
      }
    }
  }

  loadSubmission(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.staleDateWarning = '';

    this.submissionService
      .getSubmission(this.submissionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (submission) => {
          this.submission = submission;
          this.originalSubmittedUtc =
            submission.submittedUtc || submission.createdAt || null;
          this.syncFormMode();
          this.checkForStaleData();
          this.isLoading = false;
          this.retryCount = 0;

          // Load suggestions after loading submission
          this.loadSuggestedEdits();
        },
        error: (error) => {
          this.handleLoadError(error);
        },
      });
  }

  loadSuggestedEdits(): void {
    if (!this.submissionId) {
      return;
    }

    this.isLoadingSuggestions = true;
    this.suggestedEdits = null;

    this.submissionService
      .getSuggestedEdits(this.submissionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (suggestions) => {
          this.suggestedEdits = suggestions;
          this.prepopulateEditedReasonFromSuggestions();
          this.isLoadingSuggestions = false;
        },
        error: (error) => {
          this.isLoadingSuggestions = false;
          // Silently fail for suggestions - don't block the page
          // Only log if there's an unexpected error (not 503 or 404)
          if (error.status !== 503 && error.status !== 404) {
            console.warn('Failed to load suggestions:', error);
          }
          this.suggestedEdits = null;
        },
      });
  }

  regenerateSuggestions(): void {
    if (!this.submissionId || this.isRegeneratingSuggestions) {
      return;
    }

    if (
      !confirm(
        'This will regenerate suggestions for this submission. Continue?',
      )
    ) {
      return;
    }

    this.isRegeneratingSuggestions = true;

    this.submissionService
      .regenerateSuggestions(this.submissionId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (suggestions) => {
          this.suggestedEdits = suggestions;
          this.prepopulateEditedReasonFromSuggestions();
          this.isRegeneratingSuggestions = false;
          this.successMessage = 'Suggestions regenerated successfully.';
          this.scheduleTimeout(() => {
            this.successMessage = '';
          }, 3000);
        },
        error: (error) => {
          this.isRegeneratingSuggestions = false;
          this.errorMessage =
            'Failed to regenerate suggestions. Please try again.';
          console.error('Regeneration error:', error);
        },
      });
  }

  checkForStaleData(): void {
    if (!this.submission || !this.originalSubmittedUtc) {
      return;
    }

    this.isStaleData = false;
    this.staleDateWarning = '';

    // Check if submission was modified in the last 30 seconds by checking timestamp
    const submittedTime = new Date(this.originalSubmittedUtc);
    const thirtySecondsAgo = new Date(Date.now() - 30000);

    if (this.isReadOnlyView) {
      this.staleDateWarning = `This submission has already been ${this.submission.status.toLowerCase()}. You can still view the details, but editing is disabled.`;
      return;
    }

    if (this.submission.status !== 'Submitted') {
      this.isStaleData = true;
      this.staleDateWarning = `This submission is no longer available for review because it is currently ${this.submission.status.toLowerCase()}.`;
    } else if (submittedTime < thirtySecondsAgo) {
      // Warn if submission is very old (suspicious)
      // this.staleDateWarning =
      //   'Note: This submission has been pending review for a while. Consider checking if another admin already reviewed it.';
    }
  }

  syncFormMode(): void {
    if (this.isReadOnlyView) {
      this.reviewForm.disable({ emitEvent: false });
      this.reviewForm.markAsPristine();
      this.currentDecision = null;
      this.editedReasonValidationError = '';
      return;
    }

    this.reviewForm.enable({ emitEvent: false });
  }

  handleLoadError(error: any): void {
    this.isLoading = false;

    // Determine specific error message based on response
    if (error.status === 404) {
      this.errorMessage =
        'Submission not found. It may have been deleted or archived.';
    } else if (error.status === 401 || error.status === 403) {
      this.errorMessage =
        'You do not have permission to review this submission. Please verify your admin access.';
    } else if (error.status === 409) {
      this.errorMessage =
        'This submission has already been reviewed by another admin. Please return to the queue and refresh.';
    } else if (this.retryCount < this.maxRetries) {
      this.retryCount++;
      this.errorMessage = `Connection error (attempt ${this.retryCount}/${this.maxRetries}). Retrying in ${this.retryDelayMs / 1000}s...`;
      this.scheduleTimeout(() => this.loadSubmission(), this.retryDelayMs);
    } else {
      this.errorMessage =
        'Unable to load submission details after multiple attempts. ' +
        'Please check your connection and return to the queue to try again.';
    }
  }

  setCurrentDecisionAndSubmit(decision: 'Approved' | 'Rejected'): void {
    this.currentDecision = decision;
    this.updateValidationStates();

    // Only proceed if validation passes
    if (decision === 'Approved' && this.editedReasonValidationError) {
      return;
    }

    this.submitReview(decision);
  }

  submitReview(decision: AdminReviewRequest['reviewDecision']): void {
    if (
      !this.submissionId ||
      this.isSubmitting ||
      this.isStaleData ||
      this.isReadOnlyView
    ) {
      return;
    }

    // Validate form
    const editedRecognitionReason = this.editedReasonControl.value?.trim();

    if (decision === 'Approved' && !editedRecognitionReason) {
      this.errorMessage = 'Final recognition reason is required for Approve.';
      return;
    }

    if (!this.reviewForm.valid) {
      this.errorMessage = 'Please fix validation errors before submitting.';
      return;
    }

    const rowVersion = this.submission?.rowVersion;
    if (!rowVersion) {
      this.errorMessage =
        'Submission version is missing. Refresh the page and try again.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const payload: AdminReviewRequest = {
      reviewDecision: decision,
      rowVersion,
      notes: this.notesControl.value?.trim() || undefined,
      editedRecognitionReason: editedRecognitionReason || undefined,
    };

    this.submissionService
      .reviewSubmission(this.submissionId, payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.handleReviewSuccess(decision, response);
        },
        error: (error) => {
          this.handleReviewError(error, decision);
        },
      });
  }

  private prepopulateEditedReasonFromSuggestions(): void {
    if (this.isReadOnlyView) {
      return;
    }

    const suggestedText = this.suggestedEdits?.fullEditedReason?.trim();
    if (!suggestedText) {
      return;
    }

    const existingEditedReason = this.editedReasonControl.value?.trim();
    if (existingEditedReason) {
      return;
    }

    this.editedReasonControl.setValue(suggestedText, { emitEvent: false });
    this.updateValidationStates();
  }

  handleReviewSuccess(decision: string, response: SubmissionResponse): void {
    this.isSubmitting = false;
    this.successMessage = `Submission ${decision === 'Rejected' ? 'rejected' : 'approved'} successfully.`;
    this.reviewForm.markAsPristine();

    // Auto-navigate back after brief delay so user sees success message
    this.scheduleTimeout(() => {
      this.goBack();
    }, 1500);
  }

  private scheduleTimeout(callback: () => void, delayMs: number): void {
    const handle = setTimeout(() => {
      this.timeoutHandles.delete(handle);
      callback();
    }, delayMs);

    this.timeoutHandles.add(handle);
  }

  handleReviewError(error: any, decision: string): void {
    this.isSubmitting = false;

    if (error.status === 404) {
      this.errorMessage =
        'Submission not found. It may have been deleted. Please return to the queue.';
    } else if (error.status === 409) {
      this.errorMessage =
        'This submission was already reviewed by another admin. ' +
        'Please return to the queue and refresh to see the latest status.';
    } else if (error.status === 400 && error.error?.message) {
      // Backend validation error
      this.errorMessage = `Cannot ${decision}: ${error.error.message}`;
    } else if (error.status === 401 || error.status === 403) {
      this.errorMessage =
        'You do not have permission to review submissions. Please verify your admin access.';
    } else if (error.status >= 500) {
      this.errorMessage =
        'Server error while processing your review. Please try again in a moment.';
    } else {
      this.errorMessage =
        'Unable to submit review decision. Please check your connection and try again.';
    }
  }

  goBack(): void {
    if (this.hasUnsavedChanges()) {
      if (
        !confirm(
          'You have unsaved changes. Are you sure you want to leave without saving?',
        )
      ) {
        return;
      }
    }
    this.router.navigate(['/admin-review']);
  }

  highlightOriginalMistakes(): SafeHtml {
    if (
      !this.submission?.recognitionReason ||
      !this.suggestedEdits?.suggestions ||
      this.suggestedEdits.suggestions.length === 0
    ) {
      return this.sanitizer.bypassSecurityTrustHtml(
        this.submission?.recognitionReason || '',
      );
    }

    let highlightedText = this.submission.recognitionReason;

    // Sort suggestions by length descending to avoid partial replacements
    const sortedSuggestions = [...this.suggestedEdits.suggestions].sort(
      (a, b) => b.original.length - a.original.length,
    );

    // Replace each original mistake with highlighted version
    sortedSuggestions.forEach((suggestion) => {
      const regex = new RegExp(
        `\\b${this.escapeRegex(suggestion.original)}\\b`,
        'gi',
      );
      highlightedText = highlightedText.replace(
        regex,
        `<strong style="color: #d32f2f;">${suggestion.original}</strong>`,
      );
    });

    return this.sanitizer.bypassSecurityTrustHtml(highlightedText);
  }

  highlightSuggestedCorrections(): SafeHtml {
    if (
      !this.suggestedEdits?.fullEditedReason ||
      !this.suggestedEdits?.suggestions ||
      this.suggestedEdits.suggestions.length === 0
    ) {
      return this.sanitizer.bypassSecurityTrustHtml(
        this.suggestedEdits?.fullEditedReason || '',
      );
    }

    let highlightedText = this.suggestedEdits.fullEditedReason;

    // Sort suggestions by length descending to avoid partial replacements
    const sortedSuggestions = [...this.suggestedEdits.suggestions].sort(
      (a, b) => b.suggested.length - a.suggested.length,
    );

    // Replace each suggested correction with highlighted version
    sortedSuggestions.forEach((suggestion) => {
      const regex = new RegExp(
        `\\b${this.escapeRegex(suggestion.suggested)}\\b`,
        'gi',
      );
      highlightedText = highlightedText.replace(
        regex,
        `<strong style="color: #1b5e20;">${suggestion.suggested}</strong>`,
      );
    });

    return this.sanitizer.bypassSecurityTrustHtml(highlightedText);
  }

  private escapeRegex(str: string): string {
    return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  }

  downloadAttachment(
    submissionId: string | undefined,
    attachmentId: string,
    fileName: string,
  ): void {
    if (!submissionId) {
      this.errorMessage =
        'Unable to download attachment: submission ID missing.';
      return;
    }

    this.downloadingAttachmentId = attachmentId;

    this.submissionService
      .getAttachmentDownloadUrl(submissionId, attachmentId)
      .subscribe({
        next: (response) => {
          // Create a temporary link and trigger download
          const link = document.createElement('a');
          link.href = response.sasUrl;
          link.download = fileName;
          link.style.display = 'none';
          document.body.appendChild(link);
          link.click();
          document.body.removeChild(link);
          this.downloadingAttachmentId = null;
        },
        error: (error) => {
          this.downloadingAttachmentId = null;
          if (error.status === 404) {
            this.errorMessage =
              'Attachment not found. It may have been deleted.';
          } else if (error.status === 401 || error.status === 403) {
            this.errorMessage =
              'You do not have permission to download this attachment.';
          } else {
            this.errorMessage =
              'Unable to download attachment. Please try again.';
          }
        },
      });
  }
}
