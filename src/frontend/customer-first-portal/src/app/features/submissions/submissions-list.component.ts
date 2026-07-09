import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-submissions-list',
  standalone: true,
  imports: [CommonModule, RouterModule, CardModule, ButtonModule],
  template: `
    <div class="submissions-list-container">
      <div class="page-header">
        <h1>Recognition Portal</h1>
        <p>Submit recognition to celebrate exceptional Customer Service</p>
      </div>

      <div class="forms-grid">
        <p-card styleClass="form-card">
          <ng-template pTemplate="header">
            <div class="card-header">
              <div class="card-icon">👥</div>
              <h3>Employee Recognition</h3>
            </div>
          </ng-template>
          <p class="card-description">
            Nominate an employee who always puts the Customer First. Recognize
            exceptional contributions and outstanding Customer Service.
          </p>
          <a
            routerLink="/submissions/employee"
            pButton
            label="Start Recognition"
            class="btn-link"
          ></a>
        </p-card>

        <p-card styleClass="form-card">
          <ng-template pTemplate="header">
            <div class="card-header">
              <div class="card-icon">⭐</div>
              <h3>Customer Feedback</h3>
            </div>
          </ng-template>
          <p class="card-description">
            Share customer feedback about our employees. Your feedback helps us
            recognize and celebrate outstanding Customer Service.
          </p>
          <a
            routerLink="/submissions/customer"
            pButton
            label="Share Feedback"
            class="btn-link"
          ></a>
        </p-card>
      </div>

      <div class="info-section">
        <p-card styleClass="info-card info-panel-card">
          <h4>How It Works</h4>
          <ol>
            <li>Select the appropriate recognition form above</li>
            <li>Fill in the employee and recognition details</li>
            <li>Rate their Customer Service on a 5-star scale</li>
            <li>Submit for review and approval</li>
            <li>Recognized employees receive notification and recognition</li>
          </ol>
        </p-card>

        <p-card styleClass="info-card info-panel-card">
          <h4>Recognition Impact</h4>
          <p>
            Every recognition matters! Your feedback directly impacts employee
            development, company culture, and our commitment to exceptional
            customer service.
          </p>
        </p-card>
      </div>
    </div>
  `,
  styles: [
    `
      .submissions-list-container {
        max-width: 1000px;
        margin: 0 auto;
        padding: 40px 20px;
        width: 100%;
      }

      .page-header {
        text-align: center;
        margin-bottom: 50px;
      }

      .page-header h1 {
        margin: 0 0 12px 0;
        color: var(--app-heading);
        font-size: 32px;
        font-weight: 600;
      }

      .page-header p {
        margin: 0;
        color: var(--app-muted-text);
        font-size: 16px;
      }

      .forms-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
        gap: 24px;
        margin-bottom: 50px;
      }

      .form-card {
        height: 100%;
        transition:
          box-shadow 0.3s ease,
          transform 0.3s ease;
      }

      :host ::ng-deep .form-card.p-card,
      :host ::ng-deep .info-card.p-card {
        background: var(--app-surface);
        border: 1px solid var(--app-border);
        color: var(--app-text);
      }

      :host ::ng-deep .form-card.p-card {
        display: flex;
        flex-direction: column;
        height: 100%;
      }

      .form-card:hover {
        box-shadow: 0 4px 20px rgba(0, 120, 212, 0.15);
        transform: translateY(-2px);
      }

      :host ::ng-deep .form-card .p-card-header {
        padding: 1rem 1.25rem 0;
      }

      :host ::ng-deep .form-card .p-card-body {
        display: flex;
        flex: 1;
        flex-direction: column;
      }

      :host ::ng-deep .form-card .p-card-content,
      :host ::ng-deep .info-card .p-card-content {
        color: var(--app-text);
      }

      :host ::ng-deep .form-card .p-card-content {
        display: flex;
        flex: 1;
        flex-direction: column;
      }

      .card-header {
        display: flex;
        align-items: center;
        gap: 16px;
        margin-bottom: 20px;
      }

      .card-icon {
        font-size: 32px;
        line-height: 1;
      }

      .card-header h3 {
        margin: 0;
        color: var(--app-heading);
        font-size: 20px;
        font-weight: 600;
      }

      .card-description {
        color: var(--app-muted-text);
        font-size: 14px;
        line-height: 1.6;
        margin: 0 0 24px 0;
        flex-grow: 1;
      }

      .btn-link {
        margin-top: auto;
        width: 100%;
      }

      .info-section {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
        gap: 24px;
      }

      .info-card {
        height: 100%;
      }

      :host ::ng-deep .info-panel-card .p-card-body {
        padding-top: 12px !important;
      }

      :host ::ng-deep .info-panel-card .p-card-content h4 {
        margin-top: 0 !important;
      }

      .info-card h4 {
        margin: 0 0 16px 0;
        color: var(--app-heading);
        font-size: 16px;
        font-weight: 600;
      }

      .info-card ol {
        margin: 0;
        padding-left: 24px;
        color: var(--app-muted-text);
        font-size: 14px;
        line-height: 1.8;
      }

      .info-card ol li {
        margin-bottom: 8px;
      }

      .info-card p {
        margin: 0;
        color: var(--app-muted-text);
        font-size: 14px;
        line-height: 1.6;
      }

      @media (max-width: 768px) {
        .submissions-list-container {
          padding: 20px 12px;
        }

        .page-header h1 {
          font-size: 24px;
        }

        .forms-grid {
          grid-template-columns: 1fr;
          gap: 20px;
        }

        .info-section {
          grid-template-columns: 1fr;
          gap: 20px;
        }
      }
    `,
  ],
})
export class SubmissionsListComponent {}
