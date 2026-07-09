import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { Router } from '@angular/router';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [CommonModule, CardModule],
  template: `
    <p-card
      styleClass="kpi-card"
      [ngClass]="{ highlight: highlight, clickable: isClickable }"
      [attr.role]="isClickable ? 'button' : null"
      [attr.tabindex]="isClickable ? 0 : null"
      [attr.aria-label]="
        isClickable ? label + ' - navigate to review queue' : null
      "
      (click)="onCardClick()"
      (keydown.enter)="onCardClick()"
      (keydown.space)="$event.preventDefault(); onCardClick()"
    >
      <div class="kpi-content">
        <div class="kpi-label">{{ label }}</div>
        <div class="kpi-metric-row">
          <div class="kpi-icon">{{ icon }}</div>
          <div class="kpi-value">{{ value }}</div>
        </div>
      </div>
    </p-card>
  `,
  styles: [
    `
      :host {
        display: block;
        height: 100%;
      }

      p-card {
        display: block;
        height: 100%;
      }

      :host ::ng-deep .kpi-card.p-card {
        min-height: 5.8rem;
        height: 100%;
        transition:
          transform 160ms ease,
          box-shadow 160ms ease,
          border-color 160ms ease;
      }

      :host ::ng-deep p-card.highlight .kpi-card.p-card {
        background-color: color-mix(
          in srgb,
          var(--app-accent-soft) 75%,
          var(--app-surface)
        );
        border-color: var(--app-accent);
      }

      :host ::ng-deep p-card.clickable .kpi-card.p-card {
        cursor: pointer;
      }

      :host ::ng-deep p-card.clickable:hover .kpi-card.p-card {
        transform: translateY(-3px);
        border-color: var(--app-accent);
        box-shadow:
          0 10px 22px rgba(15, 23, 42, 0.2),
          0 0 0 2px color-mix(in srgb, var(--app-accent) 30%, transparent);
      }

      :host ::ng-deep p-card.clickable:focus-visible .kpi-card.p-card {
        outline: 2px solid var(--app-accent);
        outline-offset: 2px;
      }

      :host ::ng-deep .kpi-card .p-card-body,
      :host ::ng-deep .kpi-card .p-card-content {
        height: 100%;
      }

      .kpi-content {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        justify-content: space-between;
        gap: 0.25rem;
        width: 100%;
        height: 100%;
        min-height: 0;
      }

      .kpi-metric-row {
        display: flex;
        align-items: center;
        justify-content: flex-start;
        gap: 0.6rem;
        min-width: 0;
      }

      .kpi-label {
        display: block;
        width: 100%;
        font-size: clamp(0.8rem, 0.74rem + 0.2vw, 0.9rem);
        line-height: 1.15;
        color: var(--app-muted-text);
        white-space: nowrap;
        overflow: hidden;
        text-overflow: clip;
      }

      .kpi-icon {
        width: 1.8rem;
        font-size: 1.7rem;
        line-height: 1;
        text-align: left;
        flex: 0 0 auto;
      }

      .kpi-value {
        display: flex;
        align-items: center;
        justify-content: flex-start;
        font-size: 1.6rem;
        font-weight: 600;
        line-height: 1;
        color: var(--app-text);
        font-variant-numeric: tabular-nums;
      }
    `,
  ],
})
export class KpiCardComponent {
  constructor(private router: Router) {}

  @Input() label: string = '';
  @Input() value: string | number = 0;
  @Input() icon: string = '📊';
  @Input() highlight: boolean = false;
  @Input() navigateTo: string | string[] | null = null;
  @Input() queryParams: Record<string, string> | null = null;

  get isClickable(): boolean {
    return !!this.navigateTo;
  }

  onCardClick(): void {
    const target = this.navigateTo;

    if (!target) {
      return;
    }

    if (Array.isArray(target)) {
      this.router.navigate([...target], {
        queryParams: this.queryParams ?? undefined,
      });
      return;
    }

    this.router.navigate([target], {
      queryParams: this.queryParams ?? undefined,
    });
  }
}
