import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import {
  EditSuggestion,
  SuggestedEditsResponse,
} from '../../../core/services/submission.service';

@Component({
  selector: 'app-suggestion-diff-display',
  standalone: true,
  imports: [CommonModule, CardModule, ButtonModule, TooltipModule],
  template: `
    <div class="suggestion-container" *ngIf="suggestions">
      <p-card styleClass="suggestion-card">
        <ng-template pTemplate="header">
          <div class="suggestion-header">
            <div class="header-title">
              <i class="pi pi-lightbulb"></i>
              <span>AI Suggestions</span>
            </div>
            <div class="header-meta">
              <span class="model-badge">{{ suggestions.modelName }}</span>
              <span class="generated-time">
                Generated {{ suggestions.generatedUtc | date: 'short' }}
              </span>
              <p-button
                [icon]="isCollapsed ? 'pi pi-chevron-down' : 'pi pi-chevron-up'"
                [rounded]="true"
                [text]="true"
                severity="secondary"
                (onClick)="toggleCollapsed()"
                pTooltip="Toggle suggestions"
                tooltipPosition="left"
              ></p-button>
            </div>
          </div>
        </ng-template>

        <div class="suggestion-content" *ngIf="!isCollapsed">
          <div class="suggestion-section">
            <div class="section-label">
              <i class="pi pi-check"></i>
              Suggested Edits ({{ suggestions.suggestions.length }})
            </div>

            <div
              *ngIf="suggestions.suggestions.length === 0"
              class="no-changes"
            >
              <p>No changes suggested - the original text is well-written!</p>
            </div>

            <div
              *ngFor="let suggestion of suggestions.suggestions"
              class="suggestion-item"
              [ngClass]="'suggestion-' + suggestion.category"
            >
              <div class="suggestion-category">
                <span class="category-badge" [ngClass]="suggestion.category">
                  {{ formatCategory(suggestion.category) }}
                </span>
              </div>

              <div class="suggestion-diff">
                <div class="original">
                  <div class="diff-label">Original:</div>
                  <div class="diff-text strikethrough">
                    {{ suggestion.original }}
                  </div>
                </div>

                <div class="arrow">
                  <i class="pi pi-arrow-right"></i>
                </div>

                <div class="suggested">
                  <div class="diff-label">Suggested:</div>
                  <div class="diff-text highlighted">
                    {{ suggestion.suggested }}
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="full-edited-section">
            <div class="section-label">
              <i class="pi pi-file-edit"></i>
              Full Edited Text
            </div>
            <div class="edited-text-box">
              {{ suggestions.fullEditedReason }}
            </div>
            <p class="info-text">
              You can copy this text to the "Edited Recognition Reason" field
              above.
            </p>
          </div>

          <div class="suggestion-metadata">
            <div class="metadata-item">
              <span class="meta-label">Status:</span>
              <span
                class="meta-value"
                [ngClass]="suggestions.status.toLowerCase()"
              >
                {{ suggestions.status }}
              </span>
            </div>
            <div class="metadata-item">
              <span class="meta-label">Prompt Version:</span>
              <span class="meta-value">{{ suggestions.promptVersion }}</span>
            </div>
          </div>
        </div>
      </p-card>
    </div>
  `,
  styles: [
    `
      .suggestion-container {
        margin-bottom: 20px;
      }

      .suggestion-card {
        border-left: 4px solid #4caf50;
        background: #f1f8f4;
      }

      .suggestion-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        width: 100%;
        padding: 12px 0;
      }

      .header-title {
        display: flex;
        align-items: center;
        gap: 8px;
        font-weight: 600;
        color: #2c3e50;
        font-size: 1rem;
      }

      .header-title i {
        color: #4caf50;
        font-size: 1.2rem;
      }

      .header-meta {
        display: flex;
        gap: 12px;
        align-items: center;
        font-size: 0.85rem;
      }

      .model-badge {
        background: #e3f2fd;
        color: #1976d2;
        padding: 3px 8px;
        border-radius: 12px;
        font-weight: 500;
      }

      .generated-time {
        color: #687585;
        font-style: italic;
      }

      .suggestion-content {
        padding: 16px 0;
      }

      .section-label {
        display: flex;
        align-items: center;
        gap: 8px;
        font-weight: 600;
        color: #2c3e50;
        margin-bottom: 12px;
        padding-bottom: 8px;
        border-bottom: 2px solid rgba(76, 175, 80, 0.2);
      }

      .section-label i {
        color: #4caf50;
      }

      .no-changes {
        padding: 16px;
        background: #e8f5e9;
        border-radius: 4px;
        color: #2e7d32;
        text-align: center;
      }

      .suggestion-item {
        margin-bottom: 16px;
        padding: 12px;
        border-radius: 4px;
        background: white;
        border: 1px solid #e0e0e0;
      }

      .suggestion-item.suggestion-spelling {
        border-left: 3px solid #ff9800;
      }

      .suggestion-item.suggestion-grammar {
        border-left: 3px solid #2196f3;
      }

      .suggestion-item.suggestion-name {
        border-left: 3px solid #9c27b0;
      }

      .suggestion-item.suggestion-formatting {
        border-left: 3px solid #f44336;
      }

      .suggestion-category {
        margin-bottom: 8px;
      }

      .category-badge {
        display: inline-block;
        padding: 4px 8px;
        border-radius: 12px;
        font-size: 0.75rem;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }

      .category-badge.spelling {
        background: #ffe0b2;
        color: #e65100;
      }

      .category-badge.grammar {
        background: #bbdefb;
        color: #01579b;
      }

      .category-badge.name {
        background: #e1bee7;
        color: #4a148c;
      }

      .category-badge.formatting {
        background: #ffcdd2;
        color: #b71c1c;
      }

      .suggestion-diff {
        display: grid;
        grid-template-columns: 1fr auto 1fr;
        gap: 12px;
        align-items: center;
      }

      .original,
      .suggested {
        display: flex;
        flex-direction: column;
        gap: 4px;
      }

      .diff-label {
        font-size: 0.75rem;
        font-weight: 600;
        color: #687585;
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }

      .diff-text {
        padding: 8px;
        border-radius: 3px;
        font-size: 0.9rem;
        line-height: 1.4;
      }

      .diff-text.strikethrough {
        background: #ffebee;
        color: #c62828;
        text-decoration: line-through;
        border: 1px solid #ef5350;
      }

      .diff-text.highlighted {
        background: #c8e6c9;
        color: #1b5e20;
        border: 1px solid #66bb6a;
      }

      .arrow {
        display: flex;
        justify-content: center;
        color: #4caf50;
        font-size: 1.2rem;
      }

      @media (max-width: 768px) {
        .suggestion-diff {
          grid-template-columns: 1fr;
        }

        .arrow {
          transform: rotate(90deg);
          margin: 8px 0;
        }

        .header-meta {
          flex-direction: column;
          align-items: flex-start;
          gap: 6px;
        }
      }

      .full-edited-section {
        margin-top: 20px;
        padding-top: 16px;
        border-top: 1px solid rgba(76, 175, 80, 0.2);
      }

      .edited-text-box {
        background: #f5f5f5;
        border: 1px solid #ddd;
        border-radius: 4px;
        padding: 12px;
        line-height: 1.6;
        color: #2c3e50;
        margin-bottom: 8px;
      }

      .info-text {
        font-size: 0.85rem;
        color: #687585;
        font-style: italic;
        margin: 0;
      }

      .suggestion-metadata {
        margin-top: 16px;
        padding-top: 12px;
        border-top: 1px solid #e0e0e0;
        display: flex;
        gap: 20px;
      }

      .metadata-item {
        display: flex;
        gap: 8px;
        align-items: center;
        font-size: 0.85rem;
      }

      .meta-label {
        font-weight: 600;
        color: #2c3e50;
      }

      .meta-value {
        color: #687585;
      }

      .meta-value.success {
        color: #2e7d32;
        font-weight: 500;
      }

      .meta-value.failed {
        color: #c62828;
        font-weight: 500;
      }

      .meta-value.stale {
        color: #f57f17;
        font-weight: 500;
      }
    `,
  ],
})
export class SuggestionDiffDisplayComponent {
  @Input() suggestions: SuggestedEditsResponse | null = null;
  isCollapsed = false;

  toggleCollapsed(): void {
    this.isCollapsed = !this.isCollapsed;
  }

  formatCategory(category: string): string {
    const categoryMap: Record<string, string> = {
      spelling: 'Spelling',
      grammar: 'Grammar',
      name: 'Name',
      formatting: 'Formatting',
    };
    return categoryMap[category] || category;
  }
}
