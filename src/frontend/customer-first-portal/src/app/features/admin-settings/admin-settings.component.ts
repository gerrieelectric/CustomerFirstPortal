import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { AvatarModule } from 'primeng/avatar';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import {
  AdminUserService,
  AdminUser,
  AddAdminUserRequest,
  DirectoryUser,
  UpdateAdminUserRequest,
} from '../../core/services/admin-user.service';
import { SubmissionService } from '../../core/services/submission.service';

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    CardModule,
    InputTextModule,
    CheckboxModule,
    ButtonModule,
    MessageModule,
    TableModule,
    TagModule,
    SelectModule,
    AvatarModule,
    ToggleSwitchModule,
  ],
  template: `
    <div class="admin-settings-container">
      <div class="settings-header">
        <h1>⚙️ Admin Settings</h1>
        <p class="subtitle">
          Manage administrative users and their permissions
        </p>
      </div>

      <!-- Add Admin Form -->
      <p-card styleClass="add-admin-section">
        <ng-template pTemplate="title">Add New Admin User</ng-template>
        <form
          [formGroup]="addAdminForm"
          (ngSubmit)="onAddAdmin()"
          class="admin-form"
        >
          <div class="form-group">
            <label>Select Admin User</label>
            <p-select
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
              placeholder="Select an admin user"
              (onChange)="onAdminUserChange($event.value)"
              (onClear)="onAdminUserChange(null)"
              (onFilter)="onAdminUserFilter($event)"
            >
              <ng-template let-user pTemplate="item">
                <div class="nominee-option">
                  <p-avatar
                    [image]="getUserPhotoUrl(user)"
                    [label]="
                      getUserPhotoUrl(user)
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
                    [image]="getUserPhotoUrl(user)"
                    [label]="
                      getUserPhotoUrl(user)
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
                    <div class="nominee-meta">{{ user.email }}</div>
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
          </div>

          <div class="checkbox-group">
            <div class="checkbox-item">
              <p-checkbox
                id="canReview"
                formControlName="canReview"
                inputId="canReview"
                [binary]="true"
              ></p-checkbox>
              <label for="canReview">Can Review Submissions</label>
            </div>
            <div class="checkbox-item">
              <p-checkbox
                id="receiveNotifications"
                formControlName="receiveNotifications"
                inputId="receiveNotifications"
                [binary]="true"
              ></p-checkbox>
              <label for="receiveNotifications"
                >Receive Email Notifications</label
              >
            </div>
          </div>

          <div class="form-actions">
            <p-button
              type="submit"
              [disabled]="!addAdminForm.valid || isSubmitting"
              [label]="isSubmitting ? 'Adding...' : 'Add Admin User'"
              icon="pi pi-user-plus"
            ></p-button>
          </div>

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
        </form>
      </p-card>

      <!-- Admin Users List -->
      <p-card styleClass="admin-list-section">
        <ng-template pTemplate="title">Current Admin Users</ng-template>

        <div class="loading" *ngIf="isLoading">Loading admin users...</div>

        <p-table
          class="admin-table"
          *ngIf="!isLoading && adminUsers.length > 0"
          [value]="adminUsers"
          [tableStyle]="{ 'min-width': '60rem' }"
        >
          <ng-template pTemplate="header">
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Department</th>
              <th>Can Review</th>
              <th>Notifications</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-admin>
            <tr [class.inactive]="!admin.isActive">
              <td class="name-cell">{{ admin.displayName }}</td>
              <td>{{ admin.email }}</td>
              <td>{{ admin.department || '—' }}</td>
              <td class="center">
                <div class="toggle-cell">
                  <p-toggleswitch
                    [ngModel]="admin.canReview"
                    [disabled]="isAdminUpdating(admin.adminUserId)"
                    (onChange)="
                      onToggleAdminFlag(admin, 'canReview', $event.checked)
                    "
                  ></p-toggleswitch>
                  <span class="toggle-value">{{
                    admin.canReview ? 'Yes' : 'No'
                  }}</span>
                </div>
              </td>
              <td class="center">
                <div class="toggle-cell">
                  <p-toggleswitch
                    [ngModel]="admin.receiveNotifications"
                    [disabled]="isAdminUpdating(admin.adminUserId)"
                    (onChange)="
                      onToggleAdminFlag(
                        admin,
                        'receiveNotifications',
                        $event.checked
                      )
                    "
                  ></p-toggleswitch>
                  <span class="toggle-value">{{
                    admin.receiveNotifications ? 'Yes' : 'No'
                  }}</span>
                </div>
              </td>
              <td class="center">
                <div class="toggle-cell">
                  <p-toggleswitch
                    [ngModel]="admin.isActive"
                    [disabled]="isAdminUpdating(admin.adminUserId)"
                    (onChange)="
                      onToggleAdminFlag(admin, 'isActive', $event.checked)
                    "
                  ></p-toggleswitch>
                  <span
                    class="toggle-value"
                    [class.toggle-value-inactive]="!admin.isActive"
                    >{{ admin.isActive ? 'Active' : 'Inactive' }}</span
                  >
                </div>
              </td>
              <td class="actions-cell">
                <p-button
                  type="button"
                  severity="danger"
                  size="small"
                  (onClick)="onRemoveAdmin(admin.adminUserId)"
                  [disabled]="
                    isDeleting === admin.adminUserId ||
                    isAdminUpdating(admin.adminUserId)
                  "
                  [label]="
                    isDeleting === admin.adminUserId ? 'Removing...' : 'Remove'
                  "
                ></p-button>
              </td>
            </tr>
          </ng-template>
        </p-table>

        <div class="no-admins" *ngIf="!isLoading && adminUsers.length === 0">
          <p>No admin users found. Add one using the form above.</p>
        </div>
      </p-card>
    </div>
  `,
  styles: [
    `
      .admin-settings-container {
        max-width: 1400px;
        margin: 0 auto;
        padding: 30px 20px;
        font-family:
          'SF Pro Text',
          'SF Pro Display',
          'San Francisco',
          -apple-system,
          BlinkMacSystemFont,
          'Segoe UI',
          'Roboto',
          sans-serif;
        display: flex;
        flex-direction: column;
        gap: 2rem;
      }

      .settings-header {
        margin-bottom: 40px;
        padding-bottom: 20px;
        border-bottom: 2px solid var(--app-border);
      }

      .settings-header h1 {
        font-size: 32px;
        margin: 0 0 8px 0;
        color: var(--app-heading);
      }

      .subtitle {
        font-size: 14px;
        color: var(--app-muted-text);
        margin: 0;
      }

      .add-admin-section {
        margin-bottom: 64px;
      }

      .add-admin-section h2 {
        font-size: 18px;
        color: var(--app-text);
        margin: 0 0 20px 0;
      }

      .admin-form {
        display: grid;
        gap: 20px;
      }

      .form-group {
        display: flex;
        flex-direction: column;
        gap: 6px;
      }

      .form-group label {
        font-size: 13px;
        font-weight: 600;
        color: var(--app-text);
      }

      .nominee-select {
        width: 100%;
      }

      :host ::ng-deep .nominee-select.p-select {
        width: 100%;
        border-radius: var(--app-control-radius);
        border: 1px solid var(--app-border);
        background: var(--app-surface-muted);
      }

      :host ::ng-deep .nominee-select.p-select:not(.p-disabled).p-focus {
        border-color: var(--app-accent);
        box-shadow: none;
      }

      .nominee-option {
        display: flex;
        align-items: center;
        min-height: 2.8rem;
      }

      .nominee-info {
        display: flex;
        flex-direction: column;
        flex: 1;
        min-width: 0;
      }

      .nominee-name {
        font-weight: 600;
        font-size: 0.92rem;
      }

      .nominee-meta {
        font-size: 0.76rem;
        color: var(--app-muted-text);
      }

      :host ::ng-deep .avatar-large {
        margin-right: 0.65rem;
        flex-shrink: 0;
      }

      .field-hint {
        margin: 0.45rem 0 0;
        color: var(--app-muted-text);
        font-size: 0.8rem;
        font-weight: 400;
      }

      .checkbox-group {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 15px;
        margin: 10px 0;
      }

      .checkbox-item {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      .checkbox-item label {
        font-weight: 400;
        cursor: pointer;
        margin: 0;
      }

      .error {
        font-size: 12px;
        color: #e81123;
        margin-top: 4px;
      }

      .form-actions {
        display: flex;
        gap: 10px;
        margin-top: 15px;
      }

      .admin-list-section {
        margin-bottom: 2rem;
      }

      .admin-list-section h2 {
        font-size: 18px;
        color: var(--app-text);
        margin: 0 0 20px 0;
      }

      .admin-table {
        width: 100%;
        border-collapse: collapse;
        font-size: 14px;
      }

      .admin-table thead {
        background: var(--app-surface-muted);
        border-bottom: 2px solid var(--app-border);
      }

      .admin-table th {
        padding: 12px;
        text-align: left;
        font-weight: 600;
        color: var(--app-text);
      }

      .admin-table td {
        padding: 12px;
        border-bottom: 1px solid var(--app-border);
        color: var(--app-text);
      }

      .admin-table tbody tr:hover {
        background: var(--app-accent-soft);
      }

      .admin-table tbody tr.inactive {
        opacity: 0.6;
      }

      .name-cell {
        font-weight: 600;
      }

      .center {
        text-align: center;
      }

      .toggle-cell {
        display: inline-flex;
        align-items: center;
        gap: 0.55rem;
      }

      .toggle-value {
        min-width: 56px;
        text-align: left;
        font-size: 0.82rem;
        font-weight: 600;
        color: var(--app-text);
      }

      .toggle-value-inactive {
        color: var(--app-muted-text);
      }

      .status {
        display: inline-block;
        padding: 4px 8px;
        border-radius: 3px;
        background: #e7f1f8;
        color: #0078d4;
        font-size: 12px;
        font-weight: 600;
      }

      .status.active {
        background: #dffcf0;
        color: #107c10;
      }

      .actions-cell {
        text-align: right;
      }

      .loading {
        text-align: center;
        padding: 40px;
        color: var(--app-muted-text);
      }

      .no-admins {
        text-align: center;
        padding: 40px;
        color: var(--app-muted-text);
        background: var(--app-surface-muted);
        border-radius: var(--app-control-radius);
      }

      @media (max-width: 768px) {
        .checkbox-group {
          grid-template-columns: 1fr;
        }

        .admin-table {
          font-size: 12px;
        }

        .admin-table th,
        .admin-table td {
          padding: 8px;
        }
      }
    `,
  ],
})
export class AdminSettingsComponent implements OnInit, OnDestroy {
  addAdminForm!: FormGroup;
  adminUsers: AdminUser[] = [];

  directoryUsers: DirectoryUser[] = [];
  isLoadingUsers = false;
  usersLoadError = '';
  private photoCache = new Map<string, string>();

  isLoading = false;
  isSubmitting = false;
  isDeleting: number | null = null;
  updatingAdminIds = new Set<number>();

  successMessage = '';
  errorMessage = '';

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private adminUserService: AdminUserService,
    private submissionService: SubmissionService,
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {
    this.loadAdminUsers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForm(): void {
    this.addAdminForm = this.fb.group({
      nomineeUser: [null, [Validators.required]],
      canReview: [true],
      receiveNotifications: [true],
    });
  }

  getInitials(displayName: string): string {
    return displayName
      .split(' ')
      .map((name) => name.charAt(0).toUpperCase())
      .join('')
      .substring(0, 2);
  }

  getUserPhotoUrl(user: DirectoryUser): string | undefined {
    return this.photoCache.get(user.entraObjectId);
  }

  onAdminUserFilter(event: unknown): void {
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

    this.adminUserService.searchDirectoryUsers(typed.trim()).subscribe({
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

  onAdminUserChange(user: DirectoryUser | null): void {
    // User selected, form will be submitted with the user object
  }

  private loadUserPhoto(user: DirectoryUser): Promise<void> {
    return new Promise((resolve) => {
      if (this.photoCache.has(user.entraObjectId)) {
        resolve();
      } else {
        this.submissionService
          .getUserProfilePhoto(user.entraObjectId)
          .subscribe({
            next: (blob: Blob) => {
              if (!blob || blob.size === 0) {
                resolve();
                return;
              }

              const reader = new FileReader();
              reader.onload = () => {
                const dataUrl = reader.result as string;
                this.photoCache.set(user.entraObjectId, dataUrl);
                resolve();
              };
              reader.readAsDataURL(blob);
            },
            error: (error: any) => {
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

  private loadAdminUsers(): void {
    this.isLoading = true;
    this.adminUserService
      .listAdminUsers()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (admins) => {
          this.adminUsers = admins;
          this.isLoading = false;
        },
        error: (err) => {
          console.error('Failed to load admin users:', err);
          this.errorMessage = 'Failed to load admin users';
          this.isLoading = false;
        },
      });
  }

  onAddAdmin(): void {
    if (!this.addAdminForm.valid) {
      return;
    }

    const selectedUser = this.addAdminForm.get('nomineeUser')?.value;
    if (!selectedUser) {
      return;
    }

    this.successMessage = '';
    this.errorMessage = '';
    this.isSubmitting = true;

    const request: AddAdminUserRequest = {
      entraObjectId: selectedUser.entraObjectId,
      email: selectedUser.email,
      displayName: selectedUser.displayName,
      department: selectedUser.department,
      canReview: this.addAdminForm.value.canReview,
      receiveNotifications: this.addAdminForm.value.receiveNotifications,
    };

    this.adminUserService
      .addAdminUser(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (newAdmin) => {
          this.adminUsers.push(newAdmin);
          this.adminUsers.sort((a, b) =>
            a.displayName.localeCompare(b.displayName),
          );
          this.addAdminForm.reset({
            nomineeUser: null,
            canReview: true,
            receiveNotifications: true,
          });
          this.directoryUsers = [];
          this.usersLoadError = '';
          this.successMessage = `Admin user ${newAdmin.displayName} added successfully`;
          this.isSubmitting = false;

          // Clear success message after 3 seconds
          setTimeout(() => {
            this.successMessage = '';
          }, 3000);
        },
        error: (err) => {
          console.error('Failed to add admin user:', err);
          this.errorMessage = err.error?.message || 'Failed to add admin user';
          this.isSubmitting = false;
        },
      });
  }

  onRemoveAdmin(adminUserId: number): void {
    if (!confirm('Are you sure you want to remove this admin user?')) {
      return;
    }

    this.isDeleting = adminUserId;
    this.adminUserService
      .removeAdminUser(adminUserId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.adminUsers = this.adminUsers.filter(
            (a) => a.adminUserId !== adminUserId,
          );
          this.isDeleting = null;
          this.successMessage = 'Admin user removed successfully';

          // Clear success message after 3 seconds
          setTimeout(() => {
            this.successMessage = '';
          }, 3000);
        },
        error: (err) => {
          console.error('Failed to remove admin user:', err);
          this.errorMessage = 'Failed to remove admin user';
          this.isDeleting = null;
        },
      });
  }

  onToggleAdminFlag(
    admin: AdminUser,
    field: 'canReview' | 'receiveNotifications' | 'isActive',
    checked: boolean,
  ): void {
    if (this.isAdminUpdating(admin.adminUserId)) {
      return;
    }

    const previousValue = admin[field];
    admin[field] = checked;
    this.errorMessage = '';
    this.successMessage = '';
    this.updatingAdminIds.add(admin.adminUserId);

    const request: UpdateAdminUserRequest = {
      canReview: admin.canReview,
      receiveNotifications: admin.receiveNotifications,
      isActive: admin.isActive,
      department: admin.department,
    };

    this.adminUserService
      .updateAdminUser(admin.adminUserId, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (updatedAdmin) => {
          const idx = this.adminUsers.findIndex(
            (a) => a.adminUserId === updatedAdmin.adminUserId,
          );

          if (idx >= 0) {
            this.adminUsers[idx] = updatedAdmin;
          }

          this.updatingAdminIds.delete(admin.adminUserId);
        },
        error: (err) => {
          admin[field] = previousValue;
          this.updatingAdminIds.delete(admin.adminUserId);
          console.error('Failed to update admin user:', err);
          this.errorMessage =
            err.error?.message ||
            'Failed to save admin changes. Please try again.';
        },
      });
  }

  isAdminUpdating(adminUserId: number): boolean {
    return this.updatingAdminIds.has(adminUserId);
  }

  getFieldError(fieldName: string): string | null {
    const field = this.addAdminForm.get(fieldName);
    if (!field || !field.invalid || !field.touched) return null;

    if (field.errors?.['required'])
      return `${this.prettifyFieldName(fieldName)} is required.`;

    return 'Invalid input.';
  }

  private prettifyFieldName(name: string): string {
    return name
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, (str) => str.toUpperCase())
      .trim();
  }
}
