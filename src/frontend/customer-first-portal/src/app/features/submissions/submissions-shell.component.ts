import {
  Component,
  ElementRef,
  HostListener,
  ViewChild,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ToolbarModule } from 'primeng/toolbar';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-submissions-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ToolbarModule,
    ButtonModule,
    MenuModule,
  ],
  template: `
    <div class="submissions-layout">
      <p-toolbar styleClass="navbar">
        <ng-template pTemplate="start">
          <div class="navbar-brand">
            <img
              src="logo_white.svg"
              alt="Customer First Logo"
              class="logo-image"
            />
            <div>
              <h1>Customer First Employee Driven</h1>
              <p>Employee Recognition System</p>
            </div>
          </div>
        </ng-template>
        <ng-template pTemplate="end">
          <nav class="nav-links">
            <a
              routerLink="/submissions"
              routerLinkActive="active"
              [routerLinkActiveOptions]="{ exact: true }"
              class="nav-link"
              pButton
              severity="contrast"
              variant="text"
              size="small"
            >
              <i class="pi pi-star-fill nav-icon" aria-hidden="true"></i>
              <span>Recognitions</span>
            </a>
            <a
              routerLink="/submissions/employee"
              routerLinkActive="active"
              class="nav-link"
              pButton
              severity="contrast"
              variant="text"
              size="small"
            >
              <i class="pi pi-user-edit nav-icon" aria-hidden="true"></i>
              <span>Employee Form</span>
            </a>
            <a
              routerLink="/submissions/customer"
              routerLinkActive="active"
              class="nav-link"
              pButton
              severity="contrast"
              variant="text"
              size="small"
            >
              <i class="pi pi-users nav-icon" aria-hidden="true"></i>
              <span>Customer Form</span>
            </a>
            <ng-container *ngIf="isAdmin$ | async">
              <div class="nav-menu-anchor" #adminMenuAnchor>
                <button
                  type="button"
                  class="nav-link nav-dropdown-trigger"
                  [class.active]="isAdminRoute || adminMenuOpen"
                  [attr.aria-expanded]="adminMenuOpen"
                  aria-haspopup="menu"
                  pButton
                  severity="contrast"
                  variant="text"
                  size="small"
                  (click)="toggleAdminMenu($event)"
                >
                  <i class="pi pi-lock nav-icon" aria-hidden="true"></i>
                  <span>Admin</span>
                  <i class="pi pi-chevron-down nav-icon" aria-hidden="true"></i>
                </button>
                <p-menu
                  *ngIf="adminMenuOpen"
                  styleClass="admin-inline-menu"
                  [model]="adminMenuItems"
                ></p-menu>
              </div>
            </ng-container>
          </nav>
        </ng-template>
      </p-toolbar>

      <main class="content">
        <router-outlet></router-outlet>
      </main>

      <footer class="footer">
        <p>&copy; 2026 Customer First Employee Driven. All rights reserved.</p>
      </footer>
    </div>
  `,
  styles: [
    `
      .submissions-layout {
        display: flex;
        flex-direction: column;
        min-height: 100vh;
        background: var(--app-bg);
      }

      :host ::ng-deep .navbar {
        --p-toolbar-background: transparent;
        --p-toolbar-border-color: transparent;
        --p-toolbar-color: #ffffff;
        background-image:
          linear-gradient(
            135deg,
            var(--app-navbar-overlay-start) 0%,
            var(--app-navbar-overlay-end) 100%
          ),
          var(--submission-background-image);
        background-size: cover;
        background-position: center;
        color: white;
        box-shadow: 0 2px 12px rgba(0, 0, 0, 0.3);
        position: sticky;
        top: 0;
        z-index: 100;
        border-radius: 0;
      }

      .navbar-brand {
        display: flex;
        align-items: center;
        gap: 16px;
      }

      .navbar-brand h1 {
        margin: 0;
        font-size: 20px;
        font-weight: 600;
        letter-spacing: 0.3px;
        color: #ffffff;
      }

      .navbar-brand p {
        margin: 4px 0 0 0;
        font-size: 12px;
        opacity: 0.8;
        color: #ffffff;
      }

      .logo-image {
        height: 48px;
        width: auto;
        max-width: 60px;
        object-fit: contain;
      }

      .nav-links {
        display: flex;
        gap: 24px;
      }

      .nav-menu-anchor {
        position: relative;
        display: inline-flex;
      }

      :host ::ng-deep .nav-menu-anchor .admin-inline-menu.p-menu {
        position: absolute !important;
        top: calc(100% + 6px) !important;
        right: 0 !important;
        left: auto !important;
        margin: 0 !important;
        min-width: 198px;
        z-index: 110;
      }

      :host ::ng-deep .nav-link.p-button {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        color: rgba(255, 255, 255, 0.9) !important;
        background: transparent !important;
        border: none !important;
        border-radius: var(--app-control-radius) !important;
        text-decoration: none;
        font-size: 0.85rem;
        font-weight: 500;
        transition:
          color 0.2s ease,
          background-color 0.2s ease;
      }

      .nav-icon {
        font-size: 0.9rem;
        line-height: 1;
      }

      :host ::ng-deep .nav-link.p-button:hover {
        color: #ffffff !important;
        background: rgba(255, 255, 255, 0.15) !important;
      }

      :host ::ng-deep .nav-link.active.p-button {
        color: #ffffff !important;
        background: rgba(255, 255, 255, 0.2) !important;
        font-weight: 600;
      }

      .content {
        flex: 1;
      }

      .footer {
        background: var(--app-footer-bg);
        border-top: 1px solid var(--app-border);
        padding: 20px;
        text-align: center;
        color: var(--app-footer-text);
        font-size: 12px;
      }

      .footer p {
        margin: 0;
      }

      @media (max-width: 768px) {
        :host ::ng-deep .navbar .p-toolbar {
          flex-direction: column;
          gap: 12px;
        }

        .nav-links {
          flex-wrap: wrap;
          gap: 12px;
        }

        .navbar-brand {
          width: 100%;
        }

        .nav-links {
          width: 100%;
          justify-content: flex-start;
        }
      }
    `,
  ],
})
export class SubmissionsShellComponent {
  @ViewChild('adminMenuAnchor')
  private adminMenuAnchor?: ElementRef<HTMLElement>;

  private readonly authService = inject(AuthService);
  readonly isAdmin$ = this.authService.isAdmin$;
  adminMenuOpen = false;

  readonly adminMenuItems: MenuItem[] = [
    {
      label: 'Admin Dashboard',
      icon: 'pi pi-chart-bar',
      command: () => this.navigateTo('/analytics'),
    },
    {
      label: 'Submissions List',
      icon: 'pi pi-list-check',
      command: () => this.navigateTo('/admin-review'),
    },
    {
      label: 'Settings',
      icon: 'pi pi-cog',
      command: () => this.navigateTo('/admin-settings'),
    },
  ];

  constructor(private readonly router: Router) {
    // Refresh once when the shell is created to keep admin navigation accurate.
    void this.authService.isAdminAsync(true);
  }

  toggleAdminMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.adminMenuOpen = !this.adminMenuOpen;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.adminMenuOpen) {
      return;
    }

    const targetNode = event.target as Node | null;
    const anchor = this.adminMenuAnchor?.nativeElement;
    if (!targetNode || !anchor?.contains(targetNode)) {
      this.adminMenuOpen = false;
    }
  }

  @HostListener('document:keydown.escape')
  onEscapePressed(): void {
    this.adminMenuOpen = false;
  }

  get isAdminRoute(): boolean {
    return [
      '/analytics',
      '/admin-settings',
      '/admin-review',
      '/submissions-list',
    ].some((route) => this.router.url.startsWith(route));
  }

  private navigateTo(route: string): void {
    this.adminMenuOpen = false;
    void this.router.navigate([route]);
  }
}
