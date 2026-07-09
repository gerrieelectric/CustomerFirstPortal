import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RatingModule } from 'primeng/rating';

@Component({
  selector: 'app-star-rating',
  standalone: true,
  imports: [CommonModule, FormsModule, RatingModule],
  template: `
    <div class="star-rating" (mouseleave)="onRatingChange(modelValue)">
      <p-rating
        [(ngModel)]="modelValue"
        [stars]="5"
        (onRate)="onRatingChange($event.value || 0)"
      ></p-rating>
      <span class="rating-label" *ngIf="value">{{ value }} / 5</span>
    </div>
  `,
  styles: [
    `
      .star-rating {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      .rating-label {
        color: #666;
        font-size: 14px;
        margin-left: 8px;
        font-weight: 500;
      }
    `,
  ],
})
export class StarRatingComponent {
  @Input() value: number = 0;
  @Output() ratingChange = new EventEmitter<number>();
  modelValue = 0;

  ngOnChanges(): void {
    this.modelValue = this.value;
  }

  onRatingChange(rating: number): void {
    this.modelValue = rating;
    this.ratingChange.emit(rating);
  }
}
