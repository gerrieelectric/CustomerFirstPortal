import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import {
  DirectoryUser,
  ExtractedSubmissionFields,
  SubmissionService,
} from '../../../../core/services/submission.service';
import { environment } from '../../../../../environments/environment';
import { FormHeaderComponent } from '../../../../shared/components/form-header/form-header.component';
import { InputTextModule } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { RatingModule } from 'primeng/rating';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SelectModule } from 'primeng/select';
import { AvatarModule } from 'primeng/avatar';
import { FileUploadModule } from 'primeng/fileupload';
import { DialogModule } from 'primeng/dialog';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-customer-recognition',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormHeaderComponent,
    InputTextModule,
    Textarea,
    RatingModule,
    ButtonModule,
    MessageModule,
    AutoCompleteModule,
    SelectModule,
    AvatarModule,
    FileUploadModule,
    DialogModule,
  ],
  template: `
    <div class="page-wrapper recognition-form-page customer-recognition-page">
      <app-form-header
        title="Nominate an employee who always puts the Customer First"
        subtitle="Recognize an employee for exceptional Customer Service and Experience"
      ></app-form-header>

      <div class="form-wrapper">
        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <div class="form-messages">
            <p-message
              *ngIf="successMessage"
              severity="success"
              [text]="successMessage"
            ></p-message>
            <p-message
              *ngIf="errorMessage"
              severity="error"
              [text]="errorMessage"
            ></p-message>
            <p-message
              *ngIf="prefillMessage"
              severity="info"
              [text]="prefillMessage"
            ></p-message>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="customer-company-name">Company Name:</label>
              <p-autocomplete
                id="customer-company-name"
                formControlName="companyName"
                class="company-name-autocomplete"
                [suggestions]="customerSuggestions"
                [showClear]="true"
                [forceSelection]="false"
                [dropdown]="false"
                [minLength]="2"
                [delay]="250"
                inputStyleClass="form-control"
                placeholder="Type at least 2 characters to search companies"
                (completeMethod)="onCustomerSearch($event)"
              >
                <ng-template let-customer pTemplate="item">
                  <div class="customer-option">{{ customer }}</div>
                </ng-template>
              </p-autocomplete>
              <span class="error" *ngIf="getFieldError('companyName')">
                {{ getFieldError('companyName') }}
              </span>
              <small
                class="field-hint company-field-hint"
                *ngIf="!isLoadingCustomers && !customerSearchError"
              >
                Search results are filtered from the JDE customer list.
              </small>
              <small
                class="field-hint company-field-hint"
                *ngIf="isLoadingCustomers"
              >
                Searching customers...
              </small>
              <small class="error" *ngIf="customerSearchError">
                {{ customerSearchError }}
              </small>
            </div>
          </div>

          <div class="form-row two-col-row">
            <div class="form-group">
              <label for="customer-contact-name">Contact Name:</label>
              <input
                id="customer-contact-name"
                type="text"
                formControlName="customerName"
                class="form-control"
                pInputText
              />
              <span class="error" *ngIf="getFieldError('customerName')">
                {{ getFieldError('customerName') }}
              </span>
            </div>

            <div class="form-group">
              <label for="customer-contact-email">Contact Email:</label>
              <input
                id="customer-contact-email"
                type="email"
                formControlName="customerEmail"
                class="form-control"
                pInputText
              />
              <span class="error" *ngIf="getFieldError('customerEmail')">
                {{ getFieldError('customerEmail') }}
              </span>
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="customer-nominee-user">Nominee Name:</label>
              <p-select
                inputId="customer-nominee-user"
                formControlName="nomineeUser"
                [options]="directoryUsers"
                [filter]="true"
                filterBy="displayName,email,department"
                optionLabel="displayName"
                [showClear]="true"
                [loading]="isLoadingUsers"
                [virtualScroll]="true"
                [virtualScrollItemSize]="56"
                class="nominee-select"
                placeholder="Select a nominee"
                (onChange)="onNomineeChange($event.value)"
                (onClear)="onNomineeChange(null)"
                (onFilter)="onNomineeFilter($event)"
              >
                <ng-template let-user pTemplate="item">
                  <div class="nominee-option">
                    <p-avatar
                      [image]="user.profilePhotoUrl"
                      [label]="
                        user.profilePhotoUrl
                          ? undefined
                          : getInitials(user.displayName)
                      "
                      shape="circle"
                      size="large"
                      styleClass="avatar-large"
                    >
                    </p-avatar>
                    <div class="nominee-info">
                      <div class="nominee-name">{{ user.displayName }}</div>
                      <div class="nominee-meta">
                        {{ user.department || 'No department' }}
                      </div>
                    </div>
                  </div>
                </ng-template>
                <ng-template let-user pTemplate="selectedItem">
                  <div class="nominee-option" *ngIf="user">
                    <p-avatar
                      [image]="user.profilePhotoUrl"
                      [label]="
                        user.profilePhotoUrl
                          ? undefined
                          : getInitials(user.displayName)
                      "
                      shape="circle"
                      size="large"
                      styleClass="avatar-large"
                    >
                    </p-avatar>
                    <div class="nominee-info">
                      <div class="nominee-name">{{ user.displayName }}</div>
                      <div class="nominee-meta">
                        {{ user.department || 'No department' }}
                      </div>
                    </div>
                  </div>
                </ng-template>
              </p-select>
              <small
                class="field-hint"
                *ngIf="!isLoadingUsers && !usersLoadError"
              >
                Type at least 2 characters to search directory users.
              </small>
              <small class="error" *ngIf="usersLoadError">{{
                usersLoadError
              }}</small>
              <span class="error" *ngIf="getFieldError('nomineeUser')">
                {{ getFieldError('nomineeUser') }}
              </span>
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="customer-recognition-reason"
                >Reason for Nomination:</label
              >
              <textarea
                id="customer-recognition-reason"
                formControlName="recognitionReason"
                class="form-control textarea"
                rows="4"
                maxlength="2000"
                pInputTextarea
                placeholder="Describe how the employee provided outstanding Customer Service"
              ></textarea>
              <small class="char-count">
                {{ form.get('recognitionReason')?.value?.length || 0 }} / 2000
              </small>
              <span class="error" *ngIf="getFieldError('recognitionReason')">
                {{ getFieldError('recognitionReason') }}
              </span>
            </div>
          </div>

          <div class="form-row" *ngIf="attachmentsEnabled">
            <div class="form-group">
              <label for="customer-attachments"
                >Attachment(s) (optional):</label
              >
              <p-fileUpload
                id="customer-attachments"
                name="attachments[]"
                [multiple]="true"
                [accept]="acceptedAttachmentTypes"
                [maxFileSize]="maxAttachmentSizeBytes"
                [customUpload]="true"
                [showUploadButton]="false"
                [showCancelButton]="false"
                (onSelect)="onAttachmentsSelected($event)"
                (onRemove)="onAttachmentRemoved($event)"
                [auto]="false"
                chooseLabel="Choose Files"
                chooseIcon="pi pi-plus"
                class="attachment-uploader"
              >
                <ng-template pTemplate="content">
                  <ul *ngIf="selectedAttachments.length > 0">
                    <li
                      *ngFor="let file of selectedAttachments; let i = index"
                      class="flex align-items-center justify-content-between"
                    >
                      <div class="attachment-item">
                        <span class="attachment-name">{{ file.name }}</span>
                        <span class="attachment-size">{{
                          formatFileSize(file.size)
                        }}</span>
                      </div>
                      <button
                        type="button"
                        pButton
                        class="p-button-rounded p-button-danger p-button-text"
                        icon="pi pi-trash"
                        (click)="removeAttachment(i)"
                        [attr.aria-label]="'Remove ' + file.name"
                      ></button>
                    </li>
                  </ul>
                </ng-template>
              </p-fileUpload>
              <small class="field-hint">
                Allowed file types: {{ acceptedAttachmentExtensionsDisplay }}
              </small>
              <small class="field-hint">
                Max file size: {{ maxAttachmentSizeMb }} MB each
              </small>
              <small class="error" *ngIf="attachmentError">
                {{ attachmentError }}
              </small>
            </div>
          </div>

          <div class="form-row form-row-rating">
            <div class="form-group rating-group">
              <label id="customer-service-rating-label">
                Please rate the employee's Customer Service:
              </label>
              <div
                class="diamond-rating"
                role="radiogroup"
                aria-labelledby="customer-service-rating-label"
              >
                <button
                  type="button"
                  class="rating-diamond"
                  *ngFor="let value of ratingValues"
                  [class.active]="form.get('serviceRating')?.value >= value"
                  (click)="setServiceRating(value)"
                  (keydown)="onRatingKeydown($event, value)"
                  role="radio"
                  [attr.aria-checked]="
                    form.get('serviceRating')?.value === value
                  "
                  [attr.tabindex]="getRatingTabIndex(value)"
                  [attr.aria-label]="'Select rating ' + value"
                >
                  <span>{{ value }}</span>
                </button>
              </div>
              <div class="rating-labels" aria-hidden="true">
                <span *ngFor="let value of ratingValues">
                  {{ ratingLabelByValue[value] || '' }}
                </span>
              </div>
              <span
                class="error"
                *ngIf="
                  getFieldError('serviceRating') &&
                  form.get('serviceRating')?.touched
                "
              >
                {{ getFieldError('serviceRating') }}
              </span>
            </div>

            <div class="form-actions">
              <p-button
                type="submit"
                [disabled]="!form.valid || isSubmitting"
                [label]="isSubmitting ? 'Submitting...' : 'Submit'"
              ></p-button>
            </div>
          </div>
        </form>
      </div>

      <p-dialog
        header="Start with your attachments"
        [(visible)]="showIntakeDialog"
        [modal]="true"
        [draggable]="false"
        [resizable]="false"
        [closable]="!isExtracting"
        [style]="{ width: '34rem', maxWidth: '95vw' }"
        (onHide)="onIntakeDialogHide()"
      >
        <p class="intake-intro">
          Do you have an email or document about this recognition, such as a
          customer's thank-you note? Upload it here and we'll try to pre-fill
          the form for you. Files added here are also attached to your
          submission.
        </p>

        <p-fileUpload
          name="intakeAttachments[]"
          [multiple]="true"
          [accept]="acceptedAttachmentTypes"
          [maxFileSize]="maxAttachmentSizeBytes"
          [customUpload]="true"
          [showUploadButton]="false"
          [showCancelButton]="false"
          [auto]="false"
          [disabled]="isExtracting"
          chooseLabel="Choose Files"
          chooseIcon="pi pi-plus"
          class="attachment-uploader"
          (onSelect)="onIntakeFilesSelected($event)"
          (onRemove)="onIntakeFileRemoved($event)"
        >
          <ng-template pTemplate="content">
            <ul *ngIf="intakeFiles.length > 0">
              <li
                *ngFor="let file of intakeFiles; let i = index"
                class="flex align-items-center justify-content-between"
              >
                <div class="attachment-item">
                  <span class="attachment-name">{{ file.name }}</span>
                  <span class="attachment-size">{{
                    formatFileSize(file.size)
                  }}</span>
                </div>
                <button
                  type="button"
                  pButton
                  class="p-button-rounded p-button-danger p-button-text"
                  icon="pi pi-trash"
                  (click)="removeIntakeFile(i)"
                  [disabled]="isExtracting"
                  [attr.aria-label]="'Remove ' + file.name"
                ></button>
              </li>
            </ul>
          </ng-template>
        </p-fileUpload>
        <small class="field-hint">
          Allowed file types: {{ acceptedAttachmentExtensionsDisplay }}
        </small>
        <small class="error" *ngIf="intakeAttachmentError">
          {{ intakeAttachmentError }}
        </small>

        <ng-template pTemplate="footer">
          <p-button
            label="Skip"
            severity="secondary"
            [text]="true"
            [disabled]="isExtracting"
            (onClick)="skipIntake()"
          ></p-button>
          <p-button
            [label]="isExtracting ? 'Analyzing...' : 'Continue'"
            [icon]="isExtracting ? 'pi pi-spin pi-spinner' : 'pi pi-sparkles'"
            [disabled]="isExtracting"
            (onClick)="continueIntake()"
          ></p-button>
        </ng-template>
      </p-dialog>
    </div>
  `,
  styles: [],
})
export class CustomerRecognitionComponent implements OnInit {
  private readonly submissionService = inject(SubmissionService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  form: FormGroup;
  isSubmitting = false;
  successMessage = '';
  errorMessage = '';
  readonly ratingValues = [1, 2, 3, 4, 5];
  readonly ratingLabelByValue: Record<number, string> = {
    1: 'Great',
    3: 'Above & Beyond',
    5: 'WOW!',
  };
  directoryUsers: DirectoryUser[] = [];
  isLoadingUsers = false;
  usersLoadError = '';
  customerSuggestions: string[] = [];
  isLoadingCustomers = false;
  customerSearchError = '';
  private photoCache = new Map<string, string>();
  private readonly maxPhotoCacheEntries = 200;
  private readonly timeoutHandles = new Set<ReturnType<typeof setTimeout>>();
  private customerSearchSequence = 0;
  readonly attachmentsEnabled = environment.features.enableFileAttachments;
  readonly allowedAttachmentExtensions = [
    'pdf',
    'docx',
    'png',
    'jpg',
    'jpeg',
    'txt',
    'eml',
    'msg',
  ];
  readonly maxAttachmentSizeMb = 50;
  readonly maxAttachmentSizeBytes = this.maxAttachmentSizeMb * 1024 * 1024;
  readonly acceptedAttachmentTypes = this.allowedAttachmentExtensions
    .map((ext) => `.${ext}`)
    .join(',');
  readonly acceptedAttachmentExtensionsDisplay =
    this.allowedAttachmentExtensions.map((ext) => `.${ext}`).join(', ');
  selectedAttachments: File[] = [];
  attachmentError = '';
  showIntakeDialog = false;
  intakeFiles: File[] = [];
  intakeAttachmentError = '';
  isExtracting = false;
  prefillMessage = '';

  constructor() {
    this.form = this.fb.group({
      submissionType: ['CustomerToEmployee', Validators.required],
      companyName: ['', [Validators.required, Validators.maxLength(200)]],
      customerName: ['', [Validators.required, Validators.maxLength(100)]],
      customerEmail: [
        '',
        [Validators.required, Validators.email, Validators.maxLength(100)],
      ],
      nomineeUser: [null, Validators.required],
      nomineeName: ['', [Validators.required, Validators.maxLength(100)]],
      nomineeEmail: [
        '',
        [Validators.required, Validators.email, Validators.maxLength(100)],
      ],
      nomineeDepartment: ['', [Validators.maxLength(100)]],
      recognitionReason: [
        '',
        [Validators.required, Validators.maxLength(2000)],
      ],
      serviceRating: [0, [Validators.required, Validators.min(1)]],
    });

    this.destroyRef.onDestroy(() => {
      this.timeoutHandles.forEach((handle) => clearTimeout(handle));
      this.timeoutHandles.clear();
      this.photoCache.clear();
      this.directoryUsers = [];
      this.selectedAttachments = [];
      this.intakeFiles = [];
    });
  }

  ngOnInit(): void {
    if (this.attachmentsEnabled) {
      this.showIntakeDialog = true;
    }
  }

  onNomineeChange(user: DirectoryUser | null): void {
    this.form.patchValue({
      nomineeName: user?.displayName ?? '',
      nomineeEmail: user?.email ?? '',
      nomineeDepartment: user?.department ?? '',
    });
  }

  getInitials(displayName: string): string {
    return displayName
      .split(' ')
      .map((name) => name.charAt(0).toUpperCase())
      .join('')
      .substring(0, 2);
  }

  onNomineeFilter(event: unknown): void {
    const typed =
      typeof event === 'object' && event !== null && 'filter' in event
        ? String((event as { filter?: string }).filter ?? '')
        : '';

    if (typed.trim().length < 2) {
      this.directoryUsers = [];
      this.usersLoadError = '';
      return;
    }

    this.isLoadingUsers = true;
    this.usersLoadError = '';

    this.submissionService
      .searchDirectoryUsers(typed.trim())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (users) => {
          this.directoryUsers = users.map((user) => ({
            ...user,
            profilePhotoUrl: undefined,
          }));
          this.isLoadingUsers = false;
          this.loadUserPhotos(this.directoryUsers).catch((error) =>
            console.warn('Error loading user photos:', error),
          );
        },
        error: () => {
          this.directoryUsers = [];
          this.isLoadingUsers = false;
          this.usersLoadError =
            'Unable to search people right now. Please try again.';
        },
      });
  }

  onCustomerSearch(event: unknown): void {
    const typed =
      typeof event === 'object' && event !== null && 'query' in event
        ? String((event as { query?: string }).query ?? '')
        : '';

    if (typed.trim().length < 2) {
      this.customerSuggestions = [];
      this.customerSearchError = '';
      this.isLoadingCustomers = false;
      return;
    }

    const searchSequence = ++this.customerSearchSequence;
    this.isLoadingCustomers = true;
    this.customerSearchError = '';

    this.submissionService
      .searchCustomers(typed.trim())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (customers) => {
          if (searchSequence !== this.customerSearchSequence) {
            return;
          }

          this.customerSuggestions = customers;
          this.isLoadingCustomers = false;
        },
        error: () => {
          if (searchSequence !== this.customerSearchSequence) {
            return;
          }

          this.customerSuggestions = [];
          this.isLoadingCustomers = false;
          this.customerSearchError =
            'Unable to search customers right now. Please try again.';
        },
      });
  }

  private loadUserPhoto(user: DirectoryUser): Promise<void> {
    return new Promise((resolve) => {
      if (this.photoCache.has(user.entraObjectId)) {
        user.profilePhotoUrl = this.photoCache.get(user.entraObjectId);
        resolve();
      } else {
        this.submissionService
          .getUserProfilePhoto(user.entraObjectId)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (blob) => {
              if (!blob || blob.size === 0) {
                resolve();
                return;
              }

              const reader = new FileReader();
              reader.onload = () => {
                const dataUrl = reader.result as string;
                if (
                  !this.photoCache.has(user.entraObjectId) &&
                  this.photoCache.size >= this.maxPhotoCacheEntries
                ) {
                  const oldestKey = this.photoCache.keys().next().value;
                  if (oldestKey) {
                    this.photoCache.delete(oldestKey);
                  }
                }
                this.photoCache.set(user.entraObjectId, dataUrl);
                user.profilePhotoUrl = dataUrl;
                resolve();
              };
              reader.readAsDataURL(blob);
            },
            error: (error) => {
              if (error?.status !== 404) {
                console.warn(
                  `Failed to load photo for ${user.displayName}:`,
                  error,
                );
              }
              resolve();
            },
          });
      }
    });
  }

  private async loadUserPhotos(users: DirectoryUser[]): Promise<void> {
    const photoPromises = users.map((user) => this.loadUserPhoto(user));
    await Promise.all(photoPromises);
  }

  getFieldError(fieldName: string): string | null {
    const field = this.form.get(fieldName);
    if (!field || !field.invalid || !field.touched) return null;

    if (field.errors?.['required'])
      return `${this.prettifyFieldName(fieldName)} is required.`;
    if (field.errors?.['email']) return 'Must be a valid email address.';
    if (field.errors?.['maxLength']) {
      const max = field.errors['maxLength'].requiredLength;
      return `Cannot exceed ${max} characters.`;
    }
    if (field.errors?.['min']) return 'Please select a rating.';

    return 'Invalid input.';
  }

  private prettifyFieldName(name: string): string {
    return name
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, (str) => str.toUpperCase())
      .trim();
  }

  setServiceRating(value: number): void {
    const ratingControl = this.form.get('serviceRating');
    ratingControl?.setValue(value);
    ratingControl?.markAsTouched();
  }

  getRatingTabIndex(value: number): number {
    const currentRating = this.form.get('serviceRating')?.value ?? 0;

    if (currentRating < 1) {
      return value === this.ratingValues[0] ? 0 : -1;
    }

    return currentRating === value ? 0 : -1;
  }

  onRatingKeydown(event: KeyboardEvent, value: number): void {
    let nextValue = value;

    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowUp':
        nextValue = Math.min(
          this.ratingValues[this.ratingValues.length - 1],
          value + 1,
        );
        break;
      case 'ArrowLeft':
      case 'ArrowDown':
        nextValue = Math.max(this.ratingValues[0], value - 1);
        break;
      case 'Home':
        nextValue = this.ratingValues[0];
        break;
      case 'End':
        nextValue = this.ratingValues[this.ratingValues.length - 1];
        break;
      case ' ':
      case 'Enter':
        nextValue = value;
        break;
      default:
        return;
    }

    event.preventDefault();
    this.setServiceRating(nextValue);
  }

  onAttachmentsSelected(event: { files: File[] }): void {
    const files = event.files || [];
    this.attachmentError = '';

    if (files.length === 0) {
      return;
    }

    const validationErrors = this.collectValidFiles(
      files,
      this.selectedAttachments,
    );

    if (validationErrors.length > 0) {
      this.attachmentError = validationErrors.join(' ');
    }
  }

  /**
   * Validates files (extension, size) and appends new ones to the target
   * array, skipping duplicates. Returns validation error messages.
   */
  private collectValidFiles(files: File[], target: File[]): string[] {
    const validationErrors: string[] = [];
    const existingFileKeys = new Set(
      target.map((file) =>
        `${file.name}|${file.size}|${file.lastModified}`.toLowerCase(),
      ),
    );

    for (const file of files) {
      const extension = file.name.split('.').pop()?.toLowerCase() ?? '';

      if (!this.allowedAttachmentExtensions.includes(extension)) {
        validationErrors.push(`${file.name}: unsupported file type.`);
        continue;
      }

      if (file.size > this.maxAttachmentSizeBytes) {
        validationErrors.push(
          `${file.name}: exceeds ${this.maxAttachmentSizeMb} MB limit.`,
        );
        continue;
      }

      const key =
        `${file.name}|${file.size}|${file.lastModified}`.toLowerCase();
      if (!existingFileKeys.has(key)) {
        existingFileKeys.add(key);
        target.push(file);
      }
    }

    return validationErrors;
  }

  onAttachmentRemoved(event: { file: File }): void {
    const fileToRemove = event.file;
    const index = this.selectedAttachments.findIndex(
      (file) =>
        file.name === fileToRemove.name &&
        file.size === fileToRemove.size &&
        file.lastModified === fileToRemove.lastModified,
    );

    if (index >= 0) {
      this.selectedAttachments.splice(index, 1);
    }
  }

  removeAttachment(index: number): void {
    this.selectedAttachments.splice(index, 1);
  }

  onIntakeFilesSelected(event: { files: File[] }): void {
    const files = event.files || [];
    this.intakeAttachmentError = '';

    if (files.length === 0) {
      return;
    }

    const validationErrors = this.collectValidFiles(files, this.intakeFiles);

    if (validationErrors.length > 0) {
      this.intakeAttachmentError = validationErrors.join(' ');
    }
  }

  onIntakeFileRemoved(event: { file: File }): void {
    const fileToRemove = event.file;
    const index = this.intakeFiles.findIndex(
      (file) =>
        file.name === fileToRemove.name &&
        file.size === fileToRemove.size &&
        file.lastModified === fileToRemove.lastModified,
    );

    if (index >= 0) {
      this.intakeFiles.splice(index, 1);
    }
  }

  removeIntakeFile(index: number): void {
    this.intakeFiles.splice(index, 1);
  }

  skipIntake(): void {
    this.mergeIntakeFilesIntoAttachments();
    this.showIntakeDialog = false;
  }

  onIntakeDialogHide(): void {
    // Covers close via the header X: keep chosen files, skip extraction.
    if (!this.isExtracting) {
      this.mergeIntakeFilesIntoAttachments();
    }
  }

  continueIntake(): void {
    const files = [...this.intakeFiles];
    this.mergeIntakeFilesIntoAttachments();

    if (files.length === 0) {
      this.showIntakeDialog = false;
      return;
    }

    this.isExtracting = true;

    this.submissionService
      .extractSubmissionFields(files)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (fields) => {
          this.isExtracting = false;
          this.showIntakeDialog = false;

          if (fields.status === 'Success') {
            this.applyExtractedFields(fields);
          }
        },
        error: () => {
          // Extraction is best-effort; the files remain attached.
          this.isExtracting = false;
          this.showIntakeDialog = false;
        },
      });
  }

  /**
   * Moves intake dialog files into the main attachments list (deduplicated)
   * so they show in the attachments section and upload on submit.
   */
  private mergeIntakeFilesIntoAttachments(): void {
    if (this.intakeFiles.length === 0) {
      return;
    }

    this.collectValidFiles(this.intakeFiles, this.selectedAttachments);
    this.intakeFiles = [];
    this.intakeAttachmentError = '';
  }

  private applyExtractedFields(fields: ExtractedSubmissionFields): void {
    let appliedAny = false;

    if (this.patchIfEmpty('customerName', fields.customerName, 100)) {
      appliedAny = true;
    }
    if (this.patchIfEmpty('customerEmail', fields.customerEmail, 100)) {
      appliedAny = true;
    }
    if (
      this.patchIfEmpty('recognitionReason', fields.recognitionReason, 2000)
    ) {
      appliedAny = true;
    }

    this.applyExtractedCompanyName(fields.companyName);
    this.applyExtractedNominee(fields.nomineeName);

    if (appliedAny) {
      this.showPrefillNotice();
    }
  }

  /** Sets a control value only when the control is currently empty. */
  private patchIfEmpty(
    controlName: string,
    value: string | null,
    maxLength: number,
  ): boolean {
    const control = this.form.get(controlName);
    if (!control || !value) {
      return false;
    }

    const current = control.value;
    if (
      current !== null &&
      current !== undefined &&
      String(current).trim() !== ''
    ) {
      return false;
    }

    control.setValue(value.slice(0, maxLength));
    return true;
  }

  private applyExtractedCompanyName(extractedName: string | null): void {
    if (!extractedName) {
      return;
    }

    const control = this.form.get('companyName');
    if (!control || String(control.value ?? '').trim() !== '') {
      return;
    }

    this.submissionService
      .searchCustomers(extractedName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (customers) => {
          // Prefer the canonical JDE customer value when it matches.
          const match = customers.find(
            (customer) =>
              customer.trim().toLowerCase() ===
              extractedName.trim().toLowerCase(),
          );

          if (this.patchIfEmpty('companyName', match ?? extractedName, 200)) {
            this.showPrefillNotice();
          }
        },
        error: () => {
          // Free text is allowed; fall back to the extracted value.
          if (this.patchIfEmpty('companyName', extractedName, 200)) {
            this.showPrefillNotice();
          }
        },
      });
  }

  private applyExtractedNominee(extractedName: string | null): void {
    if (!extractedName || this.form.get('nomineeUser')?.value) {
      return;
    }

    this.submissionService
      .searchDirectoryUsers(extractedName)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (users) => {
          // Only auto-select when the match is unambiguous.
          if (users.length !== 1 || this.form.get('nomineeUser')?.value) {
            return;
          }

          const user: DirectoryUser = {
            ...users[0],
            profilePhotoUrl: undefined,
          };

          this.directoryUsers = [user];
          this.form.patchValue({ nomineeUser: user });
          this.onNomineeChange(user);
          this.showPrefillNotice();
          this.loadUserPhotos([user]).catch((error) =>
            console.warn('Error loading user photos:', error),
          );
        },
        error: () => {
          // Leave nominee blank for manual selection.
        },
      });
  }

  private showPrefillNotice(): void {
    this.prefillMessage =
      'Some fields were pre-filled from your attachments. Please review them and complete the rest of the form.';
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) {
      return `${bytes} B`;
    }

    if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)} KB`;
    }

    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  onSubmit(): void {
    if (!this.form.valid) return;

    this.isSubmitting = true;
    this.successMessage = '';
    this.errorMessage = '';

    const payload = {
      ...this.form.value,
      nomineeUser: undefined,
      serviceRating: this.form.value.serviceRating,
    };

    this.submissionService
      .createSubmission(payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const submissionId = response.submissionId ?? response.id;

          if (
            !submissionId ||
            !this.attachmentsEnabled ||
            this.selectedAttachments.length === 0
          ) {
            this.completeSuccessfulSubmission(false);
            return;
          }

          const uploadRequests = this.selectedAttachments.map((file) =>
            this.submissionService.uploadAttachment(submissionId, file),
          );

          forkJoin(uploadRequests)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: () => this.completeSuccessfulSubmission(false),
              error: () => this.completeSuccessfulSubmission(true),
            });
        },
        error: (error) => {
          this.isSubmitting = false;
          this.errorMessage =
            error?.error?.message ||
            'Failed to submit recognition. Please try again.';
        },
      });
  }

  private completeSuccessfulSubmission(
    hasAttachmentUploadErrors: boolean,
  ): void {
    this.isSubmitting = false;

    this.successMessage = hasAttachmentUploadErrors
      ? 'Submission saved, but one or more attachments failed to upload.'
      : 'Thank you! Your recognition has been submitted successfully.';

    this.errorMessage = hasAttachmentUploadErrors
      ? 'Try again after opening the submission details page.'
      : '';

    this.form.reset({ submissionType: 'CustomerToEmployee' });
    this.selectedAttachments = [];
    this.attachmentError = '';

    this.scheduleTimeout(() => {
      this.router.navigate(['/submissions']);
    }, 2000);
  }

  private scheduleTimeout(callback: () => void, delayMs: number): void {
    const handle = setTimeout(() => {
      this.timeoutHandles.delete(handle);
      callback();
    }, delayMs);

    this.timeoutHandles.add(handle);
  }

  onCancel(): void {
    this.router.navigate(['/submissions']);
  }
}
