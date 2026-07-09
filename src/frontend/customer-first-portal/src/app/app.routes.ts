import { Routes } from '@angular/router';
import { AnalyticsComponent } from './features/analytics/analytics.component';
import { adminGuard, adminMatchGuard } from './core/auth/admin.guard';
import { SubmissionsShellComponent } from './features/submissions/submissions-shell.component';
import { SubmissionsListComponent } from './features/submissions/submissions-list.component';
import { EmployeeRecognitionComponent } from './features/submissions/components/employee-recognition/employee-recognition.component';
import { CustomerRecognitionComponent } from './features/submissions/components/customer-recognition/customer-recognition.component';
import { AdminSettingsComponent } from './features/admin-settings/admin-settings.component';
import { AdminReviewComponent } from './features/admin-review/admin-review.component';
import { AdminReviewDetailComponent } from './features/admin-review/admin-review-detail.component';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    component: SubmissionsShellComponent,
    children: [
      {
        path: '',
        redirectTo: 'submissions',
        pathMatch: 'full',
      },
      {
        path: 'login',
        redirectTo: 'submissions',
        pathMatch: 'full',
      },
      {
        path: 'analytics',
        component: AnalyticsComponent,
        canActivate: [adminGuard],
        canMatch: [adminMatchGuard],
      },
      {
        path: 'admin-settings',
        component: AdminSettingsComponent,
        canActivate: [adminGuard],
        canMatch: [adminMatchGuard],
      },
      {
        path: 'admin-review/:submissionId',
        component: AdminReviewDetailComponent,
        canActivate: [adminGuard],
        canMatch: [adminMatchGuard],
      },
      {
        path: 'submissions-list/:submissionId',
        redirectTo: 'admin-review/:submissionId',
        pathMatch: 'full',
      },
      {
        path: 'admin-review',
        component: AdminReviewComponent,
        canActivate: [adminGuard],
        canMatch: [adminMatchGuard],
      },
      {
        path: 'submissions-list',
        redirectTo: 'admin-review',
        pathMatch: 'full',
      },
      {
        path: 'submissions',
        component: SubmissionsListComponent,
        canActivate: [authGuard],
      },
    ],
  },
  {
    path: 'submissions/employee',
    component: EmployeeRecognitionComponent,
    canActivate: [authGuard],
  },
  {
    path: 'submissions/customer',
    component: CustomerRecognitionComponent,
    canActivate: [authGuard],
  },
  {
    path: '',
    redirectTo: 'submissions',
    pathMatch: 'full',
  },
  {
    path: '**',
    redirectTo: 'submissions',
  },
];
