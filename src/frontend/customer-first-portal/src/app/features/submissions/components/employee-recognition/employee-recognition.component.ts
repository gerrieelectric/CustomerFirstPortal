import { Component, DestroyRef, inject } from '@angular/core';
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
  SubmissionService,
} from '../../../../core/services/submission.service';
import { FormHeaderComponent } from '../../../../shared/components/form-header/form-header.component';
import { InputTextModule } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { RatingModule } from 'primeng/rating';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { AvatarModule } from 'primeng/avatar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-employee-recognition',
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
    SelectModule,
    AvatarModule,
  ],
  template: `
    <div class="page-wrapper recognition-form-page employee-recognition-page">
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
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="employee-nominee-user">Nominee Name:</label>
              <p-select
                inputId="employee-nominee-user"
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
              <label for="employee-recognition-reason"
                >Reason for Nomination:</label
              >
              <textarea
                id="employee-recognition-reason"
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

          <div class="form-row form-row-rating">
            <div class="form-group rating-group">
              <label id="employee-service-rating-label">
                Please rate the employee's Customer Service:
              </label>
              <div
                class="diamond-rating"
                role="radiogroup"
                aria-labelledby="employee-service-rating-label"
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
    </div>
  `,
  styles: [],
})
export class EmployeeRecognitionComponent {
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
  private photoCache = new Map<string, string>();
  private readonly maxPhotoCacheEntries = 200;
  private readonly timeoutHandles = new Set<ReturnType<typeof setTimeout>>();

  constructor() {
    this.form = this.fb.group({
      submissionType: ['EmployeeToEmployee', Validators.required],
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
    });
  }

  onNomineeChange(user: DirectoryUser | null): void {
    this.form.patchValue({
      nomineeName: user?.displayName ?? '',
      nomineeEmail: user?.email ?? '',
      nomineeDepartment: user?.department ?? '',
    });
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
          // Prevent direct image URL rendering (which cannot send Authorization headers).
          // We fetch photos via HttpClient and then set data URLs for avatar rendering.
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
                // 204/no photo: keep initials fallback.
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
          this.isSubmitting = false;
          this.successMessage =
            'Thank you! Your recognition has been submitted successfully.';
          this.form.reset({ submissionType: 'EmployeeToEmployee' });
          this.scheduleTimeout(() => {
            this.router.navigate(['/submissions']);
          }, 2000);
        },
        error: (error) => {
          this.isSubmitting = false;
          this.errorMessage =
            error?.error?.message ||
            'Failed to submit recognition. Please try again.';
        },
      });
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
