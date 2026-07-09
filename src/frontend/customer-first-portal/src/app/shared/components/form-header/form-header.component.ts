import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-form-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="header">
      <div class="header-glare" aria-hidden="true"></div>
      <div class="hero-content">
        <img
          src="logo_white.svg"
          alt="Customer First Logo"
          class="brand-logo"
        />

        <div class="form-title-section" *ngIf="title">
          <h1>{{ title }}</h1>
          <p class="form-subtitle" *ngIf="subtitle">{{ subtitle }}</p>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .header {
        position: relative;
        overflow: hidden;
        background: linear-gradient(
          120deg,
          #0c4f9a 0%,
          #0a66c2 60%,
          #1b80d8 100%
        );
        color: #fff;
        padding: 2.5rem 1.5rem 2.25rem;
      }

      .header-glare {
        position: absolute;
        inset: -45% -25% auto auto;
        width: 68%;
        height: 220%;
        background: linear-gradient(
          120deg,
          rgba(255, 255, 255, 0.28) 0%,
          rgba(255, 255, 255, 0.08) 36%,
          rgba(255, 255, 255, 0) 70%
        );
        transform: rotate(22deg);
        pointer-events: none;
      }

      .hero-content {
        position: relative;
        z-index: 1;
        max-width: 860px;
        margin: 0 auto;
        text-align: center;
      }

      .brand-logo {
        width: 130px;
        height: auto;
        max-height: 130px;
        margin-bottom: 1.5rem;
        object-fit: contain;
        filter: drop-shadow(0 10px 28px rgba(3, 31, 66, 0.35));
      }

      .form-title-section {
        max-width: 760px;
        margin: 0 auto;
      }

      .form-title-section h1 {
        margin: 0;
        color: white;
        font-size: 2rem;
        font-weight: 700;
        line-height: 1.25;
        text-wrap: balance;
      }

      .form-subtitle {
        margin: 0.35rem 0 0;
        color: rgba(255, 255, 255, 0.9);
        font-size: 1.22rem;
        font-weight: 500;
        line-height: 1.35;
      }

      @media (max-width: 768px) {
        .header {
          padding: 2rem 1rem;
        }

        .brand-logo {
          width: 106px;
          max-height: 106px;
          margin-bottom: 1.1rem;
        }

        .form-title-section h1 {
          font-size: 1.45rem;
        }

        .form-subtitle {
          font-size: 1rem;
        }
      }
    `,
  ],
})
export class FormHeaderComponent {
  @Input() title: string | null = null;
  @Input() subtitle: string | null = null;
}
